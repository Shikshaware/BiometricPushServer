using System.Threading.Tasks;
using BiometricPushServer.Common.DTOs;
using BiometricPushServer.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BiometricPushServer.Web.Controllers.Api
{
    [ApiController]
    [Route("api/location")]
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class LocationApiController : ControllerBase
    {
        private readonly ILocationService _locationService;

        public LocationApiController(ILocationService locationService)
            => _locationService = locationService;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? clientId)
        {
            var items = await _locationService.GetAllAsync(clientId);
            return Ok(ApiResponse<object>.Ok(items));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var loc = await _locationService.GetByIdAsync(id);
            return loc == null
                ? NotFound(ApiResponse<object>.Fail("Location not found", 404))
                : Ok(ApiResponse<object>.Ok(loc));
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Create([FromBody] LocationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Fail("Invalid payload"));

            var created = await _locationService.CreateAsync(dto);
            return Ok(ApiResponse<object>.Ok(created, "Location created"));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] LocationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Fail("Invalid payload"));

            var updated = await _locationService.UpdateAsync(id, dto);
            return updated == null
                ? NotFound(ApiResponse<object>.Fail("Location not found", 404))
                : Ok(ApiResponse<object>.Ok(updated));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _locationService.DeleteAsync(id);
            return deleted
                ? Ok(ApiResponse<object>.OkMessage("Location deleted"))
                : NotFound(ApiResponse<object>.Fail("Location not found", 404));
        }
    }
}
