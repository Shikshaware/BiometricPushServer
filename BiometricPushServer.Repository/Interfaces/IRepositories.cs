using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BiometricPushServer.Domain;

namespace BiometricPushServer.Repository.Interfaces
{
    public interface IDeviceRepository : IGenericRepository<BioDevice>
    {
        Task<BioDevice?> GetBySerialNumberAsync(string serialNumber);
        Task<IEnumerable<BioDevice>> GetActiveDevicesAsync(int? clientId = null);
        Task<IEnumerable<BioDevice>> GetOnlineDevicesAsync(int? clientId = null);
    }

    public interface IAttendanceRepository : IGenericRepository<BioAttendanceLog>
    {
        Task<IEnumerable<BioAttendanceLog>> GetTodayLogsAsync(int? clientId = null);
        Task<IEnumerable<BioAttendanceLog>> GetByDeviceAsync(string deviceSN, DateTime from, DateTime to);
        Task<IEnumerable<BioAttendanceLog>> GetByUserAsync(string userCode, DateTime from, DateTime to);
        Task<bool> IsDuplicateAsync(string deviceSN, string userCode, DateTime punchTime, int windowSeconds);
    }

    public interface ICommandRepository : IGenericRepository<BioDeviceCommand>
    {
        Task<IEnumerable<BioDeviceCommand>> GetPendingCommandsAsync(string deviceSN);
        Task<IEnumerable<BioDeviceCommand>> GetAllPendingAsync();
    }

    public interface IUnitOfWork : IDisposable
    {
        IDeviceRepository Devices { get; }
        IAttendanceRepository Attendance { get; }
        ICommandRepository Commands { get; }
        IGenericRepository<BioUser> Users { get; }
        IGenericRepository<BioFingerprint> Fingerprints { get; }
        IGenericRepository<BioFaceTemplate> FaceTemplates { get; }
        IGenericRepository<BioPalmTemplate> PalmTemplates { get; }
        IGenericRepository<BioHeartbeat> Heartbeats { get; }
        IGenericRepository<BioDeviceStatus> DeviceStatuses { get; }
        IGenericRepository<BioLog> Logs { get; }
        IGenericRepository<BioErrorLog> ErrorLogs { get; }
        IGenericRepository<BioSyncHistory> SyncHistories { get; }
        IGenericRepository<ApiClient> ApiClients { get; }
        IGenericRepository<BioCompany> Companies { get; }
        IGenericRepository<BioLocation> Locations { get; }
        IGenericRepository<BioDepartment> Departments { get; }
        IGenericRepository<BioShift> Shifts { get; }
        IGenericRepository<BioHoliday> Holidays { get; }
        IGenericRepository<BioEmployeeSchedule> EmployeeSchedules { get; }
        Task<int> SaveChangesAsync();
    }
}
