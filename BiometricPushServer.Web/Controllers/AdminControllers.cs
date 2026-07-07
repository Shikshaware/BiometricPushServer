using System.Threading.Tasks;
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
        public async Task<IActionResult> Approve(int id)
        {
            await _deviceService.ApproveDeviceAsync(id);
            return RedirectToAction(nameof(Index));
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
        private readonly IUserService _userService;

        public UserController(IUserService userService) => _userService = userService;

        public async Task<IActionResult> Index(
            [FromQuery] int? clientId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            var result = await _userService.GetAllAsync(clientId, pageNumber, pageSize);
            return View(result);
        }
    }
}
