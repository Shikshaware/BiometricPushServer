using System.Linq;
using System.Threading.Tasks;
using BiometricPushServer.Common.Constants;
using BiometricPushServer.Common.DTOs;
using BiometricPushServer.Domain;
using BiometricPushServer.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BiometricPushServer.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index([FromQuery] int? clientId)
        {
            var stats = await _dashboardService.GetStatsAsync(clientId);
            return View(stats);
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

        public async Task<IActionResult> Index([FromQuery] int? clientId)
        {
            var devices = await _deviceService.GetAllDevicesAsync(clientId);
            return View(devices);
        }

        public async Task<IActionResult> Details(int id)
        {
            var device = await _deviceService.GetDeviceDtoAsync(id);
            if (device == null) return NotFound();
            return View(device);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            await _deviceService.ApproveDeviceAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Lock(int id)
        {
            var device = await _deviceService.GetDeviceDtoAsync(id);
            if (device == null) return NotFound();
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
            await _commandService.EnqueueAsync(device.SerialNumber, "RESTART");
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncTime(int id)
        {
            var device = await _deviceService.GetDeviceDtoAsync(id);
            if (device == null) return NotFound();
            await _commandService.EnqueueAsync(device.SerialNumber, "SYNCTIME");
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncAttendanceLogs(int id)
        {
            var device = await _deviceService.GetDeviceDtoAsync(id);
            if (device == null) return NotFound();
            await _commandService.EnqueueAsync(device.SerialNumber, AppConstants.CommandSyncAttendanceLogs);
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearAttendance(int id)
        {
            var device = await _deviceService.GetDeviceDtoAsync(id);
            if (device == null) return NotFound();
            await _commandService.EnqueueAsync(device.SerialNumber, "CLEAR ATT LOG");
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearUsers(int id)
        {
            var device = await _deviceService.GetDeviceDtoAsync(id);
            if (device == null) return NotFound();
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
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            var result = await _attendanceService.GetAttendanceAsync(clientId, pageNumber, pageSize);
            return View(result);
        }

        public async Task<IActionResult> Today([FromQuery] int? clientId)
        {
            var logs = await _attendanceService.GetTodayAsync(clientId);
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
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            var devices = (await _deviceService.GetAllDevicesAsync(clientId)).ToList();
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
