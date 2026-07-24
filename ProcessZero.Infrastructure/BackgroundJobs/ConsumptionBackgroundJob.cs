using Microsoft.Extensions.Logging;
using ProcessZero.Application.Interfaces;
using System;
using System.Threading.Tasks;

namespace ProcessZero.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Hangfire background job that periodically processes all active usage sessions
    /// and consumes credits based on actual elapsed time.
    ///
    /// This replaces the client-side countdown timer. The backend now owns the
    /// timer/countdown logic so that credit consumption continues even when the
    /// browser is closed or the user logs out.
    /// </summary>
    public class ConsumptionBackgroundJob
    {
        private readonly IConsumptionService _consumptionService;
        private readonly ILogger<ConsumptionBackgroundJob> _logger;

        public ConsumptionBackgroundJob(
            IConsumptionService consumptionService,
            ILogger<ConsumptionBackgroundJob> logger)
        {
            _consumptionService = consumptionService ?? throw new ArgumentNullException(nameof(consumptionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Processes all active sessions, consuming credits for sessions that
        /// have elapsed time beyond their last processed point.
        /// Called periodically by Hangfire (interval configurable via ConsumptionConfig.CheckIntervalMinutes).
        /// </summary>
        public async Task ProcessActiveSessionsAsync()
        {
            try
            {
                _logger.LogInformation("Starting active session consumption processing at {Time}", DateTime.UtcNow);

                var processed = await _consumptionService.ProcessActiveSessionsAsync();

                _logger.LogInformation("Completed active session consumption processing at {Time}. Sessions processed: {Count}",
                    DateTime.UtcNow, processed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing active sessions at {Time}", DateTime.UtcNow);
                throw; // Hangfire will handle the retry logic
            }
        }
    }
}
