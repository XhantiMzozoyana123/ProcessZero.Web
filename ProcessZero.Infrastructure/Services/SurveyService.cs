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

        /// <summary>
        /// Base contact information questions that are ALWAYS prepended to every survey.
        /// These are mandatory for lead qualification and LLM analysis.
        /// Indices 0-6 in the final survey Questions array.
        /// </summary>
        private static readonly List<SurveyQuestionDto> BASE_CONTACT_QUESTIONS = new()
        {
            new SurveyQuestionDto
            {
                Id = 1,
                Text = "Email Address",
                IsRequired = true,
                Category = QuestionCategory.Contact
            },
            new SurveyQuestionDto
            {
                Id = 2,
                Text = "First Name",
                IsRequired = true,
                Category = QuestionCategory.Contact
            },
            new SurveyQuestionDto
            {
                Id = 3,
                Text = "Last Name",
                IsRequired = true,
                Category = QuestionCategory.Contact
            },
            new SurveyQuestionDto
            {
                Id = 4,
                Text = "Phone Number",
                IsRequired = true,
                Category = QuestionCategory.Contact
            },
            new SurveyQuestionDto
            {
                Id = 5,
                Text = "Company",
                IsRequired = false,
                Category = QuestionCategory.Contact
            },
            new SurveyQuestionDto
            {
                Id = 6,
                Text = "Job Title",
                IsRequired = false,
                Category = QuestionCategory.Contact
            },
            new SurveyQuestionDto
            {
                Id = 7,
                Text = "Industry",
                IsRequired = false,
                Category = QuestionCategory.Contact
            }
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

        // ================== DB HELPERS ==================

        /// <summary>
        /// Loads a specific survey by ID.
        /// Includes prepended base contact questions (indices 0-6) + admin business questions (7+).
        /// </summary>
        private async Task<SurveyDto?> LoadSurveyByIdAsync(int surveyId, CancellationToken cancellationToken = default)
        {
            var entity = await _context.SurveyQuestions
                .AsNoTracking()
                .Where(q => q.Id == surveyId)
                .FirstOrDefaultAsync(cancellationToken);

            if (entity == null || string.IsNullOrWhiteSpace(entity.QuestionsJson))
                return null;

            return DeserializeSurveyDto(entity);
        }

        /// <summary>
        /// Deserializes a SurveyQuestion entity into SurveyDto.
        /// </summary>
        private SurveyDto? DeserializeSurveyDto(SurveyQuestion entity)
        {
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
            dto.Id = entity.Id;
            dto.Name = entity.Name;
            dto.Title = entity.Title;
            dto.Description = entity.Description;
            dto.Status = entity.Status;

            return dto;
        }

        private string GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        }

        // ================== PUBLIC / RESPONDENT ENDPOINTS ==================

        /// <summary>
        /// Get a specific survey by ID for respondents to fill out.
        /// </summary>
        public async Task<SurveyClientDto?> GetSurveyAsync(int surveyId, CancellationToken cancellationToken = default)
        {
            var survey = await LoadSurveyByIdAsync(surveyId, cancellationToken);
            if (survey == null || survey.Status != "Active")
                return null;

            return new SurveyClientDto
            {
                Id = survey.Id ?? 0,
                Name = survey.Name,
                Title = survey.Title,
                Description = survey.Description,
                Status = survey.Status,
                Questions = survey.Questions
            };
        }

        /// <summary>
        /// Submit responses to a specific survey.
        /// </summary>
        public async Task<SurveyResponseResultDto> SubmitResponseAsync(SurveyResponseSubmissionDto submission, CancellationToken cancellationToken = default)
        {
            // Validate survey exists and is Active
            var survey = await LoadSurveyByIdAsync(submission.SurveyId, cancellationToken)
                ?? throw new InvalidOperationException($"Survey {submission.SurveyId} not found.");

            if (survey.Status != "Active")
                throw new InvalidOperationException($"Survey {submission.SurveyId} is not active.");

            // CRITICAL: Extract contact information from first 7 answers
            if (submission.Answers.Count < 7)
                throw new InvalidOperationException("Survey submission must include contact information (email, first name, last name, phone, company, job, industry).");

            string email = submission.Answers[0]?.Trim() ?? "";
            string firstName = submission.Answers[1]?.Trim() ?? "";
            string lastName = submission.Answers[2]?.Trim() ?? "";
            string phone = submission.Answers[3]?.Trim() ?? "";
            string company = submission.Answers[4]?.Trim() ?? "";
            string job = submission.Answers[5]?.Trim() ?? "";
            string industry = submission.Answers[6]?.Trim() ?? "";

            // Validate required fields
            if (string.IsNullOrWhiteSpace(email))
                throw new InvalidOperationException("Email address is required.");
            if (string.IsNullOrWhiteSpace(firstName))
                throw new InvalidOperationException("First name is required.");
            if (string.IsNullOrWhiteSpace(lastName))
                throw new InvalidOperationException("Last name is required.");
            if (string.IsNullOrWhiteSpace(phone))
                throw new InvalidOperationException("Phone number is required.");

            // Create or get existing respondent (unique by SurveyId + Email)
            var respondent = await _context.SurveyRespondents
                .Where(r => r.SurveyId == submission.SurveyId && r.Email == email)
                .FirstOrDefaultAsync(cancellationToken);

            if (respondent == null)
            {
                respondent = new SurveyRespondent
                {
                    SurveyId = submission.SurveyId,
                    UserId = GetCurrentUserId(),
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    Phone = phone,
                    Company = company,
                    Job = job,
                    Industry = industry,
                    CreatedAt = DateTime.UtcNow
                };

                _context.SurveyRespondents.Add(respondent);
                await _context.SaveChangesAsync(cancellationToken);
            }

            // Extract business answers (indices 7+)
            var businessAnswers = submission.Answers.Count > 7
                ? submission.Answers.GetRange(7, submission.Answers.Count - 7)
                : new List<string>();

            // Create submission record with ALL answers
            var response = new SurveyResponse
            {
                SurveyId = submission.SurveyId,
                UserId = GetCurrentUserId(),
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
            }

            // Return result
            return new SurveyResponseResultDto
            {
                Id = response.Id,
                SurveyId = submission.SurveyId,
                Email = respondent.Email,
                FirstName = respondent.FirstName,
                LastName = respondent.LastName,
                Phone = respondent.Phone,
                Company = respondent.Company,
                Job = respondent.Job,
                Industry = respondent.Industry,
                Answers = businessAnswers,
                SubmittedAt = response.SubmittedAt
            };
        }

        // ================== ADMIN ENDPOINTS ==================

        /// <summary>
        /// List all surveys.
        /// </summary>
        public async Task<List<SurveysListDto>> ListSurveysAsync(CancellationToken cancellationToken = default)
        {
            var surveys = await _context.SurveyQuestions
                .AsNoTracking()
                .OrderByDescending(s => s.UploadedAt)
                .ToListAsync(cancellationToken);

            var result = new List<SurveysListDto>();

            foreach (var survey in surveys)
            {
                var responseCount = await _context.SurveyResponses
                    .Where(r => r.SurveyId == survey.Id)
                    .CountAsync(cancellationToken);

                result.Add(new SurveysListDto
                {
                    Id = survey.Id,
                    Name = survey.Name,
                    Title = survey.Title,
                    Status = survey.Status,
                    ResponseCount = responseCount,
                    UploadedAt = survey.UploadedAt
                });
            }

            return result;
        }

        /// <summary>
        /// Create a new survey.
        /// Contact questions are automatically prepended.
        /// </summary>
        public async Task<SurveyDto> CreateSurveyAsync(SurveyDto survey, CancellationToken cancellationToken = default)
        {
            if (survey == null)
                throw new ArgumentNullException(nameof(survey));

            if (string.IsNullOrWhiteSpace(survey.Name))
                throw new InvalidOperationException("Survey name is required.");

            var userId = GetCurrentUserId();

            // CRITICAL: Prepend mandatory contact questions to admin's business questions
            var completeQuestions = new List<SurveyQuestionDto>();
            completeQuestions.AddRange(BASE_CONTACT_QUESTIONS);

            // Add admin-provided business questions (starting at index 7)
            int questionId = 8;
            foreach (var question in survey.Questions)
            {
                question.Id = questionId++;
                question.Category = QuestionCategory.Business;
                completeQuestions.Add(question);
            }

            // Create complete survey
            var completeSurvey = new SurveyDto
            {
                Name = survey.Name,
                Title = survey.Title,
                Description = survey.Description,
                Status = survey.Status ?? "Active",
                Questions = completeQuestions
            };

            // Serialize to JSON
            var questionsJson = JsonSerializer.Serialize(completeSurvey, _jsonOptions);

            // Create entity
            var entity = new SurveyQuestion
            {
                UserId = userId,
                Name = survey.Name,
                Title = survey.Title,
                Description = survey.Description,
                Status = survey.Status ?? "Active",
                QuestionsJson = questionsJson,
                UploadedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.SurveyQuestions.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            // Return created survey
            completeSurvey.Id = entity.Id;
            return completeSurvey;
        }

        /// <summary>
        /// Update an existing survey.
        /// </summary>
        public async Task<SurveyDto> UpdateSurveyAsync(SurveyDto survey, CancellationToken cancellationToken = default)
        {
            if (survey == null)
                throw new ArgumentNullException(nameof(survey));

            if (!survey.Id.HasValue || survey.Id <= 0)
                throw new InvalidOperationException("Survey ID is required for update.");

            var entity = await _context.SurveyQuestions
                .Where(s => s.Id == survey.Id)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException($"Survey {survey.Id} not found.");

            // Prepend base contact questions
            var completeQuestions = new List<SurveyQuestionDto>();
            completeQuestions.AddRange(BASE_CONTACT_QUESTIONS);

            int questionId = 8;
            foreach (var question in survey.Questions)
            {
                question.Id = questionId++;
                question.Category = QuestionCategory.Business;
                completeQuestions.Add(question);
            }

            var completeSurvey = new SurveyDto
            {
                Id = survey.Id,
                Name = survey.Name,
                Title = survey.Title,
                Description = survey.Description,
                Status = survey.Status ?? entity.Status,
                Questions = completeQuestions
            };

            // Update entity
            entity.Name = survey.Name;
            entity.Title = survey.Title;
            entity.Description = survey.Description;
            entity.Status = survey.Status ?? entity.Status;
            entity.QuestionsJson = JsonSerializer.Serialize(completeSurvey, _jsonOptions);
            entity.UploadedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            _context.SurveyQuestions.Update(entity);
            await _context.SaveChangesAsync(cancellationToken);

            return completeSurvey;
        }

        /// <summary>
        /// Delete a survey (hard delete, cascades to responses and respondents).
        /// </summary>
        public async Task DeleteSurveyAsync(int surveyId, CancellationToken cancellationToken = default)
        {
            var entity = await _context.SurveyQuestions
                .Where(s => s.Id == surveyId)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException($"Survey {surveyId} not found.");

            // Delete cascade will handle responses and respondents
            _context.SurveyQuestions.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Get survey for admin editing.
        /// </summary>
        public async Task<SurveyDto?> GetSurveyForAdminAsync(int surveyId, CancellationToken cancellationToken = default)
        {
            return await LoadSurveyByIdAsync(surveyId, cancellationToken);
        }

        /// <summary>
        /// Get all responses for a specific survey.
        /// </summary>
        public async Task<SurveySummaryDto> GetAllResponsesSummaryAsync(int surveyId, CancellationToken cancellationToken = default)
        {
            // Get survey
            var survey = await LoadSurveyByIdAsync(surveyId, cancellationToken);
            if (survey == null)
            {
                return new SurveySummaryDto
                {
                    SurveyId = surveyId,
                    Name = "Unknown",
                    Title = "Survey Not Found",
                    TotalResponses = 0,
                    Responses = new List<SurveyResponseResultDto>(),
                    CollectedFrom = DateTime.UtcNow,
                    CollectedTo = DateTime.UtcNow
                };
            }

            // Get all responses for this survey
            var responses = await _context.SurveyResponses
                .AsNoTracking()
                .Where(r => r.SurveyId == surveyId)
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
                    continue;
                }

                responseResults.Add(new SurveyResponseResultDto
                {
                    Id = response.Id,
                    SurveyId = surveyId,
                    Email = response.Respondent.Email,
                    FirstName = response.Respondent.FirstName,
                    LastName = response.Respondent.LastName,
                    Phone = response.Respondent.Phone,
                    Company = response.Respondent.Company,
                    Job = response.Respondent.Job,
                    Industry = response.Respondent.Industry,
                    Answers = answers.Count > 7 ? answers.GetRange(7, answers.Count - 7) : new List<string>(),
                    SubmittedAt = response.SubmittedAt
                });
            }

            var collectedFrom = responseResults.OrderBy(r => r.SubmittedAt).FirstOrDefault()?.SubmittedAt ?? DateTime.UtcNow;
            var collectedTo = responseResults.OrderByDescending(r => r.SubmittedAt).FirstOrDefault()?.SubmittedAt ?? DateTime.UtcNow;

            return new SurveySummaryDto
            {
                SurveyId = surveyId,
                Name = survey.Name,
                Title = survey.Title,
                TotalResponses = responseResults.Count,
                Responses = responseResults,
                CollectedFrom = collectedFrom,
                CollectedTo = collectedTo
            };
        }

        /// <summary>
        /// Get a single response by ID.
        /// </summary>
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
                SurveyId = response.SurveyId,
                Email = response.Respondent.Email,
                FirstName = response.Respondent.FirstName,
                LastName = response.Respondent.LastName,
                Phone = response.Respondent.Phone,
                Company = response.Respondent.Company,
                Job = response.Respondent.Job,
                Industry = response.Respondent.Industry,
                Answers = answers.Count > 7 ? answers.GetRange(7, answers.Count - 7) : new List<string>(),
                SubmittedAt = response.SubmittedAt
            };
        }

        // ================== PRIVATE HELPERS ==================

        /// <summary>
        /// Uses LLM to analyze survey responses and determine if respondent qualifies for LeadLake.
        /// </summary>
        private async Task<bool> ValidateAndQualifyRespondentAsync(
            SurveyDto survey,
            SurveyResponseSubmissionDto submission,
            SurveyRespondent respondent,
            CancellationToken cancellationToken)
        {
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
            // Check if already in LeadLake
            var existingLead = await _context.LeadLakes
                .Where(l => l.Email == respondent.Email)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingLead != null)
                return;

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
                Location = string.Empty,
                Industry = industryEnum,
                Intent = LeadIntent.High,
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
    }
}
