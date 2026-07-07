using System;

namespace BiometricPushServer.Domain
{
    public class BioDeviceStatus
    {
        public long Id { get; set; }
        public int DeviceId { get; set; }
        public string DeviceSN { get; set; } = string.Empty;
        public int? ClientId { get; set; }

        public bool IsOnline { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public DateTime StatusTime { get; set; }
        public string Reason { get; set; } = string.Empty;  // HEARTBEAT, REGISTRATION, TIMEOUT

        public virtual BioDevice? Device { get; set; }
    }

    public class BioHeartbeat
    {
        public long Id { get; set; }
        public int? DeviceId { get; set; }
        public string DeviceSN { get; set; } = string.Empty;
        public int? ClientId { get; set; }

        public string IpAddress { get; set; } = string.Empty;
        public DateTime PingTime { get; set; }
        public string RawQuery { get; set; } = string.Empty;
        public string UserCount { get; set; } = string.Empty;
        public string AttCount { get; set; } = string.Empty;

        public virtual BioDevice? Device { get; set; }
    }
}
