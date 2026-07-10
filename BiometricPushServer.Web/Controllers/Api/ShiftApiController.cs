using System.Threading.Tasks;
using BiometricPushServer.Common.DTOs;
using BiometricPushServer.Service.Interfaces;
using BiometricPushServer.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BiometricPushServer.Web.Controllers.Api
{
    [ApiController]
    [Route("api/shift")]
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class ShiftApiController : ControllerBase
    {
        private readonly IShiftService _shiftService;

        public ShiftApiController(IShiftService shiftService) => _shiftService = shiftService;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? clientId)
        {
            var scopedClientId = User.ResolveClientId(clientId);
            var items = await _shiftService.GetAllAsync(scopedClientId);
            return Ok(ApiResponse<object>.Ok(items));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var shift = await _shiftService.GetByIdAsync(id);
            if (shift == null) return NotFound(ApiResponse<object>.Fail("Shift not found", 404));
            if (!User.CanAccessClient(shift.ClientId)) return Forbid();
            return Ok(ApiResponse<object>.Ok(shift));
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Create([FromBody] ShiftDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Fail("Invalid payload"));

            var claimClientId = User.GetClientIdClaim();
            if (claimClientId.HasValue)
                dto.ClientId = claimClientId.Value;

            var created = await _shiftService.CreateAsync(dto);
            return Ok(ApiResponse<object>.Ok(created, "Shift created"));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ShiftDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Fail("Invalid payload"));

            var existing = await _shiftService.GetByIdAsync(id);
            if (existing == null) return NotFound(ApiResponse<object>.Fail("Shift not found", 404));
            if (!User.CanAccessClient(existing.ClientId)) return Forbid();

            var claimClientId = User.GetClientIdClaim();
            if (claimClientId.HasValue)
                dto.ClientId = claimClientId.Value;

            var updated = await _shiftService.UpdateAsync(id, dto);
            return updated == null
                ? NotFound(ApiResponse<object>.Fail("Shift not found", 404))
                : Ok(ApiResponse<object>.Ok(updated));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _shiftService.GetByIdAsync(id);
            if (existing == null) return NotFound(ApiResponse<object>.Fail("Shift not found", 404));
            if (!User.CanAccessClient(existing.ClientId)) return Forbid();

            var deleted = await _shiftService.DeleteAsync(id);
            return deleted
                ? Ok(ApiResponse<object>.OkMessage("Shift deleted"))
                : NotFound(ApiResponse<object>.Fail("Shift not found", 404));
        }
    }
}
