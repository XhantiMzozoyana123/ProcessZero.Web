using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ProcessZero.Domain.Entities
{
    /// <summary>
    /// Market research survey definition.
    /// Each survey is independent with its own questions, responses, and respondents.
    /// </summary>
    public class Survey : BaseEntity
    {
        /// <summary>
        /// Unique name/identifier for the survey (e.g., "Market Research Q1 2025")
        /// </summary>
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Display title shown to respondents
        /// </summary>
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Optional human-friendly description explaining the survey purpose
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Survey status: "Active", "Draft", "Archived", "Closed"
        /// </summary>
        [MaxLength(50)]
        public string Status { get; set; } = "Active";

        /// <summary>
        /// Timestamp when survey was last uploaded/updated
        /// </summary>
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<SurveyQuestion> Questions { get; set; } = new List<SurveyQuestion>();
        public ICollection<SurveyRespondent> Respondents { get; set; } = new List<SurveyRespondent>();
        public ICollection<SurveyResponse> Responses { get; set; } = new List<SurveyResponse>();
    }
}