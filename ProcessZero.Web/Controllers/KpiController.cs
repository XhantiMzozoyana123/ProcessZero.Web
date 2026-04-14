using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProcessZero.Application.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;

namespace ProcessZero.Web.Controllers
{
    /// <summary>
    /// Controller for KPI-related endpoints.
    ///
    /// Entity: KPI (from ProcessZero.Domain.Entities.KPI)
    /// - Inherits BaseEntity which defines: Id (int), UserId (string), CreatedAt (DateTime), UpdatedAt (DateTime).
    /// - ProductId (int) [Required]
    /// - OutreachAttempts (int) = 0
    /// - CallsBooked (int) = 0
    /// - CallsAttended (int) = 0
    /// - DealsInfluenced (int) = 0
    /// - RevenueGenerated (decimal) = 0
    /// - DealsClosed (int) = 0
    /// - DealsAttempted (int) = 0
    /// - AverageDealSize (decimal) = 0
    /// - RevenueInfluenced (decimal) = 0
    /// - BasicClientRetention (double) = 0
    /// - ActivityConsistency (double) = 0
    /// - ActiveTeamSize (int) = 0
    /// - TeamRevenue (decimal) = 0
    /// - TeamCloseRate (double) = 0
    /// - TeamChurnRate (double) = 0
    /// - LeaderActivityLevel (double) = 0
    /// - MonthlyRecurringRevenue (decimal) = 0
    /// - GrowthRate (double) = 0
    /// - ClientRetention (double) = 0
    /// - TeamPerformanceHealth (double) = 0
    /// - BrandCompliance (double) = 0
    /// - LongTermRevenueGrowth (double) = 0
    /// - StrategicInitiativesDelivered (int) = 0
    /// - BrandRiskManagement (double) = 0
    /// - InnovationContribution (double) = 0
    /// - LeadershipStability (double) = 0
    ///
    /// Service interface: IKPIService (from ProcessZero.Application.Interfaces.IKPIService)
    /// Exposes methods the controller calls, e.g.:
    /// - Task AddOutreachAttemptsAsync(string userId, int productId, int attempts)
    /// - Task AddCallBookedAsync(string userId, int productId)
    /// - Task AddCallAttendanceAsync(string userId, int productId, bool attended)
    /// - Task AddDealsInfluencedAsync(string userId, int productId, int deals = 1)
    /// - Task AddRevenueGeneratedAsync(string userId, int productId, decimal revenue)
    /// - Task AddDealsClosedAsync(string userId, int productId, int dealsClosed, int dealsAttempted)
    /// - Task AddAverageDealSizeAsync(string userId, int productId, decimal dealAmount)
    /// - Task AddRevenueInfluencedAsync(string userId, int productId, decimal revenue)
    /// - Task AddBasicClientRetentionAsync(string userId, int productId, double retentionPercentage)
    /// - Task AddActivityConsistencyAsync(string userId, int productId, bool targetMet)
    /// - Task UpdateActiveTeamSizeAsync(string userId, int productId)
    /// - Task UpdateTeamRevenueAsync(string userId, int productId)
    /// - Task UpdateTeamCloseRateAsync(string userId, int productId)
    /// - Task UpdateTeamChurnRateAsync(string userId, int productId, double churnRate = 0)
    /// - Task UpdateLeaderActivityLevelAsync(string userId, int productId, double level = 1)
    /// - Task UpdateMonthlyRecurringRevenueAsync(string userId, int productId)
    /// - Task UpdateGrowthRateAsync(string userId, int productId, double growthRate = 0.1)
    /// - Task UpdateClientRetentionAsync(string userId, int productId, double retention = 1.0)
    /// - Task UpdateTeamPerformanceHealthAsync(string userId, int productId, double health = 1.0)
    /// - Task UpdateBrandComplianceAsync(string userId, int productId, double complianceScore)
    /// - Task UpdateLongTermRevenueGrowthAsync(string userId, int productId, double growth = 0.2)
    /// - Task UpdateStrategicInitiativesDeliveredAsync(string userId, int productId, int initiatives)
    /// - Task UpdateBrandRiskManagementAsync(string userId, int productId, double riskScore)
    /// - Task UpdateInnovationContributionAsync(string userId, int productId, double contributionScore)
    /// - Task UpdateLeadershipStabilityAsync(string userId, int productId, double stability = 1.0)
    /// - Task<KPI> GetLatestKPIAsync(string userId, int productId)
    /// - Task<decimal> GetTotalRevenueAsync(string userId, int productId)
    /// - Task<double> GetCloseRateAsync(string userId, int productId)
    ///
    /// The controller also defines several lightweight DTOs used for incoming requests (AttemptsDto,
    /// AttendanceDto, DealsDto, RevenueDto, CloseRateDto, DoubleDto, InitiativesDto) which are validated
    /// by data annotations where appropriate.
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

