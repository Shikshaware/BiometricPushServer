using System;

namespace BiometricPushServer.Domain
{
    public class BioDeviceUserMap
    {
        public int Id { get; set; }
        public int DeviceId { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public virtual BioDevice Device { get; set; } = null!;
        public virtual BioUser User { get; set; } = null!;
    }
}
