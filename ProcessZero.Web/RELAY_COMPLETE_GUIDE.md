# RelayCampaignController - Complete Developer Guide

## Quick Start

This guide covers the **RelayCampaignController** API, which manages outreach campaigns using a SmartLead-style architecture.

### Key Files to Know

| File | Purpose |
|------|---------|
| `ProcessZero.Web/Controllers/RelayCampaignController.cs` | Main API controller (orchestration) |
| `ProcessZero.Application/Interfaces/RelayDtos.cs` | All request/response DTOs |
| `ProcessZero.Infrastructure/Services/RelayLeadService.cs` | Lead batch operations (transactional) |
| `ProcessZero.Infrastructure/Services/ImportProcessor.cs` | CSV import background worker |
| `ProcessZero.Infrastructure/Services/InMemoryImportStatusService.cs` | Job status tracking |
| `ProcessZero.Domain/Entities/*.cs` | Domain entities (RelayCampaign, RelayLead, etc.) |

---

## Entities Overview

### Four Core Entities

```
RelayCampaign (Campaign Container)
  ├─ Name: Campaign identifier
  ├─ Description: Targeting notes
  ├─ IsActive: Running or paused
  ├─ DailySendLimit: Max emails/day
  └─ Leads: ICollection<RelayCampaignLead>

RelayLead (Enriched Lead Database)
  ├─ Email: UNIQUE identifier
  ├─ FirstName, LastName: Contact name
  ├─ Company, JobTitle: Organization info
  ├─ Location, Industry, Intent: Classification
  └─ Campaigns: ICollection<RelayCampaignLead>

RelayCampaignLead (Junction / State Entity)
  ├─ RelayCampaignId (FK)
  ├─ RelayLeadId (FK)
  ├─ Status: enum (Pending, Active, Replied, Completed, Bounced, Unsubscribed)
  ├─ CurrentSequenceStepId: which email step to send next
  ├─ Replied: has lead responded?
  ├─ Unsubscribed: has lead opted out?
  └─ Completed: has lead finished?

LeadLake (Raw Lead Pool)
  └─ Temporary import staging; synced to RelayLead
```

---

## API Workflow

### Step 1: Create Campaign

```bash
POST /api/relay/campaigns
Content-Type: application/json

{
  "name": "Q4 2024 Outreach",
  "description": "Tech CTOs in North America"
}
```

**Response:** `201 Created`
```json
{
  "id": 1,
  "name": "Q4 2024 Outreach",
  "description": "Tech CTOs in North America",
  "isActive": false,
  "activeLeadCount": 0,
  "totalLeadCount": 0,
  "createdAt": "2024-01-15T10:30:00Z"
}
```

**Entity Created:**
- `RelayCampaign` record with `IsActive = false`, `Leads` (empty collection)

---

### Step 2: Import Leads (CSV)

```bash
POST /api/relay/campaigns/1/import
Content-Type: multipart/form-data

file: leads.csv (Email,FirstName,LastName,Company)
```

**CSV Example:**
```csv
Email,FirstName,LastName,Company
john.doe@acme.com,John,Doe,Acme Inc
jane.smith@techcorp.io,Jane,Smith,Tech Corp
```

**Response:** `202 Accepted`
```json
{
  "jobId": "a1b2c3d4e5f6g7h8",
  "message": "Import started. Check status endpoint."
}
```

**Background Process (ImportProcessor):**
1. Parse CSV rows → create `LeadLake` entries
2. For each `LeadLake` entry:
   - Check if `Email` exists in `RelayLead`
   - If new: create `RelayLead`
   - If exists: update `RelayLead`
3. Create `RelayCampaignLead` junction records:
   - `Status = Pending`
   - `Replied = false`
   - `Unsubscribed = false`
   - `Completed = false`
   - `CurrentSequenceStepId = null`
4. Update job status via `IImportStatusService`

**Check Import Progress:**
```bash
GET /api/relay/campaigns/1/import/a1b2c3d4e5f6g7h8/status
```

**Response (in progress):**
```json
{
  "jobId": "a1b2c3d4e5f6g7h8",
  "campaignId": 1,
  "totalRows": 500,
  "processedRows": 250,
  "completed": false,
  "errorMessage": null
}
```

