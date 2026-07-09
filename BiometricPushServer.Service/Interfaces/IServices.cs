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
        Task<IEnumerable<DeviceDto>> GetAllDevicesAsync(int? clientId = null, int? locationId = null);
        Task<bool> ApproveDeviceAsync(int deviceId);
        Task<bool> SetLockedAsync(int deviceId, bool locked);
        Task UpdateHeartbeatAsync(string sn, string ipAddress, string rawQuery);
        Task MarkOfflineDevicesAsync();
        Task<DeviceDto?> GetDeviceDtoAsync(int deviceId);
        Task<DeviceDto?> UpdateDeviceAsync(int deviceId, DeviceUpdateDto dto);
        Task<int> BulkAssignLocationAsync(IEnumerable<int> deviceIds, int locationId, int? clientId = null);
    }

    public interface IAttendanceService
    {
        Task<(int saved, int duplicates)> ProcessPushAsync(string deviceSN, IEnumerable<AttendanceRecordDto> records, int? clientId);
        Task<PagedResult<AttendanceLogDto>> GetAttendanceAsync(int? clientId, int pageNumber, int pageSize, System.DateTime? from = null, System.DateTime? to = null, int? locationId = null);
        Task<IEnumerable<AttendanceLogDto>> GetTodayAsync(int? clientId = null, int? locationId = null);
        Task<IEnumerable<AttendanceLogDto>> GetByDeviceAsync(string deviceSN, System.DateTime from, System.DateTime to);
        Task<IEnumerable<AttendanceLogDto>> GetByUserAsync(string userCode, System.DateTime from, System.DateTime to, int? clientId = null);
    }

    public interface ICommandService
    {
        Task<BioDeviceCommand> EnqueueAsync(string deviceSN, string commandType, string? parameters = null, int? clientId = null, string? commandText = null);
        Task<IEnumerable<BioDeviceCommand>> GetPendingAsync(string deviceSN);
        Task<IEnumerable<BioDeviceCommand>> GetAllPendingAsync();
        Task<IEnumerable<BioDeviceCommand>> GetSentExpiredAsync();
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
        Task<PagedResult<UserDto>> GetByDeviceAsync(int deviceId, int pageNumber, int pageSize);
        Task AttachUserToDeviceAsync(int deviceId, int userId);
        Task<bool> DetachUserFromDeviceAsync(int deviceId, int userId);
        Task<bool> UserHasAnyDeviceMappingAsync(int userId);
    }

    public interface IDashboardService
    {
        Task<DashboardStatsDto> GetStatsAsync(int? clientId = null, int? locationId = null);
    }

    public interface IDepartmentService
    {
        Task<IEnumerable<DepartmentDto>> GetAllAsync(int? clientId = null);
        Task<DepartmentDto?> GetByIdAsync(int id);
        Task<DepartmentDto> CreateAsync(DepartmentDto dto);
        Task<DepartmentDto?> UpdateAsync(int id, DepartmentDto dto);
        Task<bool> DeleteAsync(int id);
    }

    public interface ILocationService
    {
        Task<IEnumerable<LocationDto>> GetAllAsync(int? clientId = null);
        Task<LocationDto?> GetByIdAsync(int id);
        Task<LocationDto> CreateAsync(LocationDto dto);
        Task<LocationDto?> UpdateAsync(int id, LocationDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
