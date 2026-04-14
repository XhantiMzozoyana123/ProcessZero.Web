using CsvHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProcessZero.Application.Constants;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using System.Formats.Asn1;
using System.Globalization;
using System.Text;

namespace ProcessZero.Web.Controllers
{
    /*
     * PayrollController
     * -----------------
     * Responsibilities:
     * - Invoke the payroll service to compute monthly payouts.
     * - Create a CSV file in the API wwwroot (folder: /payroll) containing payouts.
     * - Send notification emails to Admins and individual sales reps.
     * - Provide a protected download endpoint for admins to retrieve the CSV.
     *
     * Domain entities referenced by this flow:
     * - Payout (ProcessZero.Domain.Entities.Payout)
     *   - Id (int), UserId (string), BankAccountId (int), Amount (decimal), Month (int), Year (int), Notes (string), IsPaid (bool)
     * - Contact (ProcessZero.Domain.Entities.Contact)
     *   - Id, UserId, FirstName, LastName, Email, Phone, ClosedAmount (decimal), Status (ContactStatus)
     * - BankAccount (ProcessZero.Domain.Entities.BankAccount)
     *   - Id, UserId, AccountNumber, BankName, AccountName
     *
     * DTO used:
     * - PayrollReportResult (ProcessZero.Application.Dtos.PayrollReportResult)
     *   - Rows (List<string[]>) : CSV header + rows (Rows[0] is header)
     *   - AdminEmails (List<string>) : list of admin emails to notify
     *   - UserNotifications (List<PayrollUserNotificationDto>) : per-user payout notifications
     *
     * CSV columns produced by this controller (header):
     * [ "UserId", "UserName", "AccountNumber", "BankName", "Amount" ]
     *
     * Services used:
     * - IPayrollService.GenerateMonthlyCommissionsReportAsync() -> PayrollReportResult
     * - IEmailService.SendEmailAsync(EmailDto)
     */
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "Admin")]
    public class PayrollController : ControllerBase
    {
        private readonly IPayrollService _payrollService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public PayrollController(IPayrollService payrollService, IEmailService emailService, IConfiguration configuration)
        {
            _payrollService = payrollService;
            _emailService = emailService;
            _configuration = configuration;
        }

        // POST: api/payroll/generate-monthly-report
        [HttpPost("generate-monthly-report")]
        public async Task<IActionResult> GenerateMonthlyReport()
        {
            // Get report data from service
            PayrollReportResult report = await _payrollService.GenerateMonthlyCommissionsReportAsync();

            if (report == null || report.Rows == null || report.Rows.Count <= 1) //<--- why are records null...
                return NoContent();

            // Resolve wwwroot
            var apiWwwRoot = ResolveApiWwwRoot();
            var payrollDir = Path.Combine(apiWwwRoot, "payroll");
            if (!Directory.Exists(payrollDir)) Directory.CreateDirectory(payrollDir);

            var fileName = $"payroll_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}.csv";
            var filePath = Path.Combine(payrollDir, fileName);

            using (var stream = new System.IO.FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                // write header
                foreach (var header in report.Rows.First()) csv.WriteField(header);
                csv.NextRecord();

                foreach (var row in report.Rows.Skip(1))
                {
                    foreach (var field in row) csv.WriteField(field ?? string.Empty);
                    csv.NextRecord();
                }

                await writer.FlushAsync();
            }

            var baseUrl = _configuration["App:BaseUrl"] ?? _configuration["AppBaseUrl"] ?? "http://localhost:5000";
            // Return the protected API download endpoint (requires auth) instead of the public/static wwwroot path.
            var downloadUrl = new Uri(new Uri(baseUrl.TrimEnd('/')), $"/api/payroll/download/{fileName}").ToString();

            // send admin notifications
            foreach (var admin in report.AdminEmails)
            {
                var notice = NoticeConstant.NotifyPayrollGeneratedAdmin(admin, admin, DateTime.UtcNow.Month, DateTime.UtcNow.Year, downloadUrl);
                await _emailService.SendEmailAsync(notice);
            }

            // send individual notifications to users
            foreach (var user in report.UserNotifications)
            {
                var notice = NoticeConstant.NotifyPayrollGenerated(user.UserName, user.Email, user.Amount, DateTime.UtcNow.Month, DateTime.UtcNow.Year);
                await _emailService.SendEmailAsync(notice);
            }

            return Ok(new { downloadUrl });
        }

        // GET: api/payroll/download/{fileName}
        [HttpGet("download/{fileName}")]
        [Authorize(Policy = "Admin")]
        public IActionResult Download(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return BadRequest();

            // Allow only .csv files and reasonably sized names
            if (!fileName.EndsWith(".csv", System.StringComparison.OrdinalIgnoreCase) || fileName.Length > 260)
                return BadRequest();

            // Sanitize: strip any directory separators to prevent path traversal
            var sanitized = System.IO.Path.GetFileName(fileName);
            if (string.IsNullOrWhiteSpace(sanitized) || sanitized != fileName)
                return BadRequest(new { error = "Invalid file name." });

            var apiWwwRoot = ResolveApiWwwRoot();
            var payrollDir = System.IO.Path.Combine(apiWwwRoot, "payroll");
            var filePath = System.IO.Path.GetFullPath(System.IO.Path.Combine(payrollDir, sanitized));

            // Verify resolved path is inside the payroll directory (prevent traversal)
            if (!filePath.StartsWith(System.IO.Path.GetFullPath(payrollDir), System.StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { error = "Invalid file path." });

            if (!System.IO.File.Exists(filePath)) return NotFound();

            var stream = new System.IO.FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return File((System.IO.Stream)stream, "text/csv", sanitized);
        }

        private string ResolveApiWwwRoot()
        {
            var cfg = HttpContext.RequestServices.GetService(typeof(Microsoft.Extensions.Configuration.IConfiguration)) as Microsoft.Extensions.Configuration.IConfiguration;
            var cfgPath = cfg?["Payroll:StoragePath"];
            if (!string.IsNullOrWhiteSpace(cfgPath)) return cfgPath;

            try
            {
                var dir = new System.IO.DirectoryInfo(System.IO.Directory.GetCurrentDirectory());
                while (dir != null)
                {
                    if (dir.Name.Equals("ProcessZero.Api", System.StringComparison.OrdinalIgnoreCase))
                    {
                        var www = System.IO.Path.Combine(dir.FullName, "wwwroot");
                        if (System.IO.Directory.Exists(www)) return www;
                        System.IO.Directory.CreateDirectory(www);
                        return www;
                    }
                    dir = dir.Parent;
                }
            }
            catch { }

            try
            {
                var env = HttpContext.RequestServices.GetService(typeof(Microsoft.AspNetCore.Hosting.IWebHostEnvironment)) as Microsoft.AspNetCore.Hosting.IWebHostEnvironment;
                if (!string.IsNullOrWhiteSpace(env?.WebRootPath) && System.IO.Directory.Exists(env.WebRootPath))
                    return env.WebRootPath;
            }
            catch { }

            var fallback = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot");
            if (System.IO.Directory.Exists(fallback)) return fallback;

            throw new System.IO.DirectoryNotFoundException("API wwwroot folder not found. Configure 'Payroll:StoragePath' if needed.");
        }
    }
}
