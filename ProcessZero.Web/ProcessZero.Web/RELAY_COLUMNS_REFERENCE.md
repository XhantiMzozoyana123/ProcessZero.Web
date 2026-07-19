# Entity Columns - Quick Reference

## RelayCampaign Table Schema

```sql
CREATE TABLE RelayCampaigns (
	Id INT PRIMARY KEY IDENTITY(1,1),
	Name NVARCHAR(255) NOT NULL,
	Description NVARCHAR(MAX),
	IsActive BIT NOT NULL DEFAULT 0,
	DailySendLimit INT NOT NULL DEFAULT 100,
	StartDate DATETIME2 NULL,
	EndDate DATETIME2 NULL,
	CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
	UpdatedAt DATETIME2 NULL,

	-- Indexes
	INDEX IX_IsActive (IsActive)
);
```

### RelayCampaign in Code

```csharp
public class RelayCampaign : BaseEntity  // BaseEntity provides Id, CreatedAt, UpdatedAt
{
	public string Name { get; set; }                           // Campaign title
	public string Description { get; set; }                    // Details, targeting notes
	public bool IsActive { get; set; }                         // Running or paused?
	public int DailySendLimit { get; set; }                    // Max emails/day
	public DateTime? StartDate { get; set; }                   // Optional scheduled start
	public DateTime? EndDate { get; set; }                     // Optional scheduled end
	public ICollection<RelaySequence> Sequences { get; set; }  // Email sequences
	public ICollection<RelayCampaignInbox> Inboxes { get; set; } // Reply inboxes
	public ICollection<RelayCampaignLead> Leads { get; set; }  // Enrolled leads
}
```

---

## RelayLead Table Schema

```sql
CREATE TABLE RelayLeads (
	Id INT PRIMARY KEY IDENTITY(1,1),
	FirstName NVARCHAR(255) NOT NULL,
	LastName NVARCHAR(255) NOT NULL,
	Email NVARCHAR(255) NOT NULL UNIQUE,
	Phone NVARCHAR(20),
	Company NVARCHAR(255),
	JobTitle NVARCHAR(255),
	Location NVARCHAR(255),
	Industry INT NOT NULL,                   -- Enum: 0=Tech, 1=Finance, ... 10=Other
	Intent INT NOT NULL,                     -- Enum: 0=High, 1=Medium, 2=Low
	CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
	UpdatedAt DATETIME2 NULL,

	-- Indexes
	INDEX IX_Email (Email),
	INDEX IX_Company (Company),
	INDEX IX_Industry (Industry)
);
```

### RelayLead in Code

```csharp
public class RelayLead : BaseEntity
{
	public string FirstName { get; set; }                        // Contact first name
	public string LastName { get; set; }                         // Contact last name
	public string Email { get; set; }                            // UNIQUE, primary identifier
	public string Phone { get; set; }                            // Phone number
	public string Company { get; set; }                          // Organization
	public string JobTitle { get; set; }                         // Role at company
	public string Location { get; set; }                         // City/state/country
	public LeadLakeIndustry Industry { get; set; }               // Enum classification
	public LeadIntent Intent { get; set; }                       // Enum: buying signal strength
	public ICollection<RelayCampaignLead> Campaigns { get; set; } // Campaigns enrolled in
	public ICollection<RelayEmailActivity> Activities { get; set; } // Email events
}

public enum LeadLakeIndustry
{
	Technology = 0,
	Finance = 1,
	Healthcare = 2,
	Education = 3,
	Retail = 4,
	Manufacturing = 5,
	Energy = 6,
	Transportation = 7,
	Entertainment = 8,
	Hospitality = 9,
	Other = 10
}

public enum LeadIntent
{
	High = 0,                           // Strong signals
	Medium = 1,                         // Moderate interest
	Low = 2                             // Weak signals
}
```

---

## RelayCampaignLead Table Schema

