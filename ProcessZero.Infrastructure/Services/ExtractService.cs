using DnsClient;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using ProcessZero.Application.Constants;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain;
using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Mail;
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
        private readonly HttpClient _httpClient;

        private readonly IAIExtractorService _aiExtractService;
        public ExtractService(ApplicationDbContext context, HttpClient httpClient, IAIExtractorService aiExtractService)
        {
            _context = context;
            _httpClient = httpClient;
            _aiExtractService = aiExtractService;
        }

        public void BatchExtraction(List<SearchDto> batchSearchDtos)
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

                InitializeExtraction(searchDto);
            }
        }

        public async Task InitializeExtraction(SearchDto searchDto)
        {
            try
            {
                VideoSearch videoSearch = new VideoSearch();

                string keyword = searchDto.Keywords;
                string containerUrl = searchDto.ContainerUrl;
                int pageViewLimit = searchDto.PageViewLimit;

                var query = await videoSearch.GetVideos(keyword, pageViewLimit);
                string recipientUserName = string.Empty;

                for (int i = 0; i < query.Count; i++)
                {
                    string videoUrl = query[i].getUrl();
                    string description = await GetYouTubeVideoDescription(videoUrl);
                    string subtitles = await GetYouTubeVideoSubtitles(videoUrl);   

                    string emailFound = ExtractEmails(description);
                    string phoneFound = ExtractPhoneNumbers(description);

                    if(emailFound != null)
                    {
                        var lead = await _context.LeadLakes.FirstOrDefaultAsync(x => x.Email == emailFound);

                        if(lead == null)
                        {
                            var validateEmail = await EmailValidator(emailFound);
                            
                            if (validateEmail) 
                            {

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

                                _context.LeadLakes.Add(leadlake);
                                _context.SaveChanges();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., log the error)
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        private string ExtractPhoneNumbers(string html)
        {
            string value = "";
            try
            {
                Regex reg = new Regex(@"\+?\d[\d -]{8,}\d", RegexOptions.IgnoreCase);
                Match match;
                List<string> results = new List<string>();
                for (match = reg.Match(html); match.Success; match = match.NextMatch())
                {
                    if (!(results.Contains(match.Value)))
                        results.Add(match.Value);
                }
                value = results[0];
            }
            catch (Exception)
            {
                return "";
            }
            return value;
        }

        private string ExtractEmails(string html)
        {
            string value = "";

            try
            {
                Regex reg = new Regex(@"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,6}", RegexOptions.IgnoreCase);
                Match match;

                List<string> results = new List<string>();
                for (match = reg.Match(html); match.Success; match = match.NextMatch())
                {
                    if (!(results.Contains(match.Value)))
                        results.Add(match.Value);
                }

                value = results[0];
            }
            catch (Exception)
            {
                return "";
            }

            return value;
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

        private static async Task<string> GetYouTubeVideoSubtitles(string videoUrl)
        {

            HttpClient client = new HttpClient();

            string url = $"https://yt.processzero.xyz/transcript?url={videoUrl}";

            string response = await client.GetStringAsync(url);

            return response;
        }
    }
}
