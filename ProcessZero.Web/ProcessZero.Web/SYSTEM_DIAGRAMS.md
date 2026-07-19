# Email Account Pre-Selection - System Diagrams

## 1. System Architecture Diagram

```
┌───────────────────────────────────────────────────────────────────────────────┐
│                     RELAY EMAIL ACCOUNT PRE-SELECTION SYSTEM                  │
└───────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                            API LAYER (Controllers)                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  POST /api/relay/campaigns/{id}/accounts/{accountId}                       │
│  └─ AddEmailAccountToCampaignAsync()                                      │
│     └─ Validates & creates RelayCampaignEmailAccount                      │
│                                                                             │
│  DELETE /api/relay/campaigns/{id}/accounts/{accountId}                     │
│  └─ RemoveEmailAccountFromCampaignAsync()                                 │
│     └─ Deletes association & cancels pending emails                       │
│                                                                             │
│  GET /api/relay/campaigns/{id}/accounts                                    │
│  └─ GetCampaignEmailAccountsAsync()                                       │
│     └─ Returns pre-selected accounts for campaign                         │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
                                    ↓
┌─────────────────────────────────────────────────────────────────────────────┐
│                      SERVICE LAYER (Business Logic)                         │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  RelayService                                                              │
│  ├─ AddEmailAccountToCampaignAsync()                                      │
│  │  └─ Validates account ownership & health                              │
│  │  └─ Creates RelayCampaignEmailAccount (PRE-SELECTION ENFORCEMENT)     │
│  │                                                                         │
│  ├─ ScheduleCampaignEmailsAsync()                                        │
│  │  └─ Gets ONLY pre-selected accounts (from RelayCampaignEmailAccounts) │
│  │  └─ Never queries all accounts in database                            │
│  │  └─ Distributes leads across pre-selected accounts                    │
│  │                                                                         │
│  ├─ ProcessPendingEmailsAsync()                                          │
│  │  └─ NEW: Validates account still in campaign (NEW VALIDATION)         │
│  │  └─ NEW: Validates account still active (NEW VALIDATION)              │
│  │  └─ Sends email or cancels with reason                                │
│  │                                                                         │
│  └─ RemoveEmailAccountFromCampaignAsync()                                │
│     └─ Deletes association                                                │
│     └─ Cancels pending emails from this account                           │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
                                    ↓
┌─────────────────────────────────────────────────────────────────────────────┐
│                          DATA LAYER (Database)                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  RelayCampaigns                                                            │
│  ├─ Id, Name, Status, UserId                                             │
│  └─ Navigation: CampaignEmailAccounts (junction table)                   │
│                                                                             │
│  RelayCampaignEmailAccounts (PRE-SELECTION ENFORCEMENT POINT)            │
│  ├─ CampaignId, EmailAccountId                                          │
│  ├─ UserId (who added account) ← NEW FIELD                              │
│  ├─ IsActive                                                             │
│  ├─ Priority, SentTodayInCampaign, TotalSentInCampaign                 │
│  └─ Index: CampaignId, EmailAccountId, IsActive                        │
│  └─ Index: UserId (NEW)                                                 │
│                                                                             │
│  RelayEmailAccounts (User's Email Account)                              │
│  ├─ Id, EmailAddress, Provider                                          │
│  ├─ UserId (account owner)                                              │
│  ├─ IsActive, HealthStatus, ReputationScore                            │
│  └─ DailySendLimit, SentToday                                          │
│                                                                             │
│  RelayEmails (Individual Email Records)                                 │
│  ├─ CampaignId, EmailAccountId, LeadId                                 │
│  ├─ Status (Queued, Scheduled, Sending, Sent, Failed, Cancelled)      │
│  ├─ ErrorMessage (e.g., "Account removed from campaign")              │
│  └─ SentAt, AttemptCount                                               │
│                                                                             │
│  Background Jobs / Timers                                                │
│  ├─ ProcessPendingEmailsAsync() → runs every minute                    │
│  ├─ ScheduleCampaignEmailsAsync() → runs per active campaign          │
│  └─ AutoRefreshExpiringTokensAsync() → runs daily                     │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Email Lifecycle with Pre-Selection

```
EMAIL LIFECYCLE
═══════════════════════════════════════════════════════════════════════════════

