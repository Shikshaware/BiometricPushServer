using System;
using System.Collections.Generic;

namespace BiometricPushServer.Domain
{
    public class BioDevice
    {
        public int Id { get; set; }
        public int? ClientId { get; set; }
        public int? LocationId { get; set; }

        public string SerialNumber { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public int? Port { get; set; }
        public string FirmwareVersion { get; set; } = string.Empty;
        public string DeviceModel { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string DeviceSecret { get; set; } = string.Empty;

        public bool IsApproved { get; set; }
        public bool IsActive { get; set; }
        public bool IsLocked { get; set; }

        public int? MaxUsers { get; set; }
        public int? MaxFingerprints { get; set; }
        public int? MaxCards { get; set; }

        public DateTime? LastConnectedOn { get; set; }
        public DateTime? LastHeartbeatOn { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedOn { get; set; }

        // Navigation properties
        public virtual ICollection<BioAttendanceLog> AttendanceLogs { get; set; } = new List<BioAttendanceLog>();
        public virtual ICollection<BioDeviceCommand> Commands { get; set; } = new List<BioDeviceCommand>();
        public virtual ICollection<BioDeviceUserMap> DeviceUsers { get; set; } = new List<BioDeviceUserMap>();
        public virtual ICollection<BioDeviceStatus> StatusHistory { get; set; } = new List<BioDeviceStatus>();
        public virtual ICollection<BioHeartbeat> Heartbeats { get; set; } = new List<BioHeartbeat>();
    }
}
