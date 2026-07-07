using System;
using System.Linq;
using System.Threading.Tasks;
using BiometricPushServer.Common.Constants;
using BiometricPushServer.Repository.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BiometricPushServer.Web.Filters
{
    /// <summary>
    /// Action filter that validates X-Api-Key header against the ApiClients table.
    /// Apply [ApiKeyAuthorize] on controllers or actions that require API key auth.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ApiKeyAuthorizeAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!context.HttpContext.Request.Headers.TryGetValue(AppConstants.ApiKeyHeader, out var apiKey))
            {
                context.Result = new UnauthorizedObjectResult(new { message = "API key required" });
                return;
            }

            var uow = context.HttpContext.RequestServices.GetService(typeof(IUnitOfWork)) as IUnitOfWork;
            if (uow == null)
            {
                context.Result = new StatusCodeResult(500);
                return;
            }

            var client = await uow.ApiClients.FirstOrDefaultAsync(c =>
                c.ApiKey == apiKey.ToString() &&
                c.IsActive &&
                (c.ExpiresOn == null || c.ExpiresOn > DateTime.UtcNow));

            if (client == null)
            {
                context.Result = new UnauthorizedObjectResult(new { message = "Invalid or expired API key" });
                return;
            }

            // Optional: IP whitelist check
            if (!string.IsNullOrWhiteSpace(client.AllowedIps))
            {
                var remoteIp = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
                var allowed = client.AllowedIps
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(ip => ip.Trim());

                if (!allowed.Contains(remoteIp))
                {
                    context.Result = new ForbidResult();
                    return;
                }
            }

            // Update last used timestamp
            client.LastUsedOn = DateTime.UtcNow;
            uow.ApiClients.Update(client);
            await uow.SaveChangesAsync();

            await next();
        }
    }
}
