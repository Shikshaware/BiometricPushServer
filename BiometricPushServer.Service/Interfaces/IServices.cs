using System.Collections.Generic;
using System.Threading.Tasks;
using BiometricPushServer.Common.DTOs;
using BiometricPushServer.Domain;

namespace BiometricPushServer.Service.Interfaces
{
    public interface IDeviceService
    {
        Task<BioDevice?> GetBySerialNumberAsync(string sn);
        Task<BioDevice?> RegisterOrUpdateAsync(DeviceRegistrationDto dto, string ipAddress);
        Task<IEnumerable<DeviceDto>> GetAllDevicesAsync(int? clientId = null);
        Task<bool> ApproveDeviceAsync(int deviceId);
        Task<bool> SetLockedAsync(int deviceId, bool locked);
        Task UpdateHeartbeatAsync(string sn, string ipAddress, string rawQuery);
        Task MarkOfflineDevicesAsync();
        Task<DeviceDto?> GetDeviceDtoAsync(int deviceId);
    }

    public interface IAttendanceService
    {
        Task<(int saved, int duplicates)> ProcessPushAsync(string deviceSN, IEnumerable<AttendanceRecordDto> records, int? clientId);
        Task<PagedResult<AttendanceLogDto>> GetAttendanceAsync(int? clientId, int pageNumber, int pageSize);
        Task<IEnumerable<AttendanceLogDto>> GetTodayAsync(int? clientId = null);
        Task<IEnumerable<AttendanceLogDto>> GetByDeviceAsync(string deviceSN, System.DateTime from, System.DateTime to);
    }

    public interface ICommandService
    {
        Task<BioDeviceCommand> EnqueueAsync(string deviceSN, string commandType, string? parameters = null, int? clientId = null);
        Task<IEnumerable<BioDeviceCommand>> GetPendingAsync(string deviceSN);
        Task MarkSentAsync(int commandId);
        Task MarkExecutedAsync(int commandId, string response);
        Task MarkFailedAsync(int commandId, string reason);
    }

    public interface IUserService
    {
        Task<BioUser?> GetByCodeAsync(string userCode, int? clientId = null);
        Task<BioUser> UpsertAsync(UserDto dto);
        Task<bool> DeleteAsync(string userCode, int? clientId = null);
        Task<PagedResult<UserDto>> GetAllAsync(int? clientId, int pageNumber, int pageSize);
    }

    public interface IDashboardService
    {
        Task<DashboardStatsDto> GetStatsAsync(int? clientId = null);
    }
}
