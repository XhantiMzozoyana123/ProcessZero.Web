# RelayCampaign SmartLead System - API Documentation

## Overview

The **RelayCampaign Controller** is now a SmartLead-style, high-level API for admin users to create and manage outreach campaigns with batch lead operations, CSV import, and background processing.

### Design Principles

- **Thin Controller**: Orchestration only, no business logic
- **Explicit DTOs**: All requests/responses use records (no EF entities leaked)
- **Batch-Only**: UI never adds/removes/edits single leads; all operations are batched
- **Transactional**: Batch operations are atomic (all-or-nothing)
- **Background Jobs**: Long-running work (CSV import, campaign runs) via Hangfire
- **Clean Architecture**: Services layer handles business logic, controller is thin

---

## Base URL

```
/api/relay/campaigns
```

All endpoints require `[Authorize(Roles = "Admin")]`

---

## 📋 Campaign CRUD Operations

### Create Campaign

**Endpoint:** `POST /api/relay/campaigns`

**Request:**
```json
{
  "name": "Q4 2024 Outreach",
  "description": "B2B SaaS decision makers in tech"
}
```

**Response:** `201 Created`
```json
{
  "id": 1,
  "name": "Q4 2024 Outreach",
  "description": "B2B SaaS decision makers in tech",
  "isActive": false,
  "activeLeadCount": 0,
  "totalLeadCount": 0,
  "createdAt": "2024-01-15T10:30:00Z"
}
```

---

### Get Campaign

**Endpoint:** `GET /api/relay/campaigns/{campaignId}`

**Response:** `200 OK`
```json
{
  "id": 1,
  "name": "Q4 2024 Outreach",
  "description": "B2B SaaS decision makers in tech",
  "isActive": true,
  "totalLeadCount": 250,
  "activeLeadCount": 150,
  "completedLeadCount": 45,
  "repliedLeadCount": 12,
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-01-16T14:22:00Z"
}
```

---

### List Campaigns

**Endpoint:** `GET /api/relay/campaigns?skip=0&take=10`

**Response:** `200 OK` (array of CampaignSummaryDto)
```json
[
  {
	"id": 1,
	"name": "Q4 2024 Outreach",
	"description": "...",
	"isActive": true,
	"activeLeadCount": 150,
	"totalLeadCount": 250,
	"createdAt": "2024-01-15T10:30:00Z"
  }
]
```

---

### Update Campaign

**Endpoint:** `PUT /api/relay/campaigns/{campaignId}`

**Request:**
```json
{
  "name": "Q4 2024 - Updated",
  "description": "Revised targeting"
}
```

**Response:** `200 OK`
```json
{
  "message": "Campaign updated"
}
```

---

### Activate Campaign

**Endpoint:** `POST /api/relay/campaigns/{campaignId}/activate`

**Response:** `200 OK`
```json
{
  "message": "Campaign activated"
}
```

---

### Pause Campaign

**Endpoint:** `POST /api/relay/campaigns/{campaignId}/pause`

**Response:** `200 OK`
```json
{
  "message": "Campaign paused"
}
```

---

### Delete Campaign

**Endpoint:** `DELETE /api/relay/campaigns/{campaignId}?removeLeads=true`

**Query Parameters:**
- `removeLeads` (bool, default: true) - Also remove all leads from campaign

**Response:** `200 OK`
```json
{
  "message": "Campaign deleted"
}
```

---

## 📥 Lead Import (CSV)

### Import Leads from CSV

**Endpoint:** `POST /api/relay/campaigns/{campaignId}/import`

**Content-Type:** `multipart/form-data`

**Form Field:**
- `file` (IFormFile) - CSV file with columns: Email, FirstName, LastName, Company

**CSV Format Example:**
```csv
Email,FirstName,LastName,Company
john.doe@acme.com,John,Doe,Acme Inc
jane.smith@techcorp.io,Jane,Smith,Tech Corp
bob.johnson@startupx.com,Bob,Johnson,StartupX
```

**Constraints:**
- File size: max 10 MB
- Format: .csv only
- Required column: Email
- Optional columns: FirstName, LastName, Company

