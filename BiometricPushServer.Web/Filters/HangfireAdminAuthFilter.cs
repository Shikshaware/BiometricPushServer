using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;

namespace BiometricPushServer.Web.Filters
{
    /// <summary>
    /// Restricts the Hangfire dashboard to authenticated users with the Admin role.
    /// </summary>
    public class HangfireAdminAuthFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            return httpContext.User.Identity?.IsAuthenticated == true &&
                   httpContext.User.IsInRole("Admin");
        }
    }
}
