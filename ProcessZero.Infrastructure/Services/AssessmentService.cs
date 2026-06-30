using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain;
using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace ProcessZero.Infrastructure.Services
{
    public class AssessmentService : IAssessmentService
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        private readonly double _defaultPassMark;
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IEmailService _emailService;
        private readonly IBackgroundEmailService _backgroundEmailService;

        public AssessmentService(
            IConfiguration configuration,
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor,
            IEmailService emailService,
            IBackgroundEmailService backgroundEmailService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _backgroundEmailService = backgroundEmailService ?? throw new ArgumentNullException(nameof(backgroundEmailService));

            // Default pass mark from config (Assessment:PassMark), fallback 70%
            _defaultPassMark = 70.0;
            try
            {
                var raw = configuration["Assessment:PassMark"];
                if (!string.IsNullOrWhiteSpace(raw) && double.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
                    _defaultPassMark = parsed;
            }
            catch { }
        }

        // ---- DB helpers ----

        /// <summary>
        /// Loads the latest uploaded assessment for a productId from the Assessments table.
        /// </summary>
        private async Task<AssessmentDto?> LoadAssessmentAsync(int productId, CancellationToken cancellationToken = default)
        {
            var entity = await _context.Assessments
                .AsNoTracking()
                .Where(a => a.ProductId == productId)
                .OrderByDescending(a => a.UploadedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (entity == null || string.IsNullOrWhiteSpace(entity.QuestionsJson))
                return null;

            // Attempt to deserialize using the full AssessmentDto shape (which may include Title/Description)
            AssessmentDto? dto = null;
            try
            {
                dto = JsonSerializer.Deserialize<AssessmentDto>(entity.QuestionsJson, _jsonOptions);
            }
            catch
            {
                // Fallback: the questions JSON may be an anonymous object with MCQs/OpenQuestions only
                try
                {
                    var anon = JsonSerializer.Deserialize<JsonElement>(entity.QuestionsJson, _jsonOptions);
                    dto = new AssessmentDto
                    {
                        Title = entity.Title,
                        ProductId = entity.ProductId,
                        PassMark = entity.PassMark,
                        MCQs = anon.TryGetProperty("MCQs", out var mcqEl) ? JsonSerializer.Deserialize<List<QuestionDto>>(mcqEl.GetRawText(), _jsonOptions) ?? new List<QuestionDto>() : new List<QuestionDto>(),
                        OpenQuestions = anon.TryGetProperty("OpenQuestions", out var openEl) ? JsonSerializer.Deserialize<List<OpenQuestionDto>>(openEl.GetRawText(), _jsonOptions) ?? new List<OpenQuestionDto>() : new List<OpenQuestionDto>()
                    };

                    if (anon.TryGetProperty("Description", out var descEl) && descEl.ValueKind == JsonValueKind.String)
                        dto.Description = descEl.GetString() ?? string.Empty;
                }
                catch
                {
                    return null;
                }
            }

            if (dto == null) return null;

            // Overlay entity-level fields so they are always consistent
            dto.Title = entity.Title;
            dto.ProductId = entity.ProductId;
            dto.PassMark = entity.PassMark;

            // Ensure Description is not null
            dto.Description ??= string.Empty;

            return dto;
        }

        // ---- public API ----

        public async Task<AssessmentClientDto?> GetAssessmentAsync(int productId, CancellationToken cancellationToken = default)
        {
            var assessment = await LoadAssessmentAsync(productId, cancellationToken);
            if (assessment == null) return null;

            // Strip correct answers before sending to candidate
            return new AssessmentClientDto
            {
                Title = assessment.Title,
                Description = assessment.Description,
                ProductId = assessment.ProductId,
                MCQs = assessment.MCQs.Select(q => new QuestionClientDto
                {
                    Text = q.Text,
                    Options = q.Options,
                    Weight = q.Weight
                }).ToList(),
                OpenQuestions = assessment.OpenQuestions
            };
        }

        public async Task<SubmissionResultDto> SubmitAsync(int productId, SubmissionDto submission, CancellationToken cancellationToken = default)
        {
            var assessment = await LoadAssessmentAsync(productId, cancellationToken)
                ?? throw new InvalidOperationException($"No assessment found for productId {productId}");

            int score = 0;
            int total = 0;

            // Score MCQs using CorrectIndex stored in DB
            for (int i = 0; i < assessment.MCQs.Count; i++)
            {
                var q = assessment.MCQs[i];
                total += q.Weight;
                int answer = (i < submission.McqAnswers.Count) ? submission.McqAnswers[i] : -1;
                if (answer == q.CorrectIndex) score += q.Weight;
            }

            // Score open questions (length heuristic — manual review recommended)
            for (int i = 0; i < assessment.OpenQuestions.Count; i++)
            {
                var q = assessment.OpenQuestions[i];
                total += q.Weight;
                var resp = (i < submission.OpenAnswers.Count) ? submission.OpenAnswers[i] : null;
                if (!string.IsNullOrWhiteSpace(resp) && resp.Length > 20) score += q.Weight;
            }

            var percent = total == 0 ? 0 : (double)score / total * 100;
            var passMark = assessment.PassMark ?? _defaultPassMark;
            var passed = percent >= passMark;

            var result = new SubmissionResultDto
            {
                ProductId = productId,
                Score = score,
                Total = total,
                Percentage = percent,
                Passed = passed
            };

            // Persist submission
            try
            {
                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var entity = new AssessmentSubmission
                {
                    UserId = userId ?? string.Empty,
                    ProductId = productId,
                    Score = score,
                    Total = total,
                    Percentage = percent,
                    Passed = passed,
                    AnswersJson = JsonSerializer.Serialize(submission, _jsonOptions),
                    SubmittedAt = DateTime.UtcNow
                };

                _context.AssessmentSubmissions.Add(entity);
                await _context.SaveChangesAsync(cancellationToken);
                
                // Notify user if they passed
                if (passed && !string.IsNullOrWhiteSpace(userId))
                {
                    try
                    {
                        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
                        var productName = string.Empty;
                        if (productId > 0)
                        {
                            var product = await _context.Products.FindAsync(new object[] { productId }, cancellationToken);
                            productName = product?.Name ?? string.Empty;
                        }

                        var notice = ProcessZero.Application.Constants.NoticeConstant.NotifyAssessmentPassed(
                            user?.UserName ?? string.Empty,
                            user?.Email ?? string.Empty,
                            assessment.Title,
                            productName,
                            score,
                            total,
                            percent);
                        
                        var notice2 = ProcessZero.Application.Constants.NoticeConstant.NotifyBookMeetingWithTrainer(
                            user?.UserName ?? string.Empty,
                            user?.Email ?? string.Empty,
                            assessment.Title
                        );

                        await _emailService.SendEmailAsync(notice);
                        await _emailService.SendEmailAsync(notice2);
                    }
                    catch
                    {
                        // swallow notification errors
                    }
                }
            }
            catch
            {
                // swallow persistence errors
            }

            return result;
        }

        public async Task<SubmissionResultDto?> GetMyResultAsync(int productId, CancellationToken cancellationToken = default)
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return null;

            var submission = await _context.AssessmentSubmissions
                .AsNoTracking()
                .Where(s => s.ProductId == productId && s.UserId == userId)
                .OrderByDescending(s => s.SubmittedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (submission == null)
                return null;

            return new SubmissionResultDto
            {
                ProductId = submission.ProductId,
                Score = submission.Score,
                Total = submission.Total,
                Percentage = submission.Percentage,
                Passed = submission.Passed
            };
        }

        /// <summary>
        /// Returns the full assessment payload including correct answers for admin use.
        /// </summary>
        public async Task<AssessmentDto?> GetAssessmentForAdminAsync(int productId, CancellationToken cancellationToken = default)
        {
            return await LoadAssessmentAsync(productId, cancellationToken);
        }

        public async Task<List<AssessmentDto>> GetAllAssessmentsForAdminAsync(CancellationToken cancellationToken = default)
        {
            var entities = await _context.Assessments
                .AsNoTracking()
                .OrderByDescending(a => a.UploadedAt)
                .ToListAsync(cancellationToken);

            var result = new List<AssessmentDto>();
            foreach (var e in entities)
            {
                var dto = await LoadAssessmentAsync(e.ProductId, cancellationToken);
                if (dto != null)
                    result.Add(dto);
            }

            return result;
        }

        public async Task UploadAssessmentAsync(int productId, AssessmentDto assessment, CancellationToken cancellationToken = default)
        {
            if (assessment == null) throw new ArgumentNullException(nameof(assessment));

            // Ensure productId consistency
            assessment.ProductId = productId;

            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

            // Serialise questions (MCQs with CorrectIndex + OpenQuestions + Description) as JSON
            var questionsJson = JsonSerializer.Serialize(new
            {
                assessment.Title,
                assessment.Description,
                assessment.ProductId,
                assessment.PassMark,
                assessment.MCQs,
                assessment.OpenQuestions
            }, _jsonOptions);

            var entity = new Assessment
            {
                UserId = userId,
                ProductId = productId,
                Title = assessment.Title,
                Description = assessment.Description,
                PassMark = assessment.PassMark,
                QuestionsJson = questionsJson,
                UploadedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Assessments.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            // Notify all users that an assessment was uploaded/updated
            try
            {
                var users = await _context.Users.ToListAsync(cancellationToken);

                // get product name if productId > 0
                string productName = string.Empty;
                if (entity.ProductId > 0)
                {
                    var product = await _context.Products.FindAsync(new object[] { entity.ProductId }, cancellationToken);
                    productName = product?.Name ?? string.Empty;
                }

                foreach (var user in users)
                {
                    var notice = ProcessZero.Application.Constants.NoticeConstant.NotifyAssessmentUploaded(
                        user.UserName ?? string.Empty,
                        user.Email ?? string.Empty,
                        entity,
                        productName);
                    _backgroundEmailService.EnqueueEmail(notice);
                }
            }
            catch
            {
                // swallow email errors
            }
        }

        public async Task<List<SubmissionResultDto>> GetAllMyResultsAsync(CancellationToken cancellationToken = default)
        {
            var userId = _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
                return new List<SubmissionResultDto>();

            var submissions = await _context.AssessmentSubmissions
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync(cancellationToken);

            return submissions.Select(s => new SubmissionResultDto
            {
                UserId = s.UserId,
                ProductId = s.ProductId,
                Score = s.Score,
                Total = s.Total,
                Percentage = s.Percentage,
                Passed = s.Passed
            }).ToList();
        }

        public async Task<SubmissionResultDto?> GetMyUsersResultAsync(int productId, string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(userId))
                return null;

            var submission = await _context.AssessmentSubmissions
                .AsNoTracking()
                .Where(s => s.ProductId == productId && s.UserId == userId)
                .OrderByDescending(s => s.SubmittedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (submission == null)
                return null;

            return new SubmissionResultDto
            {
                UserId = submission.UserId,
                ProductId = submission.ProductId,
                Score = submission.Score,
                Total = submission.Total,
                Percentage = submission.Percentage,
                Passed = submission.Passed
            };
        }

        public async Task<List<SubmissionResultDto>> GetAllMyUsersAsync(CancellationToken cancellationToken = default)
        {
            var submissions = await _context.AssessmentSubmissions
                .AsNoTracking()
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync(cancellationToken);

            return submissions.Select(s => new SubmissionResultDto
            {
                UserId = s.UserId,
                ProductId = s.ProductId,
                Score = s.Score,
                Total = s.Total,
                Percentage = s.Percentage,
                Passed = s.Passed
            }).ToList();
        }
    }
}
