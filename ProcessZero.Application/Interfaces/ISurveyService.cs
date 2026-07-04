using ProcessZero.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Application.Interfaces
{
    /// <summary>
    /// Service for managing market research surveys and respondent submissions.
    /// 
    /// CRITICAL DESIGN:
    /// - Mandatory contact questions (email, firstName, lastName, phone, company, job, industry)
    ///   are ALWAYS prepended to every survey (indices 0-6)
    /// - Admin uploads business/pain point questions, which are appended (indices 7+)
    /// - Respondents submit single answers array with ALL responses (contact + business)
    /// - Contact info extracted from answers[0-6] and stored in SurveyRespondent table
    /// - Business answers stored separately for LLM analysis
    /// - LLM validates pain points and auto-qualifies leads for LeadLake insertion
    /// </summary>
    public interface ISurveyService
    {
        /// <summary>
        /// Get the active market research survey for the target audience.
        /// 
        /// Returns: SurveyClientDto with complete survey structure:
        ///   - Questions[0-6]: Mandatory contact information questions
        ///   - Questions[7+]: Admin-provided business/pain point questions
        /// 
        /// Frontend should render these questions in order.
        /// User answers should create an array of same length.
        /// 
        /// Returns null if no survey has been uploaded yet.
        /// </summary>
        Task<SurveyClientDto?> GetSurveyAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Submit responses to the market research survey.
        /// 
        /// Input: SurveyResponseSubmissionDto with answers array:
        ///   answers[0]: Email address (REQUIRED)
        ///   answers[1]: First Name (REQUIRED)
        ///   answers[2]: Last Name (REQUIRED)
        ///   answers[3]: Phone Number (REQUIRED)
        ///   answers[4]: Company (optional, can be empty)
        ///   answers[5]: Job Title (optional, can be empty)
        ///   answers[6]: Industry (optional, can be empty)
        ///   answers[7+]: Business question responses
        /// 
        /// Process:
        /// 1. Validates all required contact fields
        /// 2. Creates or retrieves SurveyRespondent by email
        /// 3. Stores complete response with all answers
        /// 4. Calls ILLMService to analyze pain points
        /// 5. If qualified, adds to LeadLake table
        /// 6. Returns SurveyResponseResultDto with extracted contact + business answers
        /// 
        /// Throws InvalidOperationException if:
        ///   - Survey not found
        ///   - Insufficient answers for contact fields
        ///   - Required contact fields are empty
        /// </summary>
        Task<SurveyResponseResultDto> SubmitResponseAsync(SurveyResponseSubmissionDto submission, CancellationToken cancellationToken = default);

        /// <summary>
        /// Admin: upload or replace the market research survey.
        /// There is only one global survey used for all audience research.
        /// </summary>
        Task UploadSurveyAsync(SurveyDto survey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Admin: retrieve the raw survey payload for editing/review.
        /// Returns null if no survey has been uploaded yet.
        /// </summary>
        Task<SurveyDto?> GetSurveyForAdminAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Admin: retrieve all responses collected from the target audience.
        /// Aggregated data is suitable for LLM analysis to create products and offers.
        /// </summary>
        Task<SurveySummaryDto> GetAllResponsesSummaryAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Admin: get individual response by ID for review or follow-up.
        /// </summary>
        Task<SurveyResponseResultDto?> GetResponseByIdAsync(int responseId, CancellationToken cancellationToken = default);
    }
}
