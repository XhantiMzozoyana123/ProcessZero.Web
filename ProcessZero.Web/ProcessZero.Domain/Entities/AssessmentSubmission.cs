using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Domain.Entities
{
    public class AssessmentSubmission : BaseEntity
    {
        public int ProductId { get; set; }
        public int Score { get; set; }
        public int Total { get; set; }
        public double Percentage { get; set; }
        public bool Passed { get; set; }

        // JSON payload of answers for auditing / review
        public string? AnswersJson { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}
