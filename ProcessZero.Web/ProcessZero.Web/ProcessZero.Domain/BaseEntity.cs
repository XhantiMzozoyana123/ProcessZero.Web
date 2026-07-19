using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ProcessZero.Domain
{
    public class BaseEntity
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        // Use UTC consistently across the application to avoid timezone issues
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; }
    }
}
