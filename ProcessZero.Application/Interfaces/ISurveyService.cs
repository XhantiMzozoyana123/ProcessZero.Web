using ProcessZero.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Application.Interfaces
{
    /// <summary>
    /// Service for managing multiple market research surveys and respondent submissions.
    /// 
    /// CRITICAL DESIGN:
    /// - Each survey is independent with its own questions, responses, and respondents
    /// - Mandatory contact questions (email, firstName, lastName, phone, company, job, industry)
    ///   are ALWAYS prepended to every survey (indices 0-6)
    /// - Admin uploads business/pain point questions, which are appended (indices 7+)
    /// - Respondents submit single answers array with ALL responses (contact + business)
    /// - Contact info extracted from answers[0-6] and stored in SurveyRespondent table
    /// - Business answers stored separately for LLM analysis
    /// - LLM validates pain points and auto-qualifies leads for LeadLake insertion
    /// - Email uniqueness is per-survey (same email can exist in different surveys)
    /// </summary>
    public interface ISurveyService
    {
        // ================== PUBLIC / RESPONDENT ENDPOINTS ==================

        /// <summary>
        /// Get a specific survey by ID for the target audience to fill out.
        /// 
        /// Returns: SurveyClientDto with complete survey structure:
        ///   - Id: Survey identifier
        ///   - Name, Title, Description: Survey metadata
        ///   - Status: Survey status (Active, Draft, Archived, Closed)
        ///   - Questions[0-6]: Mandatory contact information questions
        ///   - Questions[7+]: Admin-provided business/pain point questions
        /// 
        /// Frontend should render these questions in order.
        /// User answers should create an array of same length.
        /// 
        /// Returns null if survey not found or is not Active.
        /// </summary>
        Task<SurveyClientDto?> GetSurveyAsync(int surveyId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Submit responses to a specific market research survey.
        /// 
        /// Input: SurveyResponseSubmissionDto with:
        ///   - SurveyId: Which survey is being answered
        ///   - Answers array:
        ///     answers[0]: Email address (REQUIRED)
        ///     answers[1]: First Name (REQUIRED)
        ///     answers[2]: Last Name (REQUIRED)
        ///     answers[3]: Phone Number (REQUIRED)
        ///     answers[4]: Company (optional, can be empty)
        ///     answers[5]: Job Title (optional, can be empty)
        ///     answers[6]: Industry (optional, can be empty)
        ///     answers[7+]: Business question responses
        /// 
        /// Process:
        /// 1. Validates survey exists and is Active
        /// 2. Validates all required contact fields
        /// 3. Creates or retrieves SurveyRespondent by (SurveyId, Email)
        /// 4. Stores complete response with all answers
        /// 5. Calls ILLMService to analyze pain points
        /// 6. If qualified, adds to LeadLake table
        /// 7. Returns SurveyResponseResultDto with extracted contact + business answers
        /// 
        /// Throws InvalidOperationException if:
        ///   - Survey not found or not Active
        ///   - Insufficient answers for contact fields
        ///   - Required contact fields are empty
        /// </summary>
        Task<SurveyResponseResultDto> SubmitResponseAsync(SurveyResponseSubmissionDto submission, CancellationToken cancellationToken = default);

        // ================== ADMIN ENDPOINTS ==================

        /// <summary>
        /// Admin: list all surveys (active, draft, archived, etc.)
        /// Returns summary list for admin dashboard.
        /// </summary>
        Task<List<SurveysListDto>> ListSurveysAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Admin: create a new market research survey.
        /// Contact questions are automatically prepended.
        /// Admin only provides business/pain point questions.
        /// 
        /// Input: SurveyDto with:
        ///   - Name: Unique survey identifier (e.g., "Q1 2025 Market Research")
        ///   - Title: Display title
        ///   - Description: Survey purpose
        ///   - Questions: Business/pain point questions only (contact prepended automatically)
        /// 
        /// Returns: Created SurveyDto with Id assigned
        /// </summary>
        Task<SurveyDto> CreateSurveyAsync(SurveyDto survey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Admin: update an existing survey.
        /// Can modify title, description, questions, and status.
        /// 
        /// Input: SurveyDto with Id and updated fields
        /// Returns: Updated SurveyDto
        /// 
        /// Throws InvalidOperationException if survey not found
        /// </summary>
        Task<SurveyDto> UpdateSurveyAsync(SurveyDto survey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Admin: delete a survey (hard delete).
        /// Cascades to SurveyResponses and SurveyRespondents.
        /// 
        /// Throws InvalidOperationException if survey not found
        /// </summary>
        Task DeleteSurveyAsync(int surveyId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Admin: retrieve the raw survey payload for editing/review.
        /// 
        /// Returns: SurveyDto with full structure
        /// Returns null if survey not found
        /// </summary>
        Task<SurveyDto?> GetSurveyForAdminAsync(int surveyId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Admin: retrieve all responses collected for a specific survey.
        /// Aggregated data is suitable for LLM analysis to create products and offers.
        /// 
        /// Input: surveyId - which survey's responses to retrieve
        /// Returns: SurveySummaryDto with all responses for that survey
        /// 
        /// Throws InvalidOperationException if survey not found
        /// </summary>
        Task<SurveySummaryDto> GetAllResponsesSummaryAsync(int surveyId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Admin: get individual response by ID for review or follow-up.
        /// 
        /// Input: responseId - SurveyResponse.Id
        /// Returns: SurveyResponseResultDto with full contact and answer details
        /// Returns null if response not found
        /// </summary>
        Task<SurveyResponseResultDto?> GetResponseByIdAsync(int responseId, CancellationToken cancellationToken = default);
    }
}
