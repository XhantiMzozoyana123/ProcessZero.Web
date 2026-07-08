using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;

namespace ProcessZero.Web.Controllers
{
    /// <summary>
    /// Market Research Survey Management API (Multiple Surveys)
    /// 
    /// ╔══════════════════════════════════════════════════════════════════════════════╗
    /// ║                      MULTI-SURVEY SYSTEM DESIGN                              ║
    /// ╚══════════════════════════════════════════════════════════════════════════════╝
    /// 
    /// Each survey is INDEPENDENT and includes 7 mandatory contact information questions:
    ///   Questions[0]: Email Address (REQUIRED)
    ///   Questions[1]: First Name (REQUIRED)
    ///   Questions[2]: Last Name (REQUIRED)
    ///   Questions[3]: Phone Number (REQUIRED)
    ///   Questions[4]: Company (optional)
    ///   Questions[5]: Job Title (optional)
    ///   Questions[6]: Industry (optional)
    ///   Questions[7+]: Admin-provided business/pain point questions
    /// 
    /// These base contact questions are AUTOMATICALLY PREPENDED to every survey.
    /// 
    /// SURVEY ENDPOINTS:
    /// =================
    /// Public:
    ///   GET    /api/survey/{id}              - Get specific survey to fill out
    ///   POST   /api/survey/submit            - Submit response to a survey
    /// 
    /// Admin:
    ///   GET    /api/survey                   - List all surveys
    ///   POST   /api/survey                   - Create new survey
    ///   PUT    /api/survey/{id}              - Update existing survey
    ///   DELETE /api/survey/{id}              - Delete survey
    ///   GET    /api/survey/{id}/admin        - Get survey for editing
    ///   GET    /api/survey/{id}/responses    - Get all responses for survey
    ///   GET    /api/survey/response/{id}     - Get single response by ID
    /// 
    /// DATA MODEL:
    /// ============
    /// SurveyQuestions:
    ///   - Id: Auto-incremented primary key
    ///   - Name: Unique survey identifier
    ///   - Title: Display title
    ///   - Description: Survey purpose
    ///   - Status: Active|Draft|Archived|Closed
    ///   - QuestionsJson: Serialized questions (contact + business)
    ///   - UploadedAt: Last modified timestamp
    /// 
    /// SurveyRespondents:
    ///   - Id: Auto-incremented primary key
    ///   - SurveyId: Foreign key (same email can exist in different surveys)
    ///   - Email, FirstName, LastName, Phone, Company, Job, Industry
    ///   - Unique constraint on (SurveyId, Email)
    /// 
    /// SurveyResponses:
    ///   - Id: Auto-incremented primary key
    ///   - SurveyId: Foreign key to survey
    ///   - SurveyRespondentId: Foreign key to respondent
    ///   - AnswersJson: Full answers array (0-6 contact + 7+ business)
    ///   - SubmittedAt: Response timestamp
    /// 
    /// LeadLakes:
///   - Auto-populated by LLM qualification
///   - Email-based deduplication (global across surveys)
///
/// ╔══════════════════════════════════════════════════════════════════════════════╗
/// ║              ERROR 400 (BAD REQUEST) — FRONTEND TROUBLESHOOTING              ║
/// ╚══════════════════════════════════════════════════════════════════════════════╝
///
/// This controller is decorated with [ApiController] (see class declaration below).
/// That attribute turns on ASP.NET Core's AUTOMATIC model-state validation, which
/// means a 400 is returned by the framework ITSELF — BEFORE your action method ever
/// executes — whenever the incoming request cannot be bound to the expected model.
/// The most common reasons the UI sees a 400 from this controller are:
///
///   1. MISSING HEADER: The POST request does not include
///      "Content-Type: application/json". The model binder refuses to read the
///      body and immediately returns 400 without calling the action.
///        ➜ FIX: Always send  fetch(url, { headers: { 'Content-Type': 'application/json' }, ... })
///
///   2. MALFORMED / INVALID JSON: The body is not valid JSON, or values do not
///      match the expected C# types. Examples that trigger 400 automatically:
///        - surveyId sent as a non-numeric string ("abc") for an [HttpPost] body
///          where SurveyResponseSubmissionDto.SurveyId is `int`.
///        - answers is not an array of strings, or is missing entirely.
///        - Extra/unknown properties are fine (ignored), but a broken JSON structure
///          (e.g. trailing comma, unquoted key) is NOT.
///        ➜ FIX: Validate the JSON.stringify() payload in the browser devtools
///              Network tab before sending.
///
///   3. EMPTY / NULL BODY ON CREATE OR UPDATE:
///      In CreateSurvey and UpdateSurvey we explicitly guard:
///          if (survey == null) return BadRequest("Survey payload is required.");
///      If the JSON body fails to deserialize into SurveyDto (e.g. wrong shape),
///      `survey` arrives null and the action returns 400 by design.
///
///   4. ROUTE / TYPE MISMATCH ON GET/PUT/DELETE:
///      Endpoints like GET /api/survey/{id:int} require an integer segment.
///      If the UI calls /api/survey/abc the routing constraint fails and the
///      framework returns 400 (or 404 if no matching route) before the action runs.
///
/// ⚠  IMPORTANT STATUS-CODE INCONSISTENCY WORTH KNOWING (documented for the UI team):
///    - SubmitResponse maps ArgumentException      -> 400 BadRequest
///                            InvalidOperationException -> 404 NotFound
///      BUT the service throws InvalidOperationException for "answers < 7" and for
///      missing required contact fields (email/firstName/lastName/phone). So those
///      VALIDATION failures actually come back as 404, NOT 400. The UI should treat
///      both 400 and 404 from /api/survey/submit as "the submission was rejected".
///    - CreateSurvey/UpdateSurvey instead map InvalidOperationException -> 400,
///      so the same exception type yields a different code depending on the endpoint.
///      This is a known inconsistency; until it is unified, the frontend must handle
///      both 400 and 404 as failure for these endpoints.
///
/// SUMMARY FOR FRONTEND DEVELOPERS:
///   * A 400 from this controller is almost always a request-FORMAT problem
///     (missing Content-Type, bad JSON, wrong types, null body) — NOT a business
///     logic rejection. Inspect the Network tab request headers & body first.
///   * A 404 with a message like "Survey not found", "not active", "Email required",
///     or "must include contact information" means the server received the request
///     but rejected the DATA. Show that message to the user.
/// </summary>
    [ApiController]
    [Route("api/survey")]
    public class SurveyController : ControllerBase
    {
        private readonly ISurveyService _surveyService;

