using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProcessZero.Application.Interfaces;

namespace ProcessZero.Web.Controllers
{
    /// <summary>
    /// Provides an export endpoint that extracts all database tables for analytics, backup, or integration purposes.
    /// The extracted data includes all entities tracked by the ApplicationDbContext.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "Admin")]
    public class DataLakeController : ControllerBase
    {
        private readonly IDataLakeService _dataLakeService;

        /// <summary>
        /// Initializes a new instance of the DataLakeController.
        /// </summary>
        /// <param name="dataLakeService">The service responsible for extracting data from all database tables.</param>
        public DataLakeController(IDataLakeService dataLakeService)
        {
            _dataLakeService = dataLakeService;
        }

        /// <summary>
        /// Extracts all data from all database tables registered in the ApplicationDbContext.
        /// Returns a dictionary where keys are table names and values are lists of entity objects.
        /// Useful for data export, analytics pipelines, or external integrations.
        /// </summary>
        /// <returns>An action result containing a dictionary of table names mapped to their row data.</returns>
        [HttpGet("extract")]
        public async Task<IActionResult> Extract()
        {
            var data = await _dataLakeService.ExtractAllTablesAsync();

            return Ok(data);
        }
    }
}
