using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using BiometricPushServer.Common.Constants;
using BiometricPushServer.Repository.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BiometricPushServer.Web.Authentication
{
    /// <summary>
    /// Authenticates requests that carry an X-Api-Key header by validating the key
    /// against the ApiClients table.  Successful authentication builds a ClaimsPrincipal
    /// so that plain [Authorize] on API controllers works with either JWT or an API key.
    /// </summary>
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "ApiKey";

        private readonly IUnitOfWork _uow;

        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IUnitOfWork uow)
            : base(options, logger, encoder)
        {
            _uow = uow;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(AppConstants.ApiKeyHeader, out var apiKeyValues))
                return AuthenticateResult.NoResult();

            var apiKey = apiKeyValues.ToString();

            var client = await _uow.ApiClients.FirstOrDefaultAsync(c =>
                c.ApiKey == apiKey &&
                c.IsActive &&
                (c.ExpiresOn == null || c.ExpiresOn > DateTime.UtcNow));

            if (client == null)
                return AuthenticateResult.Fail("Invalid or expired API key");

            // Optional IP whitelist check
            if (!string.IsNullOrWhiteSpace(client.AllowedIps))
            {
                var remoteIp = Context.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
                var allowed = client.AllowedIps
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(ip => ip.Trim());

                if (!allowed.Contains(remoteIp))
                    return AuthenticateResult.Fail("IP address not allowed");
            }

            // Record last usage — throttled to at most one DB write per 5 minutes
            // to avoid a write on every API call under high load.
            if (client.LastUsedOn == null || (DateTime.UtcNow - client.LastUsedOn.Value).TotalMinutes >= 5)
            {
                client.LastUsedOn = DateTime.UtcNow;
                _uow.ApiClients.Update(client);
                await _uow.SaveChangesAsync();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, client.Name),
                new Claim(AppConstants.Claim_ApiClientId, client.Id.ToString())
            };

            if (client.ClientId.HasValue)
                claims.Add(new Claim(AppConstants.Claim_ClientId, client.ClientId.Value.ToString()));

            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);

            return AuthenticateResult.Success(ticket);
        }
    }
}
