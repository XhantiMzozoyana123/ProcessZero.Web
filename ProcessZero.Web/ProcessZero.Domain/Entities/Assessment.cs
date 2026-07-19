using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Domain.Entities
{
    /// <summary>
    /// Stores the assessment question set uploaded by an admin.
    /// ProductId = 0 is the global (platform-wide) assessment.
    /// ProductId > 0 targets a specific product.
    /// Only one active assessment per ProductId at a time (latest upload wins).
    /// </summary>
    public class Assessment : BaseEntity
    {
        // 0 = global, > 0 = product-specific
        public int ProductId { get; set; }

        public string Title { get; set; } = string.Empty;

        // Optional human-friendly description explaining expectations for this assessment
        public string Description { get; set; } = string.Empty;

        // Per-assessment pass mark (percentage). Null = use global default from appsettings.
        public double? PassMark { get; set; }

        // Full assessment payload serialised as JSON (MCQs with CorrectIndex + OpenQuestions)
        public string QuestionsJson { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