**Response:** `202 Accepted`
```json
{
  "jobId": "a1b2c3d4e5f6g7h8",
  "message": "Import started. Check status endpoint."
}
```

**Note:** Import runs as a background Hangfire job. Use the status endpoint to monitor progress.

---

### Get Import Status

**Endpoint:** `GET /api/relay/campaigns/{campaignId}/import/{jobId}/status`

**Response:** `200 OK`
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

**Response when complete:** `200 OK`
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

**Response if error occurred:** `200 OK`
```json
{
  "jobId": "a1b2c3d4e5f6g7h8",
  "campaignId": 1,
  "totalRows": 500,
  "processedRows": 145,
  "completed": true,
  "errorMessage": "CSV parsing error at row 146: Invalid email format"
}
```

---

## 👥 Batch Lead Operations

### Batch Modify Leads

**Endpoint:** `POST /api/relay/campaigns/{campaignId}/leads/batch`

**Request:**
```json
{
  "add": [10, 20, 30, 40],
  "remove": [5, 6, 7],
  "edit": [
	{
	  "leadId": 15,
	  "status": "Active",
	  "replied": false,
	  "currentSequenceStepId": 2
	},
	{
	  "leadId": 25,
	  "unsubscribed": true
	}
  ]
}
```

**Request Fields:**
- `add` (int[], optional) - LeadLake IDs to add to campaign
- `remove` (int[], optional) - LeadLake IDs to remove from campaign
- `edit` (object[], optional) - Lead state updates (partial)

**Edit Item Fields (all optional):**
- `leadId` (int, required) - LeadLake ID
- `currentSequenceStepId` (int?) - Which step to resume from
- `status` (string?) - "Pending", "Active", "Replied", "Completed", "Bounced", "Unsubscribed"
- `replied` (bool?)
- `unsubscribed` (bool?)
- `completed` (bool?)

**Response:** `200 OK`
```json
{
  "added": 4,
  "removed": 3,
  "updated": 2,
  "errors": null
}
```

**Important:**
- All operations are **transactional** (all-or-nothing)
- Do NOT use this endpoint for single-lead operations
- Errors during any phase will roll back the entire batch

---

## 🚀 Campaign Execution

### Start Campaign

**Endpoint:** `POST /api/relay/campaigns/{campaignId}/start`

**Request (optional):**
```json
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

**What Happens:**
1. Enqueues `IRelaySequenceService.ProcessSequenceAsync()` to Hangfire
2. Sequence engine evaluates each lead's state
3. Decides next action (send, delay, complete)
4. Enqueues send jobs to `IRelayEmailSenderService`
5. Tracking service imports replies in background

**Monitoring:**
- Visit Hangfire dashboard at `/hangfire` to see job status
- Use `hangfireJobId` to reference the job

---

## 🧬 Data Models (DTOs)

### CreateCampaignRequest
```csharp
public record CreateCampaignRequest(
	string Name,
	string? Description = null
);
```

### UpdateCampaignRequest
```csharp
public record UpdateCampaignRequest(
	string? Name = null,
	string? Description = null
);
```

### CampaignSummaryDto
```csharp
public record CampaignSummaryDto(
	int Id,
	string Name,
	string? Description,
	bool IsActive,
	int ActiveLeadCount,
	int TotalLeadCount,
	DateTime CreatedAt
);
```

### CampaignDetailDto
```csharp
public record CampaignDetailDto(
	int Id,
	string Name,
	string? Description,
	bool IsActive,
	int TotalLeadCount,
	int ActiveLeadCount,
	int CompletedLeadCount,
	int RepliedLeadCount,
	DateTime CreatedAt,
	DateTime? UpdatedAt
);
```

### BatchLeadModificationRequest
```csharp
public record BatchLeadModificationRequest(
	List<int>? Add,
	List<int>? Remove,
	List<UpdateLeadRequest>? Edit
);

