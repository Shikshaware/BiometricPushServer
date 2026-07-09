using System;

namespace BiometricPushServer.Domain
{
    public class BioPortalUser
    {
        public int Id { get; set; }
        public int? ClientId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "Owner";
        public string TimeZoneId { get; set; } = "UTC";
        public bool IsActive { get; set; } = true;
        public string InviteToken { get; set; } = string.Empty;
        public DateTime? InviteExpiresOn { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedOn { get; set; }
        public DateTime? LastLoginOn { get; set; }
    }
}
