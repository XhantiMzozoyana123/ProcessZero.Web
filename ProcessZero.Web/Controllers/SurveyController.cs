using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;

namespace ProcessZero.Web.Controllers
{
    /// <summary>
    /// Market Research Survey Management API
    /// 
    /// ╔══════════════════════════════════════════════════════════════════════════════╗
    /// ║                          CRITICAL SYSTEM DESIGN                              ║
    /// ╚══════════════════════════════════════════════════════════════════════════════╝
    /// 
    /// Every survey ALWAYS includes 7 mandatory contact information questions:
    ///   Questions[0]: Email Address (REQUIRED)
    ///   Questions[1]: First Name (REQUIRED)
    ///   Questions[2]: Last Name (REQUIRED)
    ///   Questions[3]: Phone Number (REQUIRED)
    ///   Questions[4]: Company (optional)
    ///   Questions[5]: Job Title (optional)
    ///   Questions[6]: Industry (optional)
    ///   Questions[7+]: Admin-provided business/pain point questions
    /// 
    /// These base contact questions are AUTOMATICALLY PREPENDED to every survey
    /// when an admin uploads business questions via PUT /api/survey/admin.
    /// 
    /// RESPONDENT SUBMISSION FORMAT:
    /// =============================
    /// Client sends answers array with ONE response per question:
    ///   POST /api/survey/submit
    ///   {
    ///     "answers": [
    ///       "jane@company.com",                          // [0] Email
    ///       "Jane",                                      // [1] FirstName
    ///       "Doe",                                       // [2] LastName
    ///       "+1-555-0123",                               // [3] Phone
    ///       "Acme Corp",                                 // [4] Company
    ///       "Operations Manager",                        // [5] Job
    ///       "Manufacturing",                             // [6] Industry
    ///       "Our scheduling process is completely manual",  // [7] Q1
    ///       "We use Excel and email spreadsheets",          // [8] Q2
    ///       "$15,000/month in labor costs"                  // [9] Q3
    ///     ]
    ///   }
    /// 
    /// DATA EXTRACTION & STORAGE:
    /// ==========================
    /// SurveyService.SubmitResponseAsync:
    ///   1. Validates answers[0-6] contains valid contact info
    ///   2. Extracts contact from answers: email, firstName, lastName, phone, company, job, industry
    ///   3. Creates/finds SurveyRespondent by email (prevents duplicates)
    ///   4. Stores FULL answers array (0-6 contact + 7+ business) in SurveyResponse.AnswersJson
    ///   5. Calls ILLMService with full context (contact + business answers)
    ///   6. If LLM returns "QUALIFY": Creates LeadLake record for sales outreach
    ///   7. Returns SurveyResponseResultDto with contact as separate fields
    /// 
    /// ENTITIES & TABLES:
    /// ==================
    /// 
    /// 1. SurveyQuestions Table
    ///    - Stores global survey definition with ALL questions (contact + business)
    ///    - QuestionsJson: Serialized SurveyDto with Questions array
    ///    - Latest survey retrieved by OrderByDescending(UploadedAt)
    /// 
    /// 2. SurveyRespondents Table
    ///    - Email: Unique identifier (prevents duplicate contacts)
    ///    - FirstName, LastName, Phone: Required contact fields
    ///    - Company, Job, Industry: Optional contact fields
    ///    - Populated when user submits survey (contact extracted from answers[0-6])
    /// 
    /// 3. SurveyResponses Table
    ///    - SurveyRespondentId: Foreign key to respondent
    ///    - AnswersJson: Serialized full answers array (indices 0-6 contact + 7+ business)
    ///    - SubmittedAt: Timestamp of submission
    /// 
    /// 4. LeadLakes Table (Auto-populated)
    ///    - Created ONLY if LLM qualification passes
    ///    - Contact details copied from SurveyRespondent
    ///    - Industry: Mapped from string to LeadLakeIndustry enum
    ///    - Intent: Set to "High" (survey respondents show high intent)
    /// 
    /// ADMIN WORKFLOW:
    /// ===============
    /// 1. Admin creates business questions: "What's your biggest challenge?", etc.
    /// 2. Admin POSTs to PUT /api/survey/admin with SurveyDto
    /// 3. Service automatically prepends 7 contact questions
    /// 4. Complete survey stored in database
    /// 5. Respondents access via GET /api/survey (see all 14 questions: 7 contact + 7 business)
    /// 
    /// PUBLIC RESPONDENT WORKFLOW:
    /// ===========================
    /// 1. Respondent GETs /api/survey (receives all questions including contact)
    /// 2. Frontend renders all questions in order
    /// 3. Respondent fills contact info AND business pain point answers
    /// 4. POSTs single answers array with 14 values to /api/survey/submit
    /// 5. Backend extracts contact, creates respondent, stores response
    /// 6. LLM analyzes answers + contact context
    /// 7. If qualified, added to LeadLake for sales outreach
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
        /// CRITICAL: The answers array MUST include contact information in the first 7 positions.
        /// 
        /// Input: SurveyResponseSubmissionDto
        ///   {
        ///     "answers": [
        ///       "jane@company.com",                          // [0] Email (REQUIRED)
        ///       "Jane",                                      // [1] FirstName (REQUIRED)
        ///       "Doe",                                       // [2] LastName (REQUIRED)
        ///       "+1-555-0123",                               // [3] Phone (REQUIRED)
        ///       "Acme Corp",                                 // [4] Company (optional)
        ///       "Operations Manager",                        // [5] Job (optional)
        ///       "Manufacturing",                             // [6] Industry (optional)
        ///       "Our scheduling process is manual",          // [7+] Business question answers
        ///       "We use Excel and email",
        ///       "$15,000/month in labor costs"
        ///     ]
        ///   }
        /// 
        /// Process:
        ///   1. Validates answers[0-6] contains all required contact info
        ///   2. Extracts contact: email, firstName, lastName, phone, company, job, industry
        ///   3. Creates or retrieves SurveyRespondent (unique by email)
        ///   4. Creates SurveyResponse row with:
        ///      - AnswersJson: FULL answers array (0-6 contact + 7+ business)
        ///      - SubmittedAt: Current UTC timestamp
        ///      - SurveyRespondentId: Foreign key to respondent
        ///   5. Calls ILLMService.GenerateTextAsync() with contact + business answers
        ///   6. If LLM returns "QUALIFY": Adds respondent to LeadLake table
        ///   7. If LLM returns "REJECT" or error: Response still saved, no lead created
        /// 
        /// Returns: SurveyResponseResultDto
        ///   - Id: SurveyResponse.Id
        ///   - Email, FirstName, LastName, Phone, Company, Job, Industry: From extracted contact
        ///   - Answers: Business question answers ONLY (indices 7+)
        ///   - SubmittedAt: Response submission timestamp
        /// 
        /// Database Tables Modified:
        ///   - SurveyRespondents: INSERT new (if email not found) or use existing
        ///   - SurveyResponses: INSERT (stores full answers including contact)
        ///   - LeadLakes: INSERT (conditional, only if LLM qualifies)
        /// 
        /// Error Cases:
        ///   400 Bad Request: Insufficient answers, missing required contact fields
        ///   404 Not Found: No survey uploaded yet
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
