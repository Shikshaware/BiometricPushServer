using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BiometricPushServer.Common.Constants;
using BiometricPushServer.Common.DTOs;
using BiometricPushServer.Domain;
using BiometricPushServer.Repository.Interfaces;
using BiometricPushServer.Service.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BiometricPushServer.Service
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _uow;

        public UserService(IUnitOfWork uow) => _uow = uow;

        public async Task<BioUser?> GetByCodeAsync(string userCode, int? clientId = null) =>
            await _uow.Users.FirstOrDefaultAsync(u =>
                u.UserCode == userCode && (clientId == null || u.ClientId == clientId));

        public async Task<BioUser> UpsertAsync(UserDto dto)
        {
            var user = await _uow.Users.FirstOrDefaultAsync(u =>
                u.UserCode == dto.UserCode && u.ClientId == dto.ClientId);

            if (user == null)
            {
                user = new BioUser
                {
                    ClientId = dto.ClientId,
                    UserCode = dto.UserCode,
                    Name = dto.Name,
                    CardNumber = dto.CardNumber,
                    Privilege = dto.Privilege,
                    IsEnabled = dto.IsEnabled,
                    DepartmentId = dto.DepartmentId,
                    CreatedOn = DateTime.UtcNow
                };
                await _uow.Users.AddAsync(user);
            }
            else
            {
                user.Name = dto.Name;
                user.CardNumber = dto.CardNumber;
                user.Privilege = dto.Privilege;
                user.IsEnabled = dto.IsEnabled;
                user.DepartmentId = dto.DepartmentId;
                user.UpdatedOn = DateTime.UtcNow;
                _uow.Users.Update(user);
            }

            await _uow.SaveChangesAsync();
            return user;
        }

        public async Task<bool> DeleteAsync(string userCode, int? clientId = null)
        {
            var user = await _uow.Users.FirstOrDefaultAsync(u =>
                u.UserCode == userCode && (clientId == null || u.ClientId == clientId));

            if (user == null) return false;
            _uow.Users.Remove(user);
            await _uow.SaveChangesAsync();
            return true;
        }

        public async Task<PagedResult<UserDto>> GetAllAsync(int? clientId, int pageNumber, int pageSize)
        {
            var query = _uow.Users.Query();
            if (clientId.HasValue) query = query.Where(u => u.ClientId == clientId);

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(u => u.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    UserCode = u.UserCode,
                    Name = u.Name,
                    CardNumber = u.CardNumber,
                    Privilege = u.Privilege,
                    IsEnabled = u.IsEnabled,
                    DepartmentId = u.DepartmentId
                })
                .ToListAsync();

            return new PagedResult<UserDto>
            {
                Items = items,
                TotalCount = total,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<UserDto>> GetByDeviceAsync(int deviceId, int pageNumber, int pageSize)
        {
            var query = _uow.DeviceUserMaps.Query()
                .Where(m => m.DeviceId == deviceId)
                .Join(_uow.Users.Query(), m => m.UserId, u => u.Id, (_, u) => u);

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(u => u.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    UserCode = u.UserCode,
                    Name = u.Name,
                    CardNumber = u.CardNumber,
                    Privilege = u.Privilege,
                    IsEnabled = u.IsEnabled,
                    DepartmentId = u.DepartmentId
                })
                .ToListAsync();

            return new PagedResult<UserDto>
            {
                Items = items,
                TotalCount = total,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task AttachUserToDeviceAsync(int deviceId, int userId)
        {
            var alreadyMapped = await _uow.DeviceUserMaps.AnyAsync(
                m => m.DeviceId == deviceId && m.UserId == userId);
            if (alreadyMapped) return;

            await _uow.DeviceUserMaps.AddAsync(new BioDeviceUserMap
            {
                DeviceId = deviceId,
                UserId = userId,
                CreatedOn = DateTime.UtcNow
            });
            await _uow.SaveChangesAsync();
        }

        public async Task<bool> DetachUserFromDeviceAsync(int deviceId, int userId)
        {
            var map = await _uow.DeviceUserMaps.FirstOrDefaultAsync(
                m => m.DeviceId == deviceId && m.UserId == userId);
            if (map == null) return false;

            _uow.DeviceUserMaps.Remove(map);
            await _uow.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UserHasAnyDeviceMappingAsync(int userId) =>
            await _uow.DeviceUserMaps.AnyAsync(m => m.UserId == userId);
    }

    public class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _uow;

        public DashboardService(IUnitOfWork uow) => _uow = uow;

        public async Task<DashboardStatsDto> GetStatsAsync(int? clientId = null, int? locationId = null)
        {
            var onlineThreshold = DateTime.UtcNow.AddMinutes(-AppConstants.OfflineThresholdMinutes);

            var allDevices = await _uow.Devices.FindAsync(d =>
                (clientId == null || d.ClientId == clientId) &&
                (locationId == null || d.LocationId == locationId));

            var todayLogs = await _uow.Attendance.GetTodayLogsAsync(clientId);
            if (locationId.HasValue)
            {
                var locationDeviceIds = allDevices.Select(d => d.Id).ToHashSet();
                todayLogs = todayLogs.Where(a => a.DeviceId.HasValue && locationDeviceIds.Contains(a.DeviceId.Value));
            }
            var pendingCmds = await _uow.Commands.GetAllPendingAsync();

            var recentPunches = todayLogs
                .OrderByDescending(a => a.PunchTime)
                .Take(20)
                .Select(a => new AttendanceLogDto
                {
                    Id = a.Id,
                    DeviceSN = a.DeviceSN,
                    UserCode = a.UserCode,
                    UserName = a.UserName,
                    PunchTime = a.PunchTime,
                    AttendanceState = a.AttendanceState,
                    VerifyMode = a.VerifyMode,
                    IsDuplicate = a.IsDuplicate,
                    CreatedOn = a.CreatedOn
                })
                .ToList();

            var deviceList = allDevices.ToList();
            return new DashboardStatsDto
            {
                TotalDevices = deviceList.Count,
                OnlineDevices = deviceList.Count(d => d.LastHeartbeatOn >= onlineThreshold),
                OfflineDevices = deviceList.Count(d => d.LastHeartbeatOn < onlineThreshold),
                TodayAttendance = todayLogs.Count(),
                TotalUsers = await _uow.Users.CountAsync(u => clientId == null || u.ClientId == clientId),
                PendingCommands = pendingCmds.Count(),
                RecentPunches = recentPunches
            };
        }
    }
}
