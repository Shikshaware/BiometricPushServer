using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BiometricPushServer.Common.Constants;
using BiometricPushServer.Common.DTOs;
using BiometricPushServer.Domain;
using BiometricPushServer.Repository.Interfaces;
using BiometricPushServer.Service.Interfaces;

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

            int saved = 0, duplicates = 0;

            foreach (var rec in records)
            {
                bool isDuplicate = await _uow.Attendance.IsDuplicateAsync(
                    deviceSN, rec.UserCode, rec.PunchTime,
                    AppConstants.DuplicateWindowSeconds);

                var log = new BioAttendanceLog
                {
                    ClientId = clientId ?? device.ClientId,
                    DeviceId = device.Id,
                    DeviceSN = deviceSN,
                    UserCode = rec.UserCode,
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
                catch (Exception)
                {
                    // EF unique constraint violation (exact duplicate) — skip
                    duplicates++;
                }
            }

            return (saved, duplicates);
        }

        public async Task<PagedResult<AttendanceLogDto>> GetAttendanceAsync(
            int? clientId, int pageNumber, int pageSize)
        {
            var query = _uow.Attendance.Query();
            if (clientId.HasValue) query = query.Where(a => a.ClientId == clientId);

            var total = query.Count();
            var items = query
                .OrderByDescending(a => a.PunchTime)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(a => MapToDto(a))
                .ToList();

            return new PagedResult<AttendanceLogDto>
            {
                Items = items,
                TotalCount = total,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<IEnumerable<AttendanceLogDto>> GetTodayAsync(int? clientId = null)
        {
            var logs = await _uow.Attendance.GetTodayLogsAsync(clientId);
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
    }
}
