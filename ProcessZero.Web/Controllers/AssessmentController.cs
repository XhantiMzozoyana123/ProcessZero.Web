using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;

namespace ProcessZero.Web.Controllers
{
    /*
     * AssessmentController
     * --------------------
     * Manages online assessments that partners must pass before accessing the platform
     * or selling specific products.
     *
     * Assessment types:
     *   - Global  (productId = 0) : platform-wide; every partner must pass to use the system.
     *   - Product (productId > 0) : product-specific; partner must pass to be eligible to sell it.
     *
     * Assessment questions are stored in the Assessments table (entity: Assessment).
     * Admins upload them via PUT endpoints — each upload creates a new row; the latest
     * row per ProductId is used when serving/scoring. Partners GET the questions (answers
     * stripped) and POST their submission. Results are scored automatically and persisted
     * in the AssessmentSubmissions table.
     *
     * ?????????????????????????????????????????????????????????????????
     * Entities / DTOs referenced
     * ?????????????????????????????????????????????????????????????????
     *
     * Assessment (ProcessZero.Domain.Entities.Assessment : BaseEntity)
     *   BaseEntity (inherited):
     *     - Id (int)             : primary key
     *     - UserId (string)      : admin who uploaded
     *     - CreatedAt (DateTime)
     *     - UpdatedAt (DateTime)
     *   Own columns:
     *     - ProductId (int)      : 0 = global, > 0 = product-specific
     *     - Title (string)       : assessment title
     *     - PassMark (double?)   : per-assessment pass mark; null = use appsettings default
     *     - QuestionsJson (string) : serialised MCQs (with CorrectIndex) + OpenQuestions
     *     - UploadedAt (DateTime)  : UTC timestamp of upload
     *
     * AssessmentSubmission (ProcessZero.Domain.Entities.AssessmentSubmission : BaseEntity)
     *   BaseEntity (inherited):
     *     - Id (int)            : primary key
     *     - UserId (string)     : the partner who took the assessment
     *     - CreatedAt (DateTime)
     *     - UpdatedAt (DateTime)
     *   Own columns:
     *     - ProductId (int)     : 0 = global assessment, > 0 = product-specific
     *     - Score (int)         : points earned
     *     - Total (int)         : maximum possible points
     *     - Percentage (double) : score / total * 100
     *     - Passed (bool)       : true when Percentage >= pass mark
     *     - AnswersJson (string?) : serialised SubmissionDto for audit / manual review
     *     - SubmittedAt (DateTime) : UTC timestamp of submission
     *
     * Product (ProcessZero.Domain.Entities.Product : BaseEntity)
     *   - Id (int)
     *   - Name (string)
     *   - Description (string)
     *   - ActualAmount (decimal)
     *   - Url (string)
     *   - NegotiableAmounts (string)
     *   - ProfilePictureBase64 (string)
     *
     * ?????????????????????????????????????????????????????????????????
     * DTOs (ProcessZero.Application.Dtos)
     * ?????????????????????????????????????????????????????????????????
     *
 * AssessmentDto (admin-facing, stored in JSON file)
 *   - Title (string)
 *   - Description (string)      : optional human-readable instructions / expectations for candidates
 *   - ProductId (int)           : 0 = global, > 0 = product
   *   - PassMark (double?)        : per-assessment override (falls back to appsettings Assessment:PassMark)
     *   - MCQs (List<QuestionDto>)
     *       - Text (string), Options (List<string>), CorrectIndex (int), Weight (int)
     *   - OpenQuestions (List<OpenQuestionDto>)
     *       - Text (string), Weight (int)
     *
 * AssessmentClientDto (partner-facing, CorrectIndex stripped)
 *   - Title (string)
 *   - Description (string)      : shown to candidates to explain expectations
 *   - ProductId (int)
     *   - MCQs (List<QuestionClientDto>)
     *       - Text (string), Options (List<string>), Weight (int)
     *   - OpenQuestions (List<OpenQuestionDto>)
     *
     * SubmissionDto (partner sends)
     *   - McqAnswers (List<int>)    : selected option index per MCQ
     *   - OpenAnswers (List<string?>) : free-text answers for open questions
     *
     * SubmissionResultDto (returned after scoring)
     *   - ProductId (int)
     *   - Score (int), Total (int), Percentage (double), Passed (bool)
     *
     * ?????????????????????????????????????????????????????????????????
     * Service: IAssessmentService
     * ?????????????????????????????????????????????????????????????????
     *   - Task<AssessmentClientDto?> GetAssessmentAsync(int productId, CancellationToken)
     *   - Task<SubmissionResultDto> SubmitAsync(int productId, SubmissionDto, CancellationToken)
     *   - Task UploadAssessmentAsync(int productId, AssessmentDto, CancellationToken)
     *
     * ?????????????????????????????????????????????????????????????????
     * Controller endpoints
     * ?????????????????????????????????????????????????????????????????
     *   GET    api/assessment/global                      -> GetGlobal()
     *   GET    api/assessment/product/{productId}         -> GetByProduct(productId)
     *   POST   api/assessment/global/submit               -> SubmitGlobal(submission)
     *   POST   api/assessment/product/{productId}/submit  -> SubmitByProduct(productId, submission)
     *   PUT    api/assessment/admin/global          [Admin] -> UploadGlobal(assessment)
     *   PUT    api/assessment/admin/product/{productId} [Admin] -> UploadByProduct(productId, assessment)
     */
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AssessmentController : ControllerBase
    {
        private readonly IAssessmentService _assessmentService;

        public AssessmentController(IAssessmentService assessmentService)
        {
            _assessmentService = assessmentService;
        }

        /// <summary>
        /// Get the global platform assessment (productId = 0).
        /// </summary>
        [HttpGet("global")]
        public async Task<IActionResult> GetGlobal(CancellationToken cancellationToken)
        {
            var assessment = await _assessmentService.GetAssessmentAsync(0, cancellationToken);
            if (assessment == null) return NotFound("Global assessment has not been uploaded yet.");
            return Ok(assessment);
        }

        /// <summary>
        /// Get the assessment for a specific product.
        /// </summary>
        [HttpGet("product/{productId:int}")]
        public async Task<IActionResult> GetByProduct(int productId, CancellationToken cancellationToken)
        {
            var assessment = await _assessmentService.GetAssessmentAsync(productId, cancellationToken);
            if (assessment == null) return NotFound($"No assessment found for productId {productId}.");
            return Ok(assessment);
        }

        /// <summary>
        /// Get the current logged-in user's latest result for the global assessment.
        /// </summary>
        [HttpGet("global/my-result")]
        public async Task<IActionResult> GetMyGlobalResult(CancellationToken cancellationToken)
        {
            var result = await _assessmentService.GetMyResultAsync(0, cancellationToken);
            if (result == null) return NotFound("You have not submitted the global assessment yet.");
            return Ok(result);
        }

        /// <summary>
        /// Submit answers for the global assessment (productId = 0).
        /// </summary>
        [HttpPost("global/submit")]
        public async Task<IActionResult> SubmitGlobal([FromBody] SubmissionDto submission, CancellationToken cancellationToken)
        {
            var result = await _assessmentService.SubmitAsync(0, submission, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Submit answers for a product-specific assessment.
        /// </summary>
        [HttpPost("product/{productId:int}/submit")]
        public async Task<IActionResult> SubmitByProduct(int productId, [FromBody] SubmissionDto submission, CancellationToken cancellationToken)
        {
            var result = await _assessmentService.SubmitAsync(productId, submission, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Admin: upload / replace the global assessment JSON (productId = 0).
        /// </summary>
        [HttpPut("admin/global")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> UploadGlobal([FromBody] AssessmentDto assessment, CancellationToken cancellationToken)
        {
            if (assessment == null) return BadRequest("Assessment payload is required.");
            await _assessmentService.UploadAssessmentAsync(0, assessment, cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Admin: get the full global assessment payload (including correct answers and description).
        /// </summary>
        [HttpGet("admin/global")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> GetGlobalForAdmin(CancellationToken cancellationToken)
        {
            // Return all assessments (global + product-specific) so admin UI can toggle them
            var assessments = await _assessmentService.GetAllAssessmentsForAdminAsync(cancellationToken);
            return Ok(assessments);
        }

        /// <summary>
        /// Admin: upload / replace the assessment JSON for a specific product.
        /// </summary>
        [HttpPut("admin/product/{productId:int}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> UploadByProduct(int productId, [FromBody] AssessmentDto assessment, CancellationToken cancellationToken)
        {
            if (assessment == null) return BadRequest("Assessment payload is required.");
            await _assessmentService.UploadAssessmentAsync(productId, assessment, cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Admin: get the full assessment payload for a specific product (including correct answers and description).
        /// </summary>
        [HttpGet("admin/product/{productId:int}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> GetByProductForAdmin(int productId, CancellationToken cancellationToken)
        {
            var assessment = await _assessmentService.GetAssessmentForAdminAsync(productId, cancellationToken);
            if (assessment == null) return NotFound($"No assessment found for productId {productId}.");
            return Ok(assessment);
        }
    }
}
