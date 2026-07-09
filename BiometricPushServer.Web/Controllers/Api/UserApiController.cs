using System.Threading.Tasks;
using BiometricPushServer.Common.DTOs;
using BiometricPushServer.Service.Interfaces;
using BiometricPushServer.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BiometricPushServer.Web.Controllers.Api
{
    [ApiController]
    [Route("api/user")]
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class UserApiController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserApiController(IUserService userService) => _userService = userService;

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? clientId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            var scopedClientId = User.ResolveClientId(clientId);
            var result = await _userService.GetAllAsync(scopedClientId, pageNumber, pageSize);
            return Ok(ApiResponse<object>.Ok(result));
        }

        [HttpGet("{userCode}")]
        public async Task<IActionResult> Get(string userCode, [FromQuery] int? clientId)
        {
            var scopedClientId = User.ResolveClientId(clientId);
            var user = await _userService.GetByCodeAsync(userCode, scopedClientId);
            if (user == null) return NotFound(ApiResponse<object>.Fail("User not found", 404));
            return Ok(ApiResponse<object>.Ok(user));
        }

        [HttpPost]
        public async Task<IActionResult> Upsert([FromBody] UserDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Fail("Invalid payload"));

            var claimClientId = User.GetClientIdClaim();
            if (claimClientId.HasValue)
                dto.ClientId = claimClientId;

            var user = await _userService.UpsertAsync(dto);
            return Ok(ApiResponse<object>.Ok(user));
        }

        [HttpDelete("{userCode}")]
        public async Task<IActionResult> Delete(string userCode, [FromQuery] int? clientId)
        {
            var scopedClientId = User.ResolveClientId(clientId);
            var deleted = await _userService.DeleteAsync(userCode, scopedClientId);
            return deleted
                ? Ok(ApiResponse<object>.OkMessage("User deleted"))
                : NotFound(ApiResponse<object>.Fail("User not found", 404));
        }
    }
}
