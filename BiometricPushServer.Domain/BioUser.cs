using System;
using System.Collections.Generic;

namespace BiometricPushServer.Domain
{
    public class BioUser
    {
        public int Id { get; set; }
        public int? ClientId { get; set; }
        public int? DepartmentId { get; set; }

        public string UserCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CardNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int Privilege { get; set; }  // 0=user,14=admin,2=admin
        public bool IsEnabled { get; set; } = true;

        public string PhotoPath { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedOn { get; set; }

        // Navigation
        public virtual ICollection<BioFingerprint> Fingerprints { get; set; } = new List<BioFingerprint>();
        public virtual ICollection<BioFaceTemplate> FaceTemplates { get; set; } = new List<BioFaceTemplate>();
        public virtual ICollection<BioDeviceUserMap> DeviceUsers { get; set; } = new List<BioDeviceUserMap>();
        public virtual ICollection<BioPalmTemplate> PalmTemplates { get; set; } = new List<BioPalmTemplate>();
    }
}
