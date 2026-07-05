using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Domain.Entities
{
    /// <summary>
    /// Stores respondent contact information for a specific survey.
    /// Email is unique per survey (same email can be used in different surveys).
    /// </summary>
    public class SurveyRespondent : BaseEntity
    {
        /// <summary>
        /// Foreign key to the survey this respondent participated in
        /// </summary>
        public int SurveyId { get; set; }

        /// <summary>
        /// Navigation property to the survey
        /// </summary>
        public SurveyQuestion? Survey { get; set; }

        /// <summary>
        /// Email address (unique per survey, composite key: SurveyId + Email)
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Respondent's first name (required contact field)
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Respondent's last name (required contact field)
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Respondent's phone number (required contact field)
        /// </summary>
        public string Phone { get; set; } = string.Empty;

        /// <summary>
        /// Respondent's company (optional contact field)
        /// </summary>
        public string Company { get; set; } = string.Empty;

        /// <summary>
        /// Respondent's job title (optional contact field)
        /// </summary>
        public string Job { get; set; } = string.Empty;

        /// <summary>
        /// Respondent's industry (optional contact field)
        /// </summary>
        public string Industry { get; set; } = string.Empty;

        // Navigation property for responses
        public ICollection<SurveyResponse> Responses { get; set; } = new List<SurveyResponse>();
    }
}
