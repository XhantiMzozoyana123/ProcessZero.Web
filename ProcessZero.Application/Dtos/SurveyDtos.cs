using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Application.Dtos
{
    /// <summary>
    /// Open-ended survey question for market research (no scoring).
    /// </summary>
    public class SurveyQuestionDto
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool IsRequired { get; set; } = true;
    }

    /// <summary>
    /// Market research survey definition for gathering pain point insights.
    /// Single global survey used to validate high-ticket pain point problems.
    /// </summary>
    public class SurveyDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<SurveyQuestionDto> Questions { get; set; } = new List<SurveyQuestionDto>();
    }

    /// <summary>
    /// Client-facing survey (same as upload, no hidden data).
    /// </summary>
    public class SurveyClientDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<SurveyQuestionDto> Questions { get; set; } = new List<SurveyQuestionDto>();
    }

    /// <summary>
    /// Respondent contact information collected during submission.
    /// </summary>
    public class SurveyRespondentSubmissionDto
    {
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public string? Company { get; set; }
        public string? Job { get; set; }
        public string? Industry { get; set; }
    }

    /// <summary>
    /// Survey response submission from a respondent.
    /// Answers are stored as JSON array of responses.
    /// </summary>
    public class SurveyResponseSubmissionDto
    {
        public SurveyRespondentSubmissionDto Respondent { get; set; } = new SurveyRespondentSubmissionDto();
        public List<string> Answers { get; set; } = new List<string>();
    }

    /// <summary>
    /// Single respondent's submission result.
    /// </summary>
    public class SurveyResponseResultDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public string? Company { get; set; }
        public string? Job { get; set; }
        public string? Industry { get; set; }
        public List<string> Answers { get; set; } = new List<string>();
        public DateTime SubmittedAt { get; set; }
    }

    /// <summary>
    /// Aggregated survey submission data for admin analysis and AI processing.
    /// Contains all responses to feed into LLM for creating market-fit products and offers.
    /// </summary>
    public class SurveySummaryDto
    {
        public string Title { get; set; } = string.Empty;
        public int TotalResponses { get; set; }
        public List<SurveyResponseResultDto> Responses { get; set; } = new List<SurveyResponseResultDto>();
        public DateTime CollectedFrom { get; set; }
        public DateTime CollectedTo { get; set; }
    }
}