        // Accepts JSON boolean, number, or object with an "attended" property and returns an int
        private static int ParseAttendance(JsonElement body)
        {
            try
            {
                switch (body.ValueKind)
                {
                    case JsonValueKind.True:
                        return 1;
                    case JsonValueKind.False:
                        return 0;
                    case JsonValueKind.Number:
                        if (body.TryGetInt32(out var n)) return n;
                        return 0;
                    case JsonValueKind.Object:
                        if (body.TryGetProperty("attended", out var prop))
                        {
                            switch (prop.ValueKind)
                            {
                                case JsonValueKind.True: return 1;
                                case JsonValueKind.False: return 0;
                                case JsonValueKind.Number: return prop.TryGetInt32(out var v) ? v : 0;
                                case JsonValueKind.String:
                                    var s = prop.GetString();
                                    if (int.TryParse(s, out var parsed)) return parsed;
                                    if (bool.TryParse(s, out var b)) return b ? 1 : 0;
                                    break;
                            }
                        }
                        break;
                    case JsonValueKind.String:
                        var str = body.GetString();
                        if (int.TryParse(str, out var pi)) return pi;
                        if (bool.TryParse(str, out var pb)) return pb ? 1 : 0;
                        break;
                }

                throw new ArgumentException("Invalid attendance payload");
            }
            catch (System.Exception ex)
            {
                throw new ArgumentException("Unable to parse attendance from request body", ex);
            }
        }

