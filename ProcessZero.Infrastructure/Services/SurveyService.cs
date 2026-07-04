using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain;
using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;

namespace ProcessZero.Infrastructure.Services
{
    public class SurveyService : ISurveyService
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILLMService _llmService;

        public SurveyService(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor,
            ILLMService llmService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
        }

        // ---- DB helpers ----

        /// <summary>
        /// Loads the latest uploaded survey (global, no product filtering).
        /// </summary>
        private async Task<SurveyDto?> LoadSurveyAsync(CancellationToken cancellationToken = default)
        {
            var entity = await _context.SurveyQuestions
                .AsNoTracking()
                .OrderByDescending(q => q.UploadedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (entity == null || string.IsNullOrWhiteSpace(entity.QuestionsJson))
                return null;

            // Attempt to deserialize
            SurveyDto? dto = null;
            try
            {
                dto = JsonSerializer.Deserialize<SurveyDto>(entity.QuestionsJson, _jsonOptions);
            }
            catch
            {
                // Fallback: the questions JSON may be an anonymous object with Questions only
                try
                {
                    var anon = JsonSerializer.Deserialize<JsonElement>(entity.QuestionsJson, _jsonOptions);
                    dto = new SurveyDto
                    {
                        Title = entity.Title,
                        Description = entity.Description,
                        Questions = anon.TryGetProperty("Questions", out var questionsEl) 
                            ? JsonSerializer.Deserialize<List<SurveyQuestionDto>>(questionsEl.GetRawText(), _jsonOptions) ?? new List<SurveyQuestionDto>() 
                            : new List<SurveyQuestionDto>()
                    };
                }
                catch
                {
                    return null;
                }
            }

            if (dto == null) return null;

            // Overlay entity-level fields so they are always consistent
            dto.Title = entity.Title;
            dto.Description = entity.Description;

            return dto;
        }

        // ---- public API ----

        public async Task<SurveyClientDto?> GetSurveyAsync(CancellationToken cancellationToken = default)
        {
            var survey = await LoadSurveyAsync(cancellationToken);
            if (survey == null) return null;

            return new SurveyClientDto
            {
                Title = survey.Title,
                Description = survey.Description,
                Questions = survey.Questions
            };
        }

        public async Task<SurveyResponseResultDto> SubmitResponseAsync(SurveyResponseSubmissionDto submission, CancellationToken cancellationToken = default)
        {
            // Validate survey exists
            var survey = await LoadSurveyAsync(cancellationToken)
                ?? throw new InvalidOperationException("No survey found. Admin must upload a survey first.");

            // Create or get existing respondent
            var respondent = await _context.SurveyRespondents
                .Where(c => c.Email == submission.Respondent.Email)
                .FirstOrDefaultAsync(cancellationToken);

            if (respondent == null)
            {
                respondent = new SurveyRespondent
                {
                    UserId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty,
                    Email = submission.Respondent.Email,
                    FirstName = submission.Respondent.FirstName ?? string.Empty,
                    LastName = submission.Respondent.LastName ?? string.Empty,
                    Phone = submission.Respondent.Phone ?? string.Empty,
                    Company = submission.Respondent.Company ?? string.Empty,
                    Job = submission.Respondent.Job ?? string.Empty,
                    Industry = submission.Respondent.Industry ?? string.Empty,
                    CreatedAt = DateTime.UtcNow
                };

                _context.SurveyRespondents.Add(respondent);
                await _context.SaveChangesAsync(cancellationToken);
            }

            // Create submission record
            var response = new SurveyResponse
            {
                UserId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty,
                SurveyRespondentId = respondent.Id,
                Respondent = respondent,
                AnswersJson = JsonSerializer.Serialize(submission.Answers, _jsonOptions),
                SubmittedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.SurveyResponses.Add(response);
            await _context.SaveChangesAsync(cancellationToken);

            // Use LLM to validate if respondent has real pain points
            try
            {
                var qualifiesForLeadLake = await ValidateAndQualifyRespondentAsync(survey, submission, respondent, cancellationToken);

                if (qualifiesForLeadLake)
                {
                    await AddToLeadLakeAsync(respondent, cancellationToken);
                }
            }
            catch
            {
                // If LLM validation fails, don't block the survey submission
                // but simply don't add to LeadLake
            }

            // Return result
            return new SurveyResponseResultDto
            {
                Id = response.Id,
                Email = respondent.Email,
                FirstName = respondent.FirstName,
                LastName = respondent.LastName,
                Phone = respondent.Phone,
                Company = respondent.Company,
                Job = respondent.Job,
                Industry = respondent.Industry,
                Answers = submission.Answers,
                SubmittedAt = response.SubmittedAt
            };
        }

        /// <summary>
        /// Uses LLM to analyze survey responses and determine if respondent has real pain points.
        /// Returns true if they qualify for LeadLake, false otherwise.
        /// </summary>
        private async Task<bool> ValidateAndQualifyRespondentAsync(
            SurveyDto survey,
            SurveyResponseSubmissionDto submission,
            SurveyRespondent respondent,
            CancellationToken cancellationToken)
        {
            // Build a prompt for the LLM to analyze the responses
            var analysisPrompt = $@"
Analyze the following survey responses to determine if this person has real, actionable pain points that indicate high-ticket business problems:

Survey Title: {survey.Title}
Survey Description: {survey.Description}

Respondent Details:
- Name: {respondent.FirstName} {respondent.LastName}
- Company: {respondent.Company}
- Job: {respondent.Job}
- Industry: {respondent.Industry}

Survey Responses:
";

            // Add questions and answers
            for (int i = 0; i < survey.Questions.Count && i < submission.Answers.Count; i++)
            {
                analysisPrompt += $"Q{i + 1}: {survey.Questions[i].Text}\nA: {submission.Answers[i]}\n\n";
            }

            analysisPrompt += @"
Based on the survey responses, determine if this person represents a real, high-ticket pain point that could lead to a B2B product sale.

Respond with ONLY one word:
- 'QUALIFY' if they show clear pain points and business problems worth pursuing
- 'REJECT' if the responses are superficial, generic, or don't indicate real problems

Do not include any explanation, just the word.";

            try
            {
                var llmResponse = await _llmService.GenerateTextAsync(analysisPrompt);
                var cleanResponse = llmResponse?.Trim().ToUpperInvariant() ?? string.Empty;

                return cleanResponse.Contains("QUALIFY");
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Adds respondent to LeadLake for sales outreach.
        /// </summary>
        private async Task AddToLeadLakeAsync(SurveyRespondent respondent, CancellationToken cancellationToken)
        {
            // Check if respondent is already in LeadLake
            var existingLead = await _context.LeadLakes
                .Where(l => l.Email == respondent.Email)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingLead != null)
                return; // Already in LeadLake, don't duplicate

            // Map industry from SurveyRespondent to LeadLakeIndustry enum
            var industryEnum = MapIndustryToLeadLakeIndustry(respondent.Industry);

            var leadLakeEntry = new LeadLake
            {
                UserId = respondent.UserId,
                FirstName = respondent.FirstName,
                LastName = respondent.LastName,
                Email = respondent.Email,
                Phone = respondent.Phone,
                Company = respondent.Company,
                Job = respondent.Job,
                Location = string.Empty, // Not captured in survey
                Industry = industryEnum,
                Intent = LeadIntent.High, // Survey respondents indicate high intent
                CreatedAt = DateTime.UtcNow
            };

            _context.LeadLakes.Add(leadLakeEntry);
            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Maps survey industry string to LeadLakeIndustry enum.
        /// </summary>
        private LeadLakeIndustry MapIndustryToLeadLakeIndustry(string industryString)
        {
            if (string.IsNullOrWhiteSpace(industryString))
                return LeadLakeIndustry.Other;

            var normalized = industryString.Trim().ToLowerInvariant();

            return normalized switch
            {
                var s when s.Contains("tech") => LeadLakeIndustry.Technology,
                var s when s.Contains("finance") || s.Contains("banking") => LeadLakeIndustry.Finance,
                var s when s.Contains("health") => LeadLakeIndustry.Healthcare,
                var s when s.Contains("education") => LeadLakeIndustry.Education,
                var s when s.Contains("retail") => LeadLakeIndustry.Retail,
                var s when s.Contains("manufact") => LeadLakeIndustry.Manufacturing,
                var s when s.Contains("energy") => LeadLakeIndustry.Energy,
                var s when s.Contains("transport") => LeadLakeIndustry.Transportation,
                var s when s.Contains("entertain") => LeadLakeIndustry.Entertainment,
                var s when s.Contains("hospital") || s.Contains("hotel") => LeadLakeIndustry.Hospitality,
                _ => LeadLakeIndustry.Other
            };
        }

        public async Task UploadSurveyAsync(SurveyDto survey, CancellationToken cancellationToken = default)
        {
            if (survey == null)
                throw new ArgumentNullException(nameof(survey));

            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

            // Serialize the survey to JSON
            var questionsJson = JsonSerializer.Serialize(survey, _jsonOptions);

            // Create new survey question entity (global survey)
            var entity = new SurveyQuestion
            {
                UserId = userId,
                Title = survey.Title,
                Description = survey.Description,
                QuestionsJson = questionsJson,
                UploadedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.SurveyQuestions.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<SurveyDto?> GetSurveyForAdminAsync(CancellationToken cancellationToken = default)
        {
            return await LoadSurveyAsync(cancellationToken);
        }

        public async Task<SurveySummaryDto> GetAllResponsesSummaryAsync(CancellationToken cancellationToken = default)
        {
            // Get the survey
            var survey = await LoadSurveyAsync(cancellationToken);
            if (survey == null)
            {
                return new SurveySummaryDto
                {
                    Title = "No Survey",
                    TotalResponses = 0,
                    Responses = new List<SurveyResponseResultDto>(),
                    CollectedFrom = DateTime.UtcNow,
                    CollectedTo = DateTime.UtcNow
                };
            }

            // Get all responses
            var responses = await _context.SurveyResponses
                .AsNoTracking()
                .Include(r => r.Respondent)
                .OrderByDescending(r => r.SubmittedAt)
                .ToListAsync(cancellationToken);

            var responseResults = new List<SurveyResponseResultDto>();
            foreach (var response in responses)
            {
                if (response.Respondent == null) continue;

                var answers = new List<string>();
                try
                {
                    if (!string.IsNullOrWhiteSpace(response.AnswersJson))
                    {
                        answers = JsonSerializer.Deserialize<List<string>>(response.AnswersJson, _jsonOptions) ?? new List<string>();
                    }
                }
                catch
                {
                    // If deserialization fails, skip this entry
                    continue;
                }

                responseResults.Add(new SurveyResponseResultDto
                {
                    Id = response.Id,
                    Email = response.Respondent.Email,
                    FirstName = response.Respondent.FirstName,
                    LastName = response.Respondent.LastName,
                    Phone = response.Respondent.Phone,
                    Company = response.Respondent.Company,
                    Job = response.Respondent.Job,
                    Industry = response.Respondent.Industry,
                    Answers = answers,
                    SubmittedAt = response.SubmittedAt
                });
            }

            var collectedFrom = responseResults.OrderBy(r => r.SubmittedAt).FirstOrDefault()?.SubmittedAt ?? DateTime.UtcNow;
            var collectedTo = responseResults.OrderByDescending(r => r.SubmittedAt).FirstOrDefault()?.SubmittedAt ?? DateTime.UtcNow;

            return new SurveySummaryDto
            {
                Title = survey.Title,
                TotalResponses = responseResults.Count,
                Responses = responseResults,
                CollectedFrom = collectedFrom,
                CollectedTo = collectedTo
            };
        }

        public async Task<SurveyResponseResultDto?> GetResponseByIdAsync(int responseId, CancellationToken cancellationToken = default)
        {
            var response = await _context.SurveyResponses
                .AsNoTracking()
                .Include(r => r.Respondent)
                .Where(r => r.Id == responseId)
                .FirstOrDefaultAsync(cancellationToken);

            if (response?.Respondent == null)
                return null;

            var answers = new List<string>();
            try
            {
                if (!string.IsNullOrWhiteSpace(response.AnswersJson))
                {
                    answers = JsonSerializer.Deserialize<List<string>>(response.AnswersJson, _jsonOptions) ?? new List<string>();
                }
            }
            catch { }

            return new SurveyResponseResultDto
            {
                Id = response.Id,
                Email = response.Respondent.Email,
                FirstName = response.Respondent.FirstName,
                LastName = response.Respondent.LastName,
                Phone = response.Respondent.Phone,
                Company = response.Respondent.Company,
                Job = response.Respondent.Job,
                Industry = response.Respondent.Industry,
                Answers = answers,
                SubmittedAt = response.SubmittedAt
            };
        }
    }
}
