using System;
using System.Threading.Tasks;
using BiometricPushServer.Common.DTOs;
using BiometricPushServer.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BiometricPushServer.Web.Controllers.Api
{
    [ApiController]
    [Route("api/attendance")]
    [Authorize]
    public class AttendanceApiController : ControllerBase
    {
        private readonly IAttendanceService _attendanceService;

        public AttendanceApiController(IAttendanceService attendanceService)
        {
            _attendanceService = attendanceService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? clientId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            var result = await _attendanceService.GetAttendanceAsync(clientId, pageNumber, pageSize);
            return Ok(ApiResponse<object>.Ok(result));
        }

        [HttpGet("today")]
        public async Task<IActionResult> GetToday([FromQuery] int? clientId)
        {
            var logs = await _attendanceService.GetTodayAsync(clientId);
            return Ok(ApiResponse<object>.Ok(logs));
        }

        [HttpGet("device/{deviceSN}")]
        public async Task<IActionResult> GetByDevice(
            string deviceSN,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var fromDate = from ?? DateTime.Today;
            var toDate = to ?? DateTime.Today.AddDays(1).AddTicks(-1);
            var logs = await _attendanceService.GetByDeviceAsync(deviceSN, fromDate, toDate);
            return Ok(ApiResponse<object>.Ok(logs));
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
    }
}
