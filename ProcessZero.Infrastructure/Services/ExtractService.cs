using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain;
using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ProcessZero.Infrastructure.Services
{
    /// <summary>
    /// Lead extraction service using HtmlAgilityPack (lightweight HTML parsing).
    /// Scrapes Yellow Pages search results and individual business pages.
    /// 
    /// Workflow:
    /// 1. Search: GET https://www.yellowpages.com/search?search_terms={keyword}&geo_location_terms={location}
    /// 2. Parse: Extract business links from search results using HtmlAgilityPack
    /// 3. Detail: Visit each business page and extract: email, phone, name, location, job, industry
    /// 4. Save: Store to LeadLakes table with duplicate detection by email
    /// 
    /// Performance: ~2-3 seconds per page (vs 15-20 with Playwright)
    /// No browser required - uses lightweight HTTP client
    /// 
    /// Phone Extraction: Handles formats like (929) 333-4233
    /// </summary>
    public class ExtractService : IExtractService
    {
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _httpClient;

        public ExtractService(ApplicationDbContext context, HttpClient httpClient)
        {
            _context = context;
            _httpClient = httpClient;

            // Set user agent to avoid blocking
            _httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36");
        }

        /// <summary>
        /// Scrapes Yellow Pages for leads based on keyword and location.
        /// </summary>
        public async Task<List<LeadLake>> ScrapeAsync(string keyword, string location, int pages)
        {
            var leads = new List<LeadLake>();
            var scrapedUrls = new HashSet<string>();

            // Normalize inputs
            keyword = Uri.EscapeDataString(keyword.Trim());
            location = Uri.EscapeDataString(location.Trim());

            // Constrain pages
            if (pages < 1 || pages > 5)
                pages = 1;

            try
            {
                for (int currentPage = 1; currentPage <= pages; currentPage++)
                {
                    try
                    {
                        // Format: https://www.yellowpages.com/search?search_terms={keyword}&geo_location_terms={location}&page={page}
                        var searchUrl = $"https://www.yellowpages.com/search?search_terms={keyword}&geo_location_terms={location}&page={currentPage}";

                        Console.WriteLine($"📄 Scraping Page {currentPage} | URL: {searchUrl}");

                        var html = await FetchHtmlAsync(searchUrl);
                        if (string.IsNullOrWhiteSpace(html))
                        {
                            Console.WriteLine($"❌ Page {currentPage}: No HTML content retrieved");
                            break;
                        }

                        var doc = new HtmlDocument();
                        doc.LoadHtml(html);

                        // Extract business links: <a class="business-name" href="/...">Business Name</a>
                        var businessNodes = doc.DocumentNode.SelectNodes("//a[contains(@class,'business-name')]");

                        if (businessNodes == null || businessNodes.Count == 0)
                        {
                            Console.WriteLine($"⚠️  Page {currentPage}: No businesses found");
                            break;
                        }

                        Console.WriteLine($"✓ Page {currentPage}: Found {businessNodes.Count} businesses");

                        // Process each business
                        foreach (var node in businessNodes)
                        {
                            try
                            {
                                var href = node.GetAttributeValue("href", "");
                                var businessName = node.InnerText?.Trim();

                                if (string.IsNullOrWhiteSpace(href) || string.IsNullOrWhiteSpace(businessName))
                                    continue;

                                var detailUrl = "https://www.yellowpages.com" + href;

                                // Skip duplicates
                                if (scrapedUrls.Contains(detailUrl))
                                    continue;

                                scrapedUrls.Add(detailUrl);

                                // Scrape detail page
                                var lead = await ScrapeBusinessDetailAsync(detailUrl, businessName, location);
                                if (lead != null)
                                {
                                    leads.Add(lead);
                                    Console.WriteLine($"  ✓ Scraped: {businessName}");
                                }

                                // Random delay to avoid detection
                                await Task.Delay(Random.Shared.Next(300, 800));
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"  ✗ Business extraction error: {ex.Message}");
                            }
                        }

                        Console.WriteLine($"✅ Page {currentPage} Complete | Total Leads: {leads.Count}");

                        // Delay between pages
                        if (currentPage < pages)
                            await Task.Delay(Random.Shared.Next(500, 1500));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Page {currentPage} error: {ex.Message}");
                    }
                }

                // Save to database
                if (leads.Count > 0)
                {
                    await SaveLeadsAsync(leads);
                    Console.WriteLine($"💾 Successfully saved {leads.Count} leads to database");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔴 Fatal error during scraping: {ex.Message}");
            }

            return leads;
        }

        /// <summary>
        /// Scrapes individual business detail page.
        /// Extracts: email, phone (including format like (929) 333-4233), address, services, contact name, etc.
        /// </summary>
        private async Task<LeadLake> ScrapeBusinessDetailAsync(string url, string businessName, string location)
        {
            try
            {
                var html = await FetchHtmlAsync(url);
                if (string.IsNullOrWhiteSpace(html))
                    return null;

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // Extract contact information
                var email = ExtractEmail(html);
                var phone = ExtractPhone(html);  // Extracts (929) 333-4233 format

                // Extract address from span.full element or similar
                var addressNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'address')]");
                var address = addressNode?.InnerText?.Trim() ?? "";

                // Extract services/description
                var servicesNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'services')]");
                var services = servicesNode?.InnerText?.Trim() ?? "";

                // Extract contact person (if available)
                var contactNode = doc.DocumentNode.SelectSingleNode("//span[contains(@class,'person-name')]");
                var contactName = contactNode?.InnerText?.Trim() ?? "";

                // Parse name
                var (firstName, lastName) = ParseFullName(contactName);

                // Infer industry and job
                var industry = InferIndustry(businessName, services);
                var job = ExtractJobTitle(services);

                return new LeadLake
                {
                    Company = businessName,
                    Email = email,
                    Phone = CleanPhone(phone),
                    Location = location,
                    FirstName = firstName,
                    LastName = lastName,
                    Job = job,
                    Industry = industry,
                    Intent = LeadIntent.Medium,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Detail scrape error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Fetches HTML content from URL with error handling.
        /// </summary>
        private async Task<string> FetchHtmlAsync(string url)
        {
            try
            {
                var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseContentRead);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"🌐 HTTP error fetching {url}: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching {url}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Extracts email from HTML using regex.
        /// Pattern: name@domain.com
        /// </summary>
        private string ExtractEmail(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return "";

            try
            {
                // Match email pattern
                var emailPattern = @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}";
                var match = Regex.Match(html, emailPattern);

                return match.Success ? match.Value : "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Extracts phone number from HTML using regex.
        /// Handles formats:
        /// - (929) 333-4233
        /// - (929)333-4233
        /// - 929-333-4233
        /// - 929.333.4233
        /// - +1-929-333-4233
        /// </summary>
        private string ExtractPhone(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return "";

            try
            {
                // This regex handles: (929) 333-4233, (929)333-4233, 929-333-4233, etc.
                // Looks for: optional (XXX), then XXX, then separator, then XXXX
                var phonePattern = @"\(?\d{3}\)?[\s\-\.]?\d{3}[\s\-\.]?\d{4}";
                var match = Regex.Match(html, phonePattern);

                if (match.Success)
                    return match.Value;

                // Also check for span.full format like: <span class="full">(929) 333-4233</span>
                var spanPattern = @"<span\s+class=[""']full[""']\s*>([^<]+)<";
                var spanMatch = Regex.Match(html, spanPattern);
                if (spanMatch.Success)
                {
                    var phoneText = spanMatch.Groups[1].Value.Trim();
                    // Extract just the phone portion from the span
                    var innerPhoneMatch = Regex.Match(phoneText, phonePattern);
                    if (innerPhoneMatch.Success)
                        return innerPhoneMatch.Value;
                }

                return "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Cleans phone number by removing "Phone:" prefix and extra whitespace.
        /// </summary>
        private string CleanPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return "";

            // Remove "Phone:" prefix if present
            phone = Regex.Replace(phone, @"^Phone:\s*", "", RegexOptions.IgnoreCase);

            // Trim whitespace
            return phone.Trim();
        }

        /// <summary>
        /// Parses full name into first and last name.
        /// </summary>
        private (string firstName, string lastName) ParseFullName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return ("", "");

            var parts = fullName.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
                return ("", "");

            if (parts.Length == 1)
                return (parts[0], "");

            return (parts[0], parts[parts.Length - 1]);
        }

        /// <summary>
        /// Extracts job title from services/description text.
        /// </summary>
        private string ExtractJobTitle(string services)
        {
            if (string.IsNullOrWhiteSpace(services))
                return "Business Owner";

            var titles = new[] 
            { 
                "Manager", "Director", "Consultant", "Specialist", "Owner", 
                "CEO", "President", "Vice President", "Accountant", "Developer", 
                "Engineer", "Technician", "Coordinator", "Administrator"
            };

            foreach (var title in titles)
            {
                if (services.Contains(title, StringComparison.OrdinalIgnoreCase))
                    return title;
            }

            return "Business Owner";
        }

        /// <summary>
        /// Infers industry from business name and services.
        /// </summary>
        private LeadLakeIndustry InferIndustry(string businessName, string services)
        {
            var combined = (businessName + " " + services).ToLower();

            if (combined.Contains("technology") || combined.Contains("software") || 
                combined.Contains("developer") || combined.Contains("programmer") || combined.Contains("it "))
                return LeadLakeIndustry.Technology;

            if (combined.Contains("finance") || combined.Contains("bank") || 
                combined.Contains("accounting") || combined.Contains("accountant") || 
                combined.Contains("insurance") || combined.Contains("broker"))
                return LeadLakeIndustry.Finance;

            if (combined.Contains("healthcare") || combined.Contains("medical") || 
                combined.Contains("doctor") || combined.Contains("hospital") || combined.Contains("clinic"))
                return LeadLakeIndustry.Healthcare;

            if (combined.Contains("education") || combined.Contains("school") || 
                combined.Contains("university") || combined.Contains("training") || combined.Contains("tutor"))
                return LeadLakeIndustry.Education;

            if (combined.Contains("retail") || combined.Contains("store") || 
                combined.Contains("shop") || combined.Contains("commerce"))
                return LeadLakeIndustry.Retail;

            if (combined.Contains("manufacturing") || combined.Contains("factory") || 
                combined.Contains("production") || combined.Contains("industrial"))
                return LeadLakeIndustry.Manufacturing;

            if (combined.Contains("energy") || combined.Contains("utility") || 
                combined.Contains("power") || combined.Contains("oil"))
                return LeadLakeIndustry.Energy;

            if (combined.Contains("transportation") || combined.Contains("logistics") || 
                combined.Contains("shipping") || combined.Contains("delivery"))
                return LeadLakeIndustry.Transportation;

            if (combined.Contains("entertainment") || combined.Contains("media") || 
                combined.Contains("music") || combined.Contains("film"))
                return LeadLakeIndustry.Entertainment;

            if (combined.Contains("hospitality") || combined.Contains("hotel") || 
                combined.Contains("restaurant") || combined.Contains("catering") || combined.Contains("cafe"))
                return LeadLakeIndustry.Hospitality;

            return LeadLakeIndustry.Other;
        }

        /// <summary>
        /// Saves leads to database with duplicate detection by email.
        /// </summary>
        private async Task SaveLeadsAsync(List<LeadLake> leads)
        {
            if (leads == null || leads.Count == 0)
                return;

            int savedCount = 0;
            foreach (var lead in leads)
            {
                try
                {
                    // Skip if minimum identifiers missing
                    if (string.IsNullOrWhiteSpace(lead.Email) && string.IsNullOrWhiteSpace(lead.Company))
                        continue;

                    // Duplicate detection by email
                    if (!string.IsNullOrWhiteSpace(lead.Email))
                    {
                        var exists = await _context.LeadLakes
                            .AsNoTracking()
                            .AnyAsync(l => l.Email == lead.Email);

                        if (exists)
                            continue;
                    }

                    _context.LeadLakes.Add(lead);
                    savedCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error adding lead: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();
            Console.WriteLine($"✅ {savedCount} new leads saved to database");
        }
    }
}
