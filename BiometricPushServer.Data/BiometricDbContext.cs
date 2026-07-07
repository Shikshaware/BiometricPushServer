using BiometricPushServer.Domain;
using Microsoft.EntityFrameworkCore;

namespace BiometricPushServer.Data
{
    public class BiometricDbContext : DbContext
    {
        public BiometricDbContext(DbContextOptions<BiometricDbContext> options) : base(options) { }

        // Device management
        public DbSet<BioDevice> BioDevices { get; set; } = null!;
        public DbSet<BioDeviceCommand> BioDeviceCommands { get; set; } = null!;
        public DbSet<BioDeviceStatus> BioDeviceStatuses { get; set; } = null!;
        public DbSet<BioHeartbeat> BioHeartbeats { get; set; } = null!;

        // Attendance
        public DbSet<BioAttendanceLog> BioAttendanceLogs { get; set; } = null!;
        public DbSet<BioTransaction> BioTransactions { get; set; } = null!;

        // Users & biometric templates
        public DbSet<BioUser> BioUsers { get; set; } = null!;
        public DbSet<BioFingerprint> BioFingerprints { get; set; } = null!;
        public DbSet<BioFaceTemplate> BioFaceTemplates { get; set; } = null!;
        public DbSet<BioPalmTemplate> BioPalmTemplates { get; set; } = null!;

        // Organisation
        public DbSet<BioCompany> BioCompanies { get; set; } = null!;
        public DbSet<BioLocation> BioLocations { get; set; } = null!;
        public DbSet<BioDepartment> BioDepartments { get; set; } = null!;
        public DbSet<BioShift> BioShifts { get; set; } = null!;
        public DbSet<BioHoliday> BioHolidays { get; set; } = null!;
        public DbSet<BioEmployeeSchedule> BioEmployeeSchedules { get; set; } = null!;

        // Audit & security
        public DbSet<BioLog> BioLogs { get; set; } = null!;
        public DbSet<BioErrorLog> BioErrorLogs { get; set; } = null!;
        public DbSet<BioSyncHistory> BioSyncHistories { get; set; } = null!;
        public DbSet<ApiClient> ApiClients { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all entity configurations from this assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(BiometricDbContext).Assembly);
        }
    }
}
