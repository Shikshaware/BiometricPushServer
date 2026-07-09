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
        public DbSet<BioDeviceUserMap> BioDeviceUserMaps { get; set; } = null!;
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
        public DbSet<BioPortalUser> BioPortalUsers { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BioDeviceUserMap>(entity =>
            {
                entity.HasKey(m => m.Id);

                entity.HasOne(m => m.Device)
                    .WithMany(d => d.DeviceUsers)
                    .HasForeignKey(m => m.DeviceId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.User)
                    .WithMany(u => u.DeviceUsers)
                    .HasForeignKey(m => m.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(m => new { m.DeviceId, m.UserId })
                    .IsUnique();
                entity.HasIndex(m => m.DeviceId);
                entity.HasIndex(m => m.UserId);
            });

            // Apply all entity configurations from this assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(BiometricDbContext).Assembly);
        }
    }
}
