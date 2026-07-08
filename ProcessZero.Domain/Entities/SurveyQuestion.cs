using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Domain.Entities
{
    /// <summary>
    /// Stores a market research survey definition with questions.
    /// Each survey is independent with its own set of questions, responses, and respondents.
    /// Base contact questions (7 fields) are automatically prepended when survey is retrieved.
    ///
    /// QUESTION STRUCTURE (mirrors the assessment MCQ + OpenEnded model):
    /// ================================================================
    /// The <see cref="QuestionsJson"/> column stores a serialised SurveyDto whose
    /// Questions list mixes TWO question types, exactly like an assessment mixes
    /// MCQs and OpenQuestions:
    ///   - MultipleChoice: a closed question with a fixed list of `Options` the
    ///                     respondent picks from (equivalent to assessment QuestionDto,
    ///                     but surveys are NOT scored, so no CorrectIndex is stored).
    ///   - OpenEnded:      a free-text question the respondent answers in their own
    ///                     words (equivalent to assessment OpenQuestionDto).
    ///
    /// The complete stored order is:
    ///   [0-6]  Contact questions (always OpenEnded text fields: email, name, phone, ...)
    ///   [7+]   Admin business questions, each EITHER MultipleChoice or OpenEnded.
    ///
    /// Answers are stored per-response in SurveyResponse.AnswersJson as a flat list of
    /// strings (one per question, in the same order): the typed text for OpenEnded
    /// questions, or the CHOSEN option's text for MultipleChoice questions.
    /// </summary>
    public class SurveyQuestion : BaseEntity
    {
        /// <summary>
        /// Unique name/identifier for the survey (e.g., "Market Research Q1 2025")
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Display title shown to respondents
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Optional human-friendly description explaining the survey purpose
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Full survey payload serialised as JSON.
        /// Contains the complete Questions list (contact questions prepended by the
        /// service + admin business questions). Each question carries its Type
        /// (MultipleChoice/OpenEnded) and, for MultipleChoice, the Options list —
        /// mirroring how an assessment stores MCQs + OpenQuestions.
        /// </summary>
        public string QuestionsJson { get; set; } = string.Empty;

        /// <summary>
        /// Survey status: "Active", "Draft", "Archived", "Closed"
        /// </summary>
        public string Status { get; set; } = "Active";

        /// <summary>
        /// Timestamp when survey was last uploaded/updated
        /// </summary>
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Navigation property for responses
        public ICollection<SurveyResponse> Responses { get; set; } = new List<SurveyResponse>();
    }
}
