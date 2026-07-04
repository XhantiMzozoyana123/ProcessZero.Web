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
    /// Collects rich data from target audience about pain points and needs.
    /// Aggregated responses feed into AI to create market-fit products and offers.
    /// </summary>
    public interface ISurveyService
    {
        /// <summary>
        /// Get the active market research survey for the target audience.
        /// Returns null if no survey has been uploaded yet.
        /// </summary>
        Task<SurveyClientDto?> GetSurveyAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Submit responses to the market research survey with respondent contact information.
        /// Stores the response and contact details for later AI analysis.
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