**Response (complete):**
```json
{
  "jobId": "a1b2c3d4e5f6g7h8",
  "campaignId": 1,
  "totalRows": 500,
  "processedRows": 500,
  "completed": true,
  "errorMessage": null
}
```

---

### Step 3: Batch Modify Leads (Optional)

Adjust lead states, assign sequence steps, mark replied/unsubscribed, etc.

```bash
POST /api/relay/campaigns/1/leads/batch
Content-Type: application/json

{
  "add": [100, 101, 102],
  "remove": [5, 6],
  "edit": [
	{
	  "leadId": 10,
	  "status": "Active",
	  "currentSequenceStepId": 2,
	  "replied": false
	},
	{
	  "leadId": 11,
	  "unsubscribed": true
	}
  ]
}
```

**Response:** `200 OK`
```json
{
  "added": 3,
  "removed": 2,
  "updated": 2,
  "errors": null
}
```

**Entity Changes:**
- **Add:** Create 3 new `RelayCampaignLead` records linking RelayLeadIds to campaign
- **Remove:** Delete 2 `RelayCampaignLead` records (junction only, not the RelayLead itself)
- **Edit:** Update 2 `RelayCampaignLead` records with new Status, CurrentSequenceStepId, Replied, Unsubscribed, Completed values

**Important:** All operations are wrapped in a DB transaction (all-or-nothing).

---

### Step 4: Start Campaign

```bash
POST /api/relay/campaigns/1/start
Content-Type: application/json

{
  "hangfireQueueName": "default"
}
```

**Response:** `202 Accepted`
```json
{
  "hangfireJobId": "job_abc123def456",
  "message": "Campaign processing started. Visit Hangfire dashboard to monitor.",
  "queue": "default"
}
```

**Background Process (IRelaySequenceService.ProcessSequenceAsync):**
1. Fetch `RelayCampaign` and related `Leads` (RelayCampaignLead)
2. For each lead with `Status = Active or Pending`:
   - Get `CurrentSequenceStepId` → fetch `RelaySequenceStep`
   - Send email via `IRelayEmailSenderService`
   - Log in `RelayEmailActivity` (Sent, Opened, Clicked, Bounced, Replied)
   - Update `RelayCampaignLead.CurrentSequenceStepId` to next step
   - Update `RelayCampaignLead.Status` based on result
3. Track opens, clicks, bounces, replies
4. Mark leads as `Replied`, `Completed`, `Bounced`, or `Unsubscribed` as appropriate

---

## Other Endpoints

### Get Campaign Details

```bash
GET /api/relay/campaigns/1
```

**Response:** `200 OK`
```json
{
  "id": 1,
  "name": "Q4 2024 Outreach",
  "description": "Tech CTOs in North America",
  "isActive": true,
  "totalLeadCount": 500,
  "activeLeadCount": 450,
  "completedLeadCount": 30,
  "repliedLeadCount": 12,
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-01-16T14:22:00Z"
}
```

### List All Campaigns

```bash
GET /api/relay/campaigns?skip=0&take=10
```

**Response:** `200 OK` (array)
```json
[
  { "id": 1, "name": "Q4 2024 Outreach", ... },
  { "id": 2, "name": "Q1 2024 Tech", ... }
]
```

### Update Campaign

```bash
PUT /api/relay/campaigns/1
Content-Type: application/json

{
  "name": "Q4 2024 - Updated",
  "description": "Refined targeting"
}
```

**Response:** `200 OK`
```json
{
  "message": "Campaign updated"
}
```

**Entity Change:** Updates `RelayCampaign.Name`, `RelayCampaign.Description`, `RelayCampaign.UpdatedAt`

### Activate Campaign

```bash
POST /api/relay/campaigns/1/activate
```

**Response:** `200 OK`
```json
{
  "message": "Campaign activated"
}
```

**Entity Change:** Sets `RelayCampaign.IsActive = true`

### Pause Campaign

```bash
POST /api/relay/campaigns/1/pause
```

**Response:** `200 OK`
```json
{
  "message": "Campaign paused"
}
```

**Entity Change:** Sets `RelayCampaign.IsActive = false`

### Delete Campaign

```bash
DELETE /api/relay/campaigns/1?removeLeads=true
```

**Response:** `200 OK`
```json
{
  "message": "Campaign deleted"
}
```

**Entity Changes:**
- If `removeLeads = true`: Deletes all `RelayCampaignLead` junction records
- Then deletes `RelayCampaign` record
- RelayLead records are NOT deleted (reusable in other campaigns)

