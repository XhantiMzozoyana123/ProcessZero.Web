using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using System.Text.Json;

/*
 * RELATIONAL SURVEY SYSTEM — Entity reference (used by this controller / service):
 *
 * The survey system was redesigned to use a proper relational structure instead
 * of serializing the entire survey and all answers into JSON blobs. Each question
 * and each answer is now stored as its own row, enabling normalised querying,
 * indexing, and reporting.
 *
 * Survey (table: Surveys)
 * - Id (int)                : primary key (inherited from BaseEntity)
 * - Name (string)           : unique survey identifier
 * - Title (string)          : display title shown to respondents
 * - Description (string)    : human-friendly description
 * - Status (string)         : "Active" | "Draft" | "Archived" | "Closed"
 * - UploadedAt (DateTime)   : last uploaded/modified timestamp
 *
 * SurveyQuestion (table: SurveyQuestions)
 * - Id (int)                : primary key (inherited from BaseEntity)
 * - SurveyId (int)          : FK to Surveys.Id
 * - Text (string)           : the question prompt
 * - Order (int)             : display order (0-6 = contact, 7+ = business)
 * - Category (enum)         : Contact | Business
 * - Type (enum)             : MultipleChoice | OpenEnded
 * - IsRequired (bool)       : whether the question must be answered
 *
 * SurveyQuestionOption (table: SurveyQuestionOptions)
 * - Id (int)                : primary key (inherited from BaseEntity)
 * - SurveyQuestionId (int)  : FK to SurveyQuestions.Id
 * - Text (string)           : option text (one row per option for MultipleChoice)
 * - Order (int)             : display order of the option
 *
 * SurveyRespondent (table: SurveyRespondents)
 * - Id (int)               : primary key (inherited from BaseEntity)
 * - SurveyId (int)         : FK to Surveys.Id (unique per SurveyId + Email)
 * - Email / FirstName / LastName / Phone / Company / Job / Industry (string)
 *
 * SurveyResponse (table: SurveyResponses)
 * - Id (int)               : primary key (inherited from BaseEntity)
 * - SurveyId (int)         : FK to Surveys.Id
 * - SurveyRespondentId(int): FK to SurveyRespondent.Id
 * - SubmittedAt (DateTime) : when the respondent submitted the survey
 *
 * SurveyAnswer (table: SurveyAnswers)
 * - Id (int)               : primary key (inherited from BaseEntity)
 * - SurveyResponseId (int) : FK to SurveyResponses.Id
 * - SurveyQuestionId (int) : FK to SurveyQuestions.Id
 * - AnswerText (string)    : the respondent's answer for that specific question
 *
 * The controller delegates all business logic to ISurveyService, which enforces
 * required contact fields, survey status, and writes each answer as a row.
 */

namespace ProcessZero.Web.Controllers
{
    [ApiController]
    [Route("api/survey")]
    public class SurveyController : ControllerBase
    {
        private readonly ISurveyService _surveyService;

        public SurveyController(ISurveyService surveyService)
        {
            _surveyService = surveyService;
        }

