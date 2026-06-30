using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;

namespace ProcessZero.Web.Controllers
{
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

        // ========================
        // PUBLIC - GET ASSESSMENTS
        // ========================

        [HttpGet("global")]
        public async Task<IActionResult> GetGlobal(CancellationToken cancellationToken)
        {
            var assessment = await _assessmentService.GetAssessmentAsync(0, cancellationToken);
            if (assessment == null)
                return NotFound("Global assessment has not been uploaded yet.");

            return Ok(assessment);
        }

        [HttpGet("product/{productId:int}")]
        public async Task<IActionResult> GetByProduct(int productId, CancellationToken cancellationToken)
        {
            var assessment = await _assessmentService.GetAssessmentAsync(productId, cancellationToken);
            if (assessment == null)
                return NotFound($"No assessment found for productId {productId}.");

            return Ok(assessment);
        }

        // ========================
        // PUBLIC - SUBMISSIONS
        // ========================

        [HttpPost("global/submit")]
        public async Task<IActionResult> SubmitGlobal([FromBody] SubmissionDto submission, CancellationToken cancellationToken)
        {
            var result = await _assessmentService.SubmitAsync(0, submission, cancellationToken);
            return Ok(result);
        }

        [HttpPost("product/{productId:int}/submit")]
        public async Task<IActionResult> SubmitByProduct(int productId, [FromBody] SubmissionDto submission, CancellationToken cancellationToken)
        {
            var result = await _assessmentService.SubmitAsync(productId, submission, cancellationToken);
            return Ok(result);
        }

        // ========================
        // USER - RESULTS
        // ========================

        [HttpGet("global/my-result")]
        public async Task<IActionResult> GetMyGlobalResult(CancellationToken cancellationToken)
        {
            var result = await _assessmentService.GetMyResultAsync(0, cancellationToken);
            if (result == null)
                return NotFound("You have not submitted the global assessment yet.");

            return Ok(result);
        }

        [HttpGet("product/{productId:int}/my-result")]
        public async Task<IActionResult> GetMyProductResult(int productId, CancellationToken cancellationToken)
        {
            var result = await _assessmentService.GetMyResultAsync(productId, cancellationToken);
            if (result == null)
                return NotFound("You have not submitted this product assessment yet.");

            return Ok(result);
        }

        [HttpGet("my-results")]
        public async Task<IActionResult> GetAllMyResults(CancellationToken cancellationToken)
        {
            var results = await _assessmentService.GetAllMyResultsAsync(cancellationToken);
            return Ok(results);
        }

        // ========================
        // ADMIN - GET ALL USERS RESULTS
        // ========================

        [Authorize(Policy = "Admin")]
        [HttpGet("admin/global/result/{userId}")]
        public async Task<IActionResult> GeUsersGlobalResult(string userId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(userId))
                return BadRequest("UserId is required.");

            var result = await _assessmentService.GetMyUsersResultAsync(0, userId, cancellationToken);
            if (result == null)
                return NotFound($"User {userId} has not submitted the global assessment yet.");

            return Ok(result);
        }

        [Authorize(Policy = "Admin")]
        [HttpGet("admin/results")]
        public async Task<IActionResult> GetAllUsersResults(CancellationToken cancellationToken)
        {
            var results = await _assessmentService.GetAllMyUsersAsync(cancellationToken);
            return Ok(results);
        }

        // ========================
        // ADMIN - MANAGEMENT
        // ========================

        [HttpPut("admin/global")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> UploadGlobal([FromBody] AssessmentDto assessment, CancellationToken cancellationToken)
        {
            if (assessment == null)
                return BadRequest("Assessment payload is required.");

            await _assessmentService.UploadAssessmentAsync(0, assessment, cancellationToken);
            return NoContent();
        }

        [HttpPut("admin/product/{productId:int}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> UploadByProduct(int productId, [FromBody] AssessmentDto assessment, CancellationToken cancellationToken)
        {
            if (assessment == null)
                return BadRequest("Assessment payload is required.");

            await _assessmentService.UploadAssessmentAsync(productId, assessment, cancellationToken);
            return NoContent();
        }

        [HttpGet("admin/product/{productId:int}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> GetProductForAdmin(int productId, CancellationToken cancellationToken)
        {
            var assessment = await _assessmentService.GetAssessmentForAdminAsync(productId, cancellationToken);
            if (assessment == null)
                return NotFound($"No assessment found for productId {productId}.");

            return Ok(assessment);
        }

        [HttpGet("admin/all")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> GetAllForAdmin(CancellationToken cancellationToken)
        {
            var assessments = await _assessmentService.GetAllAssessmentsForAdminAsync(cancellationToken);
            return Ok(assessments);
        }
    }
}