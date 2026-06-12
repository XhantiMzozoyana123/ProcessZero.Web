using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain.Entities;
using System.Threading.Tasks;

namespace ProcessZero.Web.Controllers
{
    /// <summary>
    /// 1, 2, 3 Testing.
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
    ///   
    /// 
    /// SearchDto fields:
    ///      public class SearchDto
    //    {
    //        public string Keywords { get; set; } = string.Empty;

    //    public int PageViewLimit { get; set; }

    //    public string ContainerUrl { get; set; } = string.Empty;
    //}
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "Admin")]
    public class ExtractController : ControllerBase
    {
        private readonly IExtractService _extractService;

        public ExtractController(IExtractService extractService)
        {
            _extractService = extractService;
        }

        // -----------------------------------------
        // 🔥 BATCH EXTRACTION (MAIN PIPELINE)
        // -----------------------------------------
        [HttpPost("batch")]
        public IActionResult BatchExtract([FromBody] List<SearchDto> batch)
        {
            if (batch == null || batch.Count == 0)
                return BadRequest("Batch cannot be empty");

            _extractService.BatchExtraction(batch);

            return Ok(new
            {
                message = "Batch extraction started successfully",
                count = batch.Count
            });
        }

        // -----------------------------------------
        // ⚡ SINGLE PIPELINE RUN
        // -----------------------------------------
        [HttpPost("run")]
        public async Task<IActionResult> Run([FromBody] SearchDto searchDto)
        {
            if (searchDto == null)
                return BadRequest("Invalid request");

            await _extractService.InitializeExtraction(searchDto);

            return Ok(new
            {
                message = "Extraction completed"
            });
        }

        // -----------------------------------------
        // 🧪 TEST ENDPOINT (DEBUG ONLY)
        // -----------------------------------------
        [HttpGet("health")]
        [AllowAnonymous]
        public IActionResult Health()
        {
            return Ok(new
            {
                status = "Extract API is running",
                time = DateTime.UtcNow
            });
        }
    }
}
