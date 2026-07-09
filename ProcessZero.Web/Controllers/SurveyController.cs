using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using System.Text.Json;

/*
 Entity reference (used by this controller / service):

 SurveyQuestion (table: SurveyQuestions)
 - Id (int)                : primary key (inherited from BaseEntity)
 - Name (string)          : unique survey identifier
 - Title (string)         : display title shown to respondents
 - Description (string)   : human-friendly description
 - QuestionsJson (string) : serialized SurveyDto containing all questions (contact + business)
 - Status (string)        : "Active" | "Draft" | "Archived" | "Closed"
 - UploadedAt (DateTime)  : last uploaded/modified timestamp

 SurveyRespondent (table: SurveyRespondents)
 - Id (int)               : primary key (inherited from BaseEntity)
 - SurveyId (int)         : FK to SurveyQuestions.Id
 - Email (string)         : respondent email (unique per SurveyId)
 - FirstName (string)     : contact first name
 - LastName (string)      : contact last name
 - Phone (string)         : contact phone number
 - Company (string)       : optional company
 - Job (string)           : optional job title
 - Industry (string)      : optional industry
 - CreatedAt/UpdatedAt    : audit timestamps (BaseEntity)

 SurveyResponse (table: SurveyResponses)
 - Id (int)               : primary key (inherited from BaseEntity)
 - SurveyId (int)         : FK to SurveyQuestions.Id
 - SurveyRespondentId(int): FK to SurveyRespondent.Id
 - AnswersJson (string)   : serialized List<string> with answers for every question
                           (indices 0-6 = contact fields, 7+ = business answers)
 - SubmittedAt (DateTime) : when the respondent submitted the survey
 - CreatedAt/UpdatedAt    : audit timestamps (BaseEntity)

 These entities are used by the SurveyService to store definitions, contact
 records and per-respondent submissions. The controller delegates business
 logic to ISurveyService which enforces required contact fields and status.
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

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetSurvey(int id, CancellationToken cancellationToken)
        {
            var survey = await _surveyService.GetSurveyAsync(id, cancellationToken);
            if (survey == null)
                return NotFound($"Survey {id} not found or is not active.");

            return Ok(survey);
        }

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

        [Authorize(Policy = "Admin")]
        [HttpGet]
        public async Task<IActionResult> ListSurveys(CancellationToken cancellationToken)
        {
            var surveys = await _surveyService.ListSurveysAsync(cancellationToken);
            return Ok(surveys);
        }

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
        }

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

        [Authorize(Policy = "Admin")]
        [HttpGet("{id:int}/admin")]
        public async Task<IActionResult> GetSurveyForAdmin(int id, CancellationToken cancellationToken)
        {
            var survey = await _surveyService.GetSurveyForAdminAsync(id, cancellationToken);
            if (survey == null)
                return NotFound($"Survey {id} not found.");

            return Ok(survey);
        }

        [Authorize(Policy = "Admin")]
        [HttpGet("{id:int}/responses")]
        public async Task<IActionResult> GetSurveyResponses(int id, CancellationToken cancellationToken)
        {
            var summary = await _surveyService.GetAllResponsesSummaryAsync(id, cancellationToken);
            return Ok(summary);
        }

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

