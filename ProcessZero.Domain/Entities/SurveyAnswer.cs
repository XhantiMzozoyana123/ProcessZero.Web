using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ProcessZero.Domain.Entities
{
    /// <summary>
    /// Individual answer for a specific question within a survey response.
    /// Each response consists of multiple SurveyAnswer records (one per question).
    /// </summary>
    public class SurveyAnswer : BaseEntity
    {
        /// <summary>
        /// Foreign key to the survey response this answer belongs to
        /// </summary>
        public int SurveyResponseId { get; set; }

        /// <summary>
        /// Navigation property to the response
        /// </summary>
        public SurveyResponse? SurveyResponse { get; set; }

        /// <summary>
        /// Foreign key to the question being answered
        /// </summary>
        public int SurveyQuestionId { get; set; }

        /// <summary>
        /// Navigation property to the question
        /// </summary>
        public SurveyQuestion? SurveyQuestion { get; set; }

        /// <summary>
        /// The respondent's answer text.
        /// For MultipleChoice: the chosen option's text.
        /// For OpenEnded: the typed free-text response.
        /// </summary>
        [MaxLength(2000)]
        public string AnswerText { get; set; } = string.Empty;
    }
}