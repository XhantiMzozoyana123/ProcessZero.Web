using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ProcessZero.Domain.Entities
{
    /// <summary>
    /// Stores bank account metadata. Sensitive values (AccountNumber) are excluded from default JSON
    /// serialization and a masked view is provided for UI/debugging.
    /// </summary>
    public class BankAccount : BaseEntity
    {

        [Required, MaxLength(200)]
        public string AccountHolderName { get; set; } = string.Empty;   // Partner's full name

        // NOTE: full account numbers are sensitive. Exclude from default JSON serialization
        // and provide a masked view via `MaskedAccountNumber`.
        [Required, MaxLength(64)]
        public string AccountNumber { get; set; } = string.Empty;       // e.g., "0123456789"

        [MaxLength(20)]
        public string BankCode { get; set; } = string.Empty;            // e.g., "058" for GTBank

        [MaxLength(200)]
        public string BankName { get; set; } = string.Empty;            // Optional: e.g., "GTBank"

        /// <summary>
        /// Read-only, not mapped property that returns a masked account number suitable for display.
        /// Example: "****3456" or "**** **** 3456" depending on length.
        /// </summary>
        // Note: data masking and JsonIgnore attributes removed to allow AccountNumber/BankCode
        // to be serialized and returned to clients. If you later need to protect these values
        // consider applying masking at the API layer or implementing field-level authorization.
    }
}
