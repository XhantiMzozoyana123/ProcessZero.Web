using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain.Entities;
using System.Security.Claims;

namespace ProcessZero.Web.Controllers
{
    /// <summary>
    /// API Controller for managing Lead Lake entries.
    /// Lead Lake serves as a staging area for imported leads before converting them to Contacts.
    /// </summary>
    /// <remarks>
    /// <para><strong>Entity: LeadLake</strong></para>
    /// <para>Represents a potential customer lead with basic contact and company information.</para>
    /// 
    /// <para><strong>Columns (from BaseEntity):</strong></para>
    /// <list type="table">
    ///   <item>
    ///     <term>Id</term>
    ///     <description>int - Primary key, auto-generated identifier</description>
    ///   </item>
    ///   <item>
    ///     <term>UserId</term>
    ///     <description>string - Foreign key to AspNetUsers, owner of this lead (max 450 chars, indexed)</description>
    ///   </item>
    ///   <item>
    ///     <term>CreatedAt</term>
    ///     <description>DateTime - UTC timestamp when record was created (auto-set)</description>
    ///   </item>
    ///   <item>
    ///     <term>UpdatedAt</term>
    ///     <description>DateTime - UTC timestamp when record was last modified</description>
    ///   </item>
    /// </list>
    /// 
    /// <para><strong>Columns (LeadLake specific):</strong></para>
    /// <list type="table">
    ///   <item>
    ///     <term>FirstName</term>
    ///     <description>string - Lead's first name</description>
    ///   </item>
    ///   <item>
    ///     <term>LastName</term>
    ///     <description>string - Lead's last name</description>
    ///   </item>
    ///   <item>
    ///     <term>Email</term>
    ///     <description>string - Lead's email address (max 256 chars, indexed)</description>
    ///   </item>
    ///   <item>
    ///     <term>Phone</term>
    ///     <description>string - Lead's phone number</description>
    ///   </item>
    ///   <item>
    ///     <term>Company</term>
    ///     <description>string - Lead's company name</description>
    ///   </item>
    ///   <item>
    ///     <term>Job</term>
    ///     <description>string - Lead's job title or position</description>
    ///   </item>
    ///   <item>
    ///     <term>Location</term>
    ///     <description>string - Lead's geographic location</description>
    ///   </item>
    ///   <item>
    ///     <term>Industry</term>
    ///     <description>LeadLakeIndustry enum - Business industry classification</description>
    ///   </item>
    ///   <item>
    ///     <term>Intent</term>
    ///     <description>LeadIntent enum - Purchase intent level (High, Medium, Low)</description>
    ///   </item>
    /// </list>
    /// 
    /// <para><strong>Enum: LeadLakeIndustry</strong></para>
    /// <list type="bullet">
    ///   <item><description>Technology</description></item>
    ///   <item><description>Finance</description></item>
    ///   <item><description>Healthcare</description></item>
    ///   <item><description>Education</description></item>
    ///   <item><description>Retail</description></item>
    ///   <item><description>Manufacturing</description></item>
    ///   <item><description>Energy</description></item>
    ///   <item><description>Transportation</description></item>
    ///   <item><description>Entertainment</description></item>
    ///   <item><description>Hospitality</description></item>
    ///   <item><description>Other</description></item>
    /// </list>
    /// 
    /// <para><strong>Enum: LeadIntent</strong></para>
    /// <list type="bullet">
    ///   <item><description>High - Strong purchase intent</description></item>
    ///   <item><description>Medium - Moderate interest</description></item>
    ///   <item><description>Low - Initial awareness stage</description></item>
    /// </list>
    /// 
    /// <para><strong>Database Indexes:</strong></para>
    /// <list type="bullet">
    ///   <item><description>IX_LeadLakes_UserId - Single column index for user filtering</description></item>
    ///   <item><description>IX_LeadLakes_Email - Single column index for email lookups</description></item>
    ///   <item><description>IX_LeadLakes_UserId_Email - Composite index for user-specific email searches</description></item>
    /// </list>
    /// 
    /// <para><strong>Security:</strong></para>
    /// <list type="bullet">
    ///   <item><description>All endpoints require JWT Bearer authentication</description></item>
    ///   <item><description>Batch import requires Admin role</description></item>
    ///   <item><description>Users can only access their own leads (filtered by UserId)</description></item>
    /// </list>
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LeadLakeController : ControllerBase
    {
        private readonly ILeadLakeService _leadLakeService;

        public LeadLakeController(ILeadLakeService leadLakeService)
        {
            _leadLakeService = leadLakeService;
        }

        /// <summary>
        /// Retrieves all leads owned by the authenticated user.
        /// </summary>
        /// <returns>List of LeadLake entities</returns>
        /// <response code="200">Returns all leads for the current user</response>
        /// <response code="401">User not authenticated</response>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var leads = await _leadLakeService
                .GetLeadLakesAsync();

            return Ok(leads);
        }

        /// <summary>
        /// Retrieves a specific lead by its ID.
        /// </summary>
        /// <param name="id">The unique identifier of the lead</param>
        /// <returns>Single LeadLake entity if found</returns>
        /// <response code="200">Returns the requested lead</response>
        /// <response code="404">Lead not found or access denied</response>
        /// <response code="401">User not authenticated</response>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var lead = await _leadLakeService
                .GetLeadLakeByIdAsync(id);

            if (lead == null)
                return NotFound();

            return Ok(lead);
        }

        /// <summary>
        /// Imports multiple leads in a single batch operation. Admin access required.
        /// </summary>
        /// <param name="leadLakes">List of LeadLake entities to import. UserId will be auto-assigned to the authenticated admin.</param>
        /// <returns>Number of leads successfully added</returns>
        /// <response code="200">Returns count of imported leads</response>
        /// <response code="400">Invalid request or empty batch</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User lacks Admin role</response>
        /// <remarks>
        /// Example request body:
        /// <code>
        /// [
        ///   {
        ///     "firstName": "John",
        ///     "lastName": "Doe",
        ///     "email": "john.doe@example.com",
        ///     "phone": "+1234567890",
        ///     "company": "Acme Corp",
        ///     "job": "CEO",
        ///     "location": "New York, NY",
        ///     "industry": "Technology",
        ///     "intent": "High"
        ///   }
        /// ]
        /// </code>
        /// </remarks>
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
