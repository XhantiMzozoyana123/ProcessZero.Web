using DnsClient;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using ProcessZero.Application.Constants;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain;
using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TagLib.Asf;
using YoutubeExplode;
using YouTubeSearch;

namespace ProcessZero.Infrastructure.Services
{
    public class ExtractService : IExtractService
    {
        private readonly ApplicationDbContext _context;
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAIExtractorService _aiExtractService;
        public ExtractService(ApplicationDbContext context, 
            IDbContextFactory<ApplicationDbContext> contextFactory, 
            IAIExtractorService aiExtractService,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _contextFactory = contextFactory;
            _httpClientFactory = httpClientFactory;
            _aiExtractService = aiExtractService;
        }

        public async Task BatchExtraction(List<SearchDto> batchSearchDtos)
        {
            for (int i = 0; i < batchSearchDtos.Count; i++)
            {
                var item = batchSearchDtos[i];

                SearchDto searchDto = new SearchDto()
                {
                    Keywords = item.Keywords,
                    PageViewLimit = item.PageViewLimit,
                    ContainerUrl = item.ContainerUrl
                };

                // Run the scrapper...
                await InitializeExtraction(searchDto);
            }
        }

        public async Task InitializeExtraction(SearchDto searchDto)
        {
            try
            {                
                VideoSearch videoSearch = new VideoSearch();
                var query = await videoSearch.GetVideos(searchDto.Keywords, searchDto.PageViewLimit);

                for (int i = 0; i < query.Count; i++)
                {
                    try
                    {
                        string videoUrl = query[i].getUrl();
                        string description = await GetYouTubeVideoDescription(videoUrl);

                        string emailFound = ExtractEmails(description);
                        string phoneFound = ExtractPhoneNumbers(description);

                        if (string.IsNullOrWhiteSpace(emailFound))
                            continue;

                        // 1. Create the context from the factory
                        using var context = await _contextFactory.CreateDbContextAsync();

                        // 2. Use the LOCAL 'context', NOT the class-level '_context'
                        var existingLead = await context.LeadLakes
                            .FirstOrDefaultAsync(x => x.Email == emailFound);

                        if (existingLead != null)
                            continue;

                        var validateEmail = await EmailValidator(emailFound);
                        if (!validateEmail)
                            continue;

                        var subtitles = await GetYouTubeVideoSubtitles(videoUrl);

                        var leadlake = new LeadLake
                        {
                            UserId = string.Empty,
                            FirstName = await _aiExtractService.GetFirstNameAsync(description, subtitles),
                            LastName = await _aiExtractService.GetLastNameAsync(description, subtitles),
                            Email = emailFound,
                            Phone = phoneFound,
                            Company = await _aiExtractService.GetCompanyAsync(description, subtitles),
                            Job = await _aiExtractService.GetJobTitleAsync(description, subtitles),
                            Location = await _aiExtractService.GetLocationAsync(description, subtitles),
                            Industry = await _aiExtractService.GetIndustryAsync(description, subtitles),
                            Intent = await _aiExtractService.GetIntentAsync(description, subtitles),
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        // 3. IMPORTANT: Add to the local 'context' instance
                        context.LeadLakes.Add(leadlake);

                        // 4. Save using the same local context
                        await context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Video processing error: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Extraction error: {ex.Message}");
            }
        }

        private string ExtractEmails(string html)
        {
            try
            {
                Regex reg = new Regex(@"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,6}", RegexOptions.IgnoreCase);

                var results = reg.Matches(html)
                    .Select(m => m.Value)
                    .Distinct()
                    .ToList();

                return results.FirstOrDefault() ?? "";
            }
            catch
            {
                return "";
            }
        }

        private string ExtractPhoneNumbers(string html)
        {
            try
            {
                Regex reg = new Regex(@"\+?\d[\d -]{8,}\d");

                var results = reg.Matches(html)
                    .Select(m => m.Value)
                    .Distinct()
                    .ToList();

                return results.FirstOrDefault() ?? "";
            }
            catch
            {
                return "";
            }
        }

        private async Task<bool> EmailValidator(string emailFound)
        {
            try
            {
                // Step 1: Validate email format
                var addr = new MailAddress(emailFound);
                if (addr.Address != emailFound) return false;

                // Step 2: Check MX records
                string domain = emailFound.Split('@')[1];
                var lookup = new LookupClient();
                var result = await lookup.QueryAsync(domain, QueryType.MX);
                var mxRecords = result.Answers.MxRecords().Count();
                return mxRecords > 0;
            }
            catch
            {
                return false;
            }
        }

        private async Task<string> GetYouTubeVideoDescription(string videoUrl)
        {
            var youtube = new YoutubeClient();

            var video = await youtube.Videos.GetAsync(videoUrl);

            return video.Description;
        }

        private async Task<string> GetYouTubeVideoSubtitles(string videoUrl)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();

                string url = $"http://localhost:8000/transcript?url={videoUrl}";

                string result = await client.GetStringAsync(url);

                return result;
            }
            catch(Exception ex)
            {
                return string.Empty;
            }
        }

        private static async Task<string> GetFullNameofEmailAddressOwner(string input)
        {
            try
            {
                string email = input.Trim().ToLower();

                using var md5 = MD5.Create();
                byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(email));

                string hash = BitConverter.ToString(hashBytes)
                    .Replace("-", "")
                    .ToLower();

                string gravatarUrl = $"https://www.gravatar.com/{hash}.json";
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., log the error)
            }
            return string.Empty;
        }
    }
}
