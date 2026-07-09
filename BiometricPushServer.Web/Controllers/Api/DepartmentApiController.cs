using System.Threading.Tasks;
using BiometricPushServer.Common.DTOs;
using BiometricPushServer.Service.Interfaces;
using BiometricPushServer.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BiometricPushServer.Web.Controllers.Api
{
    [ApiController]
    [Route("api/department")]
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class DepartmentApiController : ControllerBase
    {
        private readonly IDepartmentService _departmentService;

        public DepartmentApiController(IDepartmentService departmentService)
            => _departmentService = departmentService;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? clientId)
        {
            var scopedClientId = User.ResolveClientId(clientId);
            var items = await _departmentService.GetAllAsync(scopedClientId);
            return Ok(ApiResponse<object>.Ok(items));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var dept = await _departmentService.GetByIdAsync(id);
            if (dept != null && !User.CanAccessClient(dept.ClientId)) return Forbid();
            return dept == null
                ? NotFound(ApiResponse<object>.Fail("Department not found", 404))
                : Ok(ApiResponse<object>.Ok(dept));
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Create([FromBody] DepartmentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Fail("Invalid payload"));

            var claimClientId = User.GetClientIdClaim();
            if (claimClientId.HasValue)
                dto.ClientId = claimClientId;

            var created = await _departmentService.CreateAsync(dto);
            return Ok(ApiResponse<object>.Ok(created, "Department created"));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] DepartmentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Fail("Invalid payload"));

            var existing = await _departmentService.GetByIdAsync(id);
            if (existing == null) return NotFound(ApiResponse<object>.Fail("Department not found", 404));
            if (!User.CanAccessClient(existing.ClientId)) return Forbid();

            var claimClientId = User.GetClientIdClaim();
            if (claimClientId.HasValue)
                dto.ClientId = claimClientId;

            var updated = await _departmentService.UpdateAsync(id, dto);
            return updated == null
                ? NotFound(ApiResponse<object>.Fail("Department not found", 404))
                : Ok(ApiResponse<object>.Ok(updated));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _departmentService.GetByIdAsync(id);
            if (existing == null) return NotFound(ApiResponse<object>.Fail("Department not found", 404));
            if (!User.CanAccessClient(existing.ClientId)) return Forbid();

            var deleted = await _departmentService.DeleteAsync(id);
            return deleted
                ? Ok(ApiResponse<object>.OkMessage("Department deleted"))
                : NotFound(ApiResponse<object>.Fail("Department not found", 404));
        }
    }
}
