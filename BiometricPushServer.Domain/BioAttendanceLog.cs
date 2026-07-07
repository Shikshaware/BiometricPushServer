using System;

namespace BiometricPushServer.Domain
{
    public class BioAttendanceLog
    {
        public long Id { get; set; }
        public int? ClientId { get; set; }
        public int? DeviceId { get; set; }

        public string DeviceSN { get; set; } = string.Empty;
        public string UserCode { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;

        public DateTime PunchTime { get; set; }
        public int AttendanceState { get; set; }   // 0=check-in,1=check-out,2=OT-in,3=OT-out
        public int VerifyMode { get; set; }         // 1=finger,2=face,3=card,4=pin,5=palm,6=qr
        public string WorkCode { get; set; } = string.Empty;
        public string RawData { get; set; } = string.Empty;

        public bool IsDuplicate { get; set; }
        public bool IsSyncedToERP { get; set; }
        public DateTime? SyncedOn { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual BioDevice? Device { get; set; }
    }
}
