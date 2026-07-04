using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Domain.Entities
{
    /// <summary>
    /// Stores a submitted research response.
    /// ProductId = 0 is the global (platform-wide) research set.
    /// </summary>
    public class ResearchAnswer : BaseEntity
    {
        public int ProductId { get; set; }

        public int ResearchContactId { get; set; }

        public ResearchContact? Contact { get; set; }

        public string? AnswersJson { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}
