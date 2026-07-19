using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProcessZero.Application.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace ProcessZero.Web.Controllers
{
    /// <summary>
    /// Controller for KPI-related endpoints.
    ///
    /// Entity: KPI (from ProcessZero.Domain.Entities.KPI) — a daily snapshot of a
    /// sales rep's performance for a product. Inherits BaseEntity which defines:
    /// Id (int), UserId (string), CreatedAt (DateTime), UpdatedAt (DateTime).
    /// - ProductId (int) [Required]
    /// - CallsAttempted (int) — total calls dialed/attempted for the day
    /// - CallsCompleted (int) — calls successfully completed/talked to for the day
    /// - EmailsSent (int) — total outreach emails sent for the day
    /// - RepliesReceived (int) — positive email replies received for the day
    /// - MeetingsBooked (int) — meetings booked for the day
    /// - DealsClosed (int) — number of deals closed for the day
    /// - RevenueClosed (decimal) — revenue (amount) closed for the day
    /// - ActiveClients (int) — active clients used for MRR
    /// - MonthlyRecurringRevenue (decimal) — MRR derived from Contacts + Invoices
    ///
    /// Sales pipeline tracked: Outreach → Replies → Meetings Booked → Deals Closed
    /// MRR is recalculated automatically from active client contacts and their
    /// closed invoice amounts every time a sales rep updates their KPIs.
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class KpiController : ControllerBase
    {
        private readonly IKpiService _kpiService;

        public KpiController(IKpiService kpiService)
        {
            _kpiService = kpiService;
        }

        private string GetUserId() => User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        #region DTOs
        /// <summary>Count of activity records, e.g. { "count": 5 }.</summary>
        public sealed record CountDto([property: Range(1, int.MaxValue)] int Count = 1);

        /// <summary>Deal amount closed, e.g. { "amount": 15000.50 }.</summary>
        public sealed record AmountDto([property: Range(0, double.MaxValue)] decimal Amount);
        #endregion

        #region Helper
        private async Task<IActionResult> ExecuteKpiUpdate(Func<Task> kpiUpdate)
        {
            try
            {
                await kpiUpdate();
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (System.Exception ex)
            {
                return Problem(detail: ex.ToString(), statusCode: 500, title: "KPI update error");
            }
        }
        #endregion

        // ── Daily activity updates ─────────────────────────────────

        // ── Daily activity updates (sales pipeline) ───────────────────
        // Outreach layer
        [HttpPost("product/{productId:int}/calls-attempted")]
        public Task<IActionResult> AddCallsAttempted(int productId, [FromBody] CountDto? dto) =>
            ExecuteKpiUpdate(() => _kpiService.AddCallOutreachAsync(GetUserId(), productId, dto?.Count ?? 1));

        [HttpPost("product/{productId:int}/emails-sent")]
        public Task<IActionResult> AddEmailsSent(int productId, [FromBody] CountDto? dto) =>
            ExecuteKpiUpdate(() => _kpiService.AddEmailOutreachAsync(GetUserId(), productId, dto?.Count ?? 1));

        // Conversion layer
        [HttpPost("product/{productId:int}/replies-received")]
        public Task<IActionResult> AddRepliesReceived(int productId, [FromBody] CountDto? dto) =>
            ExecuteKpiUpdate(() => _kpiService.AddRepliesReceivedAsync(GetUserId(), productId, dto?.Count ?? 1));

        [HttpPost("product/{productId:int}/calls-completed")]
        public Task<IActionResult> AddCallsCompleted(int productId, [FromBody] CountDto? dto) =>
            ExecuteKpiUpdate(() => _kpiService.AddCallsMadeAsync(GetUserId(), productId, dto?.Count ?? 1));

        // Meetings & deals layer
        // Note: MeetingsBooked can be incremented automatically by MeetingService
        // when a meeting is created, or manually via this endpoint.
        [HttpPost("product/{productId:int}/meetings-booked")]
        public Task<IActionResult> AddMeetingsBooked(int productId) =>
            ExecuteKpiUpdate(() => _kpiService.AddMeetingBookedAsync(GetUserId(), productId));

        [HttpPost("product/{productId:int}/deals-closed")]
        public Task<IActionResult> AddDealsClosed(int productId, [FromBody] AmountDto dto) =>
            ExecuteKpiUpdate(() => _kpiService.AddDealClosedAsync(GetUserId(), productId, dto.Amount));

        [HttpPost("product/{productId:int}/recalculate-mrr")]
        public Task<IActionResult> RecalculateMrr(int productId) =>
            ExecuteKpiUpdate(() => _kpiService.RecalculateMrrAsync(GetUserId(), productId));

        // ── Reads ──────────────────────────────────────────────────

        // Get the latest KPI for the current sales rep and product
        [HttpGet("product/{productId:int}/latest")]
        public async Task<IActionResult> GetLatestKPI(int productId)
        {
            var kpi = await _kpiService.GetLatestKPIAsync(GetUserId(), productId);
            if (kpi is null) return NotFound();
            return Ok(kpi);
        }

        /// <summary>
        /// Admin: get a specific sales rep's latest KPI for a given product.
        /// </summary>
        [Authorize(Policy = "Admin")]
        [HttpGet("admin/partner/{userId}/product/{productId:int}/latest")]
        public async Task<IActionResult> AdminGetLatestKPI(string userId, int productId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return BadRequest("userId is required");

            var kpi = await _kpiService.GetLatestKPIAsync(userId, productId);
            if (kpi is null) return NotFound();
            return Ok(kpi);
        }
    }
}
