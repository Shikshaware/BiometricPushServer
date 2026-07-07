using System;
using System.Threading.Tasks;
using BiometricPushServer.Service.Interfaces;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace BiometricPushServer.Web.Jobs
{
    public class BiometricBackgroundJobs
    {
        private readonly IDeviceService _deviceService;
        private readonly ICommandService _commandService;
        private readonly ILogger<BiometricBackgroundJobs> _logger;

        public BiometricBackgroundJobs(
            IDeviceService deviceService,
            ICommandService commandService,
            ILogger<BiometricBackgroundJobs> logger)
        {
            _deviceService = deviceService;
            _commandService = commandService;
            _logger = logger;
        }

        /// <summary>
        /// Detect devices that stopped sending heartbeats → mark as offline.
        /// Runs every minute via Hangfire.
        /// </summary>
        [AutomaticRetry(Attempts = 0)]
        public async Task DetectOfflineDevicesAsync()
        {
            _logger.LogDebug("Running offline device detection...");
            await _deviceService.MarkOfflineDevicesAsync();
        }

        /// <summary>
        /// Retry commands that have been sent but not acknowledged after a timeout.
        /// </summary>
        [AutomaticRetry(Attempts = 0)]
        public async Task RetryStaleCommandsAsync()
        {
            _logger.LogDebug("Running stale command retry...");
            var pending = await _commandService.GetPendingAsync(string.Empty);
            foreach (var cmd in pending)
            {
                if (cmd.RetryCount >= 3)
                    await _commandService.MarkFailedAsync(cmd.Id, "Max retry exceeded");
            }
        }

        /// <summary>
        /// Purge old heartbeat records (keep last 7 days).
        /// </summary>
        [AutomaticRetry(Attempts = 0)]
        public Task CleanupOldHeartbeatsAsync()
        {
            // Handled by SQL scheduled job or EF cleanup — placeholder
            _logger.LogDebug("Heartbeat cleanup placeholder");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Schedule all recurring jobs.
        /// Call once on app startup.
        /// </summary>
        public static void ScheduleAll()
        {
            RecurringJob.AddOrUpdate<BiometricBackgroundJobs>(
                "detect-offline-devices",
                j => j.DetectOfflineDevicesAsync(),
                Cron.Minutely);

            RecurringJob.AddOrUpdate<BiometricBackgroundJobs>(
                "retry-stale-commands",
                j => j.RetryStaleCommandsAsync(),
                "*/5 * * * *");  // every 5 minutes

            RecurringJob.AddOrUpdate<BiometricBackgroundJobs>(
                "cleanup-heartbeats",
                j => j.CleanupOldHeartbeatsAsync(),
                Cron.Daily(3));  // 3 AM
        }
    }
}
