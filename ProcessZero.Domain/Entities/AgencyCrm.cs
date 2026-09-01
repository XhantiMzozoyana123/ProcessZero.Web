using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProcessZero.Domain.Entities
{
    /// <summary>
    /// A contact / company record inside an Agency's CRM (Bitrix24-style).
    /// Scoped to an AgencyProfile — only that agency (and its managers/admin) can see it.
    /// </summary>
    public class AgencyContact
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AgencyProfileId { get; set; }

        public AgencyProfile AgencyProfile { get; set; } = null!;

        [Required, StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Company { get; set; }

        [StringLength(256)]
        public string? Email { get; set; }

        [StringLength(50)]
        public string? Phone { get; set; }

        /// <summary>Free-form position/title of the contact person.</summary>
        [StringLength(150)]
        public string? Position { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }

        public bool IsCompany { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<AgencyDeal> Deals { get; set; } = new List<AgencyDeal>();
        public ICollection<AgencyActivity> Activities { get; set; } = new List<AgencyActivity>();
    }

    /// <summary>
    /// Pipeline stages for <see cref="AgencyDeal"/>, modelled on Bitrix24's default CRM funnel.
    /// </summary>
    public static class AgencyDealStage
    {
        public const string New = "New";
        public const string Qualified = "Qualified";
        public const string Proposal = "Proposal";
        public const string Negotiation = "Negotiation";
        public const string Won = "Won";
        public const string Lost = "Lost";

        public static readonly string[] All = { New, Qualified, Proposal, Negotiation, Won, Lost };
    }

    /// <summary>
    /// A deal/opportunity in an Agency's CRM pipeline (Bitrix24-style kanban).
    /// </summary>
    public class AgencyDeal
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AgencyProfileId { get; set; }

        public AgencyProfile AgencyProfile { get; set; } = null!;

        [Required]
        public int ContactId { get; set; }

        public AgencyContact Contact { get; set; } = null!;

        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>Deal value in ZAR.</summary>
        public decimal Amount { get; set; }

        /// <summary>One of <see cref="AgencyDealStage.All"/>.</summary>
        [Required, StringLength(50)]
        public string Stage { get; set; } = AgencyDealStage.New;

        /// <summary>Probability percentage 0-100.</summary>
        public int Probability { get; set; } = 50;

        public DateTime? ExpectedCloseDate { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }

        /// <summary>AspNetUsers Id of the user who created the deal.</summary>
        [StringLength(450)]
        public string? CreatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<AgencyActivity> Activities { get; set; } = new List<AgencyActivity>();
    }

    /// <summary>
    /// Timeline activity in an Agency's CRM — notes, calls, meetings, tasks and emails
    /// (mirrors Bitrix24's CRM timeline / activity feed).
    /// </summary>
    public class AgencyActivity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AgencyProfileId { get; set; }

        public AgencyProfile AgencyProfile { get; set; } = null!;

        /// <summary>One of: Note, Call, Meeting, Task, Email.</summary>
        [Required, StringLength(50)]
        public string Type { get; set; } = "Note";

        [Required, StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [StringLength(4000)]
        public string? Description { get; set; }

        public int? ContactId { get; set; }
        public AgencyContact? Contact { get; set; }

        public int? DealId { get; set; }
        public AgencyDeal? Deal { get; set; }

        /// <summary>For Task activities — when it is due.</summary>
        public DateTime? DueDate { get; set; }

        /// <summary>For Task activities — completion flag.</summary>
        public bool IsCompleted { get; set; } = false;

        /// <summary>AspNetUsers Id of the user who logged the activity.</summary>
        [StringLength(450)]
        public string? CreatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}