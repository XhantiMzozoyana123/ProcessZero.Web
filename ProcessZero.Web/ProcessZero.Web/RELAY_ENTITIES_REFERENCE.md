# Relay Campaign System - Entity Reference

## Overview

This document describes the domain entities used in the Relay Campaign system and their relationships. Understanding these entities is essential for working with the RelayCampaignController API.

---

## 1. RelayCampaign (Campaign Container)

**Purpose:** Represents an outreach campaign that groups leads and sequences for coordinated email marketing.

**Database Table:** `RelayCampaigns`

### Columns

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| `Id` | `int` (PK) | No | Primary key, auto-increment |
| `Name` | `nvarchar(255)` | No | Campaign identifier (e.g., "Q4 2024 Outreach") |
| `Description` | `nvarchar(max)` | Yes | Campaign details, targeting notes, or internal comments |
| `IsActive` | `bit` | No | Boolean flag: campaign is currently running or paused |
| `DailySendLimit` | `int` | No | Max emails to send per day (default: system config) |
| `StartDate` | `datetime2` | Yes | Campaign launch date (null = no scheduled start) |
| `EndDate` | `datetime2` | Yes | Campaign end date (null = no scheduled end) |
| `CreatedAt` | `datetime2` | No | UTC timestamp when campaign was created |
| `UpdatedAt` | `datetime2` | Yes | UTC timestamp of last modification |

### Relationships

- **Has Many:** `RelayCampaignLead` (junction records linking leads to campaign)
- **Has Many:** `RelaySequence` (email sequences to run)
- **Has Many:** `RelayCampaignInbox` (reply inboxes for tracking)

### Example

```csharp
var campaign = new RelayCampaign
{
	Name = "Q4 2024 Tech Outreach",
	Description = "Targeting SaaS CTOs in North America",
	IsActive = false,           // Starts inactive; activate via /activate endpoint
	DailySendLimit = 100,       // Send 100 emails/day max
	StartDate = DateTime.UtcNow.AddDays(1),
	EndDate = DateTime.UtcNow.AddDays(30)
};
```

---

## 2. RelayLead (Enriched Lead Database)

**Purpose:** Central repository of enriched contact data. Leads are synced from LeadLake and are reusable across campaigns.

**Database Table:** `RelayLeads`

### Columns

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| `Id` | `int` (PK) | No | Primary key, auto-increment |
| `FirstName` | `nvarchar(255)` | No | Lead's first name |
| `LastName` | `nvarchar(255)` | No | Lead's last name |
| `Email` | `nvarchar(255)` | No | Contact email (unique identifier for outreach) |
| `Phone` | `nvarchar(20)` | Yes | Phone number |
| `Company` | `nvarchar(255)` | Yes | Company/organization name |
| `JobTitle` | `nvarchar(255)` | Yes | Role title (e.g., "CTO", "VP Engineering") |
| `Location` | `nvarchar(255)` | Yes | Geographic location (city/state/country) |
| `Industry` | `int` (enum) | No | Business sector (see LeadLakeIndustry enum) |
| `Intent` | `int` (enum) | No | Engagement likelihood: 0=High, 1=Medium, 2=Low |
| `CreatedAt` | `datetime2` | No | UTC timestamp when lead was created |
| `UpdatedAt` | `datetime2` | Yes | UTC timestamp of last modification |

### Enums

**LeadLakeIndustry:**
```csharp
enum LeadLakeIndustry
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
```

**LeadIntent:**
```csharp
enum LeadIntent
{
	High = 0,     // Strong buying signals
	Medium = 1,   // Moderate interest
	Low = 2       // Low engagement likelihood
}
```

### Relationships

- **Has Many:** `RelayCampaignLead` (campaigns this lead is enrolled in)
- **Has Many:** `RelayEmailActivity` (email sends, opens, clicks, bounces)

### Example

