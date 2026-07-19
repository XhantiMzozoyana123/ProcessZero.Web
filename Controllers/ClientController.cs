using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain.Entities;
using System.Security.Claims;

namespace ProcessZero.Web.Controllers
{
    /*
 * ClientController
 * --------------
 * This controller operates on the `Contact` entity (ProcessZero.Domain.Entities.Contact)
 * and exposes CRUD endpoints for managing clients/contacts.
 *
 * Contact (inherits BaseEntity)
 * - Id (int) : primary key (from BaseEntity)
 * - UserId (string) : owner/creator user id (from BaseEntity)
 * - CreatedAt (DateTime) : record creation timestamp (from BaseEntity)
 * - UpdatedAt (DateTime) : record last-updated timestamp (from BaseEntity)
 *
 * Contact specific columns/properties:
 * - FirstName (string)
 * - LastName (string)
 * - Email (string)
 * - Phone (string)
 * - Company (string)
 * - Job (string)
 * - Location (string)
 * - ClosedAmount (decimal) : final agreed deal amount used for commission calculations
 * - Status (ContactStatus) : enum representing contact lifecycle (e.g., Active, Converted)
 *
 * Notes:
 * - The controller currently returns `Contact` entities directly. Consider mapping to
 *   DTOs if you need to hide internal fields, shape responses, or avoid exposing
 *   navigation properties in API responses.
 *
 * IClientService (used by this controller) exposes methods:
 * - GetAllContactsAsync()
 * - GetContactByIdAsync(int id)
 * - CreateContactAsync(Contact contact)
 * - UpdateContactAsync(Contact contact)
 * - DeleteContactAsync(int id)
 */

    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "Admin")]
    public class ClientController : ControllerBase
    {
        private readonly IClientService _clientService;

        public ClientController(IClientService clientService)
        {
            _clientService = clientService;
        }

        // GET: api/client
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 500);

            var contacts = await _clientService.GetAllContactsAsync(page, pageSize);
            return Ok(contacts);
        }

        // GET: api/client/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var contact = await _clientService.GetContactByIdAsync(id);
            if (contact == null) return NotFound();
            return Ok(contact);
        }

        // POST: api/client
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Contact contact)
        {
            if (contact == null) return BadRequest("Contact data is required");

            contact.UserId = User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

            var id = await _clientService.CreateContactAsync(contact);
            return CreatedAtAction(nameof(GetById), new { id }, contact);
        }

        // PUT: api/client/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Contact contact)
        {
            if (contact == null || id != contact.Id) return BadRequest("Contact ID mismatch");

            await _clientService.UpdateContactAsync(contact);
            return NoContent();
        }

        // DELETE: api/client/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _clientService.DeleteContactAsync(id);
            return NoContent();
        }
    }
}
