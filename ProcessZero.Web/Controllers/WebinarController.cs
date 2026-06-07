using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProcessZero.Web.Controllers
{
    /// <summary>
    /// Webinar Controller
    /// 
    /// SYSTEM PURPOSE:
    /// Evergreen training library for Process Zero.
    /// 
    /// ROLE MODEL:
    /// - Admin:
    ///   - Creates webinars (uploads training content)
    ///   - Updates webinars (keeps content current)
    ///   - Deletes webinars (removes outdated content)
    /// 
    /// - Sales Reps / Users:
    ///   - Read-only access to watch training content
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WebinarController : ControllerBase
    {
        private readonly IWebinarService _webinarService;

        public WebinarController(IWebinarService webinarService)
        {
            _webinarService = webinarService;
        }

        private string GetUserId() => User?.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier) ?? string.Empty;

        /// <summary>
        /// Sales reps and admins can view all training webinars.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Webinar>>> GetAll()
        {
            var webinars = await _webinarService.GetAllAsync();
            return Ok(webinars);
        }

        /// <summary>
        /// Sales reps and admins can view a single webinar.
        /// </summary>
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<Webinar>> GetById(int id)
        {
            var webinar = await _webinarService.GetByIdAsync(id);

            if (webinar == null)
                return NotFound();

            return Ok(webinar);
        }

        /// <summary>
        /// ADMIN ONLY
        /// Creates a new evergreen training webinar.
        /// Admin uploads:
        /// - Sales training videos
        /// - Product walkthroughs
        /// - Feature updates
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> Create([FromBody] Webinar webinar)
        {
            if (webinar == null) return BadRequest("Webinar is required.");

            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            webinar.UserId = userId;
            await _webinarService.CreateAsync(webinar);

            if (webinar.Id != 0)
                return CreatedAtAction(nameof(GetById), new { id = webinar.Id }, webinar);

            return NoContent();
        }

        /// <summary>
        /// ADMIN ONLY
        /// Updates an existing webinar (content maintenance).
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] Webinar webinar)
        {
            if (webinar == null) return BadRequest("Webinar is required.");
            if (webinar.Id != id) return BadRequest("Id mismatch.");

            await _webinarService.UpdateAsync(webinar);
            return NoContent();
        }

        /// <summary>
        /// ADMIN ONLY
        /// Deletes outdated or replaced webinar content.
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            await _webinarService.DeleteAsync(id);
            return NoContent();
        }
    }
}