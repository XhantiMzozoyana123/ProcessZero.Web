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
    /// - CallOutreach (int)   — call outreach made for the day
    /// - EmailOutreach (int)  — email outreach made for the day
    /// - CallsMade (int)      — calls made for the day
    /// - MeetingsBooked (int) — meetings booked for the day
    /// - DealSizeClosed (decimal) — deal size (amount) closed for the day
    /// - ActiveClients (int)  — active clients used for MRR
    /// - MonthlyRecurringRevenue (decimal) — MRR derived from Contacts + Invoices
    ///
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

        [HttpPost("product/{productId:int}/call-outreach")]
        public Task<IActionResult> AddCallOutreach(int productId, [FromBody] CountDto? dto) =>
            ExecuteKpiUpdate(() => _kpiService.AddCallOutreachAsync(GetUserId(), productId, dto?.Count ?? 1));

        [HttpPost("product/{productId:int}/email-outreach")]
        public Task<IActionResult> AddEmailOutreach(int productId, [FromBody] CountDto? dto) =>
            ExecuteKpiUpdate(() => _kpiService.AddEmailOutreachAsync(GetUserId(), productId, dto?.Count ?? 1));

        [HttpPost("product/{productId:int}/calls-made")]
        public Task<IActionResult> AddCallsMade(int productId, [FromBody] CountDto? dto) =>
            ExecuteKpiUpdate(() => _kpiService.AddCallsMadeAsync(GetUserId(), productId, dto?.Count ?? 1));

        // Note: MeetingsBooked is incremented automatically on the backend by
        // MeetingService when a meeting is created, so no endpoint is exposed for it.

        [HttpPost("product/{productId:int}/deal-closed")]
        public Task<IActionResult> AddDealClosed(int productId, [FromBody] AmountDto dto) =>
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