```csharp
var lead = new RelayLead
{
	FirstName = "John",
	LastName = "Doe",
	Email = "john.doe@acme.com",
	Phone = "+1-555-123-4567",
	Company = "Acme Inc",
	JobTitle = "CTO",
	Location = "San Francisco, CA",
	Industry = LeadLakeIndustry.Technology,
	Intent = LeadIntent.High
};
```

---

## 3. RelayCampaignLead (Junction / State Entity)

**Purpose:** Many-to-Many junction table that links `RelayCampaign` to `RelayLead` with campaign-specific state. Each record represents a lead enrolled in a campaign with tracking of their progress.

**Database Table:** `RelayCampaignLeads`

### Columns

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| `Id` | `int` (PK) | No | Primary key, auto-increment |
| `RelayCampaignId` | `int` (FK) | No | Foreign key to `RelayCampaigns.Id` |
| `RelayLeadId` | `int` (FK) | No | Foreign key to `RelayLeads.Id` |
| `CurrentSequenceStepId` | `int` (FK) | Yes | Foreign key to `RelaySequenceSteps.Id` — which step to send next |
| `Status` | `int` (enum) | No | Lead state in campaign (see CampaignLeadStatus enum) |
| `Replied` | `bit` | No | Has lead replied to any email? |
| `Unsubscribed` | `bit` | No | Did lead opt out/unsubscribe? |
| `Completed` | `bit` | No | Did lead complete the entire sequence? |
| `CreatedAt` | `datetime2` | No | UTC timestamp when lead was added to campaign |
| `UpdatedAt` | `datetime2` | Yes | UTC timestamp of last status change |

### Enums

**CampaignLeadStatus:**
```csharp
enum CampaignLeadStatus
{
	Pending = 0,        // Not yet contacted
	Active = 1,         // In progress (awaiting next action or reply)
	Replied = 2,        // Lead has responded
	Completed = 3,      // Lead finished entire sequence
	Bounced = 4,        // Email bounced (hard error)
	Unsubscribed = 5    // Lead opted out
}
```

### Relationships

