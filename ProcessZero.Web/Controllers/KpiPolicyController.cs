using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain.Entities;

namespace ProcessZero.Web.Controllers
{
    /// <summary>
    /// Controller for managing KPI policies.
    ///
    /// Entities referenced:
    /// - KpiPolicy (inherits BaseEntity)
    ///   - Id (int), UserId (string), CreatedAt (DateTime), UpdatedAt (DateTime)
    ///   - ProductId (int?)
    ///   - EffectiveFrom (DateTime), EffectiveTo (DateTime?)
    ///   - IsActive (bool)
    ///   - MinMonthlyRevenue (decimal)
    ///   - MinOutreachAttempts (int)
    ///   - MinCallsBooked (int)
    ///   - GracePeriodDays (int), AutoFreezeOnBreach (bool)
    ///
    /// - KPI (inherits BaseEntity) — snapshot of partner performance
    ///   - ProductId (int)
    ///   - OutreachAttempts (int), CallsBooked (int), CallsAttended (int)
    ///   - DealsInfluenced (int), RevenueGenerated (decimal)
    ///   - DealsClosed (int), DealsAttempted (int), AverageDealSize (decimal)
    ///   - RevenueInfluenced (decimal), BasicClientRetention (double)
    ///   - ActivityConsistency (double), ActiveTeamSize (int), TeamRevenue (decimal)
    ///   - TeamCloseRate (double), TeamChurnRate (double), LeaderActivityLevel (double)
    ///   - MonthlyRecurringRevenue (decimal), GrowthRate (double), ClientRetention (double)
    ///   - TeamPerformanceHealth (double), BrandCompliance (double)
    ///   - LongTermRevenueGrowth (double), StrategicInitiativesDelivered (int)
    ///   - BrandRiskManagement (double), InnovationContribution (double), LeadershipStability (double)
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class KpiPolicyController : ControllerBase
    {
        private readonly IKpiPolicyService _kpiPolicyService;

        public KpiPolicyController(IKpiPolicyService kpiPolicyService)
        {
            _kpiPolicyService = kpiPolicyService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var policies = await _kpiPolicyService.GetAllPoliciesAsync();
            return Ok(policies);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var policy = await _kpiPolicyService.GetPolicyByIdAsync(id);
            if (policy == null) return NotFound();
            return Ok(policy);
        }

        [HttpPost]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> Create([FromBody] KpiPolicy policy)
        {
            if (policy == null) return BadRequest("Policy is required.");

            await _kpiPolicyService.AddPolicyAsync(policy);
            if (policy.Id != 0)
                return CreatedAtAction(nameof(GetById), new { id = policy.Id }, policy);

            return NoContent();
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] KpiPolicy policy)
        {
            if (policy == null) return BadRequest("Policy is required.");
            if (policy.Id != id) return BadRequest("Id mismatch.");

            await _kpiPolicyService.UpdatePolicyAsync(policy);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            await _kpiPolicyService.DeletePolicyAsync(id);
            return NoContent();
        }
    }
}