---

## Database Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         RelayCampaigns                          │
├──────────┬─────────────────────────────────────────────────────┤
│ Id       │ 1                                                    │
│ Name     │ "Q4 2024 Outreach"                                  │
│ IsActive │ true                                                │
│ Leads    │ [Lead1→RelayCampaignLead, Lead2→RelayCampaignLead] │
└──────────┴─────────────────────────────────────────────────────┘
			│
			↓ (1:N)
	┌───────────────────────────────────────────┐
	│    RelayCampaignLeads (Junction)         │
	├───────────────────────────────────────────┤
	│ Id | RelayCampaignId | RelayLeadId       │
	│ 1  │ 1               │ 10 (John Doe)    │
	│ 2  │ 1               │ 11 (Jane Smith)  │
	│ 3  │ 1               │ 12 (Bob Johnson) │
	├───────────────────────────────────────────┤
	│ Status | Replied | Unsubscribed | Completed
	│ 1(Active) | false | false | false
	│ 2(Replied) | true | false | false
	│ 0(Pending) | false | false | false
	└───────────────────────────────────────────┘
			↓ (N:1 on each RelayLeadId)
	┌────────────────────────────────────────────────┐
	│              RelayLeads                        │
	├────────────────────────────────────────────────┤
	│ Id | Email | FirstName | LastName | Company  │
	│ 10 | john.doe@acme.com | John | Doe | Acme   │
	│ 11 | jane.smith@tech.io | Jane | Smith | Tech │
	│ 12 | bob.johnson@stx.co | Bob | Johnson | STX  │
	└────────────────────────────────────────────────┘
```

---

## Error Handling

### 400 Bad Request

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Campaign name required",
  "traceId": "0HN8..."
}
```

**Common reasons:**
- Missing required fields
- Invalid CSV file format
- File size exceeds limit
- No file uploaded

### 404 Not Found

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Campaign not found",
  "traceId": "0HN8..."
}
```

### 500 Internal Server Error

```json
{
  "detail": "Database error: The entity type 'RelayCampaign' could not be found..."
}
```

---

## Design Patterns Used

### 1. **Batch-Only Operations**
- No single-lead endpoints
- Forces efficient bulk operations
- Easier to track, audit, and optimize

### 2. **Transactional Batch Processing**
- All add/remove/edit operations in one DB transaction
- All-or-nothing semantics: prevents partial updates
- Rollback on any error

### 3. **DTOs for API Contracts**
- No EF entities leak into API responses
- Explicit contracts for clients
- Easier versioning/evolution

### 4. **Background Job Processing**
- Long-running work (CSV import, campaign execution) via Hangfire
- Non-blocking: returns `202 Accepted` immediately
- Status polling for progress tracking

### 5. **Thin Controller Pattern**
- Controller: HTTP handling + input validation + DTO mapping only
- Service layer: business logic + transactions
- No business logic in controller

### 6. **Many-to-Many with State**
- `RelayCampaignLead` junction entity
- Stores campaign-specific lead state (Status, Replied, etc.)
- RelayLead can be reused across campaigns

---

## Key Column References

### RelayCampaign
| Column | Type | Description |
|--------|------|-------------|
| `Id` | int (PK) | Campaign identifier |
| `Name` | nvarchar(255) | Campaign name |
| `IsActive` | bit | Running or paused |
| `Leads` | Collection | Enrolled leads (RelayCampaignLead) |

### RelayLead
| Column | Type | Description |
|--------|------|-------------|
| `Id` | int (PK) | Lead identifier |
| `Email` | nvarchar(255) | UNIQUE contact email |
| `FirstName` | nvarchar(255) | Lead's first name |
| `LastName` | nvarchar(255) | Lead's last name |
| `Company` | nvarchar(255) | Organization name |
| `JobTitle` | nvarchar(255) | Role at company |
| `Industry` | enum | Business sector |
| `Intent` | enum | Buying signal strength (High/Medium/Low) |

### RelayCampaignLead
| Column | Type | Description |
|--------|------|-------------|
| `Id` | int (PK) | Junction record ID |
| `RelayCampaignId` | int (FK) | Which campaign |
| `RelayLeadId` | int (FK) | Which lead |
| `Status` | enum | Lead state (Pending, Active, Replied, Completed, Bounced, Unsubscribed) |
| `CurrentSequenceStepId` | int? (FK) | Next email step to send |
| `Replied` | bit | Has lead replied? |
| `Unsubscribed` | bit | Did lead opt out? |
| `Completed` | bit | Did lead finish? |

---

## Common Queries

### Get campaign with all leads and their RelayLead data

```csharp
var campaign = await context.RelayCampaigns
	.Include(c => c.Leads)
	.ThenInclude(cl => cl.RelayLead)
	.FirstOrDefaultAsync(c => c.Id == campaignId);
