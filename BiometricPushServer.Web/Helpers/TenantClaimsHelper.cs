using System.Security.Claims;
using BiometricPushServer.Common.Constants;

namespace BiometricPushServer.Web.Helpers
{
    public static class TenantClaimsHelper
    {
        public static int? GetClientIdClaim(this ClaimsPrincipal? user)
        {
            if (user == null) return null;
            var raw = user.FindFirstValue(AppConstants.Claim_ClientId);
            return int.TryParse(raw, out var value) ? value : null;
        }

        public static int? ResolveClientId(this ClaimsPrincipal? user, int? requestedClientId)
            => user.GetClientIdClaim() ?? requestedClientId;

        public static bool CanAccessClient(this ClaimsPrincipal? user, int? resourceClientId)
        {
            var claimClientId = user.GetClientIdClaim();
            if (!claimClientId.HasValue) return true;
            return resourceClientId.HasValue && claimClientId.Value == resourceClientId.Value;
        }
    }
}
