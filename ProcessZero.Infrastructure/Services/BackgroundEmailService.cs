using Hangfire;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Infrastructure.Services
{
    /// <summary>
    /// Enqueues emails to be sent by Hangfire background worker.
    ///
    /// Important: do not depend on a scoped worker instance from here when this
    /// service is registered as a singleton. Instead use Hangfire's generic
    /// Enqueue overload so the worker is resolved from the DI container when
    /// the job runs.
    /// </summary>
    public class BackgroundEmailService : IBackgroundEmailService
    {
        public BackgroundEmailService()
        {
        }

        public void EnqueueEmail(EmailDto emailDto)
        {
            // Use the generic overload so Hangfire will resolve IBackgroundEmailWorker
            // from the service provider when the job executes. This avoids capturing
            // a scoped service in a singleton.
            BackgroundJob.Enqueue<IBackgroundEmailWorker>(worker => worker.SendAsync(emailDto));
        }
    }
}
