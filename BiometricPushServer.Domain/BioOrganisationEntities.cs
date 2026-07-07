using System;
using System.Collections.Generic;

namespace BiometricPushServer.Domain
{
    public class BioCompany
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Logo { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public virtual ICollection<BioLocation> Locations { get; set; } = new List<BioLocation>();
        public virtual ICollection<BioDepartment> Departments { get; set; } = new List<BioDepartment>();
    }

    public class BioLocation
    {
        public int Id { get; set; }
        public int? CompanyId { get; set; }
        public int? ClientId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public virtual BioCompany? Company { get; set; }
        public virtual ICollection<BioDevice> Devices { get; set; } = new List<BioDevice>();
    }

    public class BioDepartment
    {
        public int Id { get; set; }
        public int? CompanyId { get; set; }
        public int? ClientId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public virtual BioCompany? Company { get; set; }
        public virtual ICollection<BioUser> Users { get; set; } = new List<BioUser>();
    }

    public class BioShift
    {
        public int Id { get; set; }
        public int? ClientId { get; set; }
        public string Name { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int GracePeriodMinutes { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }

    public class BioHoliday
    {
        public int Id { get; set; }
        public int? ClientId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime HolidayDate { get; set; }
        public bool IsRecurring { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }

    public class BioEmployeeSchedule
    {
        public int Id { get; set; }
        public int? ClientId { get; set; }
        public int UserId { get; set; }
        public int? ShiftId { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public virtual BioUser? User { get; set; }
        public virtual BioShift? Shift { get; set; }
    }

    public class ApiClient
    {
        public int Id { get; set; }
        public int? ClientId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ApiSecret { get; set; } = string.Empty;
        public string AllowedIps { get; set; } = string.Empty;  // comma-separated
        public bool IsActive { get; set; } = true;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresOn { get; set; }
        public DateTime? LastUsedOn { get; set; }
    }
}
