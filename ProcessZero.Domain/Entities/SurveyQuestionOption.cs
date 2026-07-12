using System;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ProcessZero.Domain.Entities
{
    /// <summary>
    /// A single selectable option for a MultipleChoice survey question.
    /// Each option is stored as its own row (no JSON), linked to the parent question.
    /// </summary>
    public class SurveyQuestionOption : BaseEntity
    {
        /// <summary>
        /// Foreign key to the question this option belongs to
        /// </summary>
        public int SurveyQuestionId { get; set; }

        /// <summary>
        /// Navigation property to the parent question
        /// </summary>
        public SurveyQuestion? SurveyQuestion { get; set; }

        /// <summary>
        /// The option text shown to the respondent (e.g. "1-10", "11-50")
        /// </summary>
        [MaxLength(255)]
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Display order of the option within the question
        /// </summary>
        public int Order { get; set; }
    }
}