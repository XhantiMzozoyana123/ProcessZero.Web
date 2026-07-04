using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Domain.Entities
{
    /// <summary>
    /// Stores the research question set uploaded by an admin.
    /// ProductId = 0 is the global (platform-wide) research set.
    /// ProductId > 0 targets a specific product.
    /// </summary>
    public class ResearchQuestion : BaseEntity
    {
        // 0 = global, > 0 = product-specific
        public int ProductId { get; set; }

        public string Title { get; set; } = string.Empty;

        // Optional human-friendly description explaining the research set
        public string Description { get; set; } = string.Empty;

        // Full research payload serialised as JSON
        public string QuestionsJson { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
