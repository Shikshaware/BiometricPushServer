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
    public class AttendanceService : IAttendanceService
    {
        private readonly IUnitOfWork _uow;

        public AttendanceService(IUnitOfWork uow) => _uow = uow;

        public async Task<(int saved, int duplicates)> ProcessPushAsync(
            string deviceSN,
            IEnumerable<AttendanceRecordDto> records,
            int? clientId)
        {
            var device = await _uow.Devices.GetBySerialNumberAsync(deviceSN);
            if (device == null || !device.IsApproved)
                return (0, 0);

            var effectiveClientId = clientId ?? device.ClientId;

            // Pre-fetch users so we can populate UserName without an extra query per record
            var recordList = records.ToList();
            var userCodes = recordList.Select(r => r.UserCode).Distinct().ToList();
            var users = userCodes.Count > 0
                ? await _uow.Users.FindAsync(u =>
                    userCodes.Contains(u.UserCode) &&
                    (effectiveClientId == null || u.ClientId == effectiveClientId))
                : Enumerable.Empty<BioUser>();
            var userNameMap = users.ToDictionary(u => u.UserCode, u => u.Name, StringComparer.OrdinalIgnoreCase);

            int saved = 0, duplicates = 0;

            foreach (var rec in recordList)
            {
                bool isDuplicate = await _uow.Attendance.IsDuplicateAsync(
                    deviceSN, rec.UserCode, rec.PunchTime,
                    AppConstants.DuplicateWindowSeconds);

                userNameMap.TryGetValue(rec.UserCode, out var userName);

                var log = new BioAttendanceLog
                {
                    ClientId = effectiveClientId,
                    DeviceId = device.Id,
                    DeviceSN = deviceSN,
                    UserCode = rec.UserCode,
                    UserName = userName ?? string.Empty,
                    PunchTime = rec.PunchTime,
                    AttendanceState = rec.AttendanceState,
                    VerifyMode = rec.VerifyMode,
                    WorkCode = rec.WorkCode ?? string.Empty,
                    IsDuplicate = isDuplicate,
                    CreatedOn = DateTime.UtcNow
                };

                try
                {
                    await _uow.Attendance.AddAsync(log);
                    await _uow.SaveChangesAsync();

                    if (isDuplicate) duplicates++; else saved++;
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateException)
                {
                    // Unique constraint violation (exact duplicate punch) — treat as duplicate
                    duplicates++;
                }
            }

            return (saved, duplicates);
        }

        public async Task<PagedResult<AttendanceLogDto>> GetAttendanceAsync(
            int? clientId, int pageNumber, int pageSize,
            DateTime? from = null, DateTime? to = null, int? locationId = null)
        {
            var query = _uow.Attendance.Query();
            if (clientId.HasValue) query = query.Where(a => a.ClientId == clientId);
            if (from.HasValue) query = query.Where(a => a.PunchTime >= from.Value);
            if (to.HasValue) query = query.Where(a => a.PunchTime <= to.Value);
            if (locationId.HasValue)
            {
                var locationDeviceIds = _uow.Devices.Query()
                    .Where(d => d.LocationId == locationId &&
                                (!clientId.HasValue || d.ClientId == clientId))
                    .Select(d => d.Id);
                query = query.Where(a => a.DeviceId.HasValue && locationDeviceIds.Contains(a.DeviceId.Value));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(a => a.PunchTime)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(a => MapToDto(a))
                .ToListAsync();

            return new PagedResult<AttendanceLogDto>
            {
                Items = items,
                TotalCount = total,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<IEnumerable<AttendanceLogDto>> GetTodayAsync(int? clientId = null, int? locationId = null)
        {
            var logs = await _uow.Attendance.GetTodayLogsAsync(clientId);
            if (locationId.HasValue)
            {
                var locationDeviceIds = (await _uow.Devices.FindAsync(d =>
                        d.LocationId == locationId &&
                        (!clientId.HasValue || d.ClientId == clientId)))
                    .Select(d => d.Id)
                    .ToHashSet();
                logs = logs.Where(a => a.DeviceId.HasValue && locationDeviceIds.Contains(a.DeviceId.Value));
            }

            return logs.Select(MapToDto);
        }

        public async Task<IEnumerable<AttendanceLogDto>> GetByDeviceAsync(
            string deviceSN, DateTime from, DateTime to)
        {
            var logs = await _uow.Attendance.GetByDeviceAsync(deviceSN, from, to);
            return logs.Select(MapToDto);
        }

        private static AttendanceLogDto MapToDto(BioAttendanceLog a) => new AttendanceLogDto
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
        };

        public async Task<IEnumerable<AttendanceLogDto>> GetByUserAsync(
            string userCode, DateTime from, DateTime to, int? clientId = null)
        {
            var logs = await _uow.Attendance.GetByUserAsync(userCode, from, to, clientId);
            return logs.Select(MapToDto);
        }
    }
}
