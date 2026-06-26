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
    /// - KpiPolicy (inherits BaseEntity) — defines the performance targets and
    ///   consequences for a product/rep
    ///   - Id (int), UserId (string), CreatedAt (DateTime), UpdatedAt (DateTime)
    ///   - ProductId (int?)
    ///   - PolicyName (string) — readable name for the policy tier
    ///   - EffectiveFrom (DateTime), EffectiveTo (DateTime?)
    ///   - IsActive (bool)
    ///   - DailyEmailsTarget (int), DailyCallsTarget (int)
    ///   - MinimumReplyRate (decimal), WeeklyMeetingsTarget (int)
    ///   - MonthlyRevenueTarget (decimal), MonthlyRecurringRevenueTarget (decimal)
    ///   - PerformanceTolerance (decimal) — allowed underperformance buffer (e.g. 0.10 = 10%)
    ///   - ConsequenceLevel (enum: None, Warning, Freeze, Suspend)
    ///   - ConsequenceOnBreach (int) — mapped to ConsequenceLevel enum
    ///   - ManagerApprovalRequiredToUnfreeze (bool)
    ///
    /// - KPI (inherits BaseEntity) — daily snapshot of a sales rep's performance
    ///   - ProductId (int)
    ///   - CallsAttempted (int), CallsCompleted (int), EmailsSent (int)
    ///   - RepliesReceived (int), MeetingsBooked (int)
    ///   - DealsClosed (int), RevenueClosed (decimal)
    ///   - ActiveClients (int), MonthlyRecurringRevenue (decimal)
    ///
    /// Sales pipeline tracked: Outreach → Replies → Meetings → Deals Closed
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
