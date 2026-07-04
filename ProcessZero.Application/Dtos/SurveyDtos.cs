using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Application.Dtos
{
    /// <summary>
    /// Question category type (Contact or Business).
    /// Contact questions (email, name, phone, etc.) are mandatory and prepended to all surveys.
    /// Business questions are custom pain point questions added by admin.
    /// </summary>
    public enum QuestionCategory
    {
        Contact,  // Mandatory contact information questions
        Business  // Custom business/pain point questions
    }

    /// <summary>
    /// Open-ended survey question for market research (no scoring).
    /// Questions are categorized as either Contact (mandatory, prepended) or Business (custom).
    /// Contact questions are ALWAYS questions 0-6:
    ///   0: Email
    ///   1: FirstName
    ///   2: LastName
    ///   3: Phone
    ///   4: Company
    ///   5: Job
    ///   6: Industry
    /// Business questions start at index 7+
    /// </summary>
    public class SurveyQuestionDto
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool IsRequired { get; set; } = true;
        public QuestionCategory Category { get; set; } = QuestionCategory.Business;
    }

    /// <summary>
    /// Market research survey definition for gathering pain point insights.
    /// Single global survey used to validate high-ticket pain point problems.
    /// 
    /// IMPORTANT: Contact information questions (email, name, phone, company, job, industry)
    /// are AUTOMATICALLY prepended to this survey when uploaded.
    /// Admin only provides Business/Pain Point questions.
    /// 
    /// Final survey structure:
    ///   Questions[0]: "Email Address" (Contact, Required)
    ///   Questions[1]: "First Name" (Contact, Required)
    ///   Questions[2]: "Last Name" (Contact, Required)
    ///   Questions[3]: "Phone Number" (Contact, Required)
    ///   Questions[4]: "Company" (Contact, Optional)
    ///   Questions[5]: "Job Title" (Contact, Optional)
    ///   Questions[6]: "Industry" (Contact, Optional)
    ///   Questions[7+]: Admin-provided business questions
    /// </summary>
    public class SurveyDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        /// <summary>
        /// Business/pain point questions. Contact questions are automatically prepended by service.
        /// </summary>
        public List<SurveyQuestionDto> Questions { get; set; } = new List<SurveyQuestionDto>();
    }

    /// <summary>
    /// Client-facing survey (same as database, includes contact + business questions).
    /// This is what the frontend receives and should render.
    /// 
    /// Questions array includes:
    ///   - Indices 0-6: Contact information questions (email, name, phone, company, job, industry)
    ///   - Indices 7+: Business/pain point questions
    /// 
    /// Answers array should have same length as Questions array.
    /// </summary>
    public class SurveyClientDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        /// <summary>
        /// ALL questions including mandatory contact questions (0-6) and business questions (7+)
        /// </summary>
        public List<SurveyQuestionDto> Questions { get; set; } = new List<SurveyQuestionDto>();
    }

    /// <summary>
    /// Survey response submission from a respondent.
    /// 
    /// CRITICAL: Answers array must have one entry per question in Questions array.
    /// 
    /// Contact answers (REQUIRED):
    ///   answers[0]: Email address
    ///   answers[1]: First name
    ///   answers[2]: Last name
    ///   answers[3]: Phone number
    ///   answers[4]: Company (optional, can be empty string)
    ///   answers[5]: Job title (optional, can be empty string)
    ///   answers[6]: Industry (optional, can be empty string)
    /// 
    /// Business answers (start at index 7):
    ///   answers[7+]: Responses to pain point questions
    /// 
    /// Example:
    ///   {
    ///     "answers": [
    ///       "jane@company.com",                    // [0] Email
    ///       "Jane",                                // [1] FirstName
    ///       "Doe",                                 // [2] LastName
    ///       "+1-555-0123",                         // [3] Phone
    ///       "Acme Corp",                           // [4] Company
    ///       "Operations Manager",                  // [5] Job
    ///       "Manufacturing",                       // [6] Industry
    ///       "Our scheduling is manual...",         // [7] Q1 Answer
    ///       "We use Excel and email...",           // [8] Q2 Answer
    ///       "$15,000/month on coordination..."     // [9] Q3 Answer
    ///     ]
    ///   }
    /// </summary>
    public class SurveyResponseSubmissionDto
    {
        /// <summary>
        /// Answers array with ONE string per question.
        /// Index 0-6: Contact information responses
        /// Index 7+: Business question responses
        /// </summary>
        public List<string> Answers { get; set; } = new List<string>();
    }

    /// <summary>
    /// Single respondent's submission result.
    /// Contact information is extracted from first 7 answers.
    /// Business answers are stored in Answers array.
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
        /// <summary>
        /// Business question answers only (indices 7+ from submission).
        /// Contact information is stored in separate properties above.
        /// </summary>
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
