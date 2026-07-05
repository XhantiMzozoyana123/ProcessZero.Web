using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Domain.Entities
{
    /// <summary>
    /// Stores a submitted market research survey response.
    /// Each response is linked to a specific survey and respondent.
    /// Full answers (contact + business) are stored as JSON.
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
        public SurveyQuestion? Survey { get; set; }

        /// <summary>
        /// Foreign key to the respondent who submitted this response
        /// </summary>
        public int SurveyRespondentId { get; set; }

        /// <summary>
        /// Navigation property to the respondent
        /// </summary>
        public SurveyRespondent? Respondent { get; set; }

        /// <summary>
        /// Full answers array serialized as JSON (indices 0-6: contact, 7+: business answers)
        /// </summary>
        public string? AnswersJson { get; set; }

        /// <summary>
        /// Timestamp when response was submitted
        /// </summary>
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}
