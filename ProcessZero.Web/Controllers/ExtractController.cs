using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain.Entities;
using System.Threading.Tasks;

namespace ProcessZero.Web.Controllers
{
    /// <summary>
    /// Admin-only controller for web scraping lead data from Yellow Pages.
    /// Extracts business information and saves to LeadLake database.
    /// 
    /// Entities Involved:
    ///   - LeadLake: Target entity for scraped lead data
    ///     * Fields: FirstName, LastName, Email, Phone, Company, Job, Location, Industry, Intent, CreatedAt, UpdatedAt
    ///   - LeadLakeIndustry: Enum (Technology, Finance, Healthcare, Education, Retail, Manufacturing, Energy, Transportation, Entertainment, Hospitality, Other)
    ///   - LeadIntent: Enum (High, Medium, Low)
    /// 
    /// Database Operations:
    ///   1. On Scrape:
    ///      - SELECT * FROM LeadLakes WHERE Email = scrape.Email (duplicate check)
    ///      - INSERT INTO LeadLakes if new, SKIP if exists
    ///   2. Return: List of LeadLake entities (both new and existing)
    /// 
    /// Workflow:
    ///   Admin → POST /api/extract/scrape → Yellow Pages search → Parse business details → 
    ///   Infer industry/job → Check for duplicates → Save to database → Return results
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "Admin")]
    public class ExtractController : ControllerBase
    {
        private readonly IExtractService _extractService;
        private readonly ILogger<ExtractController> _logger;

        public ExtractController(IExtractService extractService, ILogger<ExtractController> logger)
        {
            _extractService = extractService;
            _logger = logger;
        }

        /// <summary>
        /// Scrapes business leads from Yellow Pages based on keyword and location.
        /// Extracts: name, email, phone, location, job title, and inferred industry.
        /// Automatically saves new leads to the database and skips duplicates.
        /// </summary>
        /// <param name="keyword">Search term (e.g., "software developer", "accountant")</param>
        /// <param name="location">Geographic location (e.g., "New York", "San Francisco")</param>
        /// <param name="pages">Number of result pages to scrape (default: 1, max: 5)</param>
        /// <response code="200">Scraping successful; returns list of leads (newly saved and skipped duplicates)</response>
        /// <response code="400">Invalid input parameters</response>
        /// <response code="401">User not authenticated or not admin</response>
        /// <response code="500">Scraping error or database save failure</response>
        [HttpPost("scrape")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ScrapeLeads(
            [FromQuery] string keyword,
            [FromQuery] string location,
            [FromQuery] int pages = 1,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(keyword))
                    return BadRequest("Keyword cannot be empty");

                if (string.IsNullOrWhiteSpace(location))
                    return BadRequest("Location cannot be empty");

                // Constrain pages to reasonable limits
                if (pages < 1 || pages > 5)
                    pages = 1;

                _logger.LogInformation($"Starting scrape: keyword='{keyword}', location='{location}', pages={pages}");

                // Scrape and save leads
                var leads = await _extractService.ScrapeAsync(keyword, location, pages);

                if (leads == null || leads.Count == 0)
                {
                    _logger.LogWarning($"No leads found for keyword='{keyword}', location='{location}'");
                    return Ok(new { message = "No leads found", leads = new List<LeadLake>() });
                }

                _logger.LogInformation($"Successfully scraped {leads.Count} leads");

                return Ok(new
                {
                    message = $"Successfully scraped {leads.Count} leads",
                    leads = leads
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error during scraping");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    $"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during scraping");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    $"Error during scraping: {ex.Message}");
            }
        }

        /// <summary>
        /// Health check endpoint for the extract service.
        /// </summary>
        /// <response code="200">Service is operational</response>
        [HttpGet("health")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Health()
        {
            return Ok(new { status = "healthy", service = "ExtractService" });
        }
    }
}
