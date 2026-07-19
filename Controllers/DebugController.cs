using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ProcessZero.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DebugController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public DebugController(IWebHostEnvironment env)
        {
            _env = env;
        }

        /// <summary>
        /// Returns the current user's JWT claims. Only available in Development.
        /// </summary>
        [HttpGet("claims")]
        [Authorize]
        public IActionResult GetClaims()
        {
            if (!_env.IsDevelopment())
                return NotFound();

            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            return Ok(claims);
        }

        [HttpGet("health")]
        public IActionResult Health() => Ok(new { status = "ok" });
    }
}
