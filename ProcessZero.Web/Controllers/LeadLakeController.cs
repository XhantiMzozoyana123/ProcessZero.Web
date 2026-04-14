using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain.Entities;
using System.Security.Claims;

namespace ProcessZero.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    /// <summary>
    /// Controller responsible for retrieving LeadLake (lead) records.
    /// Exposes endpoints to list leads and fetch a single lead by id. All endpoints require authentication.
    ///
    /// Model columns (LeadLake):
    /// - Id (int)          : Primary key (from BaseEntity)
    /// - UserId (string)   : Owner/creator id (from BaseEntity)
    /// - FirstName (string)
    /// - LastName (string)
    /// - Email (string)
    /// - Phone (string)
    /// - Company (string)
    /// - Job (string)
    /// - Location (string)
    /// - Industry (enum)   : <see cref="Domain.Entities.LeadLakeIndustry"/>
    /// - CreatedAt/UpdatedAt (DateTime) : from BaseEntity
    ///
    /// Notes:
    /// - This controller delegates data access to <see cref="ILeadLakeService"/>.
    /// - LeadLake is typically used as an import staging area for contacts/leads.
    /// </summary>
    public class LeadLakeController : ControllerBase
    {
        private readonly ILeadLakeService _leadLakeService;

        public LeadLakeController(ILeadLakeService leadLakeService)
        {
            _leadLakeService = leadLakeService;
        }

        /// <summary>
        /// GET: api/leadlake
        /// Returns all LeadLake entries visible to the authenticated user.
        /// The service returns a list of <see cref="Domain.Entities.LeadLake"/> objects.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var leads = await _leadLakeService
                .GetLeadLakesAsync();

            return Ok(leads);
        }

        // GET: api/leadlake/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var lead = await _leadLakeService
                .GetLeadLakeByIdAsync(id);

            if (lead == null)
                return NotFound();

            return Ok(lead);
        }

        [HttpPost("batch")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> AddBatch([FromBody] List<LeadLake> leadLakes)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (leadLakes == null || !leadLakes.Any())
                return BadRequest(new { error = "No leads provided." });

            var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            foreach (var lead in leadLakes)
                lead.UserId = userId;

            await _leadLakeService.AddBatchLeadLakesAsync(leadLakes);

            return Ok(new { added = leadLakes.Count });
        }
    }
}
