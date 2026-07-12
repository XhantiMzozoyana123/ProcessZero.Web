using System;
using System.Collections.Generic;
using System.Text;
using ProcessZero.Domain.Entities;

namespace ProcessZero.Application.Dtos
{
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
    ///
    /// QUESTION TYPES (mirrors the assessment model):
    /// ==============================================
    /// Just like assessments combine MCQs and OpenQuestions, a survey's Business
    /// questions can EACH be either:
    ///   - MultipleChoice: provide the `Options` list; the respondent selects one.
    ///   - OpenEnded:      `Options` stays empty; the respondent types a free answer.
    ///
    /// Examples:
    ///   // Open-ended business question (like OpenQuestionDto in assessments)
    ///   new SurveyQuestionDto { Type = SurveyQuestionType.OpenEnded,
    ///                           Text = "What is your biggest scheduling pain point?" }
    ///
    ///   // Multiple-choice business question (like QuestionDto in assessments, no scoring)
    ///   new SurveyQuestionDto { Type = SurveyQuestionType.MultipleChoice,
    ///                           Text = "Which best describes your team size?",
    ///                           Options = new() { "1-10", "11-50", "51-200", "200+" } }
    ///
    /// The frontend stores the respondent's answer as a single string in the
    /// `answers` array (the chosen option text for MultipleChoice, or the typed
    /// text for OpenEnded) — same position as the question index.
    /// </summary>
    public class SurveyQuestionDto
    {
        public int Id { get; set; }

        /// <summary>
        /// The question prompt shown to the respondent.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Whether the respondent MUST answer this question.
        /// For Contact questions 0-3 (email, firstName, lastName, phone) this is
        /// enforced as required by the service; Company/Job/Industry are optional.
        /// </summary>
        public bool IsRequired { get; set; } = true;

        /// <summary>
        /// Contact (mandatory, prepended, indices 0-6) or Business (custom, 7+).
        /// </summary>
        public QuestionCategory Category { get; set; } = QuestionCategory.Business;

        /// <summary>
        /// Type of question — MultipleChoice or OpenEnded.
        /// Defaults to OpenEnded (the historical behavior: free-text answers).
        /// Set to MultipleChoice and populate <see cref="Options"/> to give the
        /// respondent a fixed list to choose from (mirrors assessment MCQs).
        /// </summary>
        public SurveyQuestionType Type { get; set; } = SurveyQuestionType.OpenEnded;

        /// <summary>
        /// Selectable options for a MultipleChoice question.
        /// Ignored / left empty for OpenEnded questions. The frontend renders these
        /// as radio buttons or a dropdown, and stores the CHOSEN option's text as the
        /// answer string (same as assessment MCQ answers, but surveys are not scored
        /// so there is no CorrectIndex to track).
        /// </summary>
        public List<string> Options { get; set; } = new List<string>();
    }

    /// <summary>
    /// Market research survey definition for gathering pain point insights.
    /// Each survey is independent with its own questions, responses, and respondents.
    /// 
    /// IMPORTANT: Contact information questions (email, name, phone, company, job, industry)
    /// are AUTOMATICALLY prepended to this survey when created/updated.
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
    /// 
    /// Business questions (7+) may be MULTIPLE-CHOICE or OPEN-ENDED, exactly like an
    /// assessment mixes MCQs and OpenQuestions:
    ///   - MultipleChoice: set Type = MultipleChoice and provide Options.
    ///   - OpenEnded:      leave Type = OpenEnded (default), Options empty.
    /// </summary>
    public class SurveyDto
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        /// <summary>
        /// Business/pain point questions. Contact questions are automatically prepended by service.
        /// Each question is either MultipleChoice (has Options) or OpenEnded (free text),
        /// mirroring the assessment question model.
        /// </summary>
        public List<SurveyQuestionDto> Questions { get; set; } = new List<SurveyQuestionDto>();
    }

    /// <summary>
    /// Client-facing survey (includes contact + business questions).
    /// This is what the frontend receives and should render.
    /// 
    /// Questions array includes:
    ///   - Indices 0-6: Contact information questions (email, name, phone, company, job, industry)
    ///   - Indices 7+: Business/pain point questions
    /// 
    /// Each question carries its own Type (MultipleChoice/OpenEnded) and, for
    /// MultipleChoice, the Options list — so the client knows whether to render a
    /// textarea or a radio/dropdown, exactly like the assessment client DTO.
    /// 
    /// Answers array should have one entry per question, in the same order.
    /// </summary>
    public class SurveyClientDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        /// <summary>
        /// ALL questions including mandatory contact questions (0-6) and business questions (7+).
        /// Each item specifies its Type and Options so the UI can render MCQ vs open-ended.
        /// </summary>
        public List<SurveyQuestionDto> Questions { get; set; } = new List<SurveyQuestionDto>();
    }

    /// <summary>
    /// Survey listing item for admin to see all surveys.
    /// </summary>
    public class SurveysListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int ResponseCount { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    /// <summary>
    /// Survey response submission from a respondent.
    /// 
    /// CRITICAL: Answers array must have one entry per question in Questions array.
    /// SurveyId specifies which survey is being answered.
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
    ///   answers[7+]: Responses to pain point questions.
    ///   - For OpenEnded questions: the typed free-text answer.
    ///   - For MultipleChoice questions: the TEXT of the chosen option
    ///     (exactly as provided in SurveyQuestionDto.Options) — surveys are not
    ///     scored, so only the selected value is stored, no index/CorrectIndex.
    /// 
    /// Example (mix of open-ended and multiple-choice like an assessment):
    ///   {
    ///     "surveyId": 1,
    ///     "answers": [
    ///       "jane@company.com",                    // [0] Email
    ///       "Jane",                                // [1] FirstName
    ///       "Doe",                                 // [2] LastName
    ///       "+1-555-0123",                         // [3] Phone
    ///       "Acme Corp",                           // [4] Company
    ///       "Operations Manager",                  // [5] Job
    ///       "Manufacturing",                       // [6] Industry
    ///       "We still use spreadsheets",           // [7] Open-ended answer
    ///       "11-50",                               // [8] Multiple-choice: selected option text
    ///       "$15,000/month on coordination..."     // [9] Open-ended answer
    ///     ]
    ///   }
    /// </summary>
    public class SurveyResponseSubmissionDto
    {
        /// <summary>
        /// ID of the survey being answered
        /// </summary>
        public int SurveyId { get; set; }

        /// <summary>
        /// Answers array with ONE string per question.
        /// Index 0-6: Contact information responses
        /// Index 7+: Business question responses (open-ended text OR chosen MCQ option text)
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
        public int SurveyId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public string? Company { get; set; }
        public string? Job { get; set; }
        public string? Industry { get; set; }
        /// <summary>
        /// Business question answers only (indices 7+ from submission).
        /// For MultipleChoice questions this holds the chosen option text; for
        /// OpenEnded questions the typed free-text. Contact information is stored in
        /// separate properties above.
        /// </summary>
        public List<string> Answers { get; set; } = new List<string>();
        public DateTime SubmittedAt { get; set; }
    }

    /// <summary>
    /// Aggregated survey submission data for admin analysis and AI processing.
    /// Contains all responses for a specific survey to feed into LLM.
    /// </summary>
    public class SurveySummaryDto
    {
        public int SurveyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int TotalResponses { get; set; }
        public List<SurveyResponseResultDto> Responses { get; set; } = new List<SurveyResponseResultDto>();
        public DateTime CollectedFrom { get; set; }
        public DateTime CollectedTo { get; set; }
    }
}
