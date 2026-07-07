using System.Threading.Tasks;
using BiometricPushServer.Common.DTOs;
using BiometricPushServer.Service.Interfaces;
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
            var items = await _departmentService.GetAllAsync(clientId);
            return Ok(ApiResponse<object>.Ok(items));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var dept = await _departmentService.GetByIdAsync(id);
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

            var created = await _departmentService.CreateAsync(dto);
            return Ok(ApiResponse<object>.Ok(created, "Department created"));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] DepartmentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Fail("Invalid payload"));

            var updated = await _departmentService.UpdateAsync(id, dto);
            return updated == null
                ? NotFound(ApiResponse<object>.Fail("Department not found", 404))
                : Ok(ApiResponse<object>.Ok(updated));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _departmentService.DeleteAsync(id);
            return deleted
                ? Ok(ApiResponse<object>.OkMessage("Department deleted"))
                : NotFound(ApiResponse<object>.Fail("Department not found", 404));
        }
    }
}
