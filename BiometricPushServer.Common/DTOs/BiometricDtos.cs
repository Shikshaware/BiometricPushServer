using System;
using System.Collections.Generic;

namespace BiometricPushServer.Common.DTOs
{
    public class DeviceRegistrationDto
    {
        public string SerialNumber { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public int? Port { get; set; }
        public string FirmwareVersion { get; set; } = string.Empty;
        public string DeviceModel { get; set; } = string.Empty;
        public int? ClientId { get; set; }
    }

    public class DeviceDto
    {
        public int Id { get; set; }
        public string SerialNumber { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string FirmwareVersion { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public bool IsActive { get; set; }
        public bool IsOnline { get; set; }
        public DateTime? LastConnectedOn { get; set; }
        public DateTime? LastHeartbeatOn { get; set; }
    }

    public class AttendancePushDto
    {
        public string DeviceSN { get; set; } = string.Empty;
        public List<AttendanceRecordDto> Records { get; set; } = new List<AttendanceRecordDto>();
    }

    public class AttendanceRecordDto
    {
        public string UserCode { get; set; } = string.Empty;
        public DateTime PunchTime { get; set; }
        public int AttendanceState { get; set; }
        public int VerifyMode { get; set; }
        public string WorkCode { get; set; } = string.Empty;
    }

    public class AttendanceLogDto
    {
        public long Id { get; set; }
        public string DeviceSN { get; set; } = string.Empty;
        public string UserCode { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime PunchTime { get; set; }
        public int AttendanceState { get; set; }
        public int VerifyMode { get; set; }
        public bool IsDuplicate { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    public class UserDto
    {
        public int Id { get; set; }
        public int? ClientId { get; set; }
        public string UserCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CardNumber { get; set; } = string.Empty;
        public int Privilege { get; set; }
        public bool IsEnabled { get; set; }
        public int? DepartmentId { get; set; }
    }

    public class CommandDto
    {
        public int Id { get; set; }
        public string DeviceSN { get; set; } = string.Empty;
        public string CommandType { get; set; } = string.Empty;
        public string CommandText { get; set; } = string.Empty;
        public bool IsSent { get; set; }
        public bool IsExecuted { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    public class DashboardStatsDto
    {
        public int TotalDevices { get; set; }
        public int OnlineDevices { get; set; }
        public int OfflineDevices { get; set; }
        public int TodayAttendance { get; set; }
        public int TotalUsers { get; set; }
        public int PendingCommands { get; set; }
        public List<AttendanceLogDto> RecentPunches { get; set; } = new List<AttendanceLogDto>();
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public int StatusCode { get; set; }

        public static ApiResponse<T> Ok(T? data, string message = "Success") =>
            new ApiResponse<T> { Success = true, Data = data, Message = message, StatusCode = 200 };

        public static ApiResponse<T> OkMessage(string message) =>
            new ApiResponse<T> { Success = true, Message = message, StatusCode = 200 };

        public static ApiResponse<T> Fail(string message, int code = 400) =>
            new ApiResponse<T> { Success = false, Message = message, StatusCode = code };
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