```sql
CREATE TABLE RelayCampaignLeads (
	Id INT PRIMARY KEY IDENTITY(1,1),
	RelayCampaignId INT NOT NULL,
	RelayLeadId INT NOT NULL,
	CurrentSequenceStepId INT NULL,
	Status INT NOT NULL,                -- Enum: 0=Pending, 1=Active, 2=Replied, 3=Completed, 4=Bounced, 5=Unsubscribed
	Replied BIT NOT NULL DEFAULT 0,
	Unsubscribed BIT NOT NULL DEFAULT 0,
	Completed BIT NOT NULL DEFAULT 0,
	CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
	UpdatedAt DATETIME2 NULL,

	-- Constraints
	FOREIGN KEY (RelayCampaignId) REFERENCES RelayCampaigns(Id),
	FOREIGN KEY (RelayLeadId) REFERENCES RelayLeads(Id),
	FOREIGN KEY (CurrentSequenceStepId) REFERENCES RelaySequenceSteps(Id),

	-- Unique constraint: a lead can only be in a campaign once
	UNIQUE (RelayCampaignId, RelayLeadId),

	-- Indexes
	INDEX IX_RelayCampaignId (RelayCampaignId),
	INDEX IX_Status (Status),
	INDEX IX_Replied (Replied),
	INDEX IX_Unsubscribed (Unsubscribed),
	INDEX IX_Completed (Completed)
);
```

### RelayCampaignLead in Code

```csharp
public class RelayCampaignLead : BaseEntity
{
	// Foreign keys
	public int RelayCampaignId { get; set; }                      // Which campaign
	public RelayCampaign RelayCampaign { get; set; }              // Navigation

	public int RelayLeadId { get; set; }                          // Which lead
	public RelayLead RelayLead { get; set; }                      // Navigation

	// Sequence tracking
	public int? CurrentSequenceStepId { get; set; }               // Next email to send
	public RelaySequenceStep? CurrentSequenceStep { get; set; }   // Navigation

	// Lead state in campaign
	public CampaignLeadStatus Status { get; set; }                // Current status enum
	public bool Replied { get; set; }                             // Has replied?
	public bool Unsubscribed { get; set; }                        // Opted out?
	public bool Completed { get; set; }                           // Finished sequence?
}

public enum CampaignLeadStatus
{
	Pending = 0,                    // Not yet contacted
	Active = 1,                     // In progress
	Replied = 2,                    // Lead replied
	Completed = 3,                  // Finished sequence
	Bounced = 4,                    // Email bounced
	Unsubscribed = 5                // Opted out
}
```

---

## LeadLake Table Schema

```sql
CREATE TABLE LeadLakes (
	Id INT PRIMARY KEY IDENTITY(1,1),
	FirstName NVARCHAR(255) NOT NULL,
	LastName NVARCHAR(255) NOT NULL,
	Email NVARCHAR(255) NOT NULL,
	Phone NVARCHAR(20),
	Company NVARCHAR(255),
	Job NVARCHAR(255),                  -- Job FUNCTION (note: different column name than RelayLead.JobTitle)
	Location NVARCHAR(255),
	Industry INT NOT NULL,              -- Enum same as RelayLead
	Intent INT NOT NULL,                -- Enum same as RelayLead
	CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
	UpdatedAt DATETIME2 NULL,

	-- Indexes
	INDEX IX_Email (Email),
	INDEX IX_ImportDate (CreatedAt)
);
```

### LeadLake in Code

```csharp
public class LeadLake : BaseEntity
{
	public string FirstName { get; set; }      // From CSV column "FirstName"
	public string LastName { get; set; }       // From CSV column "LastName"
	public string Email { get; set; }          // From CSV column "Email"
	public string Phone { get; set; }
	public string Company { get; set; }        // From CSV column "Company"
	public string Job { get; set; }            // From CSV column "Job" (job FUNCTION, not title)
	public string Location { get; set; }
	public LeadLakeIndustry Industry { get; set; }
	public LeadIntent Intent { get; set; }
}
```

---

## Column Type Mapping

