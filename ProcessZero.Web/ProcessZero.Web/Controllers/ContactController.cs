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
    /// Controller responsible for managing contacts for the authenticated user.
    /// Models and types used:
    ///
    /// Contact (inherits BaseEntity):
    /// - Id (int) [from BaseEntity]
    /// - UserId (string)
    /// - CreatedAt (DateTime)
    /// - UpdatedAt (DateTime)
    /// - FirstName (string)
    /// - LastName (string)
    /// - Email (string)
    /// - Phone (string)
    /// - Company (string)
    /// - Job (string)
    /// - Location (string)
    /// - Status (ContactStatus) - enum: Reached, FollowUp, Converted, Active
    ///
    /// LeadLake (inherits BaseEntity):
    /// - Id (int) [from BaseEntity]
    /// - UserId (string)
    /// - CreatedAt (DateTime)
    /// - UpdatedAt (DateTime)
    /// - FirstName (string)
    /// - LastName (string)
    /// - Email (string)
    /// - Phone (string)
    /// - Company (string)
    /// - Job (string)
    /// - Location (string)
    /// - Industry (LeadLakeIndustry) - enum: Technology, Finance, Healthcare, Education, Retail, Manufacturing, Energy, Transportation, Entertainment, Hospitality, Other
    ///
    /// BaseEntity:
    /// - Id (int) [Key]
    /// - UserId (string)
    /// - CreatedAt (DateTime)
    /// - UpdatedAt (DateTime)
    ///
    /// IContactService (important methods used by this controller):
    /// - Task<List<Contact>> GetAllContactsByUserIdAsync(string userId)
    /// - Task<List<Contact>> GetAllContactTypesAsync(string type)
    /// - Task<Contact> GetContactByIdAsync(string id)
    /// - Task AddContactAsync(LeadLake leadLake)
    /// - Task AddBatchContactAsync(List<LeadLake> leadLakes)
    /// - Task DeleteContactAsync(string id)
    /// </summary>
    public class ContactController : ControllerBase
    {
        private readonly IContactService _contactService;

        public ContactController(IContactService contactService)
        {
            _contactService = contactService;
        }

        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = GetUserId();

            var contacts = await _contactService.GetAllContactsByUserIdAsync(userId);

            return Ok(contacts);
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetByUserId()
        {
            var userId = GetUserId();

            var contacts = await _contactService.GetAllContactsByUserIdAsync(userId);

            return Ok(contacts);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var contact = await _contactService.GetContactByIdAsync(id.ToString());

            if (contact == null)
                return NotFound();

            return Ok(contact);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] LeadLake leadlake)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            leadlake.UserId = GetUserId();

            await _contactService.AddContactAsync(leadlake);

            return CreatedAtAction(nameof(GetById), new { id = leadlake.Id }, leadlake);
        }

        [HttpPost("batch")]
        public async Task<IActionResult> AddBatch([FromBody] List<LeadLake> leadLakes)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (leadLakes == null || !leadLakes.Any())
                return BadRequest(new { error = "No leads provided." });

            var userId = GetUserId();
            foreach (var lead in leadLakes)
                lead.UserId = userId;

            await _contactService.AddBatchContactAsync(leadLakes);

            return Ok(new { added = leadLakes.Count });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Contact contact)
        {
            if (contact == null) return BadRequest("Contact is required.");
            if (id != contact.Id) return BadRequest("Id mismatch.");

            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            // Ensure the operation is performed by the owner
            contact.UserId = userId;

            try
            {
                await _contactService.UpdateContactAsync(contact);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }


        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _contactService.DeleteContactAsync(id.ToString());

            return NoContent();
        }
    }
}
