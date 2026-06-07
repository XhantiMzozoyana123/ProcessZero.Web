# RelayCampaignController - Documentation Index

## 📚 Complete Documentation Suite

This repository now includes comprehensive documentation for the RelayCampaignController system with detailed entity and column information.

---

## 📖 Documentation Files

### 1. **RELAY_CAMPAIGN_API.md** (Recommended Starting Point)
**Purpose:** Complete API reference with all endpoints, request/response examples, and workflow.

**Contents:**
- API overview and design principles
- All endpoints (CRUD, import, batch operations, campaign execution)
- Request/response examples for each operation
- Data models (DTOs)
- Workflow example (step-by-step guide)
- Best practices
- Error handling
- Architecture overview

**When to Use:**
- Building API clients
- Understanding available endpoints
- Integration testing
- API consumer reference

**Key Sections:**
- Base URL: `/api/relay/campaigns`
- Campaign CRUD: POST, GET, PUT, DELETE, activate, pause
- Lead Import: POST with CSV file
- Batch Lead Operations: Add, remove, edit in one transaction
- Campaign Execution: Start processing via Hangfire
- Workflow Example: Complete sequence from creation to start

---

### 2. **RELAY_ENTITIES_REFERENCE.md** (For Data Model Understanding)
**Purpose:** In-depth explanation of all domain entities, their columns, relationships, and usage.

**Contents:**
- RelayCampaign entity (columns, relationships, examples)
- RelayLead entity (columns, enums, examples)
- RelayCampaignLead entity (junction, state, examples)
- LeadLake entity (raw lead pool)
- Related entities (RelaySequence, RelaySequenceStep, RelayEmailActivity)
- Data flow diagram
- ER diagram
- Best practices
- Common queries (SQL examples)

**When to Use:**
- Understanding the data model
- Writing database queries
- Designing features that interact with entities
- Debugging data-related issues

**Key Sections:**
- Entity columns with type and nullable info
- Enums (CampaignLeadStatus, LeadLakeIndustry, LeadIntent)
- Relationships (1:N, N:N patterns)
- Usage patterns (CSV → LeadLake → RelayLead → RelayCampaignLead)
- Data flow diagram showing import → modification → execution

---

### 3. **RELAY_COLUMNS_REFERENCE.md** (For Quick Column Lookup)
**Purpose:** Quick reference for all table columns, types, and constraints.

**Contents:**
- Complete SQL schemas for each table
- C# entity definitions
- Column type mapping table
- Enum definitions
- BaseEntity inheritance
- Nullable reference patterns
- Collection initialization
- Audit trail (CreatedAt, UpdatedAt)
- CSV import mapping
- Query performance tips

**When to Use:**
- Looking up a specific column type
- Understanding nullable constraints
- Writing EF Core queries
- CSV mapping reference
- Performance optimization

