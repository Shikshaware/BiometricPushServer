using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BiometricPushServer.Common.DTOs;
using BiometricPushServer.Service.Interfaces;
using BiometricPushServer.Web.Helpers;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BiometricPushServer.Web.Controllers.Api
{
    [ApiController]
    [Route("api/attendance")]
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class AttendanceApiController : ControllerBase
    {
        private readonly IAttendanceService _attendanceService;
        private readonly IDeviceService _deviceService;

        public AttendanceApiController(IAttendanceService attendanceService, IDeviceService deviceService)
        {
            _attendanceService = attendanceService;
            _deviceService = deviceService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? clientId,
            [FromQuery] int? locationId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            var scopedClientId = User.ResolveClientId(clientId);
            var result = await _attendanceService.GetAttendanceAsync(scopedClientId, pageNumber, pageSize, from, to, locationId);
            return Ok(ApiResponse<object>.Ok(result));
        }

        [HttpGet("today")]
        public async Task<IActionResult> GetToday([FromQuery] int? clientId, [FromQuery] int? locationId)
        {
            var scopedClientId = User.ResolveClientId(clientId);
            var logs = await _attendanceService.GetTodayAsync(scopedClientId, locationId);
            return Ok(ApiResponse<object>.Ok(logs));
        }

        [HttpGet("device/{deviceSN}")]
        public async Task<IActionResult> GetByDevice(
            string deviceSN,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var claimClientId = User.GetClientIdClaim();
            if (claimClientId.HasValue)
            {
                var isAccessible = (await _deviceService.GetAllDevicesAsync(claimClientId.Value))
                    .Any(d => d.SerialNumber == deviceSN);
                if (!isAccessible) return Forbid();
            }

            var fromDate = from ?? DateTime.Today;
            var toDate = to ?? DateTime.Today.AddDays(1).AddTicks(-1);
            var logs = await _attendanceService.GetByDeviceAsync(deviceSN, fromDate, toDate);
            return Ok(ApiResponse<object>.Ok(logs));
        }

        [HttpGet("user/{userCode}")]
        public async Task<IActionResult> GetByUser(
            string userCode,
            [FromQuery] int? clientId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var scopedClientId = User.ResolveClientId(clientId);
            var fromDate = from ?? DateTime.Today.AddDays(-30);
            var toDate = to ?? DateTime.Today.AddDays(1).AddTicks(-1);
            var logs = await _attendanceService.GetByUserAsync(userCode, fromDate, toDate, scopedClientId);
            return Ok(ApiResponse<object>.Ok(logs));
        }

        [HttpGet("report")]
        public async Task<IActionResult> GetReport(
            [FromQuery] string period,
            [FromQuery] int? clientId,
            [FromQuery] DateTime? referenceDate,
            [FromQuery] int? locationId)
        {
            if (!Enum.TryParse<AttendanceReportPeriod>(period, true, out var parsedPeriod))
            {
                return BadRequest(ApiResponse<object>.Fail("Invalid period. Use daily, weekly or monthly."));
            }

            var scopedClientId = User.ResolveClientId(clientId);
            if (!scopedClientId.HasValue)
            {
                return BadRequest(ApiResponse<object>.Fail("clientId is required."));
            }

            var report = await _attendanceService.GetClientAttendanceReportAsync(
                scopedClientId.Value,
                parsedPeriod,
                referenceDate,
                locationId);

            return Ok(ApiResponse<object>.Ok(report));
        }

        [HttpGet("report/export")]
        public async Task<IActionResult> ExportReport(
            [FromQuery] string period,
            [FromQuery] int? clientId,
            [FromQuery] DateTime? referenceDate,
            [FromQuery] int? locationId)
        {
            if (!Enum.TryParse<AttendanceReportPeriod>(period, true, out var parsedPeriod))
            {
                return BadRequest(ApiResponse<object>.Fail("Invalid period. Use daily, weekly or monthly."));
            }

            var scopedClientId = User.ResolveClientId(clientId);
            if (!scopedClientId.HasValue)
            {
                return BadRequest(ApiResponse<object>.Fail("clientId is required."));
            }

            var report = await _attendanceService.GetClientAttendanceReportAsync(
                scopedClientId.Value,
                parsedPeriod,
                referenceDate,
                locationId);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Attendance");

            worksheet.Cell(1, 1).Value = "Date";
            worksheet.Cell(1, 2).Value = "User Code";
            worksheet.Cell(1, 3).Value = "User Name";
            worksheet.Cell(1, 4).Value = "First In";
            worksheet.Cell(1, 5).Value = "Last Out";
            worksheet.Cell(1, 6).Value = "Punch Count";
            worksheet.Cell(1, 7).Value = "Time Zone";

            for (var i = 0; i < report.Rows.Count; i++)
            {
                var row = report.Rows[i];
                var excelRow = i + 2;
                worksheet.Cell(excelRow, 1).Value = row.Date;
                worksheet.Cell(excelRow, 2).Value = row.UserCode;
                worksheet.Cell(excelRow, 3).Value = row.UserName;
                worksheet.Cell(excelRow, 4).Value = row.FirstIn;
                worksheet.Cell(excelRow, 5).Value = row.LastOut;
                worksheet.Cell(excelRow, 6).Value = row.PunchCount;
                worksheet.Cell(excelRow, 7).Value = report.TimeZoneId;
            }

            worksheet.Columns().AdjustToContents();
            worksheet.RangeUsed()?.SetAutoFilter();

            using var stream = new System.IO.MemoryStream();
            workbook.SaveAs(stream);
            var fileName = $"attendance_{report.Period}_{report.ClientId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        /// <summary>
        /// Export attendance records as CSV.
        /// GET /api/attendance/export?clientId=&from=&to=
        /// Large exports should be scoped with from/to date filters (max 50,000 rows returned).
        /// </summary>
        [HttpGet("export")]
        public async Task<IActionResult> Export(
            [FromQuery] int? clientId,
            [FromQuery] int? locationId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            const int exportLimit = 50_000;
            var scopedClientId = User.ResolveClientId(clientId);
            var result = await _attendanceService.GetAttendanceAsync(
                scopedClientId, 1, exportLimit, from, to, locationId);

            var sb = new StringBuilder();
            sb.AppendLine("Id,DeviceSN,UserCode,UserName,PunchTime,AttendanceState,VerifyMode,IsDuplicate,CreatedOn");

            foreach (var log in result.Items)
            {
                sb.AppendLine(string.Join(",",
                    log.Id,
                    Escape(log.DeviceSN),
                    Escape(log.UserCode),
                    Escape(log.UserName),
                    log.PunchTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    log.AttendanceState,
                    log.VerifyMode,
                    log.IsDuplicate,
                    log.CreatedOn.ToString("yyyy-MM-dd HH:mm:ss")));
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"attendance_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            return File(bytes, "text/csv", fileName);
        }

        /// <summary>
        /// Allow devices to push attendance directly via REST API (alternative to IClock).
        /// POST /api/attendance/push
        /// </summary>
        [HttpPost("push")]
        [AllowAnonymous]
        public async Task<IActionResult> Push(
            [FromBody] AttendancePushDto dto,
            [FromQuery] int? clientId)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.DeviceSN))
                return BadRequest(ApiResponse<object>.Fail("DeviceSN is required"));

            var (saved, duplicates) = await _attendanceService.ProcessPushAsync(
                dto.DeviceSN, dto.Records, clientId);

            return Ok(ApiResponse<object>.Ok(new { saved, duplicates }));
        }

        private static string Escape(string? value)
        {
            if (value == null) return string.Empty;
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }
    }
}
