using System;

namespace BiometricPushServer.Domain
{
    public class BioTransaction
    {
        public long Id { get; set; }
        public int? ClientId { get; set; }
        public string DeviceSN { get; set; } = string.Empty;
        public string UserCode { get; set; } = string.Empty;
        public DateTime TransactionTime { get; set; }
        public int Type { get; set; }   // same as attendance state
        public int VerifyMode { get; set; }
        public string RawData { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }

    public class BioLog
    {
        public long Id { get; set; }
        public int? ClientId { get; set; }
        public string Level { get; set; } = string.Empty;     // INFO, WARN, ERROR
        public string Source { get; set; } = string.Empty;    // controller / service name
        public string Message { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public string DeviceSN { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }

    public class BioErrorLog
    {
        public long Id { get; set; }
        public int? ClientId { get; set; }
        public string DeviceSN { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string RawData { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string StackTrace { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }

    public class BioSyncHistory
    {
        public int Id { get; set; }
        public int? ClientId { get; set; }
        public int? DeviceId { get; set; }
        public string DeviceSN { get; set; } = string.Empty;
        public string SyncType { get; set; } = string.Empty;  // ATTENDANCE, USERS, TEMPLATES
        public int RecordsSent { get; set; }
        public int RecordsReceived { get; set; }
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime StartedOn { get; set; }
        public DateTime? CompletedOn { get; set; }
    }
}