**Key Sections:**
- RelayCampaigns table schema (SQL + C#)
- RelayLeads table schema (SQL + C#)
- RelayCampaignLeads table schema (SQL + C#)
- LeadLake table schema (SQL + C#)
- Column type mapping table
- Indexes and constraints

---

### 4. **RELAY_COMPLETE_GUIDE.md** (For Developers)
**Purpose:** Complete developer guide with workflows, patterns, and troubleshooting.

**Contents:**
- Quick start guide
- Entities overview
- Complete API workflow (4 main steps)
- All other endpoints
- Database diagram
- Error handling examples
- Design patterns used
- Key column references
- Common queries (C# examples)
- Testing examples (unit & integration)
- Performance considerations
- Troubleshooting

**When to Use:**
- Starting development work
- Understanding complete workflows
- Troubleshooting issues
- Writing tests
- Performance optimization

**Key Sections:**
- Step 1: Create Campaign
- Step 2: Import Leads (CSV)
- Step 3: Batch Modify Leads
- Step 4: Start Campaign
- Other endpoints (List, Update, Activate, Pause, Delete)
- Database diagram with sample data
- Design patterns explanation

---

## 🎯 Quick Navigation by Use Case

### "I want to use the API"
→ Start with: **RELAY_CAMPAIGN_API.md**
- Find endpoint reference
- Copy curl examples
- Understand request/response format

### "I need to understand the data model"
→ Start with: **RELAY_ENTITIES_REFERENCE.md**
- Review entity structure
- Understand relationships
- See data flow diagram

### "I'm looking for a specific column"
→ Go to: **RELAY_COLUMNS_REFERENCE.md**
- Search for column name
- See SQL type and constraints
- View in C# entity definition

### "I'm writing/debugging code"
→ Start with: **RELAY_COMPLETE_GUIDE.md**
- See complete workflows
- Review testing examples
- Check troubleshooting section

### "I'm new to this system"
→ Read in order:
1. **RELAY_CAMPAIGN_API.md** (30 min) - Understand API surface
2. **RELAY_ENTITIES_REFERENCE.md** (30 min) - Understand data model
3. **RELAY_COMPLETE_GUIDE.md** (30 min) - Understand workflows & patterns

---

## 🔍 Entity Reference Quick Links

### RelayCampaign
**What it is:** Campaign container (represents an outreach campaign)
**Key columns:** Id, Name, Description, IsActive, DailySendLimit, Leads
**Find more:**
- API Details: RELAY_CAMPAIGN_API.md → "Create Campaign" section
- Entity Details: RELAY_ENTITIES_REFERENCE.md → "1. RelayCampaign"
- Column Details: RELAY_COLUMNS_REFERENCE.md → "RelayCampaign Table Schema"

### RelayLead
**What it is:** Enriched lead database (reusable across campaigns)
**Key columns:** Id, Email (UNIQUE), FirstName, LastName, Company, JobTitle, Industry, Intent
**Find more:**
- API Details: RELAY_CAMPAIGN_API.md → "Batch Modify Leads" section
- Entity Details: RELAY_ENTITIES_REFERENCE.md → "2. RelayLead"
- Column Details: RELAY_COLUMNS_REFERENCE.md → "RelayLead Table Schema"

### RelayCampaignLead
**What it is:** Junction entity with lead state (many-to-many with state)
**Key columns:** Id, RelayCampaignId, RelayLeadId, Status, CurrentSequenceStepId, Replied, Unsubscribed, Completed
**Find more:**
- API Details: RELAY_CAMPAIGN_API.md → "Batch Modify Leads" section
- Entity Details: RELAY_ENTITIES_REFERENCE.md → "3. RelayCampaignLead"
- Column Details: RELAY_COLUMNS_REFERENCE.md → "RelayCampaignLead Table Schema"

### LeadLake
**What it is:** Raw lead pool from CSV imports
**Key columns:** Id, Email, FirstName, LastName, Company, Job, Location, Industry, Intent
**Find more:**
- API Details: RELAY_CAMPAIGN_API.md → "Import Leads from CSV" section
- Entity Details: RELAY_ENTITIES_REFERENCE.md → "4. LeadLake"
- Column Details: RELAY_COLUMNS_REFERENCE.md → "LeadLake Table Schema"

---

## 🔑 Key Enums

### CampaignLeadStatus (RelayCampaignLead.Status)
```
0 = Pending        (Not yet contacted)
1 = Active         (In progress)
2 = Replied        (Lead replied)
3 = Completed      (Finished sequence)
4 = Bounced        (Email bounced)
5 = Unsubscribed   (Opted out)
```
**More info:** RELAY_ENTITIES_REFERENCE.md → "CampaignLeadStatus Enum"

### LeadLakeIndustry (RelayLead.Industry)
```
0 = Technology, 1 = Finance, 2 = Healthcare, 3 = Education,
4 = Retail, 5 = Manufacturing, 6 = Energy, 7 = Transportation,
8 = Entertainment, 9 = Hospitality, 10 = Other
```
**More info:** RELAY_ENTITIES_REFERENCE.md → "LeadLakeIndustry Enum"

### LeadIntent (RelayLead.Intent)
```
0 = High    (Strong signals)
1 = Medium  (Moderate interest)
2 = Low     (Weak signals)
```
**More info:** RELAY_ENTITIES_REFERENCE.md → "LeadIntent Enum"

---

## 📊 Column Type Reference

| Column | Type | Size | Nullable | Notes |
|--------|------|------|----------|-------|
| Email (RelayLead) | nvarchar | 255 | No | UNIQUE |
| FirstName | nvarchar | 255 | No | - |
| LastName | nvarchar | 255 | No | - |
| Name (Campaign) | nvarchar | 255 | No | - |
| Description | nvarchar | MAX | Yes | Long text |
| Phone | nvarchar | 20 | Yes | - |
| Company | nvarchar | 255 | Yes | - |
| JobTitle | nvarchar | 255 | Yes | - |
| Location | nvarchar | 255 | Yes | - |
| Industry | enum (int) | - | No | 0-10 |
| Intent | enum (int) | - | No | 0-2 |
| Status | enum (int) | - | No | 0-5 |
| IsActive | bit | - | No | 0 or 1 |
| Replied | bit | - | No | 0 or 1 |
| CreatedAt | datetime2 | - | No | Auto UTC |
| UpdatedAt | datetime2 | - | Yes | Manual UTC |

**For complete list:** RELAY_COLUMNS_REFERENCE.md → "Column Type Mapping"

---

## 🔄 API Flow Quick Reference

```
1. POST /api/relay/campaigns
   → Create RelayCampaign (IsActive=false, Leads empty)

2. POST /api/relay/campaigns/{id}/import
   → Background import CSV to LeadLake → sync to RelayLead → create RelayCampaignLead junctions
   → Returns jobId, check status via GET /api/relay/campaigns/{id}/import/{jobId}/status

3. POST /api/relay/campaigns/{id}/leads/batch (optional)
   → Add/Remove/Edit RelayCampaignLead records (transactional)

4. POST /api/relay/campaigns/{id}/activate
   → Set RelayCampaign.IsActive = true

5. POST /api/relay/campaigns/{id}/start
   → Enqueue sequence processor to Hangfire
   → Background: send emails, track opens/clicks/replies, update lead status

6. GET /api/relay/campaigns/{id}
   → Get campaign details including lead counts by status
```

**For detailed flows:** RELAY_COMPLETE_GUIDE.md → "Complete API Workflow"

---

## 📝 Code Examples

### List all campaigns
**File:** ProcessZero.Web/Controllers/RelayCampaignController.cs
**Method:** `ListCampaigns`
**Example:** RELAY_CAMPAIGN_API.md → "List Campaigns"

### Import leads with CSV
**File:** ProcessZero.Infrastructure/Services/ImportProcessor.cs
**File:** ProcessZero.Web/Controllers/RelayCampaignController.cs
**Method:** `ImportLeads` + `ProcessAsync`
**Example:** RELAY_COMPLETE_GUIDE.md → "Step 2: Import Leads (CSV)"

### Batch modify leads
**File:** ProcessZero.Infrastructure/Services/RelayLeadService.cs
**Method:** `ProcessBatchAsync`
**Controller:** RelayCampaignController.BatchModifyLeads
**Example:** RELAY_CAMPAIGN_API.md → "Batch Modify Leads"

### Start campaign
**File:** ProcessZero.Web/Controllers/RelayCampaignController.cs
**Method:** `StartCampaign`
**Example:** RELAY_COMPLETE_GUIDE.md → "Step 4: Start Campaign"

---

## 🛠️ Important Files

### Controller
- `ProcessZero.Web/Controllers/RelayCampaignController.cs`
  - Contains all endpoint implementations
  - With comprehensive entity documentation comments

### Services
- `ProcessZero.Infrastructure/Services/RelayLeadService.cs`
  - Batch lead operations (Add, Remove, Update, ProcessBatchAsync)
  - Transactional batch processing

- `ProcessZero.Infrastructure/Services/ImportProcessor.cs`
  - CSV background import worker (Hangfire job)
  - LeadLake → RelayLead sync

- `ProcessZero.Infrastructure/Services/InMemoryImportStatusService.cs`
  - Job progress tracking

### DTOs
- `ProcessZero.Application/Interfaces/RelayDtos.cs`
  - All request/response DTOs
  - Single source of DTO truth

### Entities
- `ProcessZero.Domain/Entities/RelayCampaign.cs`
- `ProcessZero.Domain/Entities/RelayLead.cs`
- `ProcessZero.Domain/Entities/RelayCampaignLead.cs`
- `ProcessZero.Domain/Entities/LeadLake.cs`

---

## ✅ Build Status

**Current Status:** ✅ Build Successful

All documentation files have been created and verified to not break compilation.

---

## 📞 Support

### For API Questions
→ See: **RELAY_CAMPAIGN_API.md**

### For Data Model Questions
→ See: **RELAY_ENTITIES_REFERENCE.md** and **RELAY_COLUMNS_REFERENCE.md**

### For Development Questions
→ See: **RELAY_COMPLETE_GUIDE.md**

### For Specific Column/Type Questions
→ See: **RELAY_COLUMNS_REFERENCE.md**

---

## 🚀 Next Steps

1. **Read:** RELAY_CAMPAIGN_API.md (overview of all endpoints)
2. **Review:** RELAY_ENTITIES_REFERENCE.md (understand data model)
3. **Implement:** Use RELAY_COMPLETE_GUIDE.md for workflows and examples
4. **Reference:** Use RELAY_COLUMNS_REFERENCE.md for column types and constraints

---

**Last Updated:** 2024
**Framework:** .NET 8, ASP.NET Core
**Architecture:** SmartLead-style Relay Campaign System

**All documentation includes comprehensive comments about entities and their columns.**
