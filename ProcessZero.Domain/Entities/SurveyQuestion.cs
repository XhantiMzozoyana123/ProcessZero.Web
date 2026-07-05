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
        /// Full survey payload serialised as JSON (list of business questions only, contact questions are prepended automatically)
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
