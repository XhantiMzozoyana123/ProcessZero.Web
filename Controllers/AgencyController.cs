using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain;
using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ProcessZero.Web.Controllers
{
    /// <summary>
    /// Agency profile + Agency CRM endpoints.
    ///
    /// Access model:
    /// - Profile management (create/update/delete/managers/password) is Admin-only.
    /// - Any authenticated user can read CRM data for an agency they can view
    ///   (Admin, the agency's own login account, or a manager granted access by the Admin).
    /// - Write operations (create/update/delete CRM records) are only allowed for
    ///   Admin, the agency's own login, or granted managers — everyone else is read-only (403).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AgencyController : ControllerBase
    {
        private readonly IAgencyService _agencyService;
        private readonly ApplicationDbContext _db;

        public AgencyController(IAgencyService agencyService, ApplicationDbContext db)
        {
            _agencyService = agencyService ?? throw new ArgumentNullException(nameof(agencyService));
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        private string GetUserId() => User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        // ─────────────────────────────────────────────────────
        // Profile management (Admin only)
        // ─────────────────────────────────────────────────────

        /// <summary>Lists all agency profiles. Admin only.</summary>
        [HttpGet]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> GetAll() => Ok(await _agencyService.GetAllAsync());

        /// <summary>Creates a new agency profile + its login account, and assigns managers. Admin only.</summary>
        [HttpPost]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateAgencyDto dto)
        {
            try
            {
                var agency = await _agencyService.CreateAsync(dto);
                return Ok(agency);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>Gets a single agency profile (with managers). Requires view access.</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var agency = await _agencyService.GetByIdAsync(id);
            if (agency is null) return NotFound();
            if (!await _agencyService.CanViewAsync(id, GetUserId())) return Forbid();
            return Ok(agency);
        }

        /// <summary>Updates name/description/active flag and replaces the manager list. Admin only.</summary>
        [HttpPut("{id:int}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateAgencyDto dto)
        {
            try
            {
                return Ok(await _agencyService.UpdateAsync(id, dto));
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>Deletes the agency profile and its linked login account. Admin only.</summary>
        [HttpDelete("{id:int}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _agencyService.DeleteAsync(id);
                return Ok(new { message = "Agency deleted." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>Resets the agency login account's password. Admin only.</summary>
        [HttpPost("{id:int}/reset-password")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetAgencyPasswordDto dto)
        {
            try
            {
                await _agencyService.ResetPasswordAsync(id, dto?.Password ?? string.Empty);
                return Ok(new { message = "Password reset successful." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>Agencies visible to the current user (own agency / managed agencies / all for admin).</summary>
        [HttpGet("my")]
        public async Task<IActionResult> MyAgencies() => Ok(await _agencyService.GetForUserAsync(GetUserId()));

        /// <summary>Access info for the current user on an agency: whether they may view and whether they may write.</summary>
        [HttpGet("{id:int}/access")]
        public async Task<IActionResult> GetAccess(int id)
        {
            var canView = await _agencyService.CanViewAsync(id, GetUserId());
            if (!canView) return Forbid();
            return Ok(new { canView, canWrite = await _agencyService.CanWriteAsync(id, GetUserId()) });
        }

        // ─────────────────────────────────────────────────────
        // Agency CRM — Contacts
        // ─────────────────────────────────────────────────────

        /// <summary>Lists all contacts in the agency CRM.</summary>
        [HttpGet("{id:int}/contacts")]
        public async Task<IActionResult> GetContacts(int id)
        {
            if (!await _agencyService.CanViewAsync(id, GetUserId())) return Forbid();
            var contacts = await _db.AgencyContacts
                .Where(c => c.AgencyProfileId == id)
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();
            return Ok(contacts);
        }

        /// <summary>Creates a contact in the agency CRM. Requires write access.</summary>
        [HttpPost("{id:int}/contacts")]
        public async Task<IActionResult> CreateContact(int id, [FromBody] AgencyContact contact)
        {
            if (!await _agencyService.CanWriteAsync(id, GetUserId()))
                return StatusCode(403, new { error = "You have read-only access to this agency." });

            if (contact is null || string.IsNullOrWhiteSpace(contact.Name))
                return BadRequest(new { error = "Contact name is required." });

            contact.Id = 0;
            contact.AgencyProfileId = id;
            contact.CreatedAt = DateTime.UtcNow;
            contact.UpdatedAt = DateTime.UtcNow;
            _db.AgencyContacts.Add(contact);
            await _db.SaveChangesAsync();
            return Ok(contact);
        }

        /// <summary>Updates a contact. Requires write access.</summary>
        [HttpPut("{id:int}/contacts/{contactId:int}")]
        public async Task<IActionResult> UpdateContact(int id, int contactId, [FromBody] AgencyContact contact)
        {
            if (!await _agencyService.CanWriteAsync(id, GetUserId()))
                return StatusCode(403, new { error = "You have read-only access to this agency." });

            var existing = await _db.AgencyContacts.FirstOrDefaultAsync(c => c.Id == contactId && c.AgencyProfileId == id);
            if (existing is null) return NotFound();

            existing.Name = contact?.Name ?? existing.Name;
            existing.Company = contact?.Company;
            existing.Email = contact?.Email;
            existing.Phone = contact?.Phone;
            existing.Position = contact?.Position;
            existing.Notes = contact?.Notes;
            existing.IsCompany = contact?.IsCompany ?? existing.IsCompany;
            existing.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(existing);
        }

        /// <summary>Deletes a contact (and its deals, via cascade). Requires write access.</summary>
        [HttpDelete("{id:int}/contacts/{contactId:int}")]
        public async Task<IActionResult> DeleteContact(int id, int contactId)
        {
            if (!await _agencyService.CanWriteAsync(id, GetUserId()))
                return StatusCode(403, new { error = "You have read-only access to this agency." });

            var existing = await _db.AgencyContacts.FirstOrDefaultAsync(c => c.Id == contactId && c.AgencyProfileId == id);
            if (existing is null) return NotFound();
            _db.AgencyContacts.Remove(existing);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Contact deleted." });
        }

        // ─────────────────────────────────────────────────────
        // Agency CRM — Deals (pipeline)
        // ─────────────────────────────────────────────────────

        /// <summary>Lists all deals in the agency CRM (with contact names).</summary>
        [HttpGet("{id:int}/deals")]
        public async Task<IActionResult> GetDeals(int id)
        {
            if (!await _agencyService.CanViewAsync(id, GetUserId())) return Forbid();
            var deals = await _db.AgencyDeals
                .Include(d => d.Contact)
                .Where(d => d.AgencyProfileId == id)
                .OrderByDescending(d => d.UpdatedAt)
                .Select(d => new
                {
                    d.Id,
                    d.AgencyProfileId,
                    d.ContactId,
                    ContactName = d.Contact.Name,
                    d.Title,
                    d.Amount,
                    d.Stage,
                    d.Probability,
                    d.ExpectedCloseDate,
                    d.Notes,
                    d.CreatedAt,
                    d.UpdatedAt
                })
                .ToListAsync();
            return Ok(deals);
        }

        /// <summary>Creates a deal in the pipeline. Requires write access.</summary>
        [HttpPost("{id:int}/deals")]
        public async Task<IActionResult> CreateDeal(int id, [FromBody] AgencyDeal deal)
        {
            if (!await _agencyService.CanWriteAsync(id, GetUserId()))
                return StatusCode(403, new { error = "You have read-only access to this agency." });

            if (deal is null || string.IsNullOrWhiteSpace(deal.Title))
                return BadRequest(new { error = "Deal title is required." });
            if (deal.ContactId <= 0)
                return BadRequest(new { error = "A contact is required for the deal." });

            var contact = await _db.AgencyContacts.FirstOrDefaultAsync(c => c.Id == deal.ContactId && c.AgencyProfileId == id);
            if (contact is null) return BadRequest(new { error = "Contact does not belong to this agency." });

            deal.Id = 0;
            deal.AgencyProfileId = id;
            deal.Stage = AgencyDealStage.All.Contains(deal.Stage) ? deal.Stage : AgencyDealStage.New;
            deal.CreatedByUserId = GetUserId();
            deal.CreatedAt = DateTime.UtcNow;
            deal.UpdatedAt = DateTime.UtcNow;
            _db.AgencyDeals.Add(deal);
            await _db.SaveChangesAsync();
            return Ok(deal);
        }

        /// <summary>Updates a deal (e.g. moving it between pipeline stages). Requires write access.</summary>
        [HttpPut("{id:int}/deals/{dealId:int}")]
        public async Task<IActionResult> UpdateDeal(int id, int dealId, [FromBody] AgencyDeal deal)
        {
            if (!await _agencyService.CanWriteAsync(id, GetUserId()))
                return StatusCode(403, new { error = "You have read-only access to this agency." });

            var existing = await _db.AgencyDeals.FirstOrDefaultAsync(d => d.Id == dealId && d.AgencyProfileId == id);
            if (existing is null) return NotFound();

            if (deal is null) return BadRequest(new { error = "Payload is required." });

            existing.Title = deal.Title ?? existing.Title;
            if (deal.ContactId > 0 && deal.ContactId != existing.ContactId)
            {
                var contact = await _db.AgencyContacts.FirstOrDefaultAsync(c => c.Id == deal.ContactId && c.AgencyProfileId == id);
                if (contact is null) return BadRequest(new { error = "Contact does not belong to this agency." });
                existing.ContactId = deal.ContactId;
            }
            existing.Amount = deal.Amount;
            if (AgencyDealStage.All.Contains(deal.Stage)) existing.Stage = deal.Stage;
            existing.Probability = deal.Probability;
            existing.ExpectedCloseDate = deal.ExpectedCloseDate;
            existing.Notes = deal.Notes;
            existing.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(existing);
        }

        /// <summary>Deletes a deal. Requires write access.</summary>
        [HttpDelete("{id:int}/deals/{dealId:int}")]
        public async Task<IActionResult> DeleteDeal(int id, int dealId)
        {
            if (!await _agencyService.CanWriteAsync(id, GetUserId()))
                return StatusCode(403, new { error = "You have read-only access to this agency." });

            var existing = await _db.AgencyDeals.FirstOrDefaultAsync(d => d.Id == dealId && d.AgencyProfileId == id);
            if (existing is null) return NotFound();
            _db.AgencyDeals.Remove(existing);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Deal deleted." });
        }

        // ─────────────────────────────────────────────────────
        // Agency CRM — Activities (timeline)
        // ─────────────────────────────────────────────────────

        /// <summary>Lists the agency CRM timeline (all activities, newest first).</summary>
        [HttpGet("{id:int}/activities")]
        public async Task<IActionResult> GetActivities(int id)
        {
            if (!await _agencyService.CanViewAsync(id, GetUserId())) return Forbid();
            var activities = await _db.AgencyActivities
                .Where(a => a.AgencyProfileId == id)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new
                {
                    a.Id,
                    a.AgencyProfileId,
                    a.Type,
                    a.Subject,
                    a.Description,
                    a.ContactId,
                    ContactName = a.Contact != null ? a.Contact.Name : null,
                    a.DealId,
                    DealTitle = a.Deal != null ? a.Deal.Title : null,
                    a.DueDate,
                    a.IsCompleted,
                    a.CreatedByUserId,
                    a.CreatedAt
                })
                .ToListAsync();
            return Ok(activities);
        }

        /// <summary>Logs an activity (note/call/meeting/task/email) on the timeline. Requires write access.</summary>
        [HttpPost("{id:int}/activities")]
        public async Task<IActionResult> CreateActivity(int id, [FromBody] AgencyActivity activity)
        {
            if (!await _agencyService.CanWriteAsync(id, GetUserId()))
                return StatusCode(403, new { error = "You have read-only access to this agency." });

            if (activity is null || string.IsNullOrWhiteSpace(activity.Subject))
                return BadRequest(new { error = "Activity subject is required." });

            activity.Id = 0;
            activity.AgencyProfileId = id;
            activity.CreatedByUserId = GetUserId();
            activity.CreatedAt = DateTime.UtcNow;
            _db.AgencyActivities.Add(activity);
            await _db.SaveChangesAsync();
            return Ok(activity);
        }

        /// <summary>Updates an activity (e.g. completing a task). Requires write access.</summary>
        [HttpPut("{id:int}/activities/{activityId:int}")]
        public async Task<IActionResult> UpdateActivity(int id, int activityId, [FromBody] AgencyActivity activity)
        {
            if (!await _agencyService.CanWriteAsync(id, GetUserId()))
                return StatusCode(403, new { error = "You have read-only access to this agency." });

            var existing = await _db.AgencyActivities.FirstOrDefaultAsync(a => a.Id == activityId && a.AgencyProfileId == id);
            if (existing is null) return NotFound();

            existing.Subject = activity?.Subject ?? existing.Subject;
            existing.Description = activity?.Description;
            existing.DueDate = activity?.DueDate ?? existing.DueDate;
            existing.IsCompleted = activity?.IsCompleted ?? existing.IsCompleted;
            await _db.SaveChangesAsync();
            return Ok(existing);
        }

        /// <summary>Deletes an activity. Requires write access.</summary>
        [HttpDelete("{id:int}/activities/{activityId:int}")]
        public async Task<IActionResult> DeleteActivity(int id, int activityId)
        {
            if (!await _agencyService.CanWriteAsync(id, GetUserId()))
                return StatusCode(403, new { error = "You have read-only access to this agency." });

            var existing = await _db.AgencyActivities.FirstOrDefaultAsync(a => a.Id == activityId && a.AgencyProfileId == id);
            if (existing is null) return NotFound();
            _db.AgencyActivities.Remove(existing);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Activity deleted." });
        }
        public class ResetAgencyPasswordDto
        {
            public string Password { get; set; } = string.Empty;
        }
    }
}