```

### Count leads by status

```csharp
var statusCounts = await context.RelayCampaignLeads
	.Where(cl => cl.RelayCampaignId == campaignId)
	.GroupBy(cl => cl.Status)
	.Select(g => new { Status = g.Key, Count = g.Count() })
	.ToListAsync();
```

### Find unsubscribed leads

```csharp
var unsubscribed = await context.RelayCampaignLeads
	.Where(cl => cl.RelayCampaignId == campaignId && cl.Unsubscribed)
	.Include(cl => cl.RelayLead)
	.Select(cl => cl.RelayLead)
	.ToListAsync();
```

---

## Testing

### Unit Test: Create Campaign

```csharp
[Fact]
public async Task CreateCampaign_WithValidRequest_ReturnsCreatedResult()
{
	// Arrange
	var request = new CreateCampaignRequest("Test Campaign", "Test Description");

	// Act
	var result = await _controller.CreateCampaign(request);

	// Assert
	var createdResult = Assert.IsType<CreatedAtActionResult>(result);
	var dto = Assert.IsType<CampaignSummaryDto>(createdResult.Value);
	Assert.Equal("Test Campaign", dto.Name);
	Assert.Equal(0, dto.TotalLeadCount);
}
```

### Integration Test: Import and Verify

```csharp
[Fact]
public async Task ImportLeads_WithValidCSV_CreatesLeadsAndJunctions()
{
	// Arrange
	var campaign = await _campaignService.CreateCampaignAsync(
		new RelayCampaign { Name = "Test" });
	var csvContent = "Email,FirstName,LastName,Company\njohn@test.com,John,Doe,Test Inc";
	var file = CreateFormFile("leads.csv", csvContent);

	// Act
	await _controller.ImportLeads(campaign.Id, file);
	await Task.Delay(1000); // Wait for background job

	// Assert
	var leads = await _context.RelayCampaignLeads
		.Where(cl => cl.RelayCampaignId == campaign.Id)
		.ToListAsync();
	Assert.Single(leads);
	Assert.Equal(CampaignLeadStatus.Pending, leads[0].Status);
}
```

---

## Performance Considerations

### Batch Size
- Typical batch: 100-1000 leads
- Larger batches (10K+) may hit transaction limits
- Use smaller batches for large imports

### Indexes
```sql
CREATE INDEX IX_RelayCampaignLeads_CampaignId ON RelayCampaignLeads(RelayCampaignId);
CREATE INDEX IX_RelayCampaignLeads_Status ON RelayCampaignLeads(Status);
CREATE INDEX IX_RelayLeads_Email ON RelayLeads(Email);
```

### Query Optimization
- Use `.Include()` to avoid N+1 queries
- Filter at DB layer (`.Where()`) before materialization (`.ToList()`)
- Paginate results for list endpoints

---

## Troubleshooting

### Import job stuck at 0%
- Check Hangfire dashboard for errors
- Verify CSV file format (Email column required)
- Check database permissions

### Leads not sending
- Verify `RelayCampaignLead.Status == Active`
- Check `CurrentSequenceStepId` is not null
- Verify `RelaySequenceStep` exists
- Check `RelayCampaign.IsActive == true`

### Duplicate leads in campaign
- Use batch `remove` then `add` to refresh
- Check for duplicate email addresses in CSV

---

## References

- **API Documentation:** `RELAY_CAMPAIGN_API.md`
- **Entity Reference:** `RELAY_ENTITIES_REFERENCE.md`
- **Columns Reference:** `RELAY_COLUMNS_REFERENCE.md`
- **Source:** `ProcessZero.Web/Controllers/RelayCampaignController.cs`

---

**Last Updated:** 2024
**Framework:** .NET 8, ASP.NET Core
**Architecture:** SmartLead-style Relay Campaign
