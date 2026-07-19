using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ProcessZero.Domain.Entities
{
    /// <summary>
    /// Survey response submission from a respondent.
    /// Each response is linked to a specific survey and respondent.
    /// Contains multiple SurveyAnswer records (one per question).
    /// </summary>
    public class SurveyResponse : BaseEntity
    {
        /// <summary>
        /// Foreign key to the survey this response belongs to
        /// </summary>
        public int SurveyId { get; set; }

        /// <summary>
        /// Navigation property to the survey
        /// </summary>
        public Survey? Survey { get; set; }

        /// <summary>
        /// Foreign key to the respondent who submitted this response
        /// </summary>
        public int SurveyRespondentId { get; set; }

        /// <summary>
        /// Navigation property to the respondent
        /// </summary>
        public SurveyRespondent? Respondent { get; set; }

        /// <summary>
        /// Timestamp when response was submitted
        /// </summary>
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        // Navigation property for answers
        public ICollection<SurveyAnswer> Answers { get; set; } = new List<SurveyAnswer>();
    }
}
