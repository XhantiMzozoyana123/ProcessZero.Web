using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using System.Security.Claims;

namespace ProcessZero.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    /// <summary>
    /// Controller responsible for scheduling and managing meetings.
    ///
    /// Models and DTOs used (read from project files):
    ///
    /// MeetingDto (ProcessZero.Application.Dtos.MeetingDto):
    /// - UserId (string)
    /// - Contact (ProcessZero.Domain.Entities.Contact)
    /// - Product (ProcessZero.Domain.Entities.Product)
    /// - Meeting (ProcessZero.Domain.Entities.Meeting)
    ///
    /// Meeting (ProcessZero.Domain.Entities.Meeting) inherits BaseEntity and contains:
    /// - Id (int) [from BaseEntity]
    /// - UserId (string) [from BaseEntity]
    /// - CreatedAt (DateTime) [from BaseEntity]
    /// - UpdatedAt (DateTime) [from BaseEntity]
    /// - ClientId (int)
    /// - ProductId (int)
    /// - MeetingDate (DateTime)
    /// - Notes (string)
    ///
    /// Contact (from ProcessZero.Domain.Entities.Contact):
    /// - FirstName, LastName, Email, Phone, Company, Job, Location, Status (ContactStatus enum)
    ///
    /// Product (from ProcessZero.Domain.Entities.Product):
    /// - Name, Description, Url, Amount (decimal)
    ///
    /// Service: IMeetingService exposes methods used by this controller:
    /// - Task AddMeetingAsync(MeetingDto meetingDto)
    /// - Task<MeetingDto> GetMeetingByIdAsync(int id)
    /// - Task<List<MeetingDto>> GetAllMeetingsAsync()
    /// - Task<List<MeetingDto>> GetAllMeetingsByUserIdAsync(string userId)
    /// - Task UpdateMeetingAsync(MeetingDto meetingDto)
    /// - Task DeleteMeetingAsync(int id)
    /// </summary>
    public class MeetingController : ControllerBase
    {
        private readonly IMeetingService _meetingService;

        /// <summary>
        /// Constructor. Requires an <see cref="IMeetingService"/> provided by DI.
        /// </summary>
        /// <param name="meetingService">Service that performs meeting-related operations.</param>
        public MeetingController(IMeetingService meetingService)
        {
            _meetingService = meetingService;
        }

        /// <summary>
        /// Helper to extract the authenticated user's id from JWT claims.
        /// Returns empty string if not present.
        /// </summary>
        private string GetUserId()
            => User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        /// <summary>
        /// GET: api/meeting
        /// Returns a list of all meetings. Typically used by admin or internal tools.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 500);

            var meetings = await _meetingService.GetAllMeetingsAsync(page, pageSize);
            return Ok(meetings);
        }

        /// <summary>
        /// GET: api/meeting/user
        /// Returns meetings for the authenticated user. Requires the user to be authenticated.
        /// </summary>
        [HttpGet("user")]
        public async Task<IActionResult> GetAllByUser()
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var meetings = await _meetingService.GetAllMeetingsByUserIdAsync(userId);
            return Ok(meetings);
        }

        /// <summary>
        /// GET: api/meeting/{id}
        /// Returns a single meeting by id.
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var meeting = await _meetingService.GetMeetingByIdAsync(id);
            if (meeting == null) return NotFound();
            return Ok(meeting);
        }

        /// <summary>
        /// POST: api/meeting
        /// Creates a new meeting. The request body must be a <see cref="MeetingDto"/> containing
        /// related entities and a <see cref="Domain.Entities.Meeting"/> object. The controller
        /// assigns the authenticated user's id to the meeting before delegating to the service.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] MeetingDto meetingDto, [FromQuery] string? notes = null)
        {
            if (meetingDto == null || meetingDto.Meeting == null) return BadRequest();
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            // ensure meeting is assigned to the current user
            meetingDto.Meeting.UserId = userId;

            await _meetingService.AddMeetingAsync(meetingDto, notes ?? string.Empty);

            // Service doesn't return created id; return NoContent to indicate success.
            return NoContent();
        }

        /// <summary>
        /// PUT: api/meeting/{id}
        /// Updates a meeting. Restricted to Admins by policy. Validates that the payload id matches the route id.
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] MeetingDto meetingDto, [FromQuery] string? notes = null)
        {
            if (meetingDto == null || meetingDto.Meeting == null) return BadRequest();
            if (meetingDto.Meeting.Id != id) return BadRequest("Id mismatch");

            await _meetingService.UpdateMeetingAsync(meetingDto, notes ?? string.Empty);
            return NoContent();
        }

        // DELETE: api/meeting/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, [FromQuery] string? notes = null)
        {
            await _meetingService.DeleteMeetingAsync(id, notes ?? string.Empty);
            return NoContent();
        }
    }
}