        /// <summary>
        /// GET api/survey/{id}
        /// Public endpoint. Returns the active survey with all its questions
        /// (contact questions 0-6 prepended, business questions 7+).
        /// Questions are read from the SurveyQuestions table, not from JSON.
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
        /// POST api/survey/submit
        /// Public endpoint. Accepts a submission body { surveyId, answers[] }.
        /// Each answer maps positionally to a SurveyQuestion row (ordered by Order).
        /// The service stores one SurveyAnswer row per question.
        /// </summary>
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitResponse([FromBody] System.Text.Json.JsonElement submissionBody, CancellationToken cancellationToken)
        {
            try
            {
                if (submissionBody.ValueKind == System.Text.Json.JsonValueKind.Null ||
                    submissionBody.ValueKind == System.Text.Json.JsonValueKind.Undefined)
                {
                    return BadRequest("Request body is required and must be valid JSON.");
                }

                bool TryGetPropertyIgnoreCase(System.Text.Json.JsonElement obj, string propertyName, out System.Text.Json.JsonElement value)
                {
                    foreach (var property in obj.EnumerateObject())
                    {
                        if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                        {
                            value = property.Value;
                            return true;
                        }
                    }

                    value = default;
                    return false;
                }

                if (!TryGetPropertyIgnoreCase(submissionBody, "surveyId", out var surveyIdElement) ||
                    surveyIdElement.ValueKind != System.Text.Json.JsonValueKind.Number ||
                    !surveyIdElement.TryGetInt32(out _))
                {
                    return BadRequest("Property 'surveyId' is required and must be an integer.");
                }

                if (!TryGetPropertyIgnoreCase(submissionBody, "answers", out var answersElement) ||
                    answersElement.ValueKind != System.Text.Json.JsonValueKind.Array)
                {
                    return BadRequest("Property 'answers' is required and must be an array of strings.");
                }

                foreach (var answer in answersElement.EnumerateArray())
                {
                    if (answer.ValueKind != System.Text.Json.JsonValueKind.String)
                        return BadRequest("All items in 'answers' must be strings.");
                }

                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var submission = System.Text.Json.JsonSerializer.Deserialize<SurveyResponseSubmissionDto>(
                    submissionBody.GetRawText(),
                    options);

                if (submission == null)
                    return BadRequest("Unable to parse submission payload.");

                var result = await _surveyService.SubmitResponseAsync(submission, cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (System.Text.Json.JsonException)
            {
                return BadRequest("Malformed JSON in request body.");
            }
        }

        /// <summary>
        /// GET api/survey
        /// Admin endpoint. Lists all surveys with response counts.
        /// Reads from the Surveys table.
        /// </summary>
        [Authorize(Policy = "Admin")]
        [HttpGet]
        public async Task<IActionResult> ListSurveys(CancellationToken cancellationToken)
        {
            var surveys = await _surveyService.ListSurveysAsync(cancellationToken);
            return Ok(surveys);
        }

        /// <summary>
        /// POST api/survey  (or api/survey/s)
        /// Admin endpoint. Creates a new survey. Contact questions are added
        /// automatically as SurveyQuestion rows (Order 0-6); the supplied business
        /// questions become rows 7+. Multiple-choice options are stored as
        /// individual SurveyQuestionOption rows (no JSON).
        /// </summary>
        [Authorize(Policy = "Admin")]
        [HttpPost]
        [Route("", Name = "CreateSurveyRoot")]
        [Route("s", Name = "CreateSurveyPlural")]
        public async Task<IActionResult> CreateSurvey([FromBody] JsonElement body, CancellationToken cancellationToken)
        {
            try
            {
                if (body.ValueKind == JsonValueKind.Null || body.ValueKind == JsonValueKind.Undefined)
                    return BadRequest("Survey payload is required and must be valid JSON.");

                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                };
                options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                var survey = System.Text.Json.JsonSerializer.Deserialize<SurveyDto>(body.GetRawText(), options);
                if (survey == null)
                    return BadRequest("Unable to parse survey payload.");

                var created = await _surveyService.CreateSurveyAsync(survey, cancellationToken);
                return CreatedAtAction(nameof(GetSurveyForAdmin), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (System.Text.Json.JsonException)
            {
                return BadRequest("Malformed JSON in request body.");
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                return StatusCode(500, $"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal error: {ex.Message}");
            }
        }

        /// <summary>
        /// PUT api/survey/{id}
        /// Admin endpoint. Updates an existing survey. All SurveyQuestion rows are
        /// replaced (contact questions re-prepended, business questions re-added).
        /// Multiple-choice options are stored as individual SurveyQuestionOption rows.
        /// Existing SurveyAnswer rows are cascade-deleted with the questions.
        /// </summary>
        [Authorize(Policy = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateSurvey(int id, [FromBody] JsonElement body, CancellationToken cancellationToken)
        {
            try
            {
                if (body.ValueKind == JsonValueKind.Null || body.ValueKind == JsonValueKind.Undefined)
                    return BadRequest("Survey payload is required and must be valid JSON.");

                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                };
                options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                var survey = System.Text.Json.JsonSerializer.Deserialize<SurveyDto>(body.GetRawText(), options);
                if (survey == null)
                    return BadRequest("Unable to parse survey payload.");

                if (!survey.Id.HasValue || survey.Id <= 0)
                    survey.Id = id;

                var updated = await _surveyService.UpdateSurveyAsync(survey, cancellationToken);
                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
            catch (System.Text.Json.JsonException)
            {
                return BadRequest("Malformed JSON in request body.");
            }
        }

        /// <summary>
        /// DELETE api/survey/{id}
        /// Admin endpoint. Hard-deletes a survey. Cascade deletes its questions,
        /// respondents, responses, and answers.
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

        /// <summary>
        /// GET api/survey/{id}/admin
        /// Admin endpoint. Returns the survey definition with its business
        /// questions (SurveyQuestion rows where Category = Business).
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

        /// <summary>
        /// GET api/survey/{id}/responses
        /// Admin endpoint. Returns all responses for a survey. Each response's
        /// answers are reconstructed from SurveyAnswer rows joined to SurveyQuestion.
        /// </summary>
        [Authorize(Policy = "Admin")]
        [HttpGet("{id:int}/responses")]
        public async Task<IActionResult> GetSurveyResponses(int id, CancellationToken cancellationToken)
        {
            var summary = await _surveyService.GetAllResponsesSummaryAsync(id, cancellationToken);
            return Ok(summary);
        }

        /// <summary>
        /// GET api/survey/response/{responseId}
        /// Admin endpoint. Returns a single response (with contact info and
        /// per-question answers loaded from SurveyAnswer rows).
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