public record UpdateLeadRequest(
	int LeadId,
	int? CurrentSequenceStepId = null,
	CampaignLeadStatus? Status = null,
	bool? Replied = null,
	bool? Unsubscribed = null,
	bool? Completed = null
);
```

### BatchResultDto
```csharp
public record BatchResultDto(
	int Added,
	int Removed,
	int Updated,
	List<string>? Errors = null
);
```

### ImportStatusDto
```csharp
public record ImportStatusDto(
	string JobId,
	int CampaignId,
	int TotalRows,
	int ProcessedRows,
	bool Completed,
	string? ErrorMessage = null
);
```

### StartCampaignRequest
```csharp
public record StartCampaignRequest(
	string? HangfireQueueName = "default"
);
```

---

## 🔄 Workflow Example

### 1. Create Campaign
```bash
curl -X POST https://app.com/api/relay/campaigns \
  -H "Content-Type: application/json" \
  -d '{"name": "Q4 Outreach", "description": "Tech companies"}'
# Response: { "id": 1, ... }
```

### 2. Import Leads
```bash
curl -X POST https://app.com/api/relay/campaigns/1/import \
  -H "Content-Type: multipart/form-data" \
  -F "file=@leads.csv"
# Response: { "jobId": "job123", "message": "..." }
```

### 3. Poll Import Status
```bash
curl https://app.com/api/relay/campaigns/1/import/job123/status
# Returns progress until completed
```

### 4. Batch Modify Leads (Optional)
```bash
curl -X POST https://app.com/api/relay/campaigns/1/leads/batch \
  -H "Content-Type: application/json" \
  -d '{
	"edit": [
	  {"leadId": 10, "status": "Active"}
	]
  }'
# Response: { "added": 0, "removed": 0, "updated": 1 }
```

### 5. Start Campaign
```bash
curl -X POST https://app.com/api/relay/campaigns/1/start
# Response: { "hangfireJobId": "job_xyz", "message": "..." }
```

### 6. Monitor
- Visit Hangfire dashboard to see job progress
- Check activity logs for sends, bounces, replies

---

## ⚠️ Best Practices

1. **Always use batch endpoints** - Never add/remove/edit single leads
2. **Poll import status** - Don't assume import completion; always check status endpoint
3. **Validate CSV before upload** - Check for duplicate emails, invalid formats
4. **Use reasonable batch sizes** - 100-1000 leads per batch is typical
5. **Monitor via Hangfire** - Complex campaigns should be monitored in Hangfire dashboard
6. **Handle 202 responses** - Endpoints with background work return 202 Accepted; poll for completion
7. **Idempotent operations** - Batch operations are idempotent; safe to retry on failure

---

## 🔐 Authorization

All endpoints require:
- Authentication (valid JWT or session)
- `Admin` role membership

---

## 📞 Error Handling

### 400 Bad Request
- Invalid request format
- Missing required fields
- Invalid CSV file

### 404 Not Found
- Campaign doesn't exist
- Import job not found

### 500 Internal Server Error
- Database error
- Hangfire enqueue failure
- CSV parsing error

### Example Error Response
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "CSV file required",
  "traceId": "0HN8..."
}
```

---

## 🏗️ Architecture

### Layer Breakdown

**Controller Layer (RelayCampaignController)**
- HTTP request/response handling
- Input validation
- DTO mapping

**Service Layer**
- `IRelayCampaignService` - Campaign CRUD
- `IRelayLeadService` - Lead batch operations (transactional)
- `IImportStatusService` - Job tracking
- `ImportProcessor` - Background CSV import

**Infrastructure Layer**
- Database access (EF Core)
- Hangfire job scheduling
- File handling

**Domain Layer**
- Entities: RelayCampaign, RelayCampaignLead, RelayLead
- Value objects and enums

---

## 🚀 Future Enhancements

- [ ] Lead preview endpoint (before commit)
- [ ] Bulk dedup by email before import
- [ ] Scheduled campaign start (vs. immediate)
- [ ] Lead segmentation and targeting rules
- [ ] Advanced A/B testing results
- [ ] Bounce/complaint handling
- [ ] Suppression list management
- [ ] Webhook notifications on job completion
