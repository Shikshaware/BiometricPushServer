using System;
using System.Linq;
using System.Threading.Tasks;
using BiometricPushServer.Repository.Interfaces;
using BiometricPushServer.Service.Interfaces;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace BiometricPushServer.Web.Jobs
{
    public class BiometricBackgroundJobs
    {
        private readonly IDeviceService _deviceService;
        private readonly ICommandService _commandService;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<BiometricBackgroundJobs> _logger;

        public BiometricBackgroundJobs(
            IDeviceService deviceService,
            ICommandService commandService,
            IUnitOfWork uow,
            ILogger<BiometricBackgroundJobs> logger)
        {
            _deviceService = deviceService;
            _commandService = commandService;
            _uow = uow;
            _logger = logger;
        }

        /// <summary>
        /// Detect devices that stopped sending heartbeats and mark them as offline.
        /// Runs every minute via Hangfire.
        /// </summary>
        [AutomaticRetry(Attempts = 0)]
        public async Task DetectOfflineDevicesAsync()
        {
            _logger.LogDebug("Running offline device detection...");
            await _deviceService.MarkOfflineDevicesAsync();
        }

        /// <summary>
        /// Mark commands that have exceeded their retry limit as permanently failed.
        /// </summary>
        [AutomaticRetry(Attempts = 0)]
        public async Task ExpireStaleCommandsAsync()
        {
            _logger.LogDebug("Expiring stale commands...");
            var pending = await _commandService.GetAllPendingAsync();
            foreach (var cmd in pending)
            {
                if (cmd.RetryCount >= 3)
                    await _commandService.MarkFailedAsync(cmd.Id, "Max retry count exceeded");
            }
        }

        /// <summary>
        /// Purge heartbeat records older than 7 days to keep the table compact.
        /// </summary>
        [AutomaticRetry(Attempts = 0)]
        public async Task CleanupOldHeartbeatsAsync()
        {
            _logger.LogDebug("Cleaning up old heartbeat records...");
            var cutoff = DateTime.UtcNow.AddDays(-7);
            var old = await _uow.Heartbeats.FindAsync(h => h.PingTime < cutoff);
            foreach (var h in old)
                _uow.Heartbeats.Remove(h);

            var removed = old.Count();
            if (removed > 0)
            {
                await _uow.SaveChangesAsync();
                _logger.LogInformation("Removed {Count} old heartbeat records", removed);
            }
        }

        /// <summary>
        /// Schedule all recurring jobs. Call once on app startup.
        /// </summary>
        public static void ScheduleAll()
        {
            RecurringJob.AddOrUpdate<BiometricBackgroundJobs>(
                "detect-offline-devices",
                j => j.DetectOfflineDevicesAsync(),
                Cron.Minutely);

            RecurringJob.AddOrUpdate<BiometricBackgroundJobs>(
                "expire-stale-commands",
                j => j.ExpireStaleCommandsAsync(),
                "*/5 * * * *");  // every 5 minutes

            RecurringJob.AddOrUpdate<BiometricBackgroundJobs>(
                "cleanup-heartbeats",
                j => j.CleanupOldHeartbeatsAsync(),
                Cron.Daily(3));  // 3 AM daily
        }
    }
}
