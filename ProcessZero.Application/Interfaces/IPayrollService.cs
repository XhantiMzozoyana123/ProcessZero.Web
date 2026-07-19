using ProcessZero.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Application.Interfaces
{
    public interface IPayrollService
    {
        /// <summary>
        /// Generate monthly commissions report: create payouts and prepare rows and notifications.
        /// The controller handles actual file creation, download URL creation and notification sending.
        /// </summary>
        Task<PayrollReportResult> GenerateMonthlyCommissionsReportAsync();
    }
}