- **Belongs To:** `RelayCampaign` (the campaign this lead is in)
- **Belongs To:** `RelayLead` (the lead's personal data)
- **Belongs To:** `RelaySequenceStep` (optional; current email to send next)

### Key Points

- **Unique Constraint:** `(RelayCampaignId, RelayLeadId)` — a lead can only be in a campaign once
- **Status Lifecycle:** `Pending` → `Active` → (`Replied` or `Completed` or `Bounced` or `Unsubscribed`)
- **Sequence Progress:** `CurrentSequenceStepId` tracks which email step to send next (null = no sequence assigned yet)
- **Flags:** `Replied`, `Unsubscribed`, `Completed` are independent booleans for quick filtering

### Example

```csharp
var campaignLead = new RelayCampaignLead
{
	RelayCampaignId = 1,                // Campaign: "Q4 2024 Outreach"
	RelayLeadId = 42,                   // Lead: john.doe@acme.com
	CurrentSequenceStepId = 2,          // Next email: 2nd in sequence
	Status = CampaignLeadStatus.Active, // Currently in progress
	Replied = false,                    // Haven't replied yet
	Unsubscribed = false,               // Still subscribed
	Completed = false                   // Hasn't finished
};
```

---

## 4. LeadLake (Raw Lead Pool)

**Purpose:** Temporary/raw lead pool, typically imported from CSV files. Source data for enriching and syncing to `RelayLead`.

**Database Table:** `LeadLakes`

### Columns

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| `Id` | `int` (PK) | No | Primary key, auto-increment |
| `FirstName` | `nvarchar(255)` | No | Lead's first name |
| `LastName` | `nvarchar(255)` | No | Lead's last name |
| `Email` | `nvarchar(255)` | No | Contact email |
| `Phone` | `nvarchar(20)` | Yes | Phone number |
| `Company` | `nvarchar(255)` | Yes | Company name |
| `Job` | `nvarchar(255)` | Yes | Job function / title |
| `Location` | `nvarchar(255)` | Yes | Geographic location |
| `Industry` | `int` (enum) | No | Industry classification (LeadLakeIndustry) |
| `Intent` | `int` (enum) | No | Intent level (LeadIntent) |
| `CreatedAt` | `datetime2` | No | UTC timestamp when imported |
| `UpdatedAt` | `datetime2` | Yes | UTC timestamp of last modification |

### Usage Pattern

1. **CSV Import:** Admin uploads `leads.csv` → rows are parsed → LeadLake entries created
2. **Sync to RelayLead:** ImportProcessor checks if email exists in RelayLead
   - If **new**: Create RelayLead from LeadLake data
   - If **exists**: Update RelayLead with enriched info
3. **Link to Campaign:** RelayLeadIds are added to campaign via batch operations

### Relationships

- **No direct relationships** — serves as source data pool
- **Indirectly used:** ImportProcessor reads LeadLake, writes/updates RelayLead

---

## 5. Related Entities (Reference)

### RelaySequence

**Purpose:** Container for a series of templated emails to send over time.

**Key Columns:**
- `Id` (PK)
- `RelayCampaignId` (FK) — which campaign
- `Name` (e.g., "First Follow-up Sequence")
- `IsActive` (bool)

### RelaySequenceStep

**Purpose:** Individual email in a sequence with timing and template.

**Key Columns:**
- `Id` (PK)
- `RelaySequenceId` (FK)
- `StepNumber` (order in sequence)
- `EmailTemplate` (template content)
- `DelayDays` (wait X days after previous step)
- `Subject` (email subject line)

### RelayEmailActivity

**Purpose:** Audit log of email events (sent, opened, clicked, bounced, replied).

**Key Columns:**
- `Id` (PK)
- `RelayLeadId` (FK)
- `RelayCampaignId` (FK)
- `EventType` (enum: Sent, Opened, Clicked, Bounced, Replied)
- `Timestamp` (UTC)

### RelayCampaignInbox

**Purpose:** Email inbox(es) configured for receiving replies from campaign leads.

**Key Columns:**
- `Id` (PK)
- `RelayCampaignId` (FK)
- `EmailAddress` (reply-to inbox)
- `Provider` (e.g., Gmail, Office365)

---

## Data Flow Diagram

```
┌─────────────────────────────────────────────────────────────┐
│ 1. IMPORT PHASE                                             │
└─────────────────────────────────────────────────────────────┘

   CSV File (Email, FirstName, LastName, Company)
		↓
   ImportProcessor (Hangfire background job)
		↓
   LeadLake (raw import pool)
		↓
   Sync/Enrich → RelayLead (enriched lead DB)
		↓
   Batch Create RelayCampaignLead (junction)
		↓
   Campaign now has Leads


┌─────────────────────────────────────────────────────────────┐
│ 2. BATCH MODIFICATION PHASE                                 │
└─────────────────────────────────────────────────────────────┘

   BatchLeadModificationRequest
   {
	 "add": [10, 20, 30],        → Add RelayLead IDs 10, 20, 30
	 "remove": [5, 6],           → Remove from RelayCampaignLead
	 "edit": [                   → Update RelayCampaignLead records
	   { "leadId": 15, "status": "Active" }
	 ]
   }
		↓
   ProcessBatchAsync (transactional in DB)
		↓
   Update RelayCampaignLead records
		↓
   Return: { added: 3, removed: 2, updated: 1 }


┌─────────────────────────────────────────────────────────────┐
│ 3. CAMPAIGN EXECUTION PHASE                                 │
└─────────────────────────────────────────────────────────────┘

   StartCampaign endpoint
		↓
   Enqueue IRelaySequenceService.ProcessSequenceAsync
		↓
   Hangfire processes campaign:
   ├─ Query RelayCampaignLead (Status = "Active" or "Pending")
   ├─ For each lead:
   │  ├─ Check CurrentSequenceStepId
   │  ├─ Fetch RelaySequenceStep (template, delay)
   │  ├─ Send email via RelayEmailService
   │  ├─ Log in RelayEmailActivity
   │  └─ Update RelayCampaignLead.CurrentSequenceStepId
   └─ Set Status → "Active", "Completed", "Replied", etc.
		↓
   RelayEmailActivity accumulates events (sends, opens, clicks)
		↓
   Campaign report: lead counts by status
```

---

## Column Size Reference

### For API Payloads

When designing CSV imports or bulk requests, consider:

- **Email:** Max 255 chars (unique constraint in RelayLead)
- **Name fields:** Max 255 chars each
- **Company/Location:** Max 255 chars
- **JobTitle:** Max 255 chars
- **Description:** Max 1,000+ chars (description can be longer)

### Database Constraints

- **Email uniqueness:** RelayLead enforces unique email (no duplicate leads)
- **Campaign name uniqueness:** Not enforced at DB level (can have multiple campaigns with same name)
- **Junction uniqueness:** `(RelayCampaignId, RelayLeadId)` is unique (no duplicate enrollments)

---

## Best Practices

### 1. Batch Operations

Always use the **batch endpoint** for lead modifications:
```bash
POST /api/relay/campaigns/{id}/leads/batch
```

**Never** use single-lead operations (they don't exist by design).

### 2. CSV Import Format

```csv
Email,FirstName,LastName,Company
john@example.com,John,Doe,Acme Inc
jane@example.com,Jane,Smith,Tech Corp
```

- **Required:** Email
- **Optional:** FirstName, LastName, Company
- **Note:** Job title and Intent must be set via edit batch operations post-import

### 3. Status Transitions

```
Pending → Active → (Replied, Completed, Bounced, Unsubscribed)
```

Don't skip states; update sequentially as campaign progresses.

### 4. Sequence Setup

Before starting a campaign:
1. Create RelayCampaign
2. Import/batch-add leads
3. Create RelaySequence(s) with steps
4. Assign CurrentSequenceStepId via batch edit
5. Then call StartCampaign

### 5. Monitoring

Use Hangfire dashboard to monitor:
- Import jobs (ImportProcessor)
- Sequence processing jobs
- Email send jobs

---

## ER Diagram

```
RelayCampaign (1)
	├─ (1:N) → RelayCampaignLead ← (N:1) RelayLead
	├─ (1:N) → RelaySequence → (1:N) RelaySequenceStep
	└─ (1:N) → RelayCampaignInbox

RelayLead (1)
	├─ (1:N) → RelayCampaignLead → (N:1) RelayCampaign
	└─ (1:N) → RelayEmailActivity

RelayEmailActivity
	├─ RelayLeadId (FK)
	└─ RelayCampaignId (FK)

LeadLake (standalone, used in import process)
	└─ (used to populate) → RelayLead
```

---

## Common Queries

### Count leads by status in a campaign
```sql
SELECT Status, COUNT(*) as Count
FROM RelayCampaignLeads
WHERE RelayCampaignId = @campaignId
GROUP BY Status;
```

### Find unsubscribed leads
```sql
SELECT rl.*
FROM RelayCampaignLeads rcl
JOIN RelayLeads rl ON rcl.RelayLeadId = rl.Id
WHERE rcl.RelayCampaignId = @campaignId AND rcl.Unsubscribed = 1;
```

### Export campaign results
```sql
SELECT 
	rl.Email,
	rl.FirstName,
	rl.LastName,
	rcl.Status,
	rcl.Replied,
	rcl.Completed,
	rcl.UpdatedAt
FROM RelayCampaignLeads rcl
JOIN RelayLeads rl ON rcl.RelayLeadId = rl.Id
WHERE rcl.RelayCampaignId = @campaignId
ORDER BY rcl.UpdatedAt DESC;
```

---

**Last Updated:** 2024
**System Version:** SmartLead-style Relay Campaign API
**Framework:** .NET 8, Entity Framework Core
