using System.Threading.Tasks;
using BiometricPushServer.Common.DTOs;
using BiometricPushServer.Service.Interfaces;
using BiometricPushServer.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BiometricPushServer.Web.Controllers.Api
{
    [ApiController]
    [Route("api/dashboard")]
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class DashboardApiController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardApiController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats([FromQuery] int? clientId, [FromQuery] int? locationId)
        {
            var scopedClientId = User.ResolveClientId(clientId);
            var stats = await _dashboardService.GetStatsAsync(scopedClientId, locationId);
            return Ok(ApiResponse<object>.Ok(stats));
        }
    }
}
