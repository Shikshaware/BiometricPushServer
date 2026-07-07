using System.Threading.Tasks;
using BiometricPushServer.Common.DTOs;
using BiometricPushServer.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BiometricPushServer.Web.Controllers.Api
{
    [ApiController]
    [Route("api/command")]
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class CommandApiController : ControllerBase
    {
        private readonly ICommandService _commandService;

        public CommandApiController(ICommandService commandService) => _commandService = commandService;

        [HttpPost]
        public async Task<IActionResult> Enqueue([FromBody] CommandDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.DeviceSN) || string.IsNullOrWhiteSpace(dto.CommandType))
                return BadRequest(ApiResponse<object>.Fail("DeviceSN and CommandType are required"));

            var cmd = await _commandService.EnqueueAsync(dto.DeviceSN, dto.CommandType);
            return Ok(ApiResponse<object>.Ok(cmd, "Command queued"));
        }

        [HttpGet("pending/{deviceSN}")]
        public async Task<IActionResult> GetPending(string deviceSN)
        {
            var cmds = await _commandService.GetPendingAsync(deviceSN);
            return Ok(ApiResponse<object>.Ok(cmds));
        }
    }
}
