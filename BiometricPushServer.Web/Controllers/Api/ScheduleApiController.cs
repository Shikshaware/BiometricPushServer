using System;
using System.Threading.Tasks;
using BiometricPushServer.Common.DTOs;
using BiometricPushServer.Service.Interfaces;
using BiometricPushServer.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BiometricPushServer.Web.Controllers.Api
{
    [ApiController]
    [Route("api/schedule")]
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class ScheduleApiController : ControllerBase
    {
        private readonly IScheduleService _scheduleService;

        public ScheduleApiController(IScheduleService scheduleService) => _scheduleService = scheduleService;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? clientId, [FromQuery] int? userId)
        {
            var scopedClientId = User.ResolveClientId(clientId);
            var items = await _scheduleService.GetAllAsync(scopedClientId, userId);
            return Ok(ApiResponse<object>.Ok(items));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var schedule = await _scheduleService.GetByIdAsync(id);
            if (schedule == null) return NotFound(ApiResponse<object>.Fail("Schedule not found", 404));
            if (!User.CanAccessClient(schedule.ClientId)) return Forbid();
            return Ok(ApiResponse<object>.Ok(schedule));
        }

        /// <summary>
        /// Returns the active schedule (and shift) for a specific employee on a given date.
        /// GET /api/schedule/user/{userId}/active?date=2026-07-10
        /// </summary>
        [HttpGet("user/{userId:int}/active")]
        public async Task<IActionResult> GetActiveForUser(int userId, [FromQuery] DateTime? date)
        {
            var schedule = await _scheduleService.GetActiveForUserAsync(userId, date);
            return schedule == null
                ? NotFound(ApiResponse<object>.Fail("No active schedule found for this user", 404))
                : Ok(ApiResponse<object>.Ok(schedule));
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Create([FromBody] EmployeeScheduleDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Fail("Invalid payload"));

            var claimClientId = User.GetClientIdClaim();
            if (claimClientId.HasValue)
                dto.ClientId = claimClientId.Value;

            var created = await _scheduleService.CreateAsync(dto);
            return Ok(ApiResponse<object>.Ok(created, "Schedule created"));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] EmployeeScheduleDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Fail("Invalid payload"));

            var existing = await _scheduleService.GetByIdAsync(id);
            if (existing == null) return NotFound(ApiResponse<object>.Fail("Schedule not found", 404));
            if (!User.CanAccessClient(existing.ClientId)) return Forbid();

            var claimClientId = User.GetClientIdClaim();
            if (claimClientId.HasValue)
                dto.ClientId = claimClientId.Value;

            var updated = await _scheduleService.UpdateAsync(id, dto);
            return updated == null
                ? NotFound(ApiResponse<object>.Fail("Schedule not found", 404))
                : Ok(ApiResponse<object>.Ok(updated));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _scheduleService.GetByIdAsync(id);
            if (existing == null) return NotFound(ApiResponse<object>.Fail("Schedule not found", 404));
            if (!User.CanAccessClient(existing.ClientId)) return Forbid();

            var deleted = await _scheduleService.DeleteAsync(id);
            return deleted
                ? Ok(ApiResponse<object>.OkMessage("Schedule deleted"))
                : NotFound(ApiResponse<object>.Fail("Schedule not found", 404));
        }
    }
}