        public SurveyController(ISurveyService surveyService)
        {
            _surveyService = surveyService;
        }

        // ================== PUBLIC / RESPONDENT ENDPOINTS ==================

        /// <summary>
        /// Retrieves a specific survey for public respondents to fill out.
        /// 
        /// Route Parameter: id (int)
        ///   - SurveyQuestion.Id of the survey to retrieve
        /// 
        /// Returns: SurveyClientDto
        ///   - Id: Survey identifier
        ///   - Name: Unique survey name
        ///   - Title: Display title
        ///   - Description: Survey introduction
        ///   - Status: Current status (Active, Draft, Archived, Closed)
        ///   - Questions: List[SurveyQuestionDto]
        ///       * Questions[0-6]: Mandatory contact information questions
        ///       * Questions[7+]: Admin-provided business/pain point questions
        /// 
        /// Error Cases:
        ///   404 Not Found: Survey not found or not Active status
        /// 
        /// Frontend should render all questions in order.
        /// Respondent should prepare answers array with same length.
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetSurvey(int id, CancellationToken cancellationToken)
        {
            var survey = await _surveyService.GetSurveyAsync(id, cancellationToken);
            if (survey == null)
                return NotFound($"Survey {id} not found or is not active.");

            return Ok(survey);
        }

        /// <summary>
        /// Submit responses to a specific survey.
        /// 
        /// Input: SurveyResponseSubmissionDto
        ///   {
        ///     "surveyId": 1,
        ///     "answers": [
        ///       "jane@company.com",                        // [0] Email (REQUIRED)
        ///       "Jane",                                    // [1] FirstName (REQUIRED)
        ///       "Doe",                                     // [2] LastName (REQUIRED)
        ///       "+1-555-0123",                             // [3] Phone (REQUIRED)
        ///       "Acme Corp",                               // [4] Company (optional)
        ///       "Operations Manager",                      // [5] Job (optional)
        ///       "Manufacturing",                           // [6] Industry (optional)
        ///       "Our scheduling process is manual",        // [7+] Business answers
        ///       "We use Excel and email spreadsheets",
        ///       "$15,000/month in labor costs"
        ///     ]
        ///   }
        /// 
        /// Process:
        ///   1. Validates survey exists and is Active
        ///   2. Validates answers[0-6] contains all required contact info
        ///   3. Extracts contact: email, firstName, lastName, phone, company, job, industry
        ///   4. Creates/retrieves SurveyRespondent (unique by SurveyId + Email)
        ///   5. Creates SurveyResponse with full answers array
        ///   6. Calls ILLMService to validate pain points
        ///   7. If qualified, adds respondent to LeadLake
        /// 
        /// Returns: SurveyResponseResultDto
        ///   - Id: SurveyResponse.Id
        ///   - SurveyId: The survey this response belongs to
        ///   - Email, FirstName, LastName, Phone, Company, Job, Industry: Contact fields
        ///   - Answers: Business answers only (indices 7+)
        ///   - SubmittedAt: Response timestamp
        /// 
        /// Error Cases:
        ///   400 Bad Request: Invalid request body, insufficient answers
        ///   404 Not Found: Survey not found, contact fields missing/invalid
        ///
        /// ────────────────────────────────────────────────────────────────────────
        /// WHY THE FRONTEND GETS A 400 / 404 FROM THIS ENDPOINT (read this first):
        /// ────────────────────────────────────────────────────────────────────────
        /// Because the controller is [ApiController], the framework validates the
        /// [FromBody] SurveyResponseSubmissionDto BEFORE this method runs:
        ///
        ///   • 400 (framework, before method body):
        ///       - Missing "Content-Type: application/json" header  → binder rejects body.
        ///       - Body is not valid JSON, or `surveyId` is not an int, or `answers`
        ///         is not a JSON array of strings. The model simply cannot bind.
        ///       The catch blocks below are NEVER reached in these cases.
        ///
        ///   • 400 (our code, ArgumentException branch):
        ///       - Currently the service does NOT throw ArgumentException from this
        ///         path, so a 400 from OUR code is rare here. If it ever does, the
        ///         message is returned verbatim to the UI.
        ///
        ///   • 404 (our code, InvalidOperationException branch) — THE COMMON CASE:
        ///       The service throws InvalidOperationException for ALL of the following,
        ///       and they are mapped to 404 (NOT 400) here:
        ///         - Survey not found / survey id does not exist.
        ///         - Survey exists but Status != "Active".
        ///         - answers.Count < 7  → "must include contact information...".
        ///         - answers[0] (email) empty/whitespace  → "Email address is required."
        ///         - answers[1] (firstName) empty → "First name is required."
        ///         - answers[2] (lastName) empty  → "Last name is required."
        ///         - answers[3] (phone) empty     → "Phone number is required."
        ///       ⚠ The docstring above says these are "400" but they actually return
        ///         404. This is a KNOWN INCONSISTENCY — the UI must treat BOTH 400
        ///         and 404 from /api/survey/submit as a rejected submission and
        ///         display ex.Message to the user.
        ///
        /// QUICK FRONTEND CHECKLIST WHEN YOU SEE A 400 ON SUBMIT:
        ///   1. DevTools → Network → look at the request. Is "Content-Type:
        ///      application/json" present? If not, add it.
        ///   2. Is the body valid JSON.parse()-able? Open it in the console.
        ///   3. Is `surveyId` a real number (not a string, not null)?
        ///   4. Does `answers` have at least 7 entries, with indices 0-3 non-empty?
        /// </summary>
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitResponse([FromBody] SurveyResponseSubmissionDto submission, CancellationToken cancellationToken)
        {
            // NOTE: If `submission` fails to bind (bad JSON / wrong types / no
            // Content-Type header), ASP.NET Core returns 400 automatically and this
            // method is never entered. The try/catch below only handles exceptions
            // thrown by the service after a SUCCESSFUL model bind.
            try
            {
                var result = await _surveyService.SubmitResponseAsync(submission, cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                // Business/data rejections (survey missing, not active, missing
                // contact info). Returned as 404 per current mapping — see note above.
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                // Rare for this endpoint; genuine bad-argument from the service.
                return BadRequest(ex.Message);
            }
        }

        // ================== ADMIN ENDPOINTS - SURVEY LISTING ==================

        /// <summary>
        /// List all surveys (active, draft, archived, etc.)
        /// 
        /// Authorization: Admin Policy required
        /// 
        /// Returns: List[SurveysListDto]
        ///   - Id: Survey identifier
        ///   - Name: Unique survey name
        ///   - Title: Display title
        ///   - Status: Current status
        ///   - ResponseCount: Number of responses collected
        ///   - UploadedAt: Last modified timestamp
        /// 
        /// Ordered by UploadedAt descending (newest first).
        /// </summary>
        [Authorize(Policy = "Admin")]
        [HttpGet]
        public async Task<IActionResult> ListSurveys(CancellationToken cancellationToken)
        {
            var surveys = await _surveyService.ListSurveysAsync(cancellationToken);
            return Ok(surveys);
        }

        // ================== ADMIN ENDPOINTS - SURVEY CRUD ==================

        /// <summary>
        /// Create a new survey.
        /// 
        /// Authorization: Admin Policy required
        /// 
        /// Input: SurveyDto
        ///   - Name: Unique survey identifier (required, e.g., "Q1 2025 Market Research")
        ///   - Title: Display title shown to respondents
        ///   - Description: Survey introduction/purpose
        ///   - Status: "Active" | "Draft" | "Archived" | "Closed" (default: "Active")
        ///   - Questions: Business/pain point questions only
        ///     (Contact questions are automatically prepended by service)
        /// 
        /// Process:
        ///   1. Validates Name is provided and unique
        ///   2. Prepends 7 mandatory contact questions
        ///   3. Appends admin-provided business questions
        ///   4. Serializes complete survey to JSON
        ///   5. Stores in SurveyQuestions table
        /// 
        /// Returns: SurveyDto (201 Created)
        ///   - Id: Newly assigned survey ID
        ///   - Complete survey structure with contact + business questions
        /// 
        /// Error Cases:
        ///   400 Bad Request: Missing required fields, invalid status
        ///   409 Conflict: Duplicate survey name
        /// </summary>
        [Authorize(Policy = "Admin")]
        [HttpPost]
        [Route("", Name = "CreateSurveyRoot")]
        [Route("s", Name = "CreateSurveyPlural")]
        public async Task<IActionResult> CreateSurvey([FromBody] SurveyDto survey, CancellationToken cancellationToken)
        {
            // 400 SOURCE #1 (explicit): If the JSON body could not be deserialized
            // into SurveyDto (wrong shape, or missing Content-Type: application/json
            // so the binder read nothing), `survey` is null and we return 400 here.
            if (survey == null)
                return BadRequest("Survey payload is required.");

            try
            {
                var created = await _surveyService.CreateSurveyAsync(survey, cancellationToken);
                return CreatedAtAction(nameof(GetSurveyForAdmin), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                // 400 SOURCE #2 (service): "Survey name is required." when Name is
                // blank. NOTE: this endpoint maps InvalidOperationException -> 400,
                // whereas SubmitResponse maps the SAME exception type -> 404. This is
                // the documented inconsistency; the UI must handle both codes as a
                // creation failure and surface ex.Message.
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Update an existing survey.
        /// 
        /// Authorization: Admin Policy required
        /// 
        /// Route Parameter: id (int)
        ///   - SurveyQuestion.Id to update
        /// 
        /// Input: SurveyDto
        ///   - Id: Must match route parameter
        ///   - Name, Title, Description, Status: Updated values
        ///   - Questions: New business questions
        /// 
        /// Process:
        ///   1. Validates survey exists
        ///   2. Prepends contact questions to updated business questions
        ///   3. Serializes and updates SurveyQuestions table
        ///   4. Sets UploadedAt to current timestamp
        /// 
        /// Returns: SurveyDto (200 OK)
        ///   - Updated survey structure
        /// 
        /// Error Cases:
        ///   404 Not Found: Survey not found
        ///   400 Bad Request: Invalid request
        /// </summary>
        [Authorize(Policy = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateSurvey(int id, [FromBody] SurveyDto survey, CancellationToken cancellationToken)
        {
            // 400 SOURCE #1 (explicit): Same null-body guard as CreateSurvey. A 400
            // here means the request body did not bind to SurveyDto — usually a
            // missing "Content-Type: application/json" header or malformed JSON.
            if (survey == null)
                return BadRequest("Survey payload is required.");

            if (!survey.Id.HasValue || survey.Id <= 0)
                survey.Id = id;

            try
            {
                var updated = await _surveyService.UpdateSurveyAsync(survey, cancellationToken);
                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                // NOTE: UpdateSurvey maps InvalidOperationException -> 404 (unlike
                // CreateSurvey which maps it -> 400). So "Survey ID is required" and
                // "Survey {id} not found" come back as 404 here. The UI should treat
                // 400 and 404 from PUT /api/survey/{id} both as a failed update.
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Delete a survey.
        /// 
        /// Authorization: Admin Policy required
        /// 
        /// Route Parameter: id (int)
        ///   - SurveyQuestion.Id to delete
        /// 
        /// Process:
        ///   1. Validates survey exists
        ///   2. Hard delete: removes survey
        ///   3. Cascade delete: removes all SurveyResponses and SurveyRespondents
        /// 
        /// Returns: 204 NoContent on success
        /// 
        /// Error Cases:
        ///   404 Not Found: Survey not found
        /// </summary>
        [Authorize(Policy = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteSurvey(int id, CancellationToken cancellationToken)
        {
            try
            {
                await _surveyService.DeleteSurveyAsync(id, cancellationToken);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }

        // ================== ADMIN ENDPOINTS - SURVEY DETAILS ==================

        /// <summary>
        /// Retrieve a survey definition for admin editing/review.
        /// 
        /// Authorization: Admin Policy required
        /// 
        /// Route Parameter: id (int)
        ///   - SurveyQuestion.Id to retrieve
        /// 
        /// Returns: SurveyDto
        ///   - Complete survey structure including contact + business questions
        ///   - Ready for editing and update
        /// 
        /// Error Cases:
        ///   404 Not Found: Survey not found
        /// </summary>
        [Authorize(Policy = "Admin")]
        [HttpGet("{id:int}/admin")]
        public async Task<IActionResult> GetSurveyForAdmin(int id, CancellationToken cancellationToken)
        {
            var survey = await _surveyService.GetSurveyForAdminAsync(id, cancellationToken);
            if (survey == null)
                return NotFound($"Survey {id} not found.");

            return Ok(survey);
        }

        // ================== ADMIN ENDPOINTS - RESPONSES & ANALYSIS ==================

        /// <summary>
        /// Retrieve all responses collected for a specific survey.
        /// 
        /// Authorization: Admin Policy required
        /// 
        /// Route Parameter: id (int)
        ///   - SurveyQuestion.Id to retrieve responses for
        /// 
        /// Returns: SurveySummaryDto
        ///   - SurveyId: The survey ID
        ///   - Name, Title: Survey metadata
        ///   - TotalResponses: Count of responses collected
        ///   - Responses: List[SurveyResponseResultDto]
        ///     Each contains:
        ///       * Id: SurveyResponse.Id
        ///       * Contact fields: Email, FirstName, LastName, Phone, Company, Job, Industry
        ///       * Answers: Business question answers (indices 7+)
        ///       * SubmittedAt: Response timestamp
        ///   - CollectedFrom: Earliest response timestamp
        ///   - CollectedTo: Latest response timestamp
        /// 
        /// Data Sources:
        ///   - SurveyQuestion table (survey metadata)
        ///   - SurveyResponse table (responses, filtered by SurveyId)
        ///   - SurveyRespondent table (contact details via FK)
        /// 
        /// Ordered by SubmittedAt descending (newest first).
        /// Used for: Admin dashboard, LLM analysis, data export.
        /// 
        /// Error Cases:
        ///   404 Not Found: Survey not found
        /// </summary>
        [Authorize(Policy = "Admin")]
        [HttpGet("{id:int}/responses")]
        public async Task<IActionResult> GetSurveyResponses(int id, CancellationToken cancellationToken)
        {
            var summary = await _surveyService.GetAllResponsesSummaryAsync(id, cancellationToken);
            return Ok(summary);
        }

        /// <summary>
        /// Retrieve a single survey response detail by ID.
        /// 
        /// Authorization: Admin Policy required
        /// 
        /// Route Parameter: responseId (int)
        ///   - SurveyResponse.Id to retrieve
        /// 
        /// Returns: SurveyResponseResultDto
        ///   - Id: SurveyResponse.Id
        ///   - SurveyId: Which survey this response belongs to
        ///   - Contact fields: Email, FirstName, LastName, Phone, Company, Job, Industry
        ///   - Answers: Business question answers (indices 7+)
        ///   - SubmittedAt: Response submission timestamp
        /// 
        /// Data Lookup:
        ///   1. Query SurveyResponse by Id
        ///   2. Join to SurveyRespondent via FK
        ///   3. Deserialize AnswersJson to extract contact + business answers
        /// 
        /// Error Cases:
        ///   404 Not Found: Response not found
        /// 
        /// Used for: Admin deep-dive, individual respondent review.
        /// </summary>
        [Authorize(Policy = "Admin")]
        [HttpGet("response/{responseId:int}")]
        public async Task<IActionResult> GetResponseById(int responseId, CancellationToken cancellationToken)
        {
            var response = await _surveyService.GetResponseByIdAsync(responseId, cancellationToken);
            if (response == null)
                return NotFound($"Response {responseId} not found.");

            return Ok(response);
        }
    }
}

