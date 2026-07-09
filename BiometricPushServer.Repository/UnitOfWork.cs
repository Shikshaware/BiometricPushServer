using System.Threading.Tasks;
using BiometricPushServer.Data;
using BiometricPushServer.Domain;
using BiometricPushServer.Repository.Interfaces;

namespace BiometricPushServer.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly BiometricDbContext _context;
        private bool _disposed;

        public IDeviceRepository Devices { get; }
        public IAttendanceRepository Attendance { get; }
        public ICommandRepository Commands { get; }
        public IGenericRepository<BioUser> Users { get; }
        public IGenericRepository<BioDeviceUserMap> DeviceUserMaps { get; }
        public IGenericRepository<BioFingerprint> Fingerprints { get; }
        public IGenericRepository<BioFaceTemplate> FaceTemplates { get; }
        public IGenericRepository<BioPalmTemplate> PalmTemplates { get; }
        public IGenericRepository<BioHeartbeat> Heartbeats { get; }
        public IGenericRepository<BioDeviceStatus> DeviceStatuses { get; }
        public IGenericRepository<BioLog> Logs { get; }
        public IGenericRepository<BioErrorLog> ErrorLogs { get; }
        public IGenericRepository<BioSyncHistory> SyncHistories { get; }
        public IGenericRepository<ApiClient> ApiClients { get; }
        public IGenericRepository<BioPortalUser> PortalUsers { get; }
        public IGenericRepository<BioCompany> Companies { get; }
        public IGenericRepository<BioLocation> Locations { get; }
        public IGenericRepository<BioDepartment> Departments { get; }
        public IGenericRepository<BioShift> Shifts { get; }
        public IGenericRepository<BioHoliday> Holidays { get; }
        public IGenericRepository<BioEmployeeSchedule> EmployeeSchedules { get; }

        public UnitOfWork(BiometricDbContext context)
        {
            _context = context;
            Devices = new DeviceRepository(context);
            Attendance = new AttendanceRepository(context);
            Commands = new CommandRepository(context);
            Users = new GenericRepository<BioUser>(context);
            DeviceUserMaps = new GenericRepository<BioDeviceUserMap>(context);
            Fingerprints = new GenericRepository<BioFingerprint>(context);
            FaceTemplates = new GenericRepository<BioFaceTemplate>(context);
            PalmTemplates = new GenericRepository<BioPalmTemplate>(context);
            Heartbeats = new GenericRepository<BioHeartbeat>(context);
            DeviceStatuses = new GenericRepository<BioDeviceStatus>(context);
            Logs = new GenericRepository<BioLog>(context);
            ErrorLogs = new GenericRepository<BioErrorLog>(context);
            SyncHistories = new GenericRepository<BioSyncHistory>(context);
            ApiClients = new GenericRepository<ApiClient>(context);
            PortalUsers = new GenericRepository<BioPortalUser>(context);
            Companies = new GenericRepository<BioCompany>(context);
            Locations = new GenericRepository<BioLocation>(context);
            Departments = new GenericRepository<BioDepartment>(context);
            Shifts = new GenericRepository<BioShift>(context);
            Holidays = new GenericRepository<BioHoliday>(context);
            EmployeeSchedules = new GenericRepository<BioEmployeeSchedule>(context);
        }

        public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();

        public void Dispose()
        {
            if (!_disposed)
            {
                _context.Dispose();
                _disposed = true;
            }
        }
    }
}