┌─ STEP 1: Account Added to Campaign
│  ┌──────────────────────────────────────────────────────────────────────┐
│  │ AddEmailAccountToCampaignAsync(campaignId=5, accountId=12)          │
│  ├──────────────────────────────────────────────────────────────────────┤
│  │ VALIDATIONS:                                                         │
│  │ ├─ Campaign exists?              ✓                                  │
│  │ ├─ Account exists?               ✓                                  │
│  │ ├─ Account active?               ✓                                  │
│  │ ├─ Account healthy?              ✓ (not Critical/Disabled)          │
│  │ └─ Not already added?            ✓                                  │
│  │                                                                      │
│  │ RESULT:                                                              │
│  │ RelayCampaignEmailAccount Created:                                  │
│  │ {                                                                    │
│  │   CampaignId: 5,                                                    │
│  │   EmailAccountId: 12,                                              │
│  │   UserId: "user-alice",  ← NEW: Tracks who added it                │
│  │   IsActive: true,                                                  │
│  │   AddedAt: 2024-01-15 10:00:00                                    │
│  │ }                                                                    │
│  │                                                                      │
│  │ Status: ✅ Account is NOW available for Campaign 5                  │
│  └──────────────────────────────────────────────────────────────────────┘
│
├─ STEP 2: Campaign Schedules Emails
│  ┌──────────────────────────────────────────────────────────────────────┐
│  │ ScheduleCampaignEmailsAsync(campaignId=5)                           │
│  ├──────────────────────────────────────────────────────────────────────┤
│  │ LOAD PRE-SELECTED ACCOUNTS:                                          │
│  │ ├─ FROM: campaign.CampaignEmailAccounts (junction table ONLY)       │
│  │ ├─ WHERE: CampaignId = 5, IsActive = true                          │
│  │ ├─ FILTER: SentToday < DailySendLimit                              │
│  │ └─ RESULT: [AccountId 12, AccountId 18]                           │
│  │                                                                      │
│  │ FOR EACH LEAD (1000 leads total):                                   │
│  │ ├─ Select FROM pre-selected accounts [12, 18]                      │
│  │ ├─ NEVER query all accounts in database                            │
│  │ ├─ NEVER use Account 20, 21, 22, etc. (not pre-selected)          │
│  │ └─ Create RelayEmail                                               │
│  │    {                                                                 │
│  │      CampaignId: 5,                                                │
│  │      LeadId: 1001,                                                 │
│  │      EmailAccountId: 12,  ← FROM PRE-SELECTED LIST ONLY           │
│  │      SequenceId: 1,                                                │
│  │      Status: "Scheduled",                                          │
│  │      ScheduledAt: 2024-01-15 14:00:00                            │
│  │    }                                                                 │
│  │                                                                      │
│  │ Status: ✅ 1000 emails scheduled (using accounts 12, 18 only)      │
│  └──────────────────────────────────────────────────────────────────────┘
│
├─ STEP 3: Emails Ready to Send
│  ┌──────────────────────────────────────────────────────────────────────┐
│  │ ProcessPendingEmailsAsync() - Called every minute                   │
│  ├──────────────────────────────────────────────────────────────────────┤
│  │ FOR EACH pending email (100 at a time):                             │
│  │                                                                      │
│  │ ┌─ NEW VALIDATION (Most Important!) ─────────────────────────┐     │
│  │ │ VERIFY: Is this account still in this campaign?            │     │
│  │ │                                                             │     │
│  │ │ Query:                                                      │     │
│  │ │ SELECT * FROM RelayCampaignEmailAccounts                  │     │
│  │ │ WHERE CampaignId = email.CampaignId (5)                  │     │
│  │ │   AND EmailAccountId = email.EmailAccountId (12)         │     │
│  │ │   AND IsActive = true                                     │     │
│  │ │                                                             │     │
│  │ │ Result:                                                     │     │
│  │ │ ├─ FOUND: ✅ Continue to next validation                  │     │
│  │ │ └─ NOT FOUND: ✅ Email.Status = Cancelled                │     │
│  │ │               Email.ErrorMessage =                        │     │
│  │ │               "Email account has been removed from this    │     │
│  │ │                campaign"                                   │     │
│  │ │               SKIP TO NEXT EMAIL (don't send)             │     │
│  │ └─────────────────────────────────────────────────────────┘     │
│  │                                                                      │
│  │ ├─ NEW: Verify account is still active                             │
│  │ │  └─ if NOT: Email.Status = Cancelled, skip                      │
│  │ │                                                                    │
│  │ ├─ Verify campaign is still active                                 │
│  │ │  └─ if NOT: Email.Status = Cancelled, skip                      │
│  │ │                                                                    │
│  │ ├─ Verify lead not unsubscribed                                    │
│  │ │  └─ if YES: Email.Status = Cancelled, skip                      │
│  │ │                                                                    │
│  │ ├─ Check account daily limit                                       │
│  │ │  └─ if exceeded: Reschedule for tomorrow                        │
│  │ │                                                                    │
│  │ ├─ Check campaign daily limit                                      │
│  │ │  └─ if exceeded: Reschedule for tomorrow                        │
│  │ │                                                                    │
│  │ ├─ Check sending window                                            │
│  │ │  └─ if outside: Reschedule to next window                       │
│  │ │                                                                    │
│  │ ├─ Check rate limiting                                             │
│  │ │  └─ if min time not reached: Reschedule                         │
│  │ │                                                                    │
│  │ └─ ALL VALIDATIONS PASS: ✅ SEND EMAIL                            │
│  │    ├─ Email.Status = "Sent"                                        │
│  │    ├─ Email.SentAt = DateTime.UtcNow                              │
│  │    ├─ EmailAccount.SentToday++                                     │
│  │    ├─ Campaign.SentToday++                                         │
│  │    └─ Update campaign lead progress                                │
│  │                                                                      │
│  │ Status: ✅ Emails sent or cancelled with clear reasons             │
│  └──────────────────────────────────────────────────────────────────────┘
│
└─ STEP 4: Account Removed From Campaign (Later)
   ┌──────────────────────────────────────────────────────────────────────┐
   │ RemoveEmailAccountFromCampaignAsync(campaignId=5, accountId=12)    │
   ├──────────────────────────────────────────────────────────────────────┤
   │ ACTIONS:                                                              │
   │ ├─ Delete RelayCampaignEmailAccount record                          │
   │ │  └─ WHERE CampaignId=5 AND EmailAccountId=12                    │
   │ │                                                                    │
   │ ├─ Find pending emails from this account                            │
   │ │  └─ SELECT * FROM RelayEmails                                    │
   │ │     WHERE CampaignId=5 AND EmailAccountId=12                    │
   │ │     AND Status IN ('Queued', 'Scheduled')                       │
   │ │                                                                    │
   │ ├─ Cancel pending emails                                            │
   │ │  └─ Status = "Cancelled"                                         │
   │ │  └─ ErrorMessage = "Account removed from campaign"              │
   │ │                                                                    │
   │ └─ PRESERVE sent emails                                             │
   │    └─ Already-sent emails NOT deleted                             │
   │    └─ Audit trail maintained                                       │
   │                                                                      │
   │ Status: ✅ Account removed, pending emails cancelled, audit trail   │
   │        maintained                                                    │
   └──────────────────────────────────────────────────────────────────────┘
```

---

## 3. Database Relationships

```
BEFORE: No Pre-Selection Enforcement
═══════════════════════════════════════════════════════════════════════════════
RelayEmailAccount (All accounts)     RelayCampaign
├─ Id: 1, Email: alice@co.com        ├─ Id: 1, Name: Campaign A
├─ Id: 2, Email: bob@co.com          └─ Status: Active
├─ Id: 3, Email: charlie@co.com
└─ Id: 4, Email: admin@co.com

Problem: Campaign 1 could use ANY account (1, 2, 3, 4)
         No explicit pre-selection mechanism


AFTER: With Pre-Selection Enforcement
═══════════════════════════════════════════════════════════════════════════════
RelayEmailAccount               RelayCampaignEmailAccount    RelayCampaign
├─ Id: 1 (Alice)                ├─ Campaign 1               ├─ Id: 1
├─ Id: 2 (Bob)                  │  ├─ Account 1 (Alice)     │  └─ Name: Campaign A
├─ Id: 3 (Charlie)              │  ├─ Account 2 (Bob)       │
└─ Id: 4 (Admin)                │  └─ IsActive: true        └─ Id: 2
                                │                              └─ Name: Campaign B
                                └─ Campaign 2
                                   ├─ Account 3 (Charlie)
                                   └─ Account 4 (Admin)

ENFORCEMENT:
├─ Campaign 1 can ONLY use Accounts 1 & 2 (explicitly added)
├─ Campaign 2 can ONLY use Accounts 3 & 4 (explicitly added)
├─ Account 1 (Alice) can ONLY be used if Alice adds it to campaign
├─ No cross-campaign account leakage
└─ Full audit trail of who added what when
```

---

## 4. Validation State Machine

```
EMAIL STATES & VALIDATIONS
═══════════════════════════════════════════════════════════════════════════════

                            [Created]
                                ↓
                        ┌───────────────┐
                        │   Scheduled   │
                        └───────────────┘
                                ↓
                ┌───────────────────────────────────┐
                │  ProcessPendingEmailsAsync()      │
                │  Validation #1: PRE-SELECTION   │  ← NEW
                └───────────────────────────────────┘
                        /               \
                       /                 \
            ✗ Account not in       ✓ Account in
              campaign               campaign
                  ↓                     ↓
            [Cancelled]        ┌────────────────┐
            Reason: "Account    │ Validation #2  │
            removed from        │ Account Active │
            campaign"           └────────────────┘
                                    /       \
                           ✗ Inactive    ✓ Active
                                ↓           ↓
                            [Cancelled] [Check Campaign]
                            Reason:         ↓
                            "Account    Campaign
                            deactivated" Active?
                                            /     \
                           ✗ Inactive  ✓ Active
                                ↓           ↓
                            [Cancelled]  [Check Lead]
                            Reason:         ↓
                            "Campaign"  Lead Invalid/
                                        Unsubscribed?
                                        /            \
                               ✓ Valid    ✗ Invalid
                                ↓            ↓
                            [Check      [Cancelled]
                             Limits]    Reason:
                                ↓        "Lead..."
                            Daily                  
                            Limits OK?
                            /        \
                   ✓ OK  ✗ Exceeded
                    ↓        ↓
                [Check    [Reschedule]
                 Window]
                    ↓
                Sending
                Window OK?
                /         \
           ✓ Yes  ✗ No
            ↓        ↓
        [Check   [Reschedule]
         Rate]
            ↓
        Rate Limit OK?
        /            \
   ✓ Yes  ✗ No
    ↓        ↓
 [SEND]  [Reschedule]
    ↓
┌──────────────┐
│ Email Sent   │
├──────────────┤
│ Status:      │
│ "Sent"       │
│ SentAt: now  │
│ Attempt++    │
└──────────────┘
```

---

## 5. Multi-User Distribution

```
MULTI-USER CAMPAIGN: 1000 Leads, 3 Users, 3 Accounts
═══════════════════════════════════════════════════════════════════════════════

Campaign Setup:
───────────────
├─ Alice adds Account 1 (limit: 100/day)
├─ Bob adds Account 2 (limit: 150/day)
└─ Charlie adds Account 3 (limit: 75/day)
   └─ Total capacity: 325 emails/day


Email Scheduling:
─────────────────
Round-Robin Distribution:
├─ Lead 1 → Account 1 (Alice)
├─ Lead 2 → Account 2 (Bob)
├─ Lead 3 → Account 3 (Charlie)
├─ Lead 4 → Account 1 (Alice)
├─ Lead 5 → Account 2 (Bob)
├─ ...
└─ Continue until all leads assigned


Distribution Results:
────────────────────
├─ Account 1 (Alice): ~334 leads → 334 emails
├─ Account 2 (Bob): ~334 leads → 334 emails
└─ Account 3 (Charlie): ~332 leads → 332 emails

BUT: Daily limits respected
├─ Account 1: 100 emails sent, 234 rescheduled
├─ Account 2: 150 emails sent, 184 rescheduled
└─ Account 3: 75 emails sent, 257 rescheduled

Total today: 325 emails sent
Remaining: 675 leads queued for tomorrow


Account Contribution Tracking:
──────────────────────────────
RelayCampaignEmailAccount:
├─ Account 1 (Alice)
│  ├─ UserId: "user-alice"
│  ├─ SentTodayInCampaign: 100
│  └─ TotalSentInCampaign: 3,450 (over campaign lifetime)
│
├─ Account 2 (Bob)
│  ├─ UserId: "user-bob"
│  ├─ SentTodayInCampaign: 150
│  └─ TotalSentInCampaign: 5,120 (over campaign lifetime)
│
└─ Account 3 (Charlie)
   ├─ UserId: "user-charlie"
   ├─ SentTodayInCampaign: 75
   └─ TotalSentInCampaign: 2,840 (over campaign lifetime)


Removal Scenario:
─────────────────
When Alice removes Account 1:

BEFORE REMOVAL:
├─ Pending emails from Account 1: 234 scheduled
├─ Daily sent from Account 1: 100
└─ Campaign status: Active

AFTER REMOVAL:
├─ RelayCampaignEmailAccount record deleted
├─ Pending emails from Account 1:
│  ├─ Status: Cancelled
│  ├─ Reason: "Account removed from campaign"
│  └─ Count: 234 emails cancelled
├─ Campaign continues with:
│  ├─ Account 2 (Bob): 150 emails/day
│  └─ Account 3 (Charlie): 75 emails/day
│  └─ Total: 225 emails/day (reduced from 325)
└─ Already-sent emails: Preserved (100 from Account 1)

RESULT: ✅ Account 1 cleanly removed, 234 pending emails cancelled, audit trail maintained
```

---

## 6. Code Flow Diagram

```
API REQUEST: AddEmailAccountToCampaignAsync(campaignId=5, accountId=12)
│
├─ HTTP POST /api/relay/campaigns/5/accounts/12
│  └─ Authorization check (Authenticated user)
│
├─ Service.AddEmailAccountToCampaignAsync(5, 12)
│  ├─ Get campaign (await _context.RelayCampaigns.FindAsync(5))
│  │  └─ if null: throw KeyNotFoundException
│  │
│  ├─ Get account (await _context.RelayEmailAccounts.FindAsync(12))
│  │  └─ if null: throw KeyNotFoundException
│  │
│  ├─ Check account.IsActive
│  │  └─ if false: throw InvalidOperationException
│  │
│  ├─ Check account.HealthStatus
│  │  └─ if Critical or Disabled: throw InvalidOperationException
│  │
│  ├─ Check for duplicates
│  │  ├─ Query: WHERE CampaignId=5 AND EmailAccountId=12
│  │  └─ if exists: throw InvalidOperationException
│  │
│  ├─ Create RelayCampaignEmailAccount
│  │  ├─ CampaignId: 5
│  │  ├─ EmailAccountId: 12
│  │  ├─ UserId: account.UserId (multi-user tracking)
│  │  ├─ IsActive: true
│  │  └─ AddedAt: DateTime.UtcNow
│  │
│  ├─ Save (await _context.SaveChangesAsync())
│  │
│  └─ Return (success)
│
└─ HTTP 200 OK
   └─ Account is now pre-selected for Campaign 5


SCHEDULED SENDING: ScheduleCampaignEmailsAsync(campaignId=5)
│
├─ Get campaign with relationships
│  ├─ Include(c => c.Sequences)
│  ├─ Include(c => c.CampaignLeads)
│  └─ Include(c => c.CampaignEmailAccounts) ← KEY: PRE-SELECTION
│
├─ Get pre-selected accounts
│  ├─ campaign.CampaignEmailAccounts ← FROM JUNCTION TABLE ONLY
│  ├─ Filter: IsActive && SentToday < DailySendLimit
│  └─ Result: Only explicitly-added accounts
│
├─ For each lead
│  ├─ Get next sequence step
│  ├─ Select A/B test variant
│  ├─ Select account from pre-selected list (round-robin)
│  └─ Create RelayEmail with selected account
│
├─ Save all emails (await _context.SaveChangesAsync())
│
└─ Return (complete)


EMAIL SENDING: ProcessPendingEmailsAsync()
│
├─ Get pending emails (take 100)
│  ├─ Include(e => e.Campaign)
│  ├─ Include(e => e.EmailAccount)
│  └─ Include(e => e.Lead)
│
├─ For each email
│  │
│  ├─ NEW: Check account still in campaign
│  │  ├─ Query: RelayCampaignEmailAccounts
│  │  │  WHERE CampaignId=email.CampaignId
│  │  │    AND EmailAccountId=email.EmailAccountId
│  │  │    AND IsActive=true
│  │  │
│  │  ├─ if NOT FOUND:
│  │  │  ├─ email.Status = "Cancelled"
│  │  │  ├─ email.ErrorMessage = "Account removed from campaign"
│  │  │  ├─ Save
│  │  │  └─ Continue (don't send)
│  │  │
│  │  └─ if FOUND: Continue...
│  │
│  ├─ NEW: Check account still active
│  │  ├─ if NOT:
│  │  │  ├─ email.Status = "Cancelled"
│  │  │  ├─ email.ErrorMessage = "Account deactivated"
│  │  │  ├─ Save
│  │  │  └─ Continue
│  │  │
│  │  └─ if active: Continue...
│  │
│  ├─ Check campaign still active
│  │  ├─ if NOT: Cancel email
│  │  └─ Continue...
│  │
│  ├─ Check lead valid
│  │  ├─ if unsubscribed/invalid: Cancel email
│  │  └─ Continue...
│  │
│  ├─ Check daily limits
│  ├─ Check sending window
│  ├─ Check rate limiting
│  │
│  ├─ IF all pass:
│  │  ├─ Send via Gmail/Microsoft/SMTP
│  │  ├─ email.Status = "Sent"
│  │  ├─ email.SentAt = DateTime.UtcNow
│  │  ├─ Update counters
│  │  └─ Save
│  │
│  └─ IF any fail:
│     ├─ email.Status = "Cancelled" or "Rescheduled"
│     ├─ Set appropriate error message
│     └─ Save
│
├─ Save batch (await _context.SaveChangesAsync())
│
└─ Return (complete)
```

---

## 7. Testing Matrix

```
TEST COVERAGE MATRIX
═══════════════════════════════════════════════════════════════════════════════

╔════════════════════════════════╦════════════╦═════════════════════════════╗
║ Test Scenario                  ║ Category   ║ Expected Result             ║
╠════════════════════════════════╬════════════╬═════════════════════════════╣
║ Add account to campaign        ║ Unit       ║ ✓ Junction record created   ║
║ Add inactive account           ║ Unit       ║ ✗ Throws exception         ║
║ Add unhealthy account          ║ Unit       ║ ✗ Throws exception         ║
║ Add duplicate account          ║ Unit       ║ ✗ Throws exception         ║
║ Remove account from campaign   ║ Unit       ║ ✓ Junction record deleted   ║
║ Get campaign accounts          ║ Unit       ║ ✓ Returns pre-selected only ║
║ Schedule emails                ║ Unit       ║ ✓ Uses only pre-selected    ║
║ Send email with valid account  ║ Unit       ║ ✓ Email sent               ║
║ Send email, account removed    ║ Unit       ║ ✓ Email cancelled          ║
║ Send email, account inactive   ║ Unit       ║ ✓ Email cancelled          ║
║─────────────────────────────────────────────────────────────────────────────║
║ Multi-user campaign            ║ Integration║ ✓ Each account isolated    ║
║ Account removal mid-campaign   ║ Integration║ ✓ Pending emails cancelled ║
║ Multiple campaigns, same user  ║ Integration║ ✓ Accounts isolated/       ║
║ Account at daily limit         ║ Integration║ ✓ Skipped in rotation     ║
║ Campaign with no accounts      ║ Integration║ ✓ No emails scheduled      ║
║ Campaign pause/resume          ║ Integration║ ✓ Email scheduling pauses  ║
║─────────────────────────────────────────────────────────────────────────────║
║ Database migration             ║ Migration  ║ ✓ UserId column created    ║
║ Rollback migration             ║ Migration  ║ ✓ Column removed safely    ║
║ Data preservation              ║ Migration  ║ ✓ No data loss             ║
║─────────────────────────────────────────────────────────────────────────────║
║ Load testing: 1000 emails      ║ Performance║ ✓ Sub-second processing    ║
║ Concurrent email sends         ║ Performance║ ✓ No race conditions       ║
║ Account removal concurrent     ║ Performance║ ✓ Consistent state         ║
╚════════════════════════════════╩════════════╩═════════════════════════════╝
```

---

## Summary

These diagrams illustrate:
1. ✅ System architecture with pre-selection enforcement
2. ✅ Complete email lifecycle with validation
3. ✅ Database relationships preventing account leakage
4. ✅ Email state machine with validations
5. ✅ Multi-user distribution with isolation
6. ✅ Code flow for each operation
7. ✅ Comprehensive testing matrix

All ensuring **only explicitly-added accounts are used by campaigns**.
