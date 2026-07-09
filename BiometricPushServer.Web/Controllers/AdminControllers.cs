using System.Linq;
using System.Threading.Tasks;
using BiometricPushServer.Common.Constants;
using BiometricPushServer.Common.DTOs;
using BiometricPushServer.Domain;
using BiometricPushServer.Repository.Interfaces;
using BiometricPushServer.Service.Interfaces;
using BiometricPushServer.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BiometricPushServer.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly IUnitOfWork _uow;

        public DashboardController(IDashboardService dashboardService, IUnitOfWork uow)
        {
            _dashboardService = dashboardService;
            _uow = uow;
        }

        public async Task<IActionResult> Index([FromQuery] int? clientId, [FromQuery] int? locationId)
        {
            var scopedClientId = User.ResolveClientId(clientId);
            var claimClientId = User.GetClientIdClaim();
            var isProviderView = !claimClientId.HasValue;
            var clients = await GetClientOptionsAsync();

            ViewBag.IsProviderView = isProviderView;
            ViewBag.Clients = clients;
            ViewBag.SelectedClientId = scopedClientId;

            if (scopedClientId.HasValue)
            {
                ViewBag.ClientTimeZoneId = clients.FirstOrDefault(c => c.ClientId == scopedClientId.Value)?.TimeZoneId ?? "UTC";
            }
            else
            {
                ViewBag.ClientTimeZoneId = "UTC";
            }

            if (isProviderView && !scopedClientId.HasValue)
            {
                return View(new DashboardStatsDto());
            }

            var stats = await _dashboardService.GetStatsAsync(scopedClientId, locationId);
            return View(stats);
        }

        private async Task<List<ClientDashboardOptionDto>> GetClientOptionsAsync()
        {
            var deviceClientIds = _uow.Devices.Query()
                .Where(d => d.ClientId.HasValue)
                .Select(d => d.ClientId!.Value);

            var ownerClientIds = _uow.PortalUsers.Query()
                .Where(u => u.ClientId.HasValue)
                .Select(u => u.ClientId!.Value);

            var clientIds = deviceClientIds
                .Union(ownerClientIds)
                .Distinct()
                .OrderBy(id => id)
                .ToList();

            if (clientIds.Count == 0)
            {
                return new List<ClientDashboardOptionDto>();
            }

            var apiClients = await _uow.ApiClients.FindAsync(a => a.ClientId.HasValue && clientIds.Contains(a.ClientId.Value));
            var portalUsers = await _uow.PortalUsers.FindAsync(u => u.ClientId.HasValue && clientIds.Contains(u.ClientId.Value) && u.IsActive);

            var nameByClientId = apiClients
                .GroupBy(a => a.ClientId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Name).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? $"Client {g.Key}");

            var timeZoneByClientId = portalUsers
                .GroupBy(u => u.ClientId!.Value)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(x => x.UpdatedOn ?? x.CreatedOn)
                          .Select(x => x.TimeZoneId)
                          .FirstOrDefault(tz => IsValidTimeZone(tz)) ?? "UTC");

            return clientIds
                .Select(id => new ClientDashboardOptionDto
                {
                    ClientId = id,
                    DisplayName = nameByClientId.TryGetValue(id, out var name) ? name : $"Client {id}",
                    TimeZoneId = timeZoneByClientId.TryGetValue(id, out var timeZone) ? timeZone : "UTC"
                })
                .ToList();
        }

        private static bool IsValidTimeZone(string? timeZoneId)
        {
            if (string.IsNullOrWhiteSpace(timeZoneId)) return false;
            try
            {
                _ = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    [Authorize]
    public class DeviceController : Controller
    {
        private readonly IDeviceService _deviceService;
        private readonly ICommandService _commandService;

        public DeviceController(IDeviceService deviceService, ICommandService commandService)
        {
            _deviceService = deviceService;
            _commandService = commandService;
        }

        public async Task<IActionResult> Index([FromQuery] int? clientId, [FromQuery] int? locationId)
        {
            var scopedClientId = User.ResolveClientId(clientId);
            var devices = await _deviceService.GetAllDevicesAsync(scopedClientId, locationId);
            return View(devices);
        }

        public async Task<IActionResult> Details(int id)
        {
            var device = await _deviceService.GetDeviceDtoAsync(id);
            if (device == null) return NotFound();
            if (!User.CanAccessClient(device.ClientId)) return Forbid();
            return View(device);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var device = await _deviceService.GetDeviceDtoAsync(id);
            if (device == null) return NotFound();
            if (!User.CanAccessClient(device.ClientId)) return Forbid();
            await _deviceService.ApproveDeviceAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Lock(int id)
        {
            var device = await _deviceService.GetDeviceDtoAsync(id);
            if (device == null) return NotFound();
            if (!User.CanAccessClient(device.ClientId)) return Forbid();
            await _deviceService.SetLockedAsync(id, true);
            await _commandService.EnqueueAsync(device.SerialNumber, "LOCK");
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(int id)
        {
            var device = await _deviceService.GetDeviceDtoAsync(id);
            if (device == null) return NotFound();
            if (!User.CanAccessClient(device.ClientId)) return Forbid();
            await _deviceService.SetLockedAsync(id, false);
            await _commandService.EnqueueAsync(device.SerialNumber, "UNLOCK");
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restart(int id)
        {
            var device = await _deviceService.GetDeviceDtoAsync(id);
            if (device == null) return NotFound();
            if (!User.CanAccessClient(device.ClientId)) return Forbid();
            await _commandService.EnqueueAsync(device.SerialNumber, "RESTART");
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncTime(int id)
        {
            var device = await _deviceService.GetDeviceDtoAsync(id);
            if (device == null) return NotFound();
            if (!User.CanAccessClient(device.ClientId)) return Forbid();
            await _commandService.EnqueueAsync(device.SerialNumber, "SYNCTIME");
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncUsers(int id)
        {
            var device = await _deviceService.GetDeviceDtoAsync(id);
            if (device == null) return NotFound();
            if (!User.CanAccessClient(device.ClientId)) return Forbid();
            await _commandService.EnqueueAsync(device.SerialNumber, AppConstants.CommandSyncUsers);
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncAttendanceLogs(int id)
        {
            var device = await _deviceService.GetDeviceDtoAsync(id);
            if (device == null) return NotFound();
            if (!User.CanAccessClient(device.ClientId)) return Forbid();
            await _commandService.EnqueueAsync(device.SerialNumber, AppConstants.CommandSyncAttendanceLogs);
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearAttendance(int id)
        {
            var device = await _deviceService.GetDeviceDtoAsync(id);
            if (device == null) return NotFound();
            if (!User.CanAccessClient(device.ClientId)) return Forbid();
            await _commandService.EnqueueAsync(device.SerialNumber, "CLEAR ATT LOG");
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearUsers(int id)
        {
            var device = await _deviceService.GetDeviceDtoAsync(id);
            if (device == null) return NotFound();
            if (!User.CanAccessClient(device.ClientId)) return Forbid();
            await _commandService.EnqueueAsync(device.SerialNumber, "CLEAR DATA");
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [Authorize]
    public class AttendanceController : Controller
    {
        private readonly IAttendanceService _attendanceService;

        public AttendanceController(IAttendanceService attendanceService)
        {
            _attendanceService = attendanceService;
        }

        public async Task<IActionResult> Index(
            [FromQuery] int? clientId,
            [FromQuery] int? locationId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            var scopedClientId = User.ResolveClientId(clientId);
            var result = await _attendanceService.GetAttendanceAsync(scopedClientId, pageNumber, pageSize, null, null, locationId);
            return View(result);
        }

        public async Task<IActionResult> Today([FromQuery] int? clientId, [FromQuery] int? locationId)
        {
            var scopedClientId = User.ResolveClientId(clientId);
            var logs = await _attendanceService.GetTodayAsync(scopedClientId, locationId);
            return View(logs);
        }
    }

    [Authorize]
    public class UserController : Controller
    {
        private const string DefaultUserGroup = "1";
        private const string DefaultUserTimezone = "0000000000000000";
        private const string DefaultUserVerifyMode = "0";
        private const string DefaultUserViceCard = "0";
        private const string DefaultFingerprintIndex = "0";
        private const string DefaultEnrollRetryCount = "3";
        private const string DefaultEnrollOverwrite = "1";

        private readonly IUserService _userService;
        private readonly IDeviceService _deviceService;
        private readonly ICommandService _commandService;

        public UserController(
            IUserService userService,
            IDeviceService deviceService,
            ICommandService commandService)
        {
            _userService = userService;
            _deviceService = deviceService;
            _commandService = commandService;
        }

        public async Task<IActionResult> Index(
            [FromQuery] int? selectedDeviceId,
            [FromQuery] string? editUserCode,
            [FromQuery] int? clientId,
            [FromQuery] int? locationId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            var scopedClientId = User.ResolveClientId(clientId);
            var devices = (await _deviceService.GetAllDevicesAsync(scopedClientId, locationId)).ToList();
            var selectedDevice = selectedDeviceId.HasValue
                ? devices.FirstOrDefault(d => d.Id == selectedDeviceId.Value)
                : devices.FirstOrDefault();

            if (selectedDevice == null)
            {
                TempData["Error"] = "No device available.";
                return View(new PagedResult<UserDto> { PageNumber = 1, PageSize = pageSize, TotalCount = 0 });
            }

            var result = await _userService.GetByDeviceAsync(selectedDevice.Id, pageNumber, pageSize);

            ViewBag.Devices = devices;
            ViewBag.SelectedDeviceId = selectedDevice.Id;
            ViewBag.SelectedDeviceSN = selectedDevice.SerialNumber;
            ViewBag.EditUserCode = editUserCode;

            return View(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(int selectedDeviceId, UserDto dto, bool enrollFingerprint = false, bool enrollFace = false)
        {
            var device = await _deviceService.GetDeviceDtoAsync(selectedDeviceId);
            if (device == null) return NotFound();
            if (!User.CanAccessClient(device.ClientId)) return Forbid();

            dto.ClientId = device.ClientId;
            var user = await _userService.UpsertAsync(dto);
            await _userService.AttachUserToDeviceAsync(selectedDeviceId, user.Id);

            await _commandService.EnqueueAsync(
                device.SerialNumber,
                "DATA UPDATE USERINFO",
                commandText: BuildUserUpsertCommand(user));

            if (enrollFingerprint)
            {
                await _commandService.EnqueueAsync(
                    device.SerialNumber,
                    "ENROLL_FP",
                    commandText: BuildFingerprintEnrollCommand(user.UserCode));
            }

            if (enrollFace)
            {
                await _commandService.EnqueueAsync(
                    device.SerialNumber,
                    "ENROLL_FACE",
                    commandText: BuildFaceEnrollCommand(user.UserCode));
            }

            TempData["Success"] = "User saved and synced to selected device.";
            return RedirectToAction(nameof(Index), new { selectedDeviceId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int selectedDeviceId, string userCode)
        {
            var device = await _deviceService.GetDeviceDtoAsync(selectedDeviceId);
            if (device == null) return NotFound();
            if (!User.CanAccessClient(device.ClientId)) return Forbid();

            var user = await _userService.GetByCodeAsync(userCode, device.ClientId);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Index), new { selectedDeviceId });
            }

            var detached = await _userService.DetachUserFromDeviceAsync(selectedDeviceId, user.Id);
            if (!detached)
            {
                TempData["Error"] = "User not found for selected device.";
                return RedirectToAction(nameof(Index), new { selectedDeviceId });
            }

            if (!await _userService.UserHasAnyDeviceMappingAsync(user.Id))
            {
                await _userService.DeleteAsync(userCode, device.ClientId);
            }

            await _commandService.EnqueueAsync(
                device.SerialNumber,
                "DATA DELETE USERINFO",
                commandText: BuildUserDeleteCommand(userCode));

            TempData["Success"] = "User deleted and synced to selected device.";
            return RedirectToAction(nameof(Index), new { selectedDeviceId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnrollFingerprint(int selectedDeviceId, string userCode)
        {
            var device = await _deviceService.GetDeviceDtoAsync(selectedDeviceId);
            if (device == null) return NotFound();
            if (!User.CanAccessClient(device.ClientId)) return Forbid();

            await _commandService.EnqueueAsync(
                device.SerialNumber,
                "ENROLL_FP",
                commandText: BuildFingerprintEnrollCommand(userCode));

            TempData["Success"] = $"Fingerprint enrollment requested for user {userCode}.";
            return RedirectToAction(nameof(Index), new { selectedDeviceId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnrollFace(int selectedDeviceId, string userCode)
        {
            var device = await _deviceService.GetDeviceDtoAsync(selectedDeviceId);
            if (device == null) return NotFound();
            if (!User.CanAccessClient(device.ClientId)) return Forbid();

            await _commandService.EnqueueAsync(
                device.SerialNumber,
                "ENROLL_FACE",
                commandText: BuildFaceEnrollCommand(userCode));

            TempData["Success"] = $"Face enrollment requested for user {userCode}.";
            return RedirectToAction(nameof(Index), new { selectedDeviceId });
        }

        private static string BuildUserUpsertCommand(BioUser user)
        {
            var sanitizedName = SanitizeCommandValue(user.Name);
            var sanitizedCard = SanitizeCommandValue(user.CardNumber);
            var safeCode = SanitizeCommandValue(user.UserCode);

            return $"DATA UPDATE USERINFO PIN={safeCode}\tName={sanitizedName}\tPri={user.Privilege}\tPasswd=\tCard={sanitizedCard}\tGrp={DefaultUserGroup}\tTZ={DefaultUserTimezone}\tVerify={DefaultUserVerifyMode}\tViceCard={DefaultUserViceCard}";
        }

        private static string BuildUserDeleteCommand(string userCode)
        {
            var safeCode = SanitizeCommandValue(userCode);
            return $"DATA DELETE USERINFO PIN={safeCode}";
        }

        private static string BuildFingerprintEnrollCommand(string userCode)
        {
            var safeCode = SanitizeCommandValue(userCode);
            return $"ENROLL_FP PIN={safeCode} FID={DefaultFingerprintIndex} RETRY={DefaultEnrollRetryCount} OVERWRITE={DefaultEnrollOverwrite}";
        }

        private static string BuildFaceEnrollCommand(string userCode)
        {
            var safeCode = SanitizeCommandValue(userCode);
            return $"ENROLL_FACE PIN={safeCode} RETRY={DefaultEnrollRetryCount}";
        }

        private static string SanitizeCommandValue(string? value) =>
            (value ?? string.Empty)
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty)
                .Replace("\t", " ")
                .Trim();
    }
}
