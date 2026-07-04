using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Domain.Entities
{
    /// <summary>
    /// Stores the market research survey question set uploaded by an admin.
    /// Used to collect pain point insights from target audience.
    /// </summary>
    public class SurveyQuestion : BaseEntity
    {
        public string Title { get; set; } = string.Empty;

        // Optional human-friendly description explaining the survey
        public string Description { get; set; } = string.Empty;

        // Full survey payload serialised as JSON
        public string QuestionsJson { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
