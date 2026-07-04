using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;

namespace ProcessZero.Web.Controllers
{
    /// <summary>
    /// Market Research Survey Management API
    /// 
    /// ENTITIES INVOLVED:
    /// ==================
    /// 
    /// 1. SurveyQuestion (Domain Entity)
    ///    Database Table: SurveyQuestions
    ///    Purpose: Stores the global survey definition
    ///    Columns:
    ///      - Id: int (Primary Key)
    ///      - Title: string (max 255) - Survey name/heading
    ///      - Description: string (max 1000) - Survey introduction/context
    ///      - QuestionsJson: longtext - Serialized JSON array of SurveyQuestionDto objects
    ///      - UploadedAt: datetime - When survey was created/updated
    ///      - UserId: string - Admin user who uploaded the survey
    ///      - CreatedAt/UpdatedAt: datetime - EF Core tracking
    ///    Index: IX_SurveyQuestions_UploadedAt (for fast retrieval of latest survey)
    /// 
    /// 2. SurveyRespondent (Domain Entity)
    ///    Database Table: SurveyRespondents
    ///    Purpose: Stores contact information for survey participants
    ///    Columns:
    ///      - Id: int (Primary Key)
    ///      - Email: string (max 255, unique) - Respondent email address
    ///      - FirstName: string (max 100) - Respondent first name
    ///      - LastName: string (max 100) - Respondent last name
    ///      - Phone: string (max 20) - Respondent phone number
    ///      - Company: string (max 255) - Organization name
    ///      - Job: string (max 255) - Job title/role
    ///      - Industry: string (max 100) - Industry classification (used for LeadLake mapping)
    ///      - UserId: string (FK to AspNetUsers) - Associated system user
    ///      - CreatedAt/UpdatedAt: datetime - EF Core tracking
    ///    Indexes: 
    ///      - IX_SurveyRespondents_Email (quick lookup by email)
    ///      - IX_SurveyRespondents_UserId (user-specific queries)
    /// 
    /// 3. SurveyResponse (Domain Entity)
    ///    Database Table: SurveyResponses
    ///    Purpose: Stores individual survey submissions and responses
    ///    Columns:
    ///      - Id: int (Primary Key)
    ///      - SurveyRespondentId: int (FK to SurveyRespondents) - Links to respondent
    ///      - AnswersJson: longtext - Serialized JSON array of response strings
    ///      - SubmittedAt: datetime - When the response was submitted
    ///      - UserId: string (FK to AspNetUsers) - System user who submitted
    ///      - CreatedAt/UpdatedAt: datetime - EF Core tracking
    ///    Index: IX_SurveyResponses_RespondentId_SubmittedAt (composite, for efficient lookup + sorting)
    ///    FK Constraint: CASCADE delete with SurveyRespondent
    /// 
    /// 4. LeadLake (Domain Entity) [Auto-populated via LLM validation]
    ///    Database Table: LeadLakes
    ///    Purpose: Stores qualified leads from survey respondents
    ///    Populated When: SurveyService.SubmitResponseAsync() runs LLM validation
    ///    Columns:
    ///      - Id: int (Primary Key)
    ///      - FirstName/LastName/Email/Phone/Company/Job: string - Contact details from SurveyRespondent
    ///      - Location: string - Geographic location (optional from survey)
    ///      - Industry: LeadLakeIndustry enum - Mapped from SurveyRespondent.Industry string
    ///      - Intent: LeadIntent enum - Set to "High" for survey qualifiers
    ///      - UserId: string (FK to AspNetUsers)
    ///      - CreatedAt/UpdatedAt: datetime - EF Core tracking
    /// 
    /// DATA FLOW WITH DTOs:
    /// ====================
    /// 
    /// FLOW 1: Upload Survey (Admin)
    ///   Admin sends: SurveyDto
    ///     - Title: "Market Research: B2B SaaS Pain Points"
    ///     - Description: "Help us understand your business challenges"
    ///     - Questions: List[SurveyQuestionDto] where each has:
    ///       * Text: "What is your biggest operational challenge?"
    ///       * IsRequired: true
    ///   → Serialized to SurveyQuestion.QuestionsJson in database
    ///   → Retrieved via GetSurveyForAdmin() for admin editing
    /// 
    /// FLOW 2: Respondent Views Survey (Public)
    ///   GET /api/survey returns: SurveyClientDto
    ///     - Same structure as SurveyDto (title, description, questions)
    ///     - Deserialized from SurveyQuestion.QuestionsJson
    /// 
    /// FLOW 3: Submit Survey Response (Public) → AI-GATED LEAD QUALIFICATION
    ///   Client sends: SurveyResponseSubmissionDto containing:
    ///     - Respondent: SurveyRespondentSubmissionDto
    ///       * Email: "jane@company.com"
    ///       * FirstName/LastName/Phone/Company/Job/Industry
    ///     - Answers: List[string] = ["Our scheduling is manual and error-prone", "Excel spreadsheets", ...]
    ///   
    ///   Service processes:
    ///     1. Create/retrieve SurveyRespondent row (unique by email)
    ///     2. Create SurveyResponse row with AnswersJson = serialized answers
    ///     3. Call ILLMService to analyze Respondent + Answers for pain points
    ///     4. If LLM returns "QUALIFY": Create LeadLake row for sales outreach
    ///     5. If LLM returns "REJECT": Only store response, no lead created
    ///   
    ///   Returns: SurveyResponseResultDto
    ///     - Id: Response record ID
    ///     - Email/FirstName/LastName/Phone/Company/Job/Industry: From SurveyRespondent
    ///     - Answers: Echo of submitted answers
    ///     - SubmittedAt: Timestamp
    /// 
    /// FLOW 4: Admin Reviews Responses (Admin)
    ///   GET /api/survey/admin/responses returns: SurveySummaryDto
    ///     - Title: From SurveyQuestion
    ///     - TotalResponses: Count of SurveyResponse rows
    ///     - Responses: List[SurveyResponseResultDto] - All submissions with respondent info
    ///     - CollectedFrom/CollectedTo: Date range of submissions
    ///   
    ///   GET /api/survey/admin/responses/{id} returns: SurveyResponseResultDto
    ///     - Single response detail for deep-dive analysis
    ///
    /// KEY DESIGN PATTERNS:
    /// ====================
    /// • Global Single Survey: One survey per deployment (no ProductId)
    /// • LLM Qualification: Survey responses auto-filtered via AI for high-quality leads
    /// • JSON Serialization: Questions and Answers stored as JSON for flexibility
    /// • Async/Await: All operations support CancellationToken for graceful shutdown
    /// • Error Resilience: LLM failure does not block survey submission
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SurveyController : ControllerBase
    {
        private readonly ISurveyService _surveyService;

        public SurveyController(ISurveyService surveyService)
        {
            _surveyService = surveyService;
        }

        // ========================
        // PUBLIC - GET SURVEY
        // ========================

        /// <summary>
        /// Retrieves the current market research survey for public respondents.
        /// 
        /// Returns: SurveyClientDto
        ///   - Title: Survey heading
        ///   - Description: Survey introduction
        ///   - Questions: List of SurveyQuestionDto objects
        ///       * Id: Question identifier
        ///       * Text: Question text to display
        ///       * IsRequired: Whether answer is mandatory
        /// 
        /// Data Source: SurveyQuestion table (latest by UploadedAt)
        /// QuestionsJson column deserialized into SurveyQuestionDto list
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSurvey(CancellationToken cancellationToken)
        {
            var survey = await _surveyService.GetSurveyAsync(cancellationToken);
            if (survey == null)
                return NotFound("Market research survey has not been uploaded yet.");

            return Ok(survey);
        }

        // ========================
        // PUBLIC - SUBMIT RESPONSE
        // ========================

        /// <summary>
        /// Accepts and processes survey responses from respondents.
        /// 
        /// Input: SurveyResponseSubmissionDto
        ///   - Respondent: SurveyRespondentSubmissionDto
        ///     * Email: Must match for duplicate detection
        ///     * FirstName, LastName, Phone, Company, Job, Industry: Contact details
        ///   - Answers: List[string] with one answer per question
        /// 
        /// Process:
        ///   1. Creates/retrieves SurveyRespondent row (unique by email)
        ///   2. Creates SurveyResponse row with:
        ///      - AnswersJson: Serialized list of answers
        ///      - SubmittedAt: Current UTC timestamp
        ///   3. Calls ILLMService.GenerateTextAsync() to validate pain points
        ///   4. If qualified, adds SurveyRespondent to LeadLake table
        ///   5. If not qualified or LLM fails, response still saved
        /// 
        /// Returns: SurveyResponseResultDto
        ///   - Id: SurveyResponse.Id
        ///   - Email, Name, Company, Job, Industry: From SurveyRespondent
        ///   - Answers: Echo of submitted answers
        ///   - SubmittedAt: Record timestamp
        /// 
        /// Database Tables Modified:
        ///   - SurveyRespondents: INSERT (if new email) or SELECT (if existing)
        ///   - SurveyResponses: INSERT
        ///   - LeadLakes: INSERT (conditional, if LLM qualifies)
        /// </summary>
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitResponse([FromBody] SurveyResponseSubmissionDto submission, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _surveyService.SubmitResponseAsync(submission, cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }

        // ========================
        // ADMIN - RESPONSES & ANALYSIS
        // ========================

        /// <summary>
        /// Retrieves aggregated summary of all survey responses for admin analysis.
        /// 
        /// Authorization: Admin Policy required
        /// 
        /// Returns: SurveySummaryDto
        ///   - Title: From SurveyQuestion.Title
        ///   - TotalResponses: COUNT of SurveyResponse rows
        ///   - Responses: List[SurveyResponseResultDto]
        ///     Each item contains:
        ///       * Respondent contact: Email, FirstName, LastName, Phone, Company, Job, Industry
        ///       * Answers: Deserialized from SurveyResponse.AnswersJson
        ///       * SubmittedAt: When response was created
        ///   - CollectedFrom: MIN(SurveyResponse.SubmittedAt)
        ///   - CollectedTo: MAX(SurveyResponse.SubmittedAt)
        /// 
        /// Data Sources:
        ///   - SurveyQuestion table (title)
        ///   - SurveyResponse table (responses, timestamps)
        ///   - SurveyRespondent table (contact details via FK join)
        /// 
        /// Used For: Admin dashboard, data export, LLM batch analysis
        /// </summary>
        [Authorize(Policy = "Admin")]
        [HttpGet("admin/responses")]
        public async Task<IActionResult> GetAllResponses(CancellationToken cancellationToken)
        {
            var summary = await _surveyService.GetAllResponsesSummaryAsync(cancellationToken);
            return Ok(summary);
        }

        /// <summary>
        /// Retrieves a single survey response detail by ID for admin inspection.
        /// 
        /// Authorization: Admin Policy required
        /// 
        /// Path Parameter: responseId (int)
        ///   - SurveyResponse.Id primary key
        /// 
        /// Returns: SurveyResponseResultDto
        ///   - Id: SurveyResponse.Id
        ///   - Respondent Info: From SurveyRespondent row via FK
        ///     * Email, FirstName, LastName, Phone, Company, Job, Industry
        ///   - Answers: Deserialized from SurveyResponse.AnswersJson
        ///   - SubmittedAt: SurveyResponse.SubmittedAt timestamp
        /// 
        /// Data Lookup:
        ///   1. Query SurveyResponse by Id
        ///   2. Join to SurveyRespondent via SurveyRespondentId FK
        ///   3. Deserialize AnswersJson to List[string]
        /// 
        /// Used For: Admin deep-dive into individual responses
        /// </summary>
        [Authorize(Policy = "Admin")]
        [HttpGet("admin/responses/{responseId:int}")]
        public async Task<IActionResult> GetResponseById(int responseId, CancellationToken cancellationToken)
        {
            var response = await _surveyService.GetResponseByIdAsync(responseId, cancellationToken);
            if (response == null)
                return NotFound($"Response {responseId} not found.");

            return Ok(response);
        }

        // ========================
        // ADMIN - SURVEY MANAGEMENT
        // ========================

        /// <summary>
        /// Creates or updates the global market research survey.
        /// 
        /// Authorization: Admin Policy required
        /// HTTP Method: PUT (idempotent update)
        /// 
        /// Input: SurveyDto
        ///   - Title: string (e.g., "B2B SaaS Pain Points Q3 2026")
        ///   - Description: string (e.g., "Help us understand your business challenges")
        ///   - Questions: List[SurveyQuestionDto]
        ///     Each question contains:
        ///       * Id: Unique identifier (for client correlation)
        ///       * Text: Question text displayed to respondents
        ///       * IsRequired: Whether answer is mandatory
        /// 
        /// Process:
        ///   1. Entire SurveyDto serialized to JSON
        ///   2. Stored in SurveyQuestion table:
        ///      - QuestionsJson: Serialized DTO
        ///      - UploadedAt: Current UTC timestamp
        ///      - UserId: From authenticated admin
        ///   3. Previous survey versions kept in database (no deletion)
        /// 
        /// Database Table: SurveyQuestions
        ///   Columns written:
        ///     - Title: From input
        ///     - Description: From input
        ///     - QuestionsJson: Serialized List[SurveyQuestionDto]
        ///     - UploadedAt: DateTime.UtcNow
        ///     - UserId: From HttpContext claims
        ///     - CreatedAt/UpdatedAt: EF Core timestamps
        /// 
        /// Returns: 204 NoContent on success
        /// </summary>
        [HttpPut("admin")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> UploadSurvey([FromBody] SurveyDto survey, CancellationToken cancellationToken)
        {
            if (survey == null)
                return BadRequest("Survey payload is required.");

            await _surveyService.UploadSurveyAsync(survey, cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Retrieves the current survey definition for admin editing/review.
        /// 
        /// Authorization: Admin Policy required
        /// 
        /// Returns: SurveyDto
        ///   - Title: Survey name
        ///   - Description: Survey introduction
        ///   - Questions: List[SurveyQuestionDto]
        ///     Each contains:
        ///       * Id: Question identifier
        ///       * Text: Question text
        ///       * IsRequired: Mandatory flag
        /// 
        /// Data Source:
        ///   - SurveyQuestion table (latest by UploadedAt DESC)
        ///   - QuestionsJson column deserialized into SurveyDto
        /// 
        /// Used For: Admin dashboard to edit/review survey before sending to respondents
        /// </summary>
        [HttpGet("admin/survey")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> GetSurveyForAdmin(CancellationToken cancellationToken)
        {
            var survey = await _surveyService.GetSurveyForAdminAsync(cancellationToken);
            if (survey == null)
                return NotFound("No survey has been uploaded yet.");

            return Ok(survey);
        }
    }
}
