using Microsoft.Extensions.Logging;
using ProcessZero.Application.Interfaces;
using System;
using System.Threading.Tasks;

namespace ProcessZero.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Hangfire background job handler for processing scheduled messages
    /// </summary>
    public class ScheduledMessagesBackgroundJob
    {
        private readonly ISchedulerService _schedulerService;
        private readonly ILogger<ScheduledMessagesBackgroundJob> _logger;

        public ScheduledMessagesBackgroundJob(
            ISchedulerService schedulerService,
            ILogger<ScheduledMessagesBackgroundJob> logger)
        {
            _schedulerService = schedulerService ?? throw new ArgumentNullException(nameof(schedulerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Processes all pending scheduled messages that are due for sending
        /// This method is called periodically by Hangfire
        /// </summary>
        public async Task ProcessScheduledMessagesAsync()
        {
            try
            {
                _logger.LogInformation("Starting scheduled messages processing job at {Time}", DateTime.UtcNow);

                await _schedulerService.ProcessPendingMessagesAsync();

                _logger.LogInformation("Completed scheduled messages processing job at {Time}", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing scheduled messages at {Time}", DateTime.UtcNow);
                throw; // Hangfire will handle the retry logic
            }
        }
    }
}