| Column Name | C# Type | SQL Type | Max Length | Nullable | Notes |
|-------------|---------|----------|-----------|----------|-------|
| Id | `int` | `INT IDENTITY` | - | No | Primary Key |
| Email | `string` | `NVARCHAR(255)` | 255 | No | UNIQUE in RelayLead |
| FirstName | `string` | `NVARCHAR(255)` | 255 | No | - |
| LastName | `string` | `NVARCHAR(255)` | 255 | No | - |
| Name | `string` | `NVARCHAR(255)` | 255 | No | Campaign name |
| Description | `string` | `NVARCHAR(MAX)` | ∞ | Yes | Long text |
| Phone | `string` | `NVARCHAR(20)` | 20 | Yes | International format |
| Company | `string` | `NVARCHAR(255)` | 255 | Yes | Organization name |
| JobTitle / Job | `string` | `NVARCHAR(255)` | 255 | Yes | Role or function |
| Location | `string` | `NVARCHAR(255)` | 255 | Yes | City/state/country |
| Industry | `enum` (int) | `INT` | - | No | LeadLakeIndustry: 0-10 |
| Intent | `enum` (int) | `INT` | - | No | LeadIntent: 0-2 |
| IsActive | `bool` | `BIT` | - | No | 0 or 1 |
| Replied | `bool` | `BIT` | - | No | 0 or 1 |
| Unsubscribed | `bool` | `BIT` | - | No | 0 or 1 |
| Completed | `bool` | `BIT` | - | No | 0 or 1 |
| Status | `enum` (int) | `INT` | - | No | CampaignLeadStatus: 0-5 |
| CreatedAt | `DateTime` | `DATETIME2` | - | No | Auto-set UTC now |
| UpdatedAt | `DateTime?` | `DATETIME2` | - | Yes | Manual update |
| StartDate | `DateTime?` | `DATETIME2` | - | Yes | Optional campaign start |
| EndDate | `DateTime?` | `DATETIME2` | - | Yes | Optional campaign end |
| DailySendLimit | `int` | `INT` | - | No | Default: 100 |

---

## Common Column Patterns

### BaseEntity (Inherited)

All domain entities inherit from `BaseEntity`:

```csharp
public abstract class BaseEntity
{
	public int Id { get; set; }                      // PK, auto-increment
	public DateTime CreatedAt { get; set; }          // Auto-set UTC now
	public DateTime? UpdatedAt { get; set; }         // Manual update
}
```

### Nullable Reference Properties

In entity definitions, nullable navigations use `?`:

```csharp
public RelaySequenceStep? CurrentSequenceStep { get; set; }  // Null if no step assigned
public DateTime? UpdatedAt { get; set; }                     // Null until first update
```

### Collections

Many-to-many navigations are initialized:

```csharp
public ICollection<RelayCampaignLead> Leads { get; set; }
	= new List<RelayCampaignLead>();  // Empty list by default
```

---

## Audit Trail

All entities track creation and updates:

```csharp
// Creation (automatic in service layer)
entity.CreatedAt = DateTime.UtcNow;

// Update (manual when modified)
entity.UpdatedAt = DateTime.UtcNow;
```

Query example:
```csharp
// Get campaigns modified in last 7 days
var recent = context.RelayCampaigns
	.Where(c => c.UpdatedAt >= DateTime.UtcNow.AddDays(-7))
	.ToList();
```

---

## CSV Import Mapping

When importing CSV to LeadLake:

| CSV Column | Mapped To | Type | Required |
|-----------|-----------|------|----------|
| Email | `LeadLake.Email` | string | Yes |
| FirstName | `LeadLake.FirstName` | string | No |
| LastName | `LeadLake.LastName` | string | No |
| Company | `LeadLake.Company` | string | No |
| Job | `LeadLake.Job` | string | No |
| Phone | `LeadLake.Phone` | string | No |
| Location | `LeadLake.Location` | string | No |

**Note:** Industry and Intent are set post-import via batch edit.

---

## Query Performance Tips

### Indexes on RelayCampaignLeads
```sql
-- Already exist:
-- IX_RelayCampaignId (fast filtering by campaign)
-- IX_Status (fast status summaries)
-- IX_Replied (find replied leads)
-- IX_Unsubscribed (find unsubscribed)
-- IX_Completed (find completed)

-- For lead lookup performance:
SELECT * FROM RelayCampaignLeads
WHERE RelayCampaignId = 1 AND Status = 1  -- Uses both indexes
```

### Avoid N+1 Queries
```csharp
// Good: Include related entities
var campaign = context.RelayCampaigns
	.Include(c => c.Leads)
	.ThenInclude(cl => cl.RelayLead)
	.FirstOrDefault(c => c.Id == campaignId);

// Bad: Lazy load in loop
foreach (var lead in campaign.Leads)
{
	var relayLead = lead.RelayLead;  // Extra query per lead!
}
```

---

**Last Updated:** 2024
**Schema Version:** 1.0 (SmartLead Relay Campaign)
**Framework:** EF Core 8 (.NET 8)
