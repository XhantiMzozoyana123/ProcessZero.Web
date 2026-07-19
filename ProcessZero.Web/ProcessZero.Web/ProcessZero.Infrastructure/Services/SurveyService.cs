using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain;
using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessZero.Infrastructure.Services
{
    public class SurveyService : ISurveyService
    {
        /// <summary>
        /// Base contact information questions that are ALWAYS prepended to every survey.
        /// Indices 0-6 in the final survey question order.
        /// </summary>
        private static readonly List<(string Text, bool IsRequired)> BASE_CONTACT_QUESTIONS = new()
        {
            ("Email Address", true),
            ("First Name", true),
            ("Last Name", true),
            ("Phone Number", true),
            ("Company", false),
            ("Job Title", false),
            ("Industry", false)
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

        private string GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "SYSTEM";
        }

        // ================== PUBLIC / RESPONDENT ENDPOINTS ==================

        public async Task<SurveyClientDto?> GetSurveyAsync(int surveyId, CancellationToken cancellationToken = default)
        {
            var survey = await _context.Surveys
                .AsNoTracking()
                .Where(s => s.Id == surveyId && s.Status == "Active")
                .FirstOrDefaultAsync(cancellationToken);

            if (survey == null)
                return null;

            var questions = await _context.SurveyQuestions
                .AsNoTracking()
                .Where(q => q.SurveyId == surveyId)
                .OrderBy(q => q.Order)
                .Include(q => q.Options)
                .ToListAsync(cancellationToken);

            return new SurveyClientDto
            {
                Id = survey.Id,
                Name = survey.Name,
                Title = survey.Title,
                Description = survey.Description,
                Status = survey.Status,
                Questions = questions.Select(MapToQuestionDto).ToList()
            };
        }

        public async Task<SurveyResponseResultDto> SubmitResponseAsync(SurveyResponseSubmissionDto submission, CancellationToken cancellationToken = default)
        {
            var survey = await _context.Surveys
                .AsNoTracking()
                .Where(s => s.Id == submission.SurveyId)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException($"Survey {submission.SurveyId} not found.");

            if (survey.Status != "Active")
                throw new InvalidOperationException($"Survey {submission.SurveyId} is not active.");

            var questions = await _context.SurveyQuestions
                .AsNoTracking()
                .Where(q => q.SurveyId == submission.SurveyId)
                .OrderBy(q => q.Order)
                .ToListAsync(cancellationToken);

            if (questions.Count == 0)
                throw new InvalidOperationException("Survey has no questions.");

            if (submission.Answers.Count != questions.Count)
                throw new InvalidOperationException($"Expected {questions.Count} answers but received {submission.Answers.Count}.");

            // Validate required contact fields (first 7 questions are always contact)
            for (int i = 0; i < questions.Count && i < BASE_CONTACT_QUESTIONS.Count; i++)
            {
                if (BASE_CONTACT_QUESTIONS[i].IsRequired && string.IsNullOrWhiteSpace(submission.Answers[i]))
                {
                    throw new InvalidOperationException($"{BASE_CONTACT_QUESTIONS[i].Text} is required.");
                }
            }

            string email = submission.Answers[0]?.Trim() ?? "";
            string firstName = submission.Answers[1]?.Trim() ?? "";
            string lastName = submission.Answers[2]?.Trim() ?? "";
            string phone = submission.Answers[3]?.Trim() ?? "";
            string company = submission.Answers.Count > 4 ? submission.Answers[4]?.Trim() ?? "" : "";
            string job = submission.Answers.Count > 5 ? submission.Answers[5]?.Trim() ?? "" : "";
            string industry = submission.Answers.Count > 6 ? submission.Answers[6]?.Trim() ?? "" : "";

            if (string.IsNullOrWhiteSpace(email))
                throw new InvalidOperationException("Email address is required.");
            if (string.IsNullOrWhiteSpace(firstName))
                throw new InvalidOperationException("First name is required.");
            if (string.IsNullOrWhiteSpace(lastName))
                throw new InvalidOperationException("Last name is required.");
            if (string.IsNullOrWhiteSpace(phone))
                throw new InvalidOperationException("Phone number is required.");

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

            var response = new SurveyResponse
            {
                SurveyId = submission.SurveyId,
                UserId = GetCurrentUserId(),
                SurveyRespondentId = respondent.Id,
                Respondent = respondent,
                SubmittedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.SurveyResponses.Add(response);
            await _context.SaveChangesAsync(cancellationToken);

            // Create individual answer records
            for (int i = 0; i < questions.Count; i++)
            {
                _context.SurveyAnswers.Add(new SurveyAnswer
                {
                    SurveyResponseId = response.Id,
                    SurveyQuestionId = questions[i].Id,
                    AnswerText = submission.Answers[i]?.Trim() ?? string.Empty,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync(cancellationToken);

            try
            {
                var qualifies = await ValidateAndQualifyRespondentAsync(survey, questions, submission, respondent, cancellationToken);
                if (qualifies)
                {
                    await AddToLeadLakeAsync(respondent, cancellationToken);
                }
            }
            catch
            {
                // LLM validation failure should not block submission
            }

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
                Answers = submission.Answers.Skip(BASE_CONTACT_QUESTIONS.Count).ToList(),
                SubmittedAt = response.SubmittedAt
            };
        }

        // ================== ADMIN ENDPOINTS ==================

        public async Task<List<SurveysListDto>> ListSurveysAsync(CancellationToken cancellationToken = default)
        {
            var surveys = await _context.Surveys
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

        public async Task<SurveyDto> CreateSurveyAsync(SurveyDto survey, CancellationToken cancellationToken = default)
        {
            if (survey == null)
                throw new ArgumentNullException(nameof(survey));
            if (string.IsNullOrWhiteSpace(survey.Name))
                throw new InvalidOperationException("Survey name is required.");

            var userId = GetCurrentUserId();

            var entity = new Survey
            {
                UserId = userId,
                Name = survey.Name,
                Title = survey.Title,
                Description = survey.Description,
                Status = survey.Status ?? "Active",
                UploadedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.Surveys.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            // Add contact questions (order 0-6)
            int questionOrder = 0;
            foreach (var contact in BASE_CONTACT_QUESTIONS)
            {
                _context.SurveyQuestions.Add(new SurveyQuestion
                {
                    SurveyId = entity.Id,
                    UserId = userId,
                    Text = contact.Text,
                    Order = questionOrder++,
                    Category = QuestionCategory.Contact,
                    IsRequired = contact.IsRequired,
                    Type = SurveyQuestionType.OpenEnded,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync(cancellationToken);

            // Add business questions (order 7+)
            foreach (var q in survey.Questions)
            {
                var question = new SurveyQuestion
                {
                    SurveyId = entity.Id,
                    UserId = userId,
                    Text = q.Text,
                    Order = questionOrder++,
                    Category = QuestionCategory.Business,
                    IsRequired = q.IsRequired,
                    Type = q.Type,
                    CreatedAt = DateTime.UtcNow
                };
                _context.SurveyQuestions.Add(question);
                await _context.SaveChangesAsync(cancellationToken);

                // Add individual option rows for MultipleChoice questions
                if (q.Type == SurveyQuestionType.MultipleChoice && q.Options != null)
                {
                    foreach (var opt in q.Options.Select((text, idx) => new { text, idx }))
                    {
                        _context.SurveyQuestionOptions.Add(new SurveyQuestionOption
                        {
                            SurveyQuestionId = question.Id,
                            Text = opt.text,
                            Order = opt.idx,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }

            return await GetSurveyForAdminAsync(entity.Id, cancellationToken)
                ?? throw new InvalidOperationException("Failed to retrieve created survey.");
        }

        public async Task<SurveyDto> UpdateSurveyAsync(SurveyDto survey, CancellationToken cancellationToken = default)
        {
            if (survey == null)
                throw new ArgumentNullException(nameof(survey));
            if (!survey.Id.HasValue || survey.Id <= 0)
                throw new InvalidOperationException("Survey ID is required for update.");

            var entity = await _context.Surveys
                .Where(s => s.Id == survey.Id)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException($"Survey {survey.Id} not found.");

            entity.Name = survey.Name;
            entity.Title = survey.Title;
            entity.Description = survey.Description;
            entity.Status = survey.Status ?? entity.Status;
            entity.UploadedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            // Remove existing questions and re-add (cascade deletes answers too)
            var existingQuestions = await _context.SurveyQuestions
                .Where(q => q.SurveyId == entity.Id)
                .ToListAsync(cancellationToken);
            _context.SurveyQuestions.RemoveRange(existingQuestions);
            await _context.SaveChangesAsync(cancellationToken);

            // Add contact questions (order 0-6)
            int questionOrder = 0;
            foreach (var contact in BASE_CONTACT_QUESTIONS)
            {
                _context.SurveyQuestions.Add(new SurveyQuestion
                {
                    SurveyId = entity.Id,
                    UserId = entity.UserId,
                    Text = contact.Text,
                    Order = questionOrder++,
                    Category = QuestionCategory.Contact,
                    IsRequired = contact.IsRequired,
                    Type = SurveyQuestionType.OpenEnded,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync(cancellationToken);

            // Add business questions (order 7+)
            foreach (var q in survey.Questions)
            {
                var question = new SurveyQuestion
                {
                    SurveyId = entity.Id,
                    UserId = entity.UserId,
                    Text = q.Text,
                    Order = questionOrder++,
                    Category = QuestionCategory.Business,
                    IsRequired = q.IsRequired,
                    Type = q.Type,
                    CreatedAt = DateTime.UtcNow
                };
                _context.SurveyQuestions.Add(question);
                await _context.SaveChangesAsync(cancellationToken);

                // Add individual option rows for MultipleChoice questions
                if (q.Type == SurveyQuestionType.MultipleChoice && q.Options != null)
                {
                    foreach (var opt in q.Options.Select((text, idx) => new { text, idx }))
                    {
                        _context.SurveyQuestionOptions.Add(new SurveyQuestionOption
                        {
                            SurveyQuestionId = question.Id,
                            Text = opt.text,
                            Order = opt.idx,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }

            return await GetSurveyForAdminAsync(entity.Id, cancellationToken)
                ?? throw new InvalidOperationException("Failed to retrieve updated survey.");
        }

        public async Task DeleteSurveyAsync(int surveyId, CancellationToken cancellationToken = default)
        {
            var entity = await _context.Surveys
                .Where(s => s.Id == surveyId)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException($"Survey {surveyId} not found.");

            _context.Surveys.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<SurveyDto?> GetSurveyForAdminAsync(int surveyId, CancellationToken cancellationToken = default)
        {
            var survey = await _context.Surveys
                .AsNoTracking()
                .Where(s => s.Id == surveyId)
                .FirstOrDefaultAsync(cancellationToken);

            if (survey == null)
                return null;

            var questions = await _context.SurveyQuestions
                .AsNoTracking()
                .Where(q => q.SurveyId == surveyId)
                .OrderBy(q => q.Order)
                .Include(q => q.Options)
                .ToListAsync(cancellationToken);

            return new SurveyDto
            {
                Id = survey.Id,
                Name = survey.Name,
                Title = survey.Title,
                Description = survey.Description,
                Status = survey.Status,
                Questions = questions
                    .Where(q => q.Category == QuestionCategory.Business)
                    .Select(MapToQuestionDto)
                    .ToList()
            };
        }

        public async Task<SurveySummaryDto> GetAllResponsesSummaryAsync(int surveyId, CancellationToken cancellationToken = default)
        {
            var survey = await _context.Surveys
                .AsNoTracking()
                .Where(s => s.Id == surveyId)
                .FirstOrDefaultAsync(cancellationToken);

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

            var questions = await _context.SurveyQuestions
                .AsNoTracking()
                .Where(q => q.SurveyId == surveyId && q.Category == QuestionCategory.Business)
                .OrderBy(q => q.Order)
                .Select(q => q.Id)
                .ToListAsync(cancellationToken);

            var responses = await _context.SurveyResponses
                .AsNoTracking()
                .Where(r => r.SurveyId == surveyId)
                .Include(r => r.Respondent)
                .Include(r => r.Answers)
                .OrderByDescending(r => r.SubmittedAt)
                .ToListAsync(cancellationToken);

            var responseResults = new List<SurveyResponseResultDto>();
            foreach (var response in responses)
            {
                if (response.Respondent == null) continue;

                var businessAnswers = response.Answers
                    .Where(a => questions.Contains(a.SurveyQuestionId))
                    .OrderBy(a => a.Id)
                    .Select(a => a.AnswerText)
                    .ToList();

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
                    Answers = businessAnswers,
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

        public async Task<SurveyResponseResultDto?> GetResponseByIdAsync(int responseId, CancellationToken cancellationToken = default)
        {
            var response = await _context.SurveyResponses
                .AsNoTracking()
                .Include(r => r.Respondent)
                .Include(r => r.Answers)
                .Where(r => r.Id == responseId)
                .FirstOrDefaultAsync(cancellationToken);

            if (response?.Respondent == null)
                return null;

            var businessAnswers = response.Answers
                .OrderBy(a => a.Id)
                .Select(a => a.AnswerText)
                .ToList();

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
                Answers = businessAnswers,
                SubmittedAt = response.SubmittedAt
            };
        }

        // ================== PRIVATE HELPERS ==================

        private SurveyQuestionDto MapToQuestionDto(SurveyQuestion q)
        {
            // Options are loaded separately via Include in queries
            List<string> options = q.Options?.OrderBy(opt => opt.Order).Select(opt => opt.Text).ToList() ?? new List<string>();
            return new SurveyQuestionDto
            {
                Id = q.Id,
                Text = q.Text,
                IsRequired = q.IsRequired,
                Category = q.Category,
                Type = q.Type,
                Options = options
            };
        }

        private async Task<bool> ValidateAndQualifyRespondentAsync(
            Survey survey,
            List<SurveyQuestion> questions,
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

            for (int i = 0; i < questions.Count && i < submission.Answers.Count; i++)
            {
                analysisPrompt += $"Q{i + 1}: {questions[i].Text}\nA: {submission.Answers[i]}\n\n";
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

        private async Task AddToLeadLakeAsync(SurveyRespondent respondent, CancellationToken cancellationToken)
        {
            var existingLead = await _context.LeadLakes
                .Where(l => l.Email == respondent.Email)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingLead != null)
                return;

            var industryEnum = MapIndustryToLeadLakeIndustry(respondent.Industry);

            _context.LeadLakes.Add(new LeadLake
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
            });

            await _context.SaveChangesAsync(cancellationToken);
        }

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