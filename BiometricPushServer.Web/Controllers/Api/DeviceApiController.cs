using System.Threading.Tasks;
using BiometricPushServer.Common.Constants;
using BiometricPushServer.Common.DTOs;
using BiometricPushServer.Service.Interfaces;
using BiometricPushServer.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BiometricPushServer.Web.Controllers.Api
{
    [ApiController]
    [Route("api/device")]
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class DeviceApiController : ControllerBase
    {
        private readonly IDeviceService _deviceService;
        private readonly ICommandService _commandService;

        public DeviceApiController(IDeviceService deviceService, ICommandService commandService)
        {
            _deviceService = deviceService;
            _commandService = commandService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? clientId, [FromQuery] int? locationId)
        {
            var scopedClientId = User.ResolveClientId(clientId);
            var devices = await _deviceService.GetAllDevicesAsync(scopedClientId, locationId);
            return Ok(ApiResponse<object>.Ok(devices));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var device = await _deviceService.GetDeviceDtoAsync(id);
            if (device == null) return NotFound(ApiResponse<object>.Fail("Device not found", 404));
            if (!User.CanAccessClient(device.ClientId)) return Forbid();
            return Ok(ApiResponse<object>.Ok(device));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] DeviceUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Fail("Invalid payload"));

            var existing = await _deviceService.GetDeviceDtoAsync(id);
            if (existing == null) return NotFound(ApiResponse<object>.Fail("Device not found", 404));
            if (!User.CanAccessClient(existing.ClientId)) return Forbid();

            var claimClientId = User.GetClientIdClaim();
            if (claimClientId.HasValue)
                dto.ClientId = claimClientId.Value;

            var result = await _deviceService.UpdateDeviceAsync(id, dto);
            return result == null
                ? NotFound(ApiResponse<object>.Fail("Device not found", 404))
                : Ok(ApiResponse<object>.Ok(result));
        }

        [HttpPost("{id:int}/approve")]
        public async Task<IActionResult> Approve(int id)
        {
            var device = await _deviceService.GetDeviceDtoAsync(id);
            if (device == null) return NotFound(ApiResponse<object>.Fail("Device not found", 404));
            if (!User.CanAccessClient(device.ClientId)) return Forbid();

            var result = await _deviceService.ApproveDeviceAsync(id);
            return result
                ? Ok(ApiResponse<object>.OkMessage("Device approved"))
                : BadRequest(ApiResponse<object>.Fail("Device must have ClientId and LocationId before approval"));
        }

        [HttpPost("{id:int}/lock")]
        public async Task<IActionResult> Lock(int id)
        {
            var device = await _deviceService.GetDeviceDtoAsync(id);
            if (device == null) return NotFound(ApiResponse<object>.Fail("Device not found", 404));
            if (!User.CanAccessClient(device.ClientId)) return Forbid();
            await _deviceService.SetLockedAsync(id, true);
            await _commandService.EnqueueAsync(device.SerialNumber, "LOCK");
            return Ok(ApiResponse<object>.OkMessage("Lock command queued"));
        }

        [HttpPost("{id:int}/unlock")]
        public async Task<IActionResult> Unlock(int id)
        {
            var device = await _deviceService.GetDeviceDtoAsync(id);
            if (device == null) return NotFound(ApiResponse<object>.Fail("Device not found", 404));
            if (!User.CanAccessClient(device.ClientId)) return Forbid();
            await _deviceService.SetLockedAsync(id, false);
            await _commandService.EnqueueAsync(device.SerialNumber, "UNLOCK");
            return Ok(ApiResponse<object>.OkMessage("Unlock command queued"));
        }

        [HttpPost("{id:int}/restart")]
        public async Task<IActionResult> Restart(int id)
        {
            var device = await _deviceService.GetDeviceDtoAsync(id);
            if (device == null) return NotFound();
            if (!User.CanAccessClient(device.ClientId)) return Forbid();
            await _commandService.EnqueueAsync(device.SerialNumber, "RESTART");
            return Ok(ApiResponse<object>.OkMessage("Restart command queued"));
        }

        [HttpPost("{id:int}/synctime")]
        public async Task<IActionResult> SyncTime(int id)
        {
            var device = await _deviceService.GetDeviceDtoAsync(id);
            if (device == null) return NotFound();
            if (!User.CanAccessClient(device.ClientId)) return Forbid();
            await _commandService.EnqueueAsync(device.SerialNumber, "SYNCTIME");
            return Ok(ApiResponse<object>.OkMessage("SyncTime command queued"));
        }

        [HttpPost("{id:int}/syncattendancelogs")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SyncAttendanceLogs(int id)
        {
            var device = await _deviceService.GetDeviceDtoAsync(id);
            if (device == null) return NotFound();
            if (!User.CanAccessClient(device.ClientId)) return Forbid();
            await _commandService.EnqueueAsync(device.SerialNumber, AppConstants.CommandSyncAttendanceLogs);
            return Ok(ApiResponse<object>.OkMessage("Attendance log sync command queued"));
        }

        [HttpPost("{id:int}/clearattendance")]
        public async Task<IActionResult> ClearAttendance(int id)
        {
            var device = await _deviceService.GetDeviceDtoAsync(id);
            if (device == null) return NotFound();
            if (!User.CanAccessClient(device.ClientId)) return Forbid();
            await _commandService.EnqueueAsync(device.SerialNumber, "CLEAR ATT LOG");
            return Ok(ApiResponse<object>.OkMessage("Clear attendance command queued"));
        }

        [HttpPost("{id:int}/clearusers")]
        public async Task<IActionResult> ClearUsers(int id)
        {
            var device = await _deviceService.GetDeviceDtoAsync(id);
            if (device == null) return NotFound();
            if (!User.CanAccessClient(device.ClientId)) return Forbid();
            await _commandService.EnqueueAsync(device.SerialNumber, "CLEAR DATA");
            return Ok(ApiResponse<object>.OkMessage("Clear users command queued"));
        }

        [HttpPost("bulk-assign-location")]
        public async Task<IActionResult> BulkAssignLocation([FromBody] DeviceBulkAssignLocationDto dto, [FromQuery] int? clientId)
        {
            if (dto == null || dto.LocationId <= 0 || dto.DeviceIds.Count == 0)
                return BadRequest(ApiResponse<object>.Fail("LocationId and deviceIds are required"));

            var scopedClientId = User.ResolveClientId(clientId);
            var updated = await _deviceService.BulkAssignLocationAsync(dto.DeviceIds, dto.LocationId, scopedClientId);
            return Ok(ApiResponse<object>.Ok(new { updated }));
        }
    }
}
