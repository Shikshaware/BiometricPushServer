using BiometricPushServer.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BiometricPushServer.Data.EntityConfigurations
{
    public class BioDeviceConfiguration : IEntityTypeConfiguration<BioDevice>
    {
        public void Configure(EntityTypeBuilder<BioDevice> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.SerialNumber).IsRequired().HasMaxLength(100);
            builder.Property(x => x.DeviceName).HasMaxLength(200);
            builder.Property(x => x.IpAddress).HasMaxLength(50);
            builder.Property(x => x.FirmwareVersion).HasMaxLength(100);
            builder.Property(x => x.DeviceModel).HasMaxLength(100);
            builder.Property(x => x.Manufacturer).HasMaxLength(100);
            builder.Property(x => x.DeviceSecret).HasMaxLength(256);
            builder.Property(x => x.Location).HasMaxLength(300);

            builder.HasIndex(x => x.SerialNumber).IsUnique();
            builder.HasIndex(x => x.ClientId);
            builder.HasIndex(x => x.LocationId);
            builder.HasIndex(x => new { x.ClientId, x.LocationId });
        }
    }

    public class BioAttendanceLogConfiguration : IEntityTypeConfiguration<BioAttendanceLog>
    {
        public void Configure(EntityTypeBuilder<BioAttendanceLog> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.DeviceSN).IsRequired().HasMaxLength(100);
            builder.Property(x => x.UserCode).IsRequired().HasMaxLength(50);
            builder.Property(x => x.UserName).HasMaxLength(200);
            builder.Property(x => x.WorkCode).HasMaxLength(50);
            builder.Property(x => x.RawData).HasMaxLength(2000);

            // Unique constraint to prevent duplicate punches
            builder.HasIndex(x => new { x.DeviceSN, x.UserCode, x.PunchTime }).IsUnique();
            builder.HasIndex(x => x.ClientId);
            builder.HasIndex(x => x.PunchTime);

            builder.HasOne(x => x.Device)
                   .WithMany(d => d.AttendanceLogs)
                   .HasForeignKey(x => x.DeviceId)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }

    public class BioUserConfiguration : IEntityTypeConfiguration<BioUser>
    {
        public void Configure(EntityTypeBuilder<BioUser> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.UserCode).IsRequired().HasMaxLength(50);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
            builder.Property(x => x.CardNumber).HasMaxLength(50);
            builder.Property(x => x.Password).HasMaxLength(256);
            builder.Property(x => x.PhotoPath).HasMaxLength(500);
            builder.Property(x => x.Email).HasMaxLength(200);
            builder.Property(x => x.Phone).HasMaxLength(30);

            builder.HasIndex(x => new { x.ClientId, x.UserCode }).IsUnique();
        }
    }

    public class BioDeviceCommandConfiguration : IEntityTypeConfiguration<BioDeviceCommand>
    {
        public void Configure(EntityTypeBuilder<BioDeviceCommand> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.DeviceSN).IsRequired().HasMaxLength(100);
            builder.Property(x => x.CommandType).IsRequired().HasMaxLength(50);
            builder.Property(x => x.CommandText).HasMaxLength(2000);
            builder.Property(x => x.Parameters).HasMaxLength(4000);
            builder.Property(x => x.ResponseText).HasMaxLength(2000);

            builder.HasIndex(x => new { x.DeviceSN, x.IsSent, x.IsExecuted });

            builder.HasOne(x => x.Device)
                   .WithMany(d => d.Commands)
                   .HasForeignKey(x => x.DeviceId)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }

    public class BioDeviceStatusConfiguration : IEntityTypeConfiguration<BioDeviceStatus>
    {
        public void Configure(EntityTypeBuilder<BioDeviceStatus> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.DeviceSN).IsRequired().HasMaxLength(100);
            builder.Property(x => x.IpAddress).HasMaxLength(50);
            builder.Property(x => x.Reason).HasMaxLength(100);

            builder.HasIndex(x => new { x.DeviceSN, x.StatusTime });

            builder.HasOne(x => x.Device)
                   .WithMany(d => d.StatusHistory)
                   .HasForeignKey(x => x.DeviceId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class BioHeartbeatConfiguration : IEntityTypeConfiguration<BioHeartbeat>
    {
        public void Configure(EntityTypeBuilder<BioHeartbeat> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.DeviceSN).IsRequired().HasMaxLength(100);
            builder.Property(x => x.IpAddress).HasMaxLength(50);
            builder.Property(x => x.RawQuery).HasMaxLength(2000);

            builder.HasIndex(x => new { x.DeviceSN, x.PingTime });

            builder.HasOne(x => x.Device)
                   .WithMany(d => d.Heartbeats)
                   .HasForeignKey(x => x.DeviceId)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }

    public class BioFingerprintConfiguration : IEntityTypeConfiguration<BioFingerprint>
    {
        public void Configure(EntityTypeBuilder<BioFingerprint> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.UserCode).IsRequired().HasMaxLength(50);

            builder.HasIndex(x => new { x.UserId, x.FingerIndex }).IsUnique();

            builder.HasOne(x => x.User)
                   .WithMany(u => u.Fingerprints)
                   .HasForeignKey(x => x.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class BioFaceTemplateConfiguration : IEntityTypeConfiguration<BioFaceTemplate>
    {
        public void Configure(EntityTypeBuilder<BioFaceTemplate> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.UserCode).IsRequired().HasMaxLength(50);
            builder.Property(x => x.PhotoPath).HasMaxLength(500);

            builder.HasOne(x => x.User)
                   .WithMany(u => u.FaceTemplates)
                   .HasForeignKey(x => x.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class BioPalmTemplateConfiguration : IEntityTypeConfiguration<BioPalmTemplate>
    {
        public void Configure(EntityTypeBuilder<BioPalmTemplate> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.UserCode).IsRequired().HasMaxLength(50);

            builder.HasOne(x => x.User)
                   .WithMany(u => u.PalmTemplates)
                   .HasForeignKey(x => x.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class BioCompanyConfiguration : IEntityTypeConfiguration<BioCompany>
    {
        public void Configure(EntityTypeBuilder<BioCompany> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
            builder.Property(x => x.Phone).HasMaxLength(30);
            builder.Property(x => x.Email).HasMaxLength(200);
            builder.Property(x => x.Logo).HasMaxLength(500);
            builder.HasIndex(x => x.Code).IsUnique();
        }
    }

    public class BioLocationConfiguration : IEntityTypeConfiguration<BioLocation>
    {
        public void Configure(EntityTypeBuilder<BioLocation> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Address).HasMaxLength(500);

            builder.HasOne(x => x.Company)
                   .WithMany(c => c.Locations)
                   .HasForeignKey(x => x.CompanyId)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }

    public class BioDepartmentConfiguration : IEntityTypeConfiguration<BioDepartment>
    {
        public void Configure(EntityTypeBuilder<BioDepartment> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Code).HasMaxLength(50);

            builder.HasOne(x => x.Company)
                   .WithMany(c => c.Departments)
                   .HasForeignKey(x => x.CompanyId)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }

    public class BioPortalUserConfiguration : IEntityTypeConfiguration<BioPortalUser>
    {
        public void Configure(EntityTypeBuilder<BioPortalUser> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Username).IsRequired().HasMaxLength(100);
            builder.Property(x => x.PasswordHash).HasMaxLength(500);
            builder.Property(x => x.Role).IsRequired().HasMaxLength(30);
            builder.Property(x => x.InviteToken).HasMaxLength(200);

            builder.HasIndex(x => x.InviteToken).IsUnique();
            builder.HasIndex(x => new { x.ClientId, x.Username }).IsUnique();
        }
    }

    public class ApiClientConfiguration : IEntityTypeConfiguration<ApiClient>
    {
        public void Configure(EntityTypeBuilder<ApiClient> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
            builder.Property(x => x.ApiKey).IsRequired().HasMaxLength(100);
            builder.Property(x => x.ApiSecret).IsRequired().HasMaxLength(256);
            builder.Property(x => x.AllowedIps).HasMaxLength(1000);
            builder.HasIndex(x => x.ApiKey).IsUnique();
        }
    }

    public class BioLogConfiguration : IEntityTypeConfiguration<BioLog>
    {
        public void Configure(EntityTypeBuilder<BioLog> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Level).IsRequired().HasMaxLength(20);
            builder.Property(x => x.Source).HasMaxLength(200);
            builder.Property(x => x.DeviceSN).HasMaxLength(100);
            builder.HasIndex(x => x.CreatedOn);
        }
    }

    public class BioErrorLogConfiguration : IEntityTypeConfiguration<BioErrorLog>
    {
        public void Configure(EntityTypeBuilder<BioErrorLog> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.DeviceSN).HasMaxLength(100);
            builder.Property(x => x.Url).HasMaxLength(500);
            builder.Property(x => x.Method).HasMaxLength(10);
            builder.HasIndex(x => x.CreatedOn);
        }
    }

    public class BioSyncHistoryConfiguration : IEntityTypeConfiguration<BioSyncHistory>
    {
        public void Configure(EntityTypeBuilder<BioSyncHistory> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.DeviceSN).HasMaxLength(100);
            builder.Property(x => x.SyncType).IsRequired().HasMaxLength(50);
            builder.Property(x => x.ErrorMessage).HasMaxLength(2000);
            builder.HasIndex(x => x.StartedOn);
        }
    }

    public class BioTransactionConfiguration : IEntityTypeConfiguration<BioTransaction>
    {
        public void Configure(EntityTypeBuilder<BioTransaction> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.DeviceSN).IsRequired().HasMaxLength(100);
            builder.Property(x => x.UserCode).IsRequired().HasMaxLength(50);
            builder.Property(x => x.RawData).HasMaxLength(2000);

            builder.HasIndex(x => new { x.DeviceSN, x.UserCode, x.TransactionTime });
            builder.HasIndex(x => x.ClientId);
            builder.HasIndex(x => x.TransactionTime);
        }
    }
}
