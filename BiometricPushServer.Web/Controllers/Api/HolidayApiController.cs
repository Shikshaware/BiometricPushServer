using System.Threading.Tasks;
using BiometricPushServer.Common.DTOs;
using BiometricPushServer.Service.Interfaces;
using BiometricPushServer.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BiometricPushServer.Web.Controllers.Api
{
    [ApiController]
    [Route("api/holiday")]
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class HolidayApiController : ControllerBase
    {
        private readonly IHolidayService _holidayService;

        public HolidayApiController(IHolidayService holidayService) => _holidayService = holidayService;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? clientId, [FromQuery] int? year)
        {
            var scopedClientId = User.ResolveClientId(clientId);
            var items = await _holidayService.GetAllAsync(scopedClientId, year);
            return Ok(ApiResponse<object>.Ok(items));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var holiday = await _holidayService.GetByIdAsync(id);
            if (holiday == null) return NotFound(ApiResponse<object>.Fail("Holiday not found", 404));
            if (!User.CanAccessClient(holiday.ClientId)) return Forbid();
            return Ok(ApiResponse<object>.Ok(holiday));
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Create([FromBody] HolidayDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Fail("Invalid payload"));

            var claimClientId = User.GetClientIdClaim();
            if (claimClientId.HasValue)
                dto.ClientId = claimClientId.Value;

            var created = await _holidayService.CreateAsync(dto);
            return Ok(ApiResponse<object>.Ok(created, "Holiday created"));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] HolidayDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Fail("Invalid payload"));

            var existing = await _holidayService.GetByIdAsync(id);
            if (existing == null) return NotFound(ApiResponse<object>.Fail("Holiday not found", 404));
            if (!User.CanAccessClient(existing.ClientId)) return Forbid();

            var claimClientId = User.GetClientIdClaim();
            if (claimClientId.HasValue)
                dto.ClientId = claimClientId.Value;

            var updated = await _holidayService.UpdateAsync(id, dto);
            return updated == null
                ? NotFound(ApiResponse<object>.Fail("Holiday not found", 404))
                : Ok(ApiResponse<object>.Ok(updated));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _holidayService.GetByIdAsync(id);
            if (existing == null) return NotFound(ApiResponse<object>.Fail("Holiday not found", 404));
            if (!User.CanAccessClient(existing.ClientId)) return Forbid();

            var deleted = await _holidayService.DeleteAsync(id);
            return deleted
                ? Ok(ApiResponse<object>.OkMessage("Holiday deleted"))
                : NotFound(ApiResponse<object>.Fail("Holiday not found", 404));
        }
    }
}
