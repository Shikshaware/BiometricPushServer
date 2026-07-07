using System;

namespace BiometricPushServer.Domain
{
    public class BioDeviceCommand
    {
        public int Id { get; set; }
        public int? DeviceId { get; set; }
        public string DeviceSN { get; set; } = string.Empty;
        public int? ClientId { get; set; }

        public string CommandType { get; set; } = string.Empty;  // RESTART, SYNC_TIME, CLEAR_ATT, etc.
        public string CommandText { get; set; } = string.Empty;
        public string Parameters { get; set; } = string.Empty;  // JSON params

        public bool IsSent { get; set; }
        public bool IsExecuted { get; set; }
        public bool IsFailed { get; set; }
        public string ResponseText { get; set; } = string.Empty;
        public int RetryCount { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime? SentOn { get; set; }
        public DateTime? ExecutedOn { get; set; }
        public DateTime? ExpiresOn { get; set; }

        // Navigation
        public virtual BioDevice? Device { get; set; }
    }
}
