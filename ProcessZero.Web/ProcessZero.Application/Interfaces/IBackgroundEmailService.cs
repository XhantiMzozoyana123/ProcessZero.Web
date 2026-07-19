using ProcessZero.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Application.Interfaces
{
    /// <summary>
    /// Enqueues email delivery as a Hangfire background job so the calling
    /// API request is not blocked by SMTP round-trips.
    /// </summary>
    public interface IBackgroundEmailService
    {
        /// <summary>
        /// Enqueue a single email for background delivery via Hangfire.
        /// Returns immediately without waiting for the email to be sent.
        /// </summary>
        void EnqueueEmail(EmailDto emailDto);
    }
}