        private static int? TryGetIntProperty(JsonElement body, string propertyName)
        {
            try
            {
                if (body.ValueKind == JsonValueKind.Object && body.TryGetProperty(propertyName, out var prop))
                {
                    if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var v)) return v;
                    if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out var s)) return s;
                    if (prop.ValueKind == JsonValueKind.True) return 1;
                    if (prop.ValueKind == JsonValueKind.False) return 0;
                }
                else if (body.ValueKind == JsonValueKind.Number && body.TryGetInt32(out var n)) return n;
                else if (body.ValueKind == JsonValueKind.String && int.TryParse(body.GetString(), out var parsed)) return parsed;
            }
            catch { }
            return null;
        }

        private static decimal? TryGetDecimalProperty(JsonElement body, string propertyName)
        {
            try
            {
                if (body.ValueKind == JsonValueKind.Object && body.TryGetProperty(propertyName, out var prop))
                {
                    if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDecimal(out var v)) return v;
                    if (prop.ValueKind == JsonValueKind.String && decimal.TryParse(prop.GetString(), out var s)) return s;
                }
                else if (body.ValueKind == JsonValueKind.Number && body.TryGetDecimal(out var n)) return n;
                else if (body.ValueKind == JsonValueKind.String && decimal.TryParse(body.GetString(), out var parsed)) return parsed;
            }
            catch { }
            return null;
        }

        private static double? TryGetDoubleProperty(JsonElement body, string propertyName)
        {
            try
            {
                if (body.ValueKind == JsonValueKind.Object && body.TryGetProperty(propertyName, out var prop))
                {
                    if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDouble(out var v)) return v;
                    if (prop.ValueKind == JsonValueKind.String && double.TryParse(prop.GetString(), out var s)) return s;
                }
                else if (body.ValueKind == JsonValueKind.Number && body.TryGetDouble(out var n)) return n;
                else if (body.ValueKind == JsonValueKind.String && double.TryParse(body.GetString(), out var parsed)) return parsed;
            }
            catch { }
            return null;
        }

        private string GetUserId() => User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        #region DTOs
        // Data Transfer Objects (DTOs) for request bodies
        /// <summary>
        /// AttemptsDto
        /// - Maps to KPI.OutreachAttempts (int).
        /// - Validation: non-negative integer.
        /// - Example JSON: { "attempts": 5 }
        /// </summary>
        public sealed record AttemptsDto([property: Range(0, int.MaxValue)] int Attempts);

        /// <summary>
        /// AttendanceDto
        /// - Maps to KPI.CallsAttended or ActivityConsistency depending on context.
        /// - Example JSON: { "attended": true }
        /// </summary>
        public sealed record AttendanceDto(bool Attended);

        /// <summary>
        /// DealsDto
        /// - Maps to KPI.DealsInfluenced or other deal counters depending on endpoint.
        /// - Example JSON: { "deals": 2 }
        /// </summary>
        public sealed record DealsDto([property: Range(0, int.MaxValue)] int Deals);

        /// <summary>
        /// RevenueDto
        /// - Maps to decimal revenue fields such as KPI.RevenueGenerated, KPI.RevenueInfluenced or KPI.AverageDealSize.
        /// - Use decimal in JSON to represent currency (no currency symbol).
        /// - Example JSON: { "revenue": 15000.50 }
        /// </summary>
        public sealed record RevenueDto([property: Range(0, double.MaxValue)] decimal Revenue);

        /// <summary>
        /// CloseRateDto
        /// - Contains counts used to compute close rate (DealsClosed / DealsAttempted).
        /// - Maps to KPI.DealsClosed and KPI.DealsAttempted.
        /// - Validation ensures DealsAttempted >= 1 to avoid division by zero.
        /// - Example JSON: { "dealsClosed": 3, "dealsAttempted": 10 }
        /// </summary>
        public sealed record CloseRateDto(
            [property: Range(0, int.MaxValue)] int DealsClosed,
            [property: Range(1, int.MaxValue)] int DealsAttempted
        );

        /// <summary>
        /// DoubleDto
        /// - Generic double-valued DTO used for normalized scores (0.0–1.0).
        /// - Map to KPI fields such as BasicClientRetention, GrowthRate, BrandCompliance, etc.
        /// - Example JSON: { "value": 0.85 }
        /// </summary>
        public sealed record DoubleDto(double Value);

        /// <summary>
        /// InitiativesDto
        /// - Maps to KPI.StrategicInitiativesDelivered (int).
        /// - Example JSON: { "initiatives": 2 }
        /// </summary>
        public sealed record InitiativesDto([property: Range(0, int.MaxValue)] int Initiatives);
        #endregion

        #region Helper
        // Common helper for executing KPI updates and returning NoContent
        // Catches exceptions and returns a Problem response so clients get useful diagnostics
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
                // Return server error details to aid development debugging
                return Problem(detail: ex.ToString(), statusCode: 500, title: "KPI update error");
            }
        }
        #endregion

        // Actions allowed for basic Sales Partners (level 1)
        [HttpPost("partner/product/{productId:int}/outreach")]
        public Task<IActionResult> UpdateOutreachAttempts(int productId, [FromBody] JsonElement body)
        {
            int attempts = TryGetIntProperty(body, "attempts") ?? ParseAttendance(body);
            return ExecuteKpiUpdate(() => _kpiService.AddOutreachAttemptsAsync(GetUserId(), productId, attempts));
        }

        [HttpPost("partner/product/{productId:int}/calls/booked")]
        public Task<IActionResult> IncrementCallsBooked(int productId) =>
            ExecuteKpiUpdate(() => _kpiService.AddCallBookedAsync(GetUserId(), productId));

        [HttpPost("partner/product/{productId:int}/calls/attendance")]
        public Task<IActionResult> ConfirmCallAttendance(int productId, [FromBody] JsonElement body)
        {
            int attended = ParseAttendance(body);
            return ExecuteKpiUpdate(() => _kpiService.AddCallAttendanceAsync(GetUserId(), productId, attended));
        }

        [HttpPost("partner/product/{productId:int}/deals/influenced")]
        public Task<IActionResult> IncrementDealsInfluenced(int productId, [FromBody] JsonElement body)
        {
            int deals = TryGetIntProperty(body, "deals") ?? ParseAttendance(body);
            return ExecuteKpiUpdate(() => _kpiService.AddDealsInfluencedAsync(GetUserId(), productId, deals));
        }

        [HttpPost("partner/product/{productId:int}/revenue")]
        public Task<IActionResult> AddRevenueGenerated(int productId, [FromBody] RevenueDto dto) =>
            ExecuteKpiUpdate(() => _kpiService.AddRevenueGeneratedAsync(GetUserId(), productId, dto.Revenue));

        [HttpPost("partner/product/{productId:int}/close-rate")]
        public Task<IActionResult> UpdateCloseRate(int productId, [FromBody] JsonElement body)
        {
            int dealsClosed = TryGetIntProperty(body, "dealsClosed") ?? ParseAttendance(body);
            int dealsAttempted = TryGetIntProperty(body, "dealsAttempted") ?? ParseAttendance(body);
            return ExecuteKpiUpdate(() => _kpiService.AddDealsClosedAsync(GetUserId(), productId, dealsClosed, dealsAttempted));
        }

        [HttpPost("partner/product/{productId:int}/average-deal")]
        public Task<IActionResult> UpdateAverageDealSize(int productId, [FromBody] JsonElement body)
        {
            decimal deal = TryGetDecimalProperty(body, "revenue") ?? Convert.ToDecimal(ParseAttendance(body));
            return ExecuteKpiUpdate(() => _kpiService.AddAverageDealSizeAsync(GetUserId(), productId, deal));
        }

        [HttpPost("partner/product/{productId:int}/revenue-influenced")]
        public Task<IActionResult> UpdateRevenueInfluenced(int productId, [FromBody] JsonElement body)
        {
            decimal revenue = TryGetDecimalProperty(body, "revenue") ?? Convert.ToDecimal(ParseAttendance(body));
            return ExecuteKpiUpdate(() => _kpiService.AddRevenueInfluencedAsync(GetUserId(), productId, revenue));
        }

        [HttpPost("partner/product/{productId:int}/client-retention")]
        public Task<IActionResult> UpdateBasicClientRetention(int productId, [FromBody] JsonElement body)
        {
            double val = TryGetDoubleProperty(body, "value") ?? ParseAttendance(body);
            return ExecuteKpiUpdate(() => _kpiService.AddBasicClientRetentionAsync(GetUserId(), productId, val));
        }

        [HttpPost("partner/product/{productId:int}/activity-consistency")]
        public Task<IActionResult> UpdateActivityConsistency(int productId, [FromBody] JsonElement body)
        {
            int attended = ParseAttendance(body);
            return ExecuteKpiUpdate(() => _kpiService.AddActivityConsistencyAsync(GetUserId(), productId, attended));
        }

        [HttpPost("partner/product/{productId:int}/team/active-size")]
        public Task<IActionResult> UpdateActiveTeamSize(int productId) =>
            ExecuteKpiUpdate(() => _kpiService.UpdateActiveTeamSizeAsync(GetUserId(), productId));

        [HttpPost("partner/product/{productId:int}/team/revenue")]
        public Task<IActionResult> UpdateTeamRevenue(int productId) =>
            ExecuteKpiUpdate(() => _kpiService.UpdateTeamRevenueAsync(GetUserId(), productId));

        [HttpPost("partner/product/{productId:int}/team/close-rate")]
        public Task<IActionResult> UpdateTeamCloseRate(int productId) =>
            ExecuteKpiUpdate(() => _kpiService.UpdateTeamCloseRateAsync(GetUserId(), productId));

        [HttpPost("partner/product/{productId:int}/team/churn-rate")]
        public Task<IActionResult> UpdateTeamChurnRate(int productId) =>
            ExecuteKpiUpdate(() => _kpiService.UpdateTeamChurnRateAsync(GetUserId(), productId));

        [HttpPost("partner/product/{productId:int}/leader/activity-level")]
        public Task<IActionResult> UpdateLeaderActivityLevel(int productId) =>
            ExecuteKpiUpdate(() => _kpiService.UpdateLeaderActivityLevelAsync(GetUserId(), productId));

        [HttpPost("partner/product/{productId:int}/mrr")]
        public Task<IActionResult> UpdateMonthlyRecurringRevenue(int productId) =>
            ExecuteKpiUpdate(() => _kpiService.UpdateMonthlyRecurringRevenueAsync(GetUserId(), productId));

        [HttpPost("partner/product/{productId:int}/growth-rate")]
        public Task<IActionResult> UpdateGrowthRate(int productId) =>
            ExecuteKpiUpdate(() => _kpiService.UpdateGrowthRateAsync(GetUserId(), productId));

        [HttpPost("partner/product/{productId:int}/team-performance-health")]
        public Task<IActionResult> UpdateTeamPerformanceHealth(int productId) =>
            ExecuteKpiUpdate(() => _kpiService.UpdateTeamPerformanceHealthAsync(GetUserId(), productId));

        [HttpPost("partner/product/{productId:int}/brand-compliance")]
        public Task<IActionResult> UpdateBrandCompliance(int productId, [FromBody] DoubleDto dto) =>
            ExecuteKpiUpdate(() => _kpiService.UpdateBrandComplianceAsync(GetUserId(), productId, dto.Value));

        [HttpPost("partner/product/{productId:int}/client-retention/external")]
        public Task<IActionResult> UpdateClientRetentionExternal(int productId) =>
            ExecuteKpiUpdate(() => _kpiService.UpdateClientRetentionAsync(GetUserId(), productId));

        [HttpPost("partner/product/{productId:int}/long-term-growth")]
        public Task<IActionResult> UpdateLongTermRevenueGrowth(int productId) =>
            ExecuteKpiUpdate(() => _kpiService.UpdateLongTermRevenueGrowthAsync(GetUserId(), productId));

        [HttpPost("partner/product/{productId:int}/strategic-initiatives")]
        public Task<IActionResult> UpdateStrategicInitiativesDelivered(int productId, [FromBody] InitiativesDto dto) =>
            ExecuteKpiUpdate(() => _kpiService.UpdateStrategicInitiativesDeliveredAsync(GetUserId(), productId, dto.Initiatives));

        [HttpPost("partner/product/{productId:int}/brand-risk")]
        public Task<IActionResult> UpdateBrandRiskManagement(int productId, [FromBody] DoubleDto dto) =>
            ExecuteKpiUpdate(() => _kpiService.UpdateBrandRiskManagementAsync(GetUserId(), productId, dto.Value));

        [HttpPost("partner/product/{productId:int}/innovation")]
        public Task<IActionResult> UpdateInnovationContribution(int productId, [FromBody] DoubleDto dto) =>
            ExecuteKpiUpdate(() => _kpiService.UpdateInnovationContributionAsync(GetUserId(), productId, dto.Value));

        [HttpPost("partner/product/{productId:int}/leadership-stability")]
        public Task<IActionResult> UpdateLeadershipStability(int productId) =>
            ExecuteKpiUpdate(() => _kpiService.UpdateLeadershipStabilityAsync(GetUserId(), productId));

        // Get the latest KPI for a specific partner and product
        [HttpGet("partner/product/{productId:int}/latest")]
        public async Task<IActionResult> GetLatestKPI(int productId)
        {
            var kpi = await _kpiService.GetLatestKPIAsync(GetUserId(), productId);
            if (kpi is null) return NotFound();
            return Ok(kpi);
        }

        // Get the latest KPI for a specific partner and product by admin
        [HttpGet("partner/product/{productId:int}/latest")]
        public async Task<IActionResult> GetLatestKPIForAdmin(int productId, [FromQuery] string userId)
        {
            var kpi = await _kpiService.GetLatestKPIAsync(userId, productId);
            if (kpi is null) return NotFound();
            return Ok(kpi);
        }


        /// <summary>
        /// Admin: get a specific partner's latest KPI for a given product.
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
