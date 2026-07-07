using System;

namespace BiometricPushServer.Domain
{
    public class BioFingerprint
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? ClientId { get; set; }

        public string UserCode { get; set; } = string.Empty;
        public int FingerIndex { get; set; }   // 0-9 finger positions
        public string Template { get; set; } = string.Empty;  // Base64 template
        public int Size { get; set; }
        public int Valid { get; set; } = 1;

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedOn { get; set; }

        // Navigation
        public virtual BioUser? User { get; set; }
    }
}
