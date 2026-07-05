using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProcessZero.Application.Interfaces;

namespace ProcessZero.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LLMController : ControllerBase
    {
        private readonly ILLMService _llmService;

        public LLMController(ILLMService llmService)
        {
            _llmService = llmService;
        }

        /// <summary>
        /// Generates text based on the provided prompt using the LLM service.
        /// </summary>
        /// <param name="prompt">The prompt to generate text from</param>
        /// <returns>Generated text response</returns>
        [HttpPost("generate-text")]
        public async Task<IActionResult> GenerateText([FromBody] GenerateTextRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Prompt))
                return BadRequest("Prompt is required");

            try
            {
                var result = await _llmService.GenerateTextAsync(request.Prompt);
                return Ok(new { text = result });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { error = ex.Message });
            }
        }
    }

    /// <summary>
    /// Request model for generating text via LLM
    /// </summary>
    public class GenerateTextRequest
    {
        public string Prompt { get; set; }
    }
}
