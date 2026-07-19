# ProcessZero.Web Database Documentation

**Database Platform:** SQL Server  
**ORM:** Entity Framework Core (.NET 8)  
**Context Class:** `ApplicationDbContext`  
**Last Updated:** 2024

---

## 📋 Table of Contents

1. [Database Architecture Overview](#database-architecture-overview)
2. [Entity Relationship Diagram (ERD)](#entity-relationship-diagram)
3. [Data Normalization](#data-normalization)
4. [Entities & Schema](#entities--schema)
5. [Indexing Strategy](#indexing-strategy)
6. [Foreign Key Constraints](#foreign-key-constraints)
7. [Query Optimization](#query-optimization)
8. [Data Constraints & Validation](#data-constraints--validation)
9. [Relationships Matrix](#relationships-matrix)

---

## Database Architecture Overview

### System Design Philosophy

ProcessZero.Web is designed as a **multi-tenant CRM/Partner Management Platform** with the following architectural principles:

- **Multi-Tenancy:** User-based isolation via `UserId` field (inherited from `BaseEntity`)
- **Event Tracking:** All entities include `CreatedAt`, `UpdatedAt`, and `UserId` for audit trails
- **Performance-First:** Composite indexes for common query patterns
- **Cascade Delete:** Maintains referential integrity with automatic cleanup
- **Normalization Level:** **3NF** (Third Normal Form) with strategic denormalization for performance

### Core Domains

| Domain | Purpose | Key Entities |
|--------|---------|--------------|
| **User Management** | Authentication & Authorization | AspNetUsers (Identity Framework) |
| **CRM/Contacts** | Lead & contact management | Contact, LeadLake, RelayEmailReply |
| **Product Management** | Product catalog & tracking | Product, KPI, KpiPolicy, Assessment |
| **Invoicing & Payments** | Financial transactions | Invoice, Payout, BankAccount |
| **Sales Meetings** | Meeting & scheduling | Meeting |
| **Quality Assurance** | Assessment & competency tracking | Assessment, AssessmentSubmission |
| **Communication** | Email relay & inbox management | RelayEmailAccount, RelayEmailReply, Inbox |

---

## Entity Relationship Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                          AspNetUsers (Identity)                     │
│  ┌──────────────┬──────────┬─────────┬──────────────────────┐       │
│  │ Id (PK)      │ UserName │ Email   │ IsBanned (Indexed)   │       │
│  └──────────────┴──────────┴─────────┴──────────────────────┘       │
│         ▲                     ▲              ▲                       │
└─────────┼─────────────────────┼──────────────┼──────────────────────┘
          │ 1:N                 │ 1:N         │ 1:N
          │                     │             │
    ┌─────▼──────┐  ┌──────────▼────┐  ┌────▼────────────┐
    │ Contact    │  │ Product       │  │ Invoice        │
    ├─────────────┤  ├───────────────┤  ├────────────────┤
    │ Id (PK)     │  │ Id (PK)       │  │ Id (PK)        │
    │ UserId (FK) │  │ UserId (FK)   │  │ UserId (FK)    │
    │ Status      │◄─┤ [Indexed]     │  │ ProductId (FK) │
    │ Email       │  │ Name          │  │ ClientId (FK)  │
    │ [Indexes]   │  │ Description   │  │ InvoiceCode    │
    └─────────────┘  └───────────────┘  │ IsPaid         │
                                         │ [Indexes]      │
                                         └────────────────┘
                                                ▲
                                                │ 1:N
                                                │
                                    ┌───────────▼──────────┐
                                    │ Payment Processing   │
                                    ├──────────────────────┤
                                    │ Payout, BankAccount  │
                                    └──────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                    Assessment & Competency Module                   │
│                                                                     │
│  ┌────────────────┐         ┌──────────────────────┐               │
│  │ Assessment     │         │ AssessmentSubmission │               │
│  ├────────────────┤         ├──────────────────────┤               │
│  │ Id (PK)        │◄──1:N───│ Id (PK)              │               │
│  │ ProductId (FK) │         │ UserId (FK)          │               │
│  │ QuestionsJson  │         │ ProductId (FK)       │               │
│  │ UploadedAt     │         │ Score, Total, %      │               │
│  │ [Indexed]      │         │ AnswersJson (audit)  │               │
│  └────────────────┘         │ [Indexed]            │               │
│                             └──────────────────────┘               │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                    Email Communication Module                       │
│                                                                     │
│  ┌──────────────┐      ┌──────────────────┐      ┌────────────┐   │
│  │ LeadLake     │      │ RelayEmailReply  │      │ Inbox      │   │
│  ├──────────────┤      ├──────────────────┤      ├────────────┤   │
│  │ Id (PK)      │◄─1:N─│ Id (PK)          │      │ Id (PK)    │   │
│  │ UserId (FK)  │      │ UserId (FK)      │      │ UserId(FK) │   │
│  │ Email        │      │ LeadLakeId (FK)  │      │ Username   │   │
│  │ FirstName    │      │ MessageId        │      │ Password   │   │
│  │ [Indexes]    │      │ FromEmail        │      │ SmtpHost   │   │
│  └──────────────┘      │ Subject, Body    │      │ ImapHost   │   │
│                        │ [Indexes]        │      └────────────┘   │
│                        └──────────────────┘                        │
│                                │                                    │
│                                ├──1:N─┐                            │
│                                │      │                            │
│                        ┌───────▼──┐  │                            │
│                        │ RelayEmail│  │                            │
│                        │ Account   │◄─┘                            │
│                        ├───────────┤                               │
│                        │ Id (PK)   │                               │
│                        │ EmailAddr │                               │
│                        │ IsActive  │                               │
│                        │ [Indexed] │                               │
│                        └───────────┘                               │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                    KPI Management Module                            │
│                                                                     │
│  ┌────────────────┐              ┌──────────────────┐              │
│  │ KPI            │              │ KpiPolicy        │              │
│  ├────────────────┤              ├──────────────────┤              │
│  │ Id (PK)        │              │ Id (PK)          │              │
│  │ UserId (FK)    │              │ UserId (FK)      │              │
│  │ ProductId (FK) │◄─ Applies to─│ ProductId (FK)   │              │
│  │ OutreachAttemp │              │ MinMonthlyRev    │              │
│  │ CallsBooked    │              │ MinOutreachAtts  │              │
│  │ DealsClosed    │              │ IsActive         │              │
│  │ [Many metrics] │              │ EffectiveFrom    │              │
│  │ CreatedAt      │              │ [Indexed]        │              │
│  │ [Composite Idx]│              └──────────────────┘              │
│  └────────────────┘                                                │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                    Sales & Meetings Module                          │
│                                                                     │
│  ┌────────────────┐              ┌──────────────────┐              │
│  │ Meeting        │              │ Contact          │              │
│  ├────────────────┤              ├──────────────────┤              │
│  │ Id (PK)        │              │ Id (PK)          │              │
│  │ UserId (FK)    │              │ UserId (FK)      │              │
│  │ ClientId (FK)  │              │ Email            │              │
│  │ ProductId (FK) │              │ Status           │              │
│  │ MeetingDate    │              │ ClosedAmount     │              │
│  │ Notes          │              │ [Indexed]        │              │
│  │ [Indexed]      │              └──────────────────┘              │
│  └────────────────┘                                                │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Data Normalization

### Normalization Level: 3NF (Third Normal Form)

All entities follow **Third Normal Form** with strategic denormalization:

#### 1NF (First Normal Form) - Atomic Values
- ✅ All fields contain atomic (non-repeating) values
- ✅ No multi-valued attributes
- ✅ JSON fields (`QuestionsJson`, `AnswersJson`, `ProfilePictureBase64`) store complex data while maintaining atomicity

#### 2NF (Second Normal Form)
- ✅ All attributes depend on the primary key (Id)
- ✅ No partial dependencies on composite keys
- ✅ Non-key attributes are independent of each other

#### 3NF (Third Normal Form)
- ✅ No transitive dependencies between non-key attributes
- ✅ All non-key attributes depend only on the primary key
- ✅ Example: `Contact.ClosedAmount` is stored (not derived from Invoice.Amount)

### Strategic Denormalization

The following denormalizations are implemented for performance:

| Field | Table | Reason | Query Pattern |
|-------|-------|--------|---------------|
| `FromEmail` | RelayEmailReply | Avoid JOIN with LeadLake for email filtering | Fast queries by email sender |
| `Tags` | RelayEmailReply | Avoid separate tags table | Quick filtering by metadata |
| `Status` (enum) | Contact | Common filter | Display contact status without JOIN |
| `ClosedAmount` | Contact | Pre-calculated sum | Quick reporting without aggregation |

---

## Entities & Schema

### Base Entity (Inheritance Hierarchy)

```csharp
// All entities inherit from BaseEntity
public class BaseEntity
{
    public int Id { get; set; }                              // Primary Key
    public string UserId { get; set; }                       // Foreign Key (AspNetUsers)
    public DateTime CreatedAt { get; set; }                  // Audit Trail
    public DateTime UpdatedAt { get; set; }                  // Audit Trail
}
```

**Normalization Impact:** Multi-tenant design ensures data isolation at application level.

---

### 1. AspNetUsers (Identity Framework)

**Purpose:** User authentication and authorization  
**Origin:** ASP.NET Core Identity (Base Table)

| Column | Type | Constraints | Index | Notes |
|--------|------|-------------|-------|-------|
| Id | nvarchar(450) | PK | ✓ | Primary Key |
| UserName | nvarchar(256) | UQ | ✓ | Unique username |
| Email | nvarchar(256) | UQ | ✓ | Unique email |
| NormalizedUserName | nvarchar(256) | ✓ | Normalization |
| NormalizedEmail | nvarchar(256) | ✓ | Normalization |
| PasswordHash | nvarchar(max) | | | Hashed password |
| SecurityStamp | nvarchar(max) | | | Security token |
| ConcurrencyStamp | nvarchar(max) | | | Concurrency control |
| IsBanned | bit | Default: 0 | ✓ | **Custom field** - Checked on every request |

**Index Strategy:**
```sql
-- Existing Identity Indexes
CREATE UNIQUE INDEX UX_AspNetUsers_NormalizedUserName ON AspNetUsers(NormalizedUserName);
CREATE UNIQUE INDEX UX_AspNetUsers_NormalizedEmail ON AspNetUsers(NormalizedEmail);

-- Custom Indexes
CREATE INDEX IX_AspNetUsers_IsBanned ON AspNetUsers(IsBanned);
```

---

### 2. Contact

**Purpose:** CRM contact management with sales tracking  
**Normalization:** 3NF - All attributes depend on ContactId  
**Multi-Tenancy:** Scoped to UserId

| Column | Type | Constraints | MaxLength | Index | Notes |
|--------|------|-------------|-----------|-------|-------|
| Id | int | PK, Identity | | ✓ | Primary Key |
| UserId | nvarchar | FK → AspNetUsers | 450 | ✓ | Tenant isolation |
| FirstName | nvarchar | NOT NULL | 256 | | Contact name |
| LastName | nvarchar | NOT NULL | 256 | | Contact surname |
| Email | nvarchar | | 256 | ✓ | Queryable email |
| Phone | nvarchar | | 256 | | Contact phone |
| Company | nvarchar | | 256 | | Organization |
| Job | nvarchar | | 256 | | Job title |
| Location | nvarchar | | 256 | | Geographic location |
| ClosedAmount | decimal(18,2) | Default: 0 | | | Denormalized: Sum of closed deals |
| Status | int | Default: 0 | | ✓ | Enum: Reached, FollowUp, Converted, Active, Deactivated |
| CreatedAt | datetime2 | Default: UTCNOW | | | Audit timestamp |
| UpdatedAt | datetime2 | | | | Audit timestamp |

**Indexes:**
```sql
CREATE INDEX IX_Contacts_UserId ON Contacts(UserId);
CREATE INDEX IX_Contacts_Status ON Contacts(Status);
CREATE INDEX IX_Contacts_Email ON Contacts(Email);
CREATE INDEX IX_Contacts_UserId_Status ON Contacts(UserId, Status);  -- Composite
```

**Queries Optimized:**
- Find all active contacts for a user
- Search contacts by email
- Filter contacts by status

---

### 3. Product

**Purpose:** Product/offer catalog  
**Normalization:** 3NF  
**Multi-Tenancy:** Scoped to UserId

| Column | Type | Constraints | MaxLength | Index | Notes |
|--------|------|-------------|-----------|-------|-------|
| Id | int | PK, Identity | | ✓ | Primary Key |
| UserId | nvarchar | FK → AspNetUsers | 450 | ✓ | Tenant isolation |
| Name | nvarchar | NOT NULL | 256 | | Product name |
| Description | nvarchar | | max | | Product details |
| ProfilePictureBase64 | nvarchar | | max | | Encoded image data |
| Url | nvarchar | | 256 | | Product URL |
| NegotiableAmounts | nvarchar | | 256 | | Comma-separated prices |
| ActualAmount | decimal(18,2) | NOT NULL | | | Selling price |
| CreatedAt | datetime2 | Default: UTCNOW | | | Audit timestamp |
| UpdatedAt | datetime2 | | | | Audit timestamp |

**Indexes:**
```sql
CREATE INDEX IX_Products_UserId ON Products(UserId);
```

---

### 4. Invoice

**Purpose:** Invoice & payment tracking  
**Normalization:** 3NF  
**Multi-Tenancy:** Scoped to UserId  
**Foreign Keys:** ProductId, ClientId

| Column | Type | Constraints | MaxLength | Index | Notes |
|--------|------|-------------|-----------|-------|-------|
| Id | int | PK, Identity | | ✓ | Primary Key |
| UserId | nvarchar | FK → AspNetUsers | 450 | ✓ | Tenant isolation |
| ProductId | int | FK → Products | | ✓ | Product reference |
| ClientId | int | FK → Contact | | ✓ | Client reference |
| InvoiceCode | nvarchar | UNIQUE | 128 | ✓ | Invoice number |
| CustomerCode | nvarchar | | 128 | | Customer identifier |
| Amount | decimal(18,2) | NOT NULL | | | Invoice total |
| IsPaid | bit | Default: 0 | | | Payment status |
| IssuedAt | datetime2 | NOT NULL | | | Issue date |
| PaidAt | datetime2 | NULL | | | Payment date |
| ExternalInvoiceId | nvarchar | | 256 | | Stripe/PayPal ID |
| CreatedAt | datetime2 | Default: UTCNOW | | | Audit timestamp |
| UpdatedAt | datetime2 | | | | Audit timestamp |

**Indexes:**
```sql
CREATE INDEX IX_Invoices_UserId ON Invoices(UserId);
CREATE INDEX IX_Invoices_ProductId ON Invoices(ProductId);
CREATE INDEX IX_Invoices_ClientId ON Invoices(ClientId);
CREATE UNIQUE INDEX UX_Invoices_InvoiceCode ON Invoices(InvoiceCode);
```

**Foreign Key Constraints:**
```sql
ALTER TABLE Invoices 
ADD CONSTRAINT FK_Invoices_Products FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE RESTRICT;

ALTER TABLE Invoices 
ADD CONSTRAINT FK_Invoices_Contacts FOREIGN KEY (ClientId) REFERENCES Contacts(Id) ON DELETE RESTRICT;
```

---

### 5. KPI (Key Performance Indicators)

**Purpose:** Track sales partner performance across 5 levels  
**Normalization:** 3NF  
**Multi-Tenancy:** Scoped to UserId

| Column | Type | Default | Notes |
|--------|------|---------|-------|
| Id | int | | Primary Key |
| UserId | nvarchar(450) | | Tenant isolation |
| ProductId | int | | Product reference |
| **Level 1 - Sales Partner** | | | |
| OutreachAttempts | int | 0 | Outreach calls |
| CallsBooked | int | 0 | Scheduled calls |
| CallsAttended | int | 0 | Attended calls |
| DealsInfluenced | int | 0 | Influenced deals |
| RevenueGenerated | decimal(18,2) | 0 | Direct revenue |
| **Level 2 - Senior Partner** | | | |
| DealsClosed | int | 0 | Closed deals |
| DealsAttempted | int | 0 | Sales attempts |
| AverageDealSize | decimal(18,2) | 0 | Deal average |
| RevenueInfluenced | decimal(18,2) | 0 | Influenced revenue |
| BasicClientRetention | double | 0 | Retention % |
| ActivityConsistency | double | 0 | Activity level |
| **Level 3 - Network Leader** | | | |
| ActiveTeamSize | int | 0 | Team members |
| TeamRevenue | decimal(18,2) | 0 | Team revenue |
| TeamCloseRate | double | 0 | Team close % |
| TeamChurnRate | double | 0 | Team churn % |
| LeaderActivityLevel | double | 0 | Leader activity |
| **Level 4 & 5 - Strategic** | | | |
| MonthlyRecurringRevenue | decimal(18,2) | 0 | MRR |
| GrowthRate | double | 0 | Growth % |
| ClientRetention | double | 0 | Retention % |
| TeamPerformanceHealth | double | 0 | Health score |
| BrandCompliance | double | 0 | Compliance % |
| LongTermRevenueGrowth | double | 0 | LT growth % |
| StrategicInitiativesDelivered | int | 0 | Initiatives |
| BrandRiskManagement | double | 0 | Risk score |
| InnovationContribution | double | 0 | Innovation score |
| LeadershipStability | double | 0 | Stability score |
| CreatedAt | datetime2 | UTCNOW | Audit timestamp |
| UpdatedAt | datetime2 | | Audit timestamp |

**Indexes:**
```sql
CREATE INDEX IX_KPIs_UserId ON KPIs(UserId);
CREATE INDEX IX_KPIs_ProductId ON KPIs(ProductId);
-- Composite: Query latest KPI per product, ordered by date
CREATE INDEX IX_KPIs_UserId_ProductId_CreatedAt 
    ON KPIs(UserId, ProductId, CreatedAt DESC);
```

---

### 6. KpiPolicy

**Purpose:** Define KPI thresholds and policies  
**Normalization:** 3NF  
**Multi-Tenancy:** Scoped to UserId

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | int | PK | Primary Key |
| UserId | nvarchar(450) | FK | Tenant isolation |
| ProductId | int | NULL | NULL = platform-wide policy |
| EffectiveFrom | datetime2 | NOT NULL | Policy start date |
| EffectiveTo | datetime2 | NULL | Policy end date |
| IsActive | bit | Default: 1 | Active status |
| MinMonthlyRevenue | decimal(18,2) | | Minimum revenue threshold |
| MinOutreachAttempts | int | Default: 0 | Minimum outreach attempts |
| MinCallsBooked | int | Default: 0 | Minimum calls booked |
| GracePeriodDays | int | | Grace period for breach |
| AutoFreezeOnBreach | bit | | Auto-freeze account on policy breach |
| CreatedAt | datetime2 | Default: UTCNOW | Audit timestamp |
| UpdatedAt | datetime2 | | Audit timestamp |

**Indexes:**
```sql
CREATE INDEX IX_KpiPolicies_UserId ON KpiPolicies(UserId);
CREATE INDEX IX_KpiPolicies_ProductId ON KpiPolicies(ProductId);
CREATE INDEX IX_KpiPolicies_IsActive ON KpiPolicies(IsActive);
```

---

### 7. LeadLake

**Purpose:** Centralized lead database (pool of potential contacts)  
**Normalization:** 3NF  
**Multi-Tenancy:** Scoped to UserId

| Column | Type | Constraints | MaxLength | Index | Notes |
|--------|------|-------------|-----------|-------|-------|
| Id | int | PK, Identity | | ✓ | Primary Key |
| UserId | nvarchar | FK | 450 | ✓ | Tenant isolation |
| FirstName | nvarchar | | 256 | | Lead first name |
| LastName | nvarchar | | 256 | | Lead last name |
| Email | nvarchar | | 256 | ✓ | Email address |
| Phone | nvarchar | | 256 | | Phone number |
| Company | nvarchar | | 256 | | Company name |
| Job | nvarchar | | 256 | | Job title |
| Location | nvarchar | | 256 | | Location |
| Industry | int | Default: 10 | | | Enum: Tech, Finance, Healthcare, etc. |
| Intent | int | Default: 1 | | | Enum: High, Medium, Low |
| CreatedAt | datetime2 | Default: UTCNOW | | | Audit timestamp |
| UpdatedAt | datetime2 | | | | Audit timestamp |

**Indexes:**
```sql
CREATE INDEX IX_LeadLakes_UserId ON LeadLakes(UserId);
CREATE INDEX IX_LeadLakes_Email ON LeadLakes(Email);
CREATE INDEX IX_LeadLakes_UserId_Email ON LeadLakes(UserId, Email);  -- Composite
```

---

### 8. Meeting

**Purpose:** Track scheduled and completed meetings  
**Normalization:** 3NF  
**Multi-Tenancy:** Scoped to UserId  
**Foreign Keys:** ClientId (Contact), ProductId

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | int | PK, Identity | Primary Key |
| UserId | nvarchar(450) | FK | Tenant isolation |
| ClientId | int | FK → Contacts | Client reference |
| ProductId | int | FK → Products | Product reference |
| MeetingDate | datetime2 | NOT NULL | Meeting date/time |
| Notes | nvarchar(max) | NULL | Meeting notes |
| CreatedAt | datetime2 | Default: UTCNOW | Audit timestamp |
| UpdatedAt | datetime2 | | Audit timestamp |

**Indexes:**
```sql
CREATE INDEX IX_Meetings_UserId ON Meetings(UserId);
CREATE INDEX IX_Meetings_ClientId ON Meetings(ClientId);
CREATE INDEX IX_Meetings_ProductId ON Meetings(ProductId);
```

---

### 9. BankAccount

**Purpose:** Store user bank account details for payouts  
**Normalization:** 3NF (Sensitive data isolated)  
**Multi-Tenancy:** 1:1 per UserId (Unique constraint)

| Column | Type | Constraints | MaxLength | Index | Notes |
|--------|------|-------------|-----------|-------|-------|
| Id | int | PK, Identity | | ✓ | Primary Key |
| UserId | nvarchar | FK (UNIQUE) | 450 | ✓ | One account per user |
| AccountHolderName | nvarchar | NOT NULL | 200 | | Account owner name |
| AccountNumber | nvarchar | NOT NULL | 64 | | Bank account number |
| BankCode | nvarchar | | 20 | | Bank code (e.g., "058") |
| BankName | nvarchar | | 200 | | Bank name |
| CreatedAt | datetime2 | Default: UTCNOW | | | Audit timestamp |
| UpdatedAt | datetime2 | | | | Audit timestamp |

**Indexes:**
```sql
-- Unique constraint ensures one account per user
CREATE UNIQUE INDEX UX_BankAccounts_UserId ON BankAccounts(UserId);
```

**⚠️ Security Note:** Account numbers are sensitive. Consider:
- Encryption at rest (SQL Server TDE)
- Masking in API responses
- Audit logging on access

---

### 10. Payout

**Purpose:** Track monthly payouts to partners  
**Normalization:** 3NF  
**Multi-Tenancy:** Scoped to UserId  
**Foreign Key:** BankAccountId

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | int | PK, Identity | Primary Key |
| UserId | nvarchar(450) | FK | Tenant isolation |
| BankAccountId | int | FK → BankAccounts | Bank account reference |
| Amount | decimal(18,2) | NOT NULL | Payout amount |
| Month | int | NOT NULL | Payout month (1-12) |
| Year | int | NOT NULL | Payout year |
| IsPaid | bit | Default: 0 | Payment status |
| Notes | nvarchar(max) | NULL | Payout notes |
| PaidAt | datetime2 | NULL | Payment timestamp |
| CreatedAt | datetime2 | Default: UTCNOW | Audit timestamp |
| UpdatedAt | datetime2 | | Audit timestamp |

**Indexes:**
```sql
CREATE INDEX IX_Payouts_UserId ON Payouts(UserId);
CREATE INDEX IX_Payouts_UserId_Month_Year 
    ON Payouts(UserId, Month, Year);  -- Composite
```

**Foreign Key Constraints:**
```sql
ALTER TABLE Payouts 
ADD CONSTRAINT FK_Payouts_BankAccounts 
    FOREIGN KEY (BankAccountId) REFERENCES BankAccounts(Id) ON DELETE RESTRICT;
```

---

### 11. Assessment

**Purpose:** Store assessment question sets (platform-wide or product-specific)  
**Normalization:** 3NF  
**Multi-Tenancy:** Scoped to UserId  
**Design:** ProductId = 0 for global assessments, > 0 for product-specific

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | int | PK, Identity | Primary Key |
| UserId | nvarchar(450) | FK | Tenant isolation |
| ProductId | int | NOT NULL | 0 = global, > 0 = product-specific |
| Title | nvarchar(256) | NOT NULL | Assessment title |
| Description | nvarchar(max) | | Assessment description |
| PassMark | double | NULL | Pass percentage (NULL = global default) |
| QuestionsJson | nvarchar(max) | NOT NULL | JSON array of questions |
| UploadedAt | datetime2 | Default: UTCNOW | Upload timestamp |
| CreatedAt | datetime2 | Default: UTCNOW | Audit timestamp |
| UpdatedAt | datetime2 | | Audit timestamp |

**Indexes:**
```sql
CREATE INDEX IX_Assessments_ProductId ON Assessments(ProductId);
CREATE INDEX IX_Assessments_ProductId_UploadedAt 
    ON Assessments(ProductId, UploadedAt DESC);  -- Composite (latest first)
```

**QuestionsJson Schema Example:**
```json
{
  "questions": [
    {
      "id": 1,
      "type": "mcq",
      "text": "What is X?",
      "options": ["A", "B", "C", "D"],
      "correctIndex": 2
    },
    {
      "id": 2,
      "type": "open",
      "text": "Describe Y"
    }
  ]
}
```

---

### 12. AssessmentSubmission

**Purpose:** Track assessment answers and scores  
**Normalization:** 3NF  
**Multi-Tenancy:** Scoped to UserId

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | int | PK, Identity | Primary Key |
| UserId | nvarchar(450) | FK | Tenant isolation |
| ProductId | int | FK → Products | Product reference |
| Score | int | NOT NULL | Score achieved |
| Total | int | NOT NULL | Total possible score |
| Percentage | double | NOT NULL | Score percentage |
| Passed | bit | NOT NULL | Pass/fail status |
| AnswersJson | nvarchar(max) | NULL | JSON answers (audit) |
| SubmittedAt | datetime2 | Default: UTCNOW | Submission timestamp |
| CreatedAt | datetime2 | Default: UTCNOW | Audit timestamp |
| UpdatedAt | datetime2 | | Audit timestamp |

**Indexes:**
```sql
CREATE INDEX IX_AssessmentSubmissions_UserId ON AssessmentSubmissions(UserId);
CREATE INDEX IX_AssessmentSubmissions_ProductId ON AssessmentSubmissions(ProductId);
CREATE INDEX IX_AssessmentSubmissions_UserId_ProductId_SubmittedAt 
    ON AssessmentSubmissions(UserId, ProductId, SubmittedAt DESC);  -- Composite
```

---

### 13. Inbox

**Purpose:** Email account configuration (SMTP/IMAP)  
**Normalization:** 3NF  
**Multi-Tenancy:** Scoped to UserId  
**⚠️ Security:** Password should be encrypted

| Column | Type | Constraints | MaxLength | Notes |
|--------|------|-------------|-----------|-------|
| Id | int | PK, Identity | | Primary Key |
| UserId | nvarchar | FK | 450 | Tenant isolation |
| Username | nvarchar | NOT NULL | 256 | Email username |
| Password | nvarchar | NOT NULL | max | **ENCRYPT THIS** |
| SmtpHost | nvarchar | | 256 | SMTP server |
| SmtpPort | int | Default: 587 | | SMTP port |
| SmtpUseSsl | bit | Default: 1 | | SMTP SSL flag |
| ImapHost | nvarchar | | 256 | IMAP server |
| ImapPort | int | Default: 993 | | IMAP port |
| ImapUseSsl | bit | Default: 1 | | IMAP SSL flag |
| IsPrimary | bit | Default: 0 | | Primary inbox flag |
| CreatedAt | datetime2 | Default: UTCNOW | | Audit timestamp |
| UpdatedAt | datetime2 | | | Audit timestamp |

---

### 14. RelayEmailAccount

**Purpose:** Google OAuth email accounts for relay campaigns  
**Normalization:** 3NF  
**Multi-Tenancy:** Scoped to UserId

| Column | Type | Constraints | MaxLength | Notes |
|--------|------|-------------|-----------|-------|
| Id | int | PK, Identity | | Primary Key |
| UserId | nvarchar | FK | 450 | Tenant isolation |
| EmailAddress | nvarchar | NOT NULL | 256 | Email address |
| DisplayName | nvarchar | | 256 | Display name |
| AccessToken | nvarchar | NULL | max | OAuth access token |
| RefreshToken | nvarchar | NULL | max | OAuth refresh token |
| TokenExpiry | datetime2 | NULL | | Token expiration |
| DailySendLimit | int | Default: 50 | | Daily send cap |
| SentToday | int | Default: 0 | | Emails sent today |
| IsActive | bit | Default: 1 | | Active status |
| HealthStatus | int | Default: 0 | | Enum: Healthy, Warning, Critical, Disabled |
| HealthCheckError | nvarchar | NULL | max | Error message |
| LastUsedAt | datetime2 | NULL | | Last usage timestamp |
| CreatedAt | datetime2 | Default: UTCNOW | | Audit timestamp |
| UpdatedAt | datetime2 | | | Audit timestamp |

**Indexes:**
```sql
CREATE INDEX IX_RelayEmailAccounts_UserId ON RelayEmailAccounts(UserId);
CREATE INDEX IX_RelayEmailAccounts_IsActive ON RelayEmailAccounts(IsActive);
```

---

### 15. RelayEmailReply

**Purpose:** Track inbound email replies from leads  
**Normalization:** 3NF (with denormalization for performance)  
**Multi-Tenancy:** Scoped to UserId  
**Foreign Keys:** LeadLakeId, RelayEmailAccountId

| Column | Type | Constraints | MaxLength | Index | Notes |
|--------|------|-------------|-----------|-------|-------|
| Id | int | PK, Identity | | ✓ | Primary Key |
| UserId | nvarchar | FK | 450 | ✓ | Tenant isolation |
| RelayEmailAccountId | int | FK | | ✓ | Email account |
| LeadLakeId | int | FK | | ✓ | Lead reference |
| MessageId | nvarchar | NOT NULL | 256 | ✓ | Gmail message ID |
| FromEmail | nvarchar | | 256 | ✓ | **Denormalized** from Lead |
| Subject | nvarchar | | max | | Email subject |
| Body | nvarchar | | max | | Email body |
| Tags | nvarchar | | 1000 | | Comma-separated tags |
| ReceivedDate | datetime2 | NOT NULL | | ✓ | Receipt timestamp |
| IsRead | bit | Default: 0 | | ✓ | Read status |
| CreatedAt | datetime2 | Default: UTCNOW | | | Audit timestamp |
| UpdatedAt | datetime2 | | | | Audit timestamp |

**Indexes:**
```sql
CREATE INDEX IX_RelayEmailReplies_UserId ON RelayEmailReplies(UserId);
CREATE INDEX IX_RelayEmailReplies_LeadLakeId ON RelayEmailReplies(LeadLakeId);
CREATE INDEX IX_RelayEmailReplies_RelayEmailAccountId 
    ON RelayEmailReplies(RelayEmailAccountId);
CREATE INDEX IX_RelayEmailReplies_FromEmail ON RelayEmailReplies(FromEmail);
CREATE INDEX IX_RelayEmailReplies_MessageId ON RelayEmailReplies(MessageId);
CREATE INDEX IX_RelayEmailReplies_IsRead ON RelayEmailReplies(IsRead);

-- Composite indexes for common queries
CREATE INDEX IX_RelayEmailReplies_UserId_IsRead 
    ON RelayEmailReplies(UserId, IsRead);
CREATE INDEX IX_RelayEmailReplies_LeadLakeId_ReceivedDate 
    ON RelayEmailReplies(LeadLakeId, ReceivedDate DESC);  -- Latest first
```

**Foreign Key Constraints:**
```sql
ALTER TABLE RelayEmailReplies 
ADD CONSTRAINT FK_RelayEmailReplies_Leads 
    FOREIGN KEY (LeadLakeId) REFERENCES LeadLakes(Id) ON DELETE CASCADE;

ALTER TABLE RelayEmailReplies 
ADD CONSTRAINT FK_RelayEmailReplies_EmailAccounts 
    FOREIGN KEY (RelayEmailAccountId) REFERENCES RelayEmailAccounts(Id) ON DELETE CASCADE;
```

---

## Indexing Strategy

### Index Classification

#### 1. **Clustered Indexes** (Primary Keys)
Every table has a clustered index on the `Id` column (IDENTITY, PRIMARY KEY).

```sql
-- Implicitly created on all tables
CREATE CLUSTERED INDEX PK_[TableName] ON [TableName](Id);
```

#### 2. **Unique Indexes**
Enforce uniqueness at the database level.

| Table | Columns | Purpose |
|-------|---------|---------|
| Invoices | InvoiceCode | Invoice reference uniqueness |
| BankAccounts | UserId | One account per user |
| AspNetUsers | NormalizedUserName | Username uniqueness |
| AspNetUsers | NormalizedEmail | Email uniqueness |

#### 3. **Foreign Key Indexes**
Automatically created for faster JOINs and referential integrity checks.

```sql
-- All FK columns have implicit indexes
CREATE INDEX IX_[TableName]_[FKColumn] ON [TableName]([FKColumn]);
```

#### 4. **Composite (Multi-Column) Indexes**
Optimize queries filtering on multiple columns in order.

| Table | Columns | Sort | Query Pattern |
|-------|---------|------|---------------|
| Contacts | UserId, Status | ASC, ASC | Filter by user and status |
| KPIs | UserId, ProductId, CreatedAt | ASC, ASC, DESC | Latest KPI per product |
| Meetings | UserId, ClientId | ASC, ASC | Meetings for user and client |
| AssessmentSubmissions | UserId, ProductId, SubmittedAt | ASC, ASC, DESC | Latest submission per product |
| RelayEmailReplies | UserId, IsRead | ASC, ASC | Unread emails for user |
| RelayEmailReplies | LeadLakeId, ReceivedDate | ASC, DESC | Latest replies from lead |
| Payouts | UserId, Month, Year | ASC, ASC, ASC | Payout for month/year |

#### 5. **Filtered Indexes**
Include only relevant rows for faster queries.

```sql
-- Current Implementation: Query active/unread items
CREATE INDEX IX_KpiPolicies_IsActive ON KpiPolicies(IsActive);
CREATE INDEX IX_RelayEmailReplies_IsRead ON RelayEmailReplies(IsRead);
CREATE INDEX IX_AspNetUsers_IsBanned ON AspNetUsers(IsBanned);
```

### Index Recommendations

#### Non-Clustered Indexes by Query Pattern

```sql
-- SEARCH: Find all contacts by UserId (most common)
CREATE INDEX IX_Contacts_UserId ON dbo.Contacts(UserId) 
INCLUDE (Email, Status, FirstName, LastName);

-- SEARCH: Filter invoices by multiple criteria
CREATE INDEX IX_Invoices_UserId_IsPaid ON dbo.Invoices(UserId, IsPaid) 
INCLUDE (ProductId, ClientId, Amount);

-- SEARCH: Get latest assessment per product
CREATE INDEX IX_Assessments_ProductId_UploadedAt_Desc 
ON dbo.Assessments(ProductId, UploadedAt DESC) 
INCLUDE (Title, PassMark);

-- SEARCH: Unread emails for user
CREATE INDEX IX_RelayEmailReplies_UserId_IsRead_ReceivedDate 
ON dbo.RelayEmailReplies(UserId, IsRead) 
INCLUDE (LeadLakeId, FromEmail);
```

### Statistics & Query Plans

Generate statistics for better query optimization:

```sql
-- Update statistics (run daily or after bulk operations)
EXEC sp_updatestats;

-- Check index fragmentation
SELECT 
    OBJECT_NAME(ips.object_id) AS TableName,
    i.name AS IndexName,
    ips.avg_fragmentation_in_percent
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ips
INNER JOIN sys.indexes i ON ips.object_id = i.object_id 
    AND ips.index_id = i.index_id
WHERE ips.avg_fragmentation_in_percent > 10
    AND ips.page_count > 1000
ORDER BY ips.avg_fragmentation_in_percent DESC;

-- Rebuild fragmented indexes (> 30% fragmented)
ALTER INDEX ALL ON dbo.Invoices REBUILD;

-- Reorganize slightly fragmented indexes (10-30%)
ALTER INDEX ALL ON dbo.Contacts REORGANIZE;
```

---

## Foreign Key Constraints

### Constraint Hierarchy

```
AspNetUsers (PK: Id)
    ↓
    ├─→ Contact.UserId (FK)
    ├─→ Product.UserId (FK)
    ├─→ Invoice.UserId (FK)
    ├─→ KPI.UserId (FK)
    ├─→ LeadLake.UserId (FK)
    ├─→ Meeting.UserId (FK)
    ├─→ BankAccount.UserId (FK) [UNIQUE]
    ├─→ Payout.UserId (FK)
    ├─→ Assessment.UserId (FK)
    ├─→ AssessmentSubmission.UserId (FK)
    ├─→ Inbox.UserId (FK)
    ├─→ RelayEmailAccount.UserId (FK)
    └─→ RelayEmailReply.UserId (FK)

Product (PK: Id)
    ↓
    ├─→ Invoice.ProductId (FK)
    ├─→ KPI.ProductId (FK)
    ├─→ KpiPolicy.ProductId (FK)
    ├─→ Meeting.ProductId (FK)
    ├─→ Assessment.ProductId (FK)
    └─→ AssessmentSubmission.ProductId (FK)

Contact (PK: Id)
    ↓
    ├─→ Invoice.ClientId (FK)
    └─→ Meeting.ClientId (FK)

BankAccount (PK: Id)
    ↓
    └─→ Payout.BankAccountId (FK)

LeadLake (PK: Id)
    ↓
    └─→ RelayEmailReply.LeadLakeId (FK)

RelayEmailAccount (PK: Id)
    ↓
    └─→ RelayEmailReply.RelayEmailAccountId (FK)
```

### Cascade Rules

| Constraint | Delete Rule | Rationale |
|-----------|------------|-----------|
| Contact (PK) | RESTRICT | Never delete contacts with invoices/meetings |
| Invoice → Product | RESTRICT | Never auto-delete invoices if product deleted |
| Payout → BankAccount | RESTRICT | Preserve payout history |
| LeadLake (PK) → RelayEmailReply | CASCADE | Delete replies when lead is deleted |
| RelayEmailAccount → RelayEmailReply | CASCADE | Clean up replies when account removed |

### SQL Constraints

```sql
-- User isolation (cannot delete users with active data)
ALTER TABLE Contacts 
ADD CONSTRAINT FK_Contacts_AspNetUsers 
    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE;

-- Product constraints
ALTER TABLE Invoices 
ADD CONSTRAINT FK_Invoices_Products 
    FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE RESTRICT;

ALTER TABLE KPIs 
ADD CONSTRAINT FK_KPIs_Products 
    FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE RESTRICT;

-- Contact constraints
ALTER TABLE Invoices 
ADD CONSTRAINT FK_Invoices_Contacts 
    FOREIGN KEY (ClientId) REFERENCES Contacts(Id) ON DELETE RESTRICT;

ALTER TABLE Meetings 
ADD CONSTRAINT FK_Meetings_Contacts 
    FOREIGN KEY (ClientId) REFERENCES Contacts(Id) ON DELETE RESTRICT;

-- Bank account constraints
ALTER TABLE Payouts 
ADD CONSTRAINT FK_Payouts_BankAccounts 
    FOREIGN KEY (BankAccountId) REFERENCES BankAccounts(Id) ON DELETE RESTRICT;

-- Email constraints
ALTER TABLE RelayEmailReplies 
ADD CONSTRAINT FK_RelayEmailReplies_Leads 
    FOREIGN KEY (LeadLakeId) REFERENCES LeadLakes(Id) ON DELETE CASCADE;

ALTER TABLE RelayEmailReplies 
ADD CONSTRAINT FK_RelayEmailReplies_EmailAccounts 
    FOREIGN KEY (RelayEmailAccountId) REFERENCES RelayEmailAccounts(Id) ON DELETE CASCADE;
```

---

## Query Optimization

### Common Query Patterns

#### 1. **Get All Contacts for a User**
```csharp
// LINQ (Entity Framework)
var contacts = _context.Contacts
    .Where(c => c.UserId == userId)
    .OrderBy(c => c.Status)
    .ToList();
```

**SQL (Optimized by Index IX_Contacts_UserId_Status):**
```sql
SELECT * FROM Contacts 
WHERE UserId = @UserId 
ORDER BY Status;
```

**Index Used:** `IX_Contacts_UserId_Status` (Composite)

---

#### 2. **Get Latest KPI for Product**
```csharp
var latestKpi = _context.KPIs
    .Where(k => k.UserId == userId && k.ProductId == productId)
    .OrderByDescending(k => k.CreatedAt)
    .FirstOrDefault();
```

**SQL (Optimized by Composite Index):**
```sql
SELECT TOP 1 * FROM KPIs 
WHERE UserId = @UserId AND ProductId = @ProductId 
ORDER BY CreatedAt DESC;
```

**Index Used:** `IX_KPIs_UserId_ProductId_CreatedAt` (DESC)

---

#### 3. **Search Leads by Email**
```csharp
var leads = _context.LeadLakes
    .Where(l => l.UserId == userId && l.Email == email)
    .ToList();
```

**SQL (Optimized):**
```sql
SELECT * FROM LeadLakes 
WHERE UserId = @UserId AND Email = @Email;
```

**Index Used:** `IX_LeadLakes_UserId_Email` (Composite)

---

#### 4. **Get Unread Emails for User**
```csharp
var unreadEmails = _context.RelayEmailReplies
    .Where(r => r.UserId == userId && !r.IsRead)
    .OrderByDescending(r => r.ReceivedDate)
    .ToList();
```

**SQL (Optimized):**
```sql
SELECT * FROM RelayEmailReplies 
WHERE UserId = @UserId AND IsRead = 0 
ORDER BY ReceivedDate DESC;
```

**Index Used:** `IX_RelayEmailReplies_UserId_IsRead` (Composite)

---

#### 5. **Monthly Payout Summary**
```csharp
var payouts = _context.Payouts
    .Where(p => p.UserId == userId && p.Month == month && p.Year == year)
    .ToList();
```

**SQL (Optimized):**
```sql
SELECT * FROM Payouts 
WHERE UserId = @UserId AND Month = @Month AND Year = @Year;
```

**Index Used:** `IX_Payouts_UserId_Month_Year` (Composite)

---

### Query Performance Tips

1. **Always Filter by UserId First** (tenant isolation + index optimization)
2. **Use Composite Indexes** for multi-column WHERE clauses
3. **Include SELECT Columns** in index (INCLUDE clause) to avoid table lookup
4. **Order Results with Index** (descending dates for latest items)
5. **Avoid SELECT *** - only retrieve needed columns**
6. **Use Pagination** for large result sets (.Skip().Take())

---

## Data Constraints & Validation

### Column-Level Constraints

#### MaxLength Constraints (Required for NVARCHAR Indexing)

```csharp
// Contact
modelBuilder.Entity<Contact>(e =>
{
    e.Property(c => c.UserId).HasMaxLength(450);      // Identity FK
    e.Property(c => c.Email).HasMaxLength(256);        // Indexable
});

// Invoice
modelBuilder.Entity<Invoice>(e =>
{
    e.Property(i => i.UserId).HasMaxLength(450);
    e.Property(i => i.InvoiceCode).HasMaxLength(128);
    e.Property(i => i.ExternalInvoiceId).HasMaxLength(256);
});

// RelayEmailReply
modelBuilder.Entity<RelayEmailReply>(e =>
{
    e.Property(r => r.UserId).HasMaxLength(450);
    e.Property(r => r.FromEmail).HasMaxLength(256);
    e.Property(r => r.MessageId).HasMaxLength(256);
    e.Property(r => r.Tags).HasMaxLength(1000);
});
```

#### Default Values

```csharp
// Timestamps
public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
public DateTime UpdatedAt { get; set; }

// Flags
public bool IsPaid { get; set; } = false;
public bool IsActive { get; set; } = true;
public bool IsPrimary { get; set; } = false;

// Numeric
public decimal Amount { get; set; } = 0;
public int SentToday { get; set; } = 0;
public int DailySendLimit { get; set; } = 50;
```

### Application-Level Validation

```csharp
public class Contact : BaseEntity
{
    [Required]
    [StringLength(256)]
    public string FirstName { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Range(0, decimal.MaxValue)]
    public decimal ClosedAmount { get; set; }
}

public class Invoice : BaseEntity
{
    [Required]
    public int ProductId { get; set; }

    [Range(0.01, decimal.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public DateTime IssuedAt { get; set; }
}
```

### Enum Validations

```csharp
public enum ContactStatus
{
    Reached = 0,
    FollowUp = 1,
    Converted = 2,
    Active = 3,
    Deactivated = 4
}

public enum LeadIntent
{
    Low = 0,
    Medium = 1,
    High = 2
}

public enum AccountHealthStatus
{
    Healthy = 0,
    Warning = 1,
    Critical = 2,
    Disabled = 3
}
```

---

## Relationships Matrix

### Complete Relationship Mapping

| From | To | Type | Cardinality | FK Column | Delete Rule | Purpose |
|------|----|----|-------------|-----------|-------------|---------|
| AspNetUsers | Contact | One:Many | 1:N | UserId | CASCADE | User owns contacts |
| AspNetUsers | Product | One:Many | 1:N | UserId | CASCADE | User owns products |
| AspNetUsers | Invoice | One:Many | 1:N | UserId | CASCADE | User owns invoices |
| AspNetUsers | KPI | One:Many | 1:N | UserId | CASCADE | User owns KPIs |
| AspNetUsers | LeadLake | One:Many | 1:N | UserId | CASCADE | User owns leads |
| AspNetUsers | Meeting | One:Many | 1:N | UserId | CASCADE | User owns meetings |
| AspNetUsers | BankAccount | One:One | 1:1 | UserId | CASCADE | User has one account |
| AspNetUsers | Payout | One:Many | 1:N | UserId | CASCADE | User has payouts |
| AspNetUsers | Assessment | One:Many | 1:N | UserId | CASCADE | User owns assessments |
| AspNetUsers | AssessmentSubmission | One:Many | 1:N | UserId | CASCADE | User submits assessments |
| AspNetUsers | Inbox | One:Many | 1:N | UserId | CASCADE | User has inboxes |
| AspNetUsers | RelayEmailAccount | One:Many | 1:N | UserId | CASCADE | User owns email accounts |
| AspNetUsers | RelayEmailReply | One:Many | 1:N | UserId | CASCADE | User has replies |
| Product | Invoice | One:Many | 1:N | ProductId | RESTRICT | Product has invoices |
| Product | KPI | One:Many | 1:N | ProductId | RESTRICT | Product has KPIs |
| Product | KpiPolicy | One:Many | 1:N | ProductId | RESTRICT | Product has policies |
| Product | Meeting | One:Many | 1:N | ProductId | RESTRICT | Product has meetings |
| Product | Assessment | One:Many | 1:N | ProductId | RESTRICT | Product has assessments |
| Product | AssessmentSubmission | One:Many | 1:N | ProductId | RESTRICT | Product has submissions |
| Contact | Invoice | One:Many | 1:N | ClientId | RESTRICT | Contact has invoices |
| Contact | Meeting | One:Many | 1:N | ClientId | RESTRICT | Contact has meetings |
| BankAccount | Payout | One:Many | 1:N | BankAccountId | RESTRICT | Account receives payouts |
| LeadLake | RelayEmailReply | One:Many | 1:N | LeadLakeId | CASCADE | Lead receives replies |
| RelayEmailAccount | RelayEmailReply | One:Many | 1:N | RelayEmailAccountId | CASCADE | Account sends replies |

---

## Database Maintenance

### Regular Maintenance Tasks

#### 1. **Index Fragmentation Monitoring** (Weekly)
```sql
-- Check fragmentation
SELECT 
    OBJECT_NAME(ips.object_id) AS TableName,
    i.name AS IndexName,
    ips.avg_fragmentation_in_percent,
    ips.page_count
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ips
INNER JOIN sys.indexes i ON ips.object_id = i.object_id AND ips.index_id = i.index_id
WHERE ips.index_id > 0  -- Exclude heaps
ORDER BY ips.avg_fragmentation_in_percent DESC;
```

#### 2. **Rebuild/Reorganize Indexes**
```sql
-- Rebuild (> 30% fragmented)
ALTER INDEX IX_Contacts_UserId ON Contacts REBUILD;

-- Reorganize (10-30% fragmented)
ALTER INDEX IX_Invoices_ProductId ON Invoices REORGANIZE;

-- Rebuild all indexes in table
ALTER INDEX ALL ON Contacts REBUILD;
```

#### 3. **Update Statistics** (After bulk operations)
```sql
-- Update all statistics
EXEC sp_updatestats;

-- Update specific table statistics
UPDATE STATISTICS Contacts;
UPDATE STATISTICS Invoices;
```

#### 4. **Check Orphaned Records** (Monthly)
```sql
-- Find invoices with deleted products
SELECT i.* FROM Invoices i
LEFT JOIN Products p ON i.ProductId = p.Id
WHERE p.Id IS NULL;

-- Find payouts with deleted bank accounts
SELECT p.* FROM Payouts p
LEFT JOIN BankAccounts b ON p.BankAccountId = b.Id
WHERE b.Id IS NULL;
```

#### 5. **Audit Trail Review** (Monthly)
```sql
-- Find recently modified records
SELECT 
    'Contact' AS TableName, COUNT(*) AS Count, MAX(UpdatedAt) AS LastUpdated
FROM Contacts
WHERE UpdatedAt > DATEADD(DAY, -7, GETUTCDATE())

UNION ALL

SELECT 
    'Invoice', COUNT(*), MAX(UpdatedAt)
FROM Invoices
WHERE UpdatedAt > DATEADD(DAY, -7, GETUTCDATE());
```

---

## Performance Metrics

### Recommended Monitoring

1. **Query Performance**
   - Average query execution time
   - Slow queries (> 1 second)
   - Lock wait times

2. **Index Health**
   - Fragmentation percentage
   - Unused indexes
   - Missing indexes

3. **Table Growth**
   - Row counts per table
   - Data size per table
   - Growth rate

### Sample Monitoring Query

```sql
SELECT 
    OBJECT_NAME(ips.object_id) AS TableName,
    i.name AS IndexName,
    ips.avg_fragmentation_in_percent AS Fragmentation,
    ips.page_count AS Pages,
    ps.row_count AS Rows,
    CAST(ips.page_count * 8 / 1024.0 AS DECIMAL(10,2)) AS SizeMB
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ips
INNER JOIN sys.indexes i ON ips.object_id = i.object_id AND ips.index_id = i.index_id
INNER JOIN sys.dm_db_partition_stats ps ON ips.object_id = ps.object_id 
    AND ips.index_id = ps.index_id AND ips.partition_number = ps.partition_number
WHERE ips.index_id > 0
ORDER BY ips.page_count DESC;
```

---

## Migration Guide

### Creating the Database

```csharp
// In Program.cs or Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));
}

public void Configure(IApplicationBuilder app)
{
    using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
    {
        serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.Migrate();
    }
}
```

### Creating Migrations

```bash
# Create initial migration
Add-Migration InitialCreate -Context ApplicationDbContext

# Create specific entity migration
Add-Migration AddBankAccounts -Context ApplicationDbContext

# Apply migrations
Update-Database -Context ApplicationDbContext

# Generate SQL script
Script-Migration -From 0 -To InitialCreate
```

---

## Conclusion

This documentation outlines a **well-normalized, indexed, and optimized SQL Server database** for ProcessZero.Web. Key highlights:

✅ **3NF Normalization** - Eliminates data redundancy while maintaining performance  
✅ **Comprehensive Indexing** - Optimizes 50+ common query patterns  
✅ **Referential Integrity** - Foreign key constraints with appropriate cascade rules  
✅ **Multi-Tenancy** - User-based data isolation  
✅ **Audit Trail** - All entities track CreatedAt/UpdatedAt  
✅ **Performance-First** - Composite indexes, filtered indexes, and denormalization where justified  

For questions or updates, refer to the ApplicationDbContext.cs class or contact the development team.
