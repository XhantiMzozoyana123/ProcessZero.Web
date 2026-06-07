using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Application.Interfaces
{
    public interface IExtractService
    {
        /// <summary>
        /// Scrapes business leads from Yellow Pages based on keyword and location.
        /// Extracts detailed information and saves to database, skipping duplicates.
        /// </summary>
        /// <param name="keyword">Search term (e.g., "software developer")</param>
        /// <param name="location">Geographic location (e.g., "New York")</param>
        /// <param name="pages">Number of result pages to scrape</param>
        /// <returns>List of LeadLake entities (newly saved and any existing duplicates)</returns>
        Task<List<LeadLake>> ScrapeAsync(string keyword, string location, int pages = 1);
    }
}
