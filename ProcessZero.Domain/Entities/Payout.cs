using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Domain.Entities
{
    public class Payout : BaseEntity
    {
        // Reference to the user's bank account
        public int BankAccountId { get; set; }

        // The amount to pay
        public decimal Amount { get; set; }

        // Month & year of the payout
        public int Month { get; set; }
        public int Year { get; set; }

        // Status flags
        public bool IsPaid { get; set; } = false; // Have we processed this payout yet?

        // Optional: Notes about the payout
        public string Notes { get; set; }

        // When the payout was processed manually
        public DateTime? PaidAt { get; set; }
    }
}
