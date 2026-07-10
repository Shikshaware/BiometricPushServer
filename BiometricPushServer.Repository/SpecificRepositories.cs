using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BiometricPushServer.Common.Constants;
using BiometricPushServer.Data;
using BiometricPushServer.Domain;
using BiometricPushServer.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BiometricPushServer.Repository
{
    public class DeviceRepository : GenericRepository<BioDevice>, IDeviceRepository
    {
        public DeviceRepository(BiometricDbContext context) : base(context) { }

        public async Task<BioDevice?> GetBySerialNumberAsync(string serialNumber) =>
            await _dbSet.FirstOrDefaultAsync(d => d.SerialNumber == serialNumber);

        public async Task<IEnumerable<BioDevice>> GetActiveDevicesAsync(int? clientId = null)
        {
            var query = _dbSet.Where(d => d.IsActive && d.IsApproved);
            if (clientId.HasValue) query = query.Where(d => d.ClientId == clientId);
            return await query.OrderBy(d => d.DeviceName).ToListAsync();
        }

        public async Task<IEnumerable<BioDevice>> GetOnlineDevicesAsync(int? clientId = null)
        {
            var threshold = DateTime.UtcNow.AddMinutes(-AppConstants.OfflineThresholdMinutes);
            var query = _dbSet.Where(d => d.IsActive && d.LastHeartbeatOn >= threshold);
            if (clientId.HasValue) query = query.Where(d => d.ClientId == clientId);
            return await query.ToListAsync();
        }
    }

    public class AttendanceRepository : GenericRepository<BioAttendanceLog>, IAttendanceRepository
    {
        public AttendanceRepository(BiometricDbContext context) : base(context) { }

        public async Task<IEnumerable<BioAttendanceLog>> GetTodayLogsAsync(int? clientId = null)
        {
            var today = DateTime.UtcNow.Date;
            var query = _dbSet.Where(a => a.PunchTime >= today);
            if (clientId.HasValue) query = query.Where(a => a.ClientId == clientId);
            return await query.OrderByDescending(a => a.PunchTime).ToListAsync();
        }

        public async Task<IEnumerable<BioAttendanceLog>> GetByDeviceAsync(string deviceSN, DateTime from, DateTime to) =>
            await _dbSet
                .Where(a => a.DeviceSN == deviceSN && a.PunchTime >= from && a.PunchTime <= to)
                .OrderBy(a => a.PunchTime)
                .ToListAsync();

        public async Task<IEnumerable<BioAttendanceLog>> GetByUserAsync(
            string userCode, DateTime from, DateTime to, int? clientId = null)
        {
            var query = _dbSet
                .Where(a => a.UserCode == userCode && a.PunchTime >= from && a.PunchTime <= to);
            if (clientId.HasValue)
                query = query.Where(a => a.ClientId == clientId);
            return await query.OrderBy(a => a.PunchTime).ToListAsync();
        }

        public async Task<bool> IsDuplicateAsync(string deviceSN, string userCode, DateTime punchTime, int windowSeconds)
        {
            var windowStart = punchTime.AddSeconds(-windowSeconds);
            var windowEnd = punchTime.AddSeconds(windowSeconds);
            return await _dbSet.AnyAsync(a =>
                a.DeviceSN == deviceSN &&
                a.UserCode == userCode &&
                a.PunchTime >= windowStart &&
                a.PunchTime <= windowEnd);
        }
    }

    public class CommandRepository : GenericRepository<BioDeviceCommand>, ICommandRepository
    {
        public CommandRepository(BiometricDbContext context) : base(context) { }

        public async Task<IEnumerable<BioDeviceCommand>> GetPendingCommandsAsync(string deviceSN) =>
            await _dbSet
                .Where(c => c.DeviceSN == deviceSN && !c.IsSent && !c.IsFailed &&
                            (c.ExpiresOn == null || c.ExpiresOn > DateTime.UtcNow))
                .OrderBy(c => c.CreatedOn)
                .ToListAsync();

        public async Task<IEnumerable<BioDeviceCommand>> GetAllPendingAsync() =>
            await _dbSet
                .Where(c => !c.IsSent && !c.IsFailed &&
                            (c.ExpiresOn == null || c.ExpiresOn > DateTime.UtcNow))
                .OrderBy(c => c.CreatedOn)
                .ToListAsync();

        /// <summary>
        /// Returns commands that were sent to the device but never acknowledged (executed or failed),
        /// and whose expiry time has passed. These are stuck commands that should be marked failed.
        /// </summary>
        public async Task<IEnumerable<BioDeviceCommand>> GetSentExpiredAsync() =>
            await _dbSet
                .Where(c => c.IsSent && !c.IsExecuted && !c.IsFailed &&
                            c.ExpiresOn != null && c.ExpiresOn <= DateTime.UtcNow)
                .OrderBy(c => c.CreatedOn)
                .ToListAsync();
    }
}
