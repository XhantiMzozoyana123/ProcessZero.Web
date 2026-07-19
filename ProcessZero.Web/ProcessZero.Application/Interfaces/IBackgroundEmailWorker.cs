using ProcessZero.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Application.Interfaces
{
    /// <summary>
    /// Worker invoked by Hangfire to perform actual email delivery.
    /// Implementations should be registered in DI so Hangfire can activate them.
    /// </summary>
    public interface IBackgroundEmailWorker
    {
        Task SendAsync(EmailDto emailDto);
    }
}
