using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BiometricPushServer.Common.DTOs;
using BiometricPushServer.Common.Extensions;
using BiometricPushServer.Service.Interfaces;
using BiometricPushServer.Web.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace BiometricPushServer.Web.Controllers
{
    /// <summary>
    /// IClock push protocol endpoint.
    /// Biometric devices (ZKTeco, eSSL, Hikvision etc.) push attendance
    /// data to /iclock/cdata and poll /iclock/getrequest for remote commands.
    /// </summary>
    [Route("iclock")]
    [IgnoreAntiforgeryToken]   // Device-to-server protocol; not a browser form
    public class IClockController : Controller
    {
        private readonly IDeviceService _deviceService;
        private readonly IAttendanceService _attendanceService;
        private readonly ICommandService _commandService;
        private readonly IHubContext<AttendanceHub> _hub;
        private readonly ILogger<IClockController> _logger;

        public IClockController(
            IDeviceService deviceService,
            IAttendanceService attendanceService,
            ICommandService commandService,
            IHubContext<AttendanceHub> hub,
            ILogger<IClockController> logger)
        {
            _deviceService = deviceService;
            _attendanceService = attendanceService;
            _commandService = commandService;
            _hub = hub;
            _logger = logger;
        }

        /// <summary>
        /// Device registration and attendance push endpoint.
        /// GET  /iclock/cdata?SN=xxx  → device keeps alive / registers
        /// POST /iclock/cdata?SN=xxx  → device pushes attendance records
        /// </summary>
        [HttpGet("cdata")]
        public async Task<IActionResult> CDataGet([FromQuery] string SN)
        {
            if (string.IsNullOrWhiteSpace(SN))
                return BadRequest();

            var ip = GetClientIp();
            _logger.LogInformation("IClock CDATA GET from SN={SN} IP={Ip}",
                SanitizeForLog(SN), SanitizeForLog(ip));

            await _deviceService.RegisterOrUpdateAsync(new DeviceRegistrationDto
            {
                SerialNumber = SN,
                DeviceName = SN,
                IpAddress = ip
            }, ip);

            await _deviceService.UpdateHeartbeatAsync(SN, ip, Request.QueryString.Value ?? string.Empty);

            // Standard IClock response: UTC timestamp + server info
            var now = DateTime.UtcNow;
            var response =
                $"GET OPTION FROM: {SN}\r\n" +
                $"Stamp={now:yyyyMMddHHmmss}\r\n" +
                "ATTLOGStamp=None\r\n" +
                "OPERLOGStamp=None\r\n" +
                "ATTPHOTOStamp=None\r\n" +
                "ErrorDelay=30\r\n" +
                "Delay=10\r\n" +
                "TransTimes=00:00;06:00\r\n" +
                "TransInterval=1\r\n" +
                "TransFlag=TransData AttLog OpLog AttPhoto\r\n" +
                "Realtime=1\r\n" +
                "Encrypt=None\r\n";

            return Content(response, "text/plain", Encoding.UTF8);
        }

        [HttpPost("cdata")]
        public async Task<IActionResult> CDataPost([FromQuery] string SN, [FromQuery] string? table)
        {
            if (string.IsNullOrWhiteSpace(SN))
                return BadRequest();

            var ip = GetClientIp();
            var body = await ReadBodyAsync();

            _logger.LogDebug("IClock CDATA POST SN={SN} Table={Table} Body={Body}",
                SanitizeForLog(SN), SanitizeForLog(table), SanitizeForLog(body));

            var device = await _deviceService.GetBySerialNumberAsync(SN);

            if (device == null)
            {
                await _deviceService.RegisterOrUpdateAsync(new DeviceRegistrationDto
                {
                    SerialNumber = SN,
                    DeviceName = SN
                }, ip);
            }

            await _deviceService.UpdateHeartbeatAsync(SN, ip, body);

            if (string.Equals(table, "ATTLOG", StringComparison.OrdinalIgnoreCase))
            {
                var records = ParseAttendanceLogs(SN, body);
                var (saved, duplicates) = await _attendanceService.ProcessPushAsync(
                    SN, records, device?.ClientId);

                _logger.LogInformation(
                    "IClock ATTLOG SN={SN}: saved={Saved} duplicates={Dup}",
                    SanitizeForLog(SN), saved, duplicates);

                // Push live updates via SignalR
                if (saved > 0)
                {
                    var todayLogs = await _attendanceService.GetTodayAsync(device?.ClientId);
                    await _hub.Clients.All.SendAsync("AttendanceUpdated", todayLogs);
                }
            }

            return Content("OK", "text/plain", Encoding.UTF8);
        }

        /// <summary>
        /// Device polls this endpoint to receive remote commands (restart, sync time, etc.)
        /// </summary>
        [HttpGet("getrequest")]
        public async Task<IActionResult> GetRequest([FromQuery] string SN)
        {
            if (string.IsNullOrWhiteSpace(SN))
                return Content(string.Empty, "text/plain", Encoding.UTF8);

            var ip = GetClientIp();
            await _deviceService.UpdateHeartbeatAsync(SN, ip, string.Empty);

            var pending = await _commandService.GetPendingAsync(SN);
            var pendingList = pending.ToList();

            if (!pendingList.Any())
                return Content(string.Empty, "text/plain", Encoding.UTF8);

            var sb = new StringBuilder();
            foreach (var cmd in pendingList)
            {
                sb.AppendLine($"C:{cmd.Id}:{cmd.CommandText}");
                await _commandService.MarkSentAsync(cmd.Id);
            }

            return Content(sb.ToString(), "text/plain", Encoding.UTF8);
        }

        /// <summary>
        /// Device reports command execution result.
        /// </summary>
        [HttpPost("devicecmd")]
        public async Task<IActionResult> DeviceCmd([FromQuery] string SN)
        {
            if (string.IsNullOrWhiteSpace(SN))
                return BadRequest();

            var body = await ReadBodyAsync();
            _logger.LogInformation("IClock DeviceCmd SN={SN} Response={Body}",
                SanitizeForLog(SN), SanitizeForLog(body));

            // Parse: "ID=<id>\r\nReturn=<code>\r\nCMD=<type>"
            var lines = body.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int? cmdId = null;
            string returnCode = "0";

            foreach (var line in lines)
            {
                if (line.StartsWith("ID=", StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(line[3..], out int id))
                    cmdId = id;
                else if (line.StartsWith("Return=", StringComparison.OrdinalIgnoreCase))
                    returnCode = line[7..];
            }

            if (cmdId.HasValue)
            {
                if (returnCode == "0")
                    await _commandService.MarkExecutedAsync(cmdId.Value, body);
                else
                    await _commandService.MarkFailedAsync(cmdId.Value, $"Device returned: {returnCode}");
            }

            return Content("OK", "text/plain", Encoding.UTF8);
        }

        // ── helpers ──────────────────────────────────────────────────────────

        private string GetClientIp() =>
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // Pre-compiled Regex for log-injection prevention (reused across calls)
        private static readonly System.Text.RegularExpressions.Regex _logControlCharsRegex =
            new System.Text.RegularExpressions.Regex(
                @"[\r\n\t\x00-\x1F\x7F]",
                System.Text.RegularExpressions.RegexOptions.Compiled);

        /// <summary>
        /// Removes newlines and control characters to prevent log-injection attacks.
        /// </summary>
        private static string SanitizeForLog(string? value) =>
            value == null ? string.Empty :
            _logControlCharsRegex.Replace(value, "_").Truncate(200);

        private async Task<string> ReadBodyAsync()
        {
            Request.EnableBuffering();
            using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            Request.Body.Position = 0;
            return body;
        }

        /// <summary>
        /// Parses IClock ATTLOG lines.
        /// Format: UserCode  PunchTime  AttState  VerifyMode  WorkCode  Reserved
        /// e.g.: "1   2024-03-15 09:00:00  0  1  0  0"
        /// </summary>
        private static IEnumerable<AttendanceRecordDto> ParseAttendanceLogs(string sn, string body)
        {
            var records = new List<AttendanceRecordDto>();

            foreach (var line in body.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Trim().Split('\t');
                if (parts.Length < 2) continue;

                if (!DateTime.TryParse(parts[1].Trim(), out var punchTime)) continue;

                records.Add(new AttendanceRecordDto
                {
                    UserCode = parts[0].Trim(),
                    PunchTime = punchTime,
                    AttendanceState = parts.Length > 2 && int.TryParse(parts[2], out int state) ? state : 0,
                    VerifyMode = parts.Length > 3 && int.TryParse(parts[3], out int vm) ? vm : 1,
                    WorkCode = parts.Length > 4 ? parts[4].Trim() : string.Empty
                });
            }

            return records;
        }
    }
}
