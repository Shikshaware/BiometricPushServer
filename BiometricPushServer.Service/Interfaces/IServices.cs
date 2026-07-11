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
        Task UpdateConnectionAsync(string sn, string ipAddress);
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
        Task<AttendanceReportDto> GetClientAttendanceReportAsync(int clientId, AttendanceReportPeriod period, System.DateTime? referenceDate = null, int? locationId = null);
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

    public interface IShiftService
    {
        Task<IEnumerable<ShiftDto>> GetAllAsync(int? clientId = null);
        Task<ShiftDto?> GetByIdAsync(int id);
        Task<ShiftDto> CreateAsync(ShiftDto dto);
        Task<ShiftDto?> UpdateAsync(int id, ShiftDto dto);
        Task<bool> DeleteAsync(int id);
    }

    public interface IHolidayService
    {
        Task<IEnumerable<HolidayDto>> GetAllAsync(int? clientId = null, int? year = null);
        Task<HolidayDto?> GetByIdAsync(int id);
        Task<HolidayDto> CreateAsync(HolidayDto dto);
        Task<HolidayDto?> UpdateAsync(int id, HolidayDto dto);
        Task<bool> DeleteAsync(int id);
    }

    public interface IScheduleService
    {
        Task<IEnumerable<EmployeeScheduleDto>> GetAllAsync(int? clientId = null, int? userId = null);
        Task<EmployeeScheduleDto?> GetByIdAsync(int id);
        Task<EmployeeScheduleDto> CreateAsync(EmployeeScheduleDto dto);
        Task<EmployeeScheduleDto?> UpdateAsync(int id, EmployeeScheduleDto dto);
        Task<bool> DeleteAsync(int id);
        Task<EmployeeScheduleDto?> GetActiveForUserAsync(int userId, System.DateTime? date = null);
    }
}
