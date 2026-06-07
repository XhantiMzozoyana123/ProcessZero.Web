# Multi-User Email Account Ownership Model

## Overview
The system supports multiple users adding their own email accounts to shared campaigns with strict account ownership and pre-selection enforcement.

## Entity Relationships

### RelayEmailAccount (User's Personal Account)
```csharp
public class RelayEmailAccount
{
    public int Id { get; set; }
    public string EmailAddress { get; set; }
    public string UserId { get; set; }  // ← OWNER: who owns this account

    // OAuth tokens, SMTP settings, etc.
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTime? TokenExpiry { get; set; }

    // Health & reputation
    public bool IsActive { get; set; }
    public AccountHealthStatus HealthStatus { get; set; }
    public int ReputationScore { get; set; }

    // Daily sending limits
    public int DailySendLimit { get; set; }
    public int SentToday { get; set; }
}
```
**Purpose**: Represents one user's email account (Gmail, Outlook, or SMTP)

---

### RelayCampaignEmailAccount (Campaign-Account Association)
```csharp
public class RelayCampaignEmailAccount : BaseEntity
{
    public int CampaignId { get; set; }
    public int EmailAccountId { get; set; }
    public string UserId { get; set; }  // ← WHO ADDED IT: which user added this account to campaign

    public bool IsActive { get; set; }
    public int Priority { get; set; }
    public int SentTodayInCampaign { get; set; }
    public int TotalSentInCampaign { get; set; }
    public DateTime AddedAt { get; set; }
    public DateTime? LastSentAt { get; set; }

    // Navigation properties
    public virtual RelayCampaign Campaign { get; set; }
    public virtual RelayEmailAccount EmailAccount { get; set; }
}
```
**Purpose**: Pre-selection junction table tracking which accounts contribute to which campaigns

---

## Multi-User Scenario Example

### Scenario: Distributed Campaign with 3 Users

```
Campaign: "Sales Q1 Outreach"
├─ Admin: system-user-1
├─ Max Daily Send: 300 emails
│
├─ Participant 1: user-alice (Max 100/day)
│   └─ Account 1: alice@company.com
│       └─ Added to campaign at 2024-01-15 10:00 AM
│       └─ Status: Active, ReputationScore: 95
│
├─ Participant 2: user-bob (Max 100/day)
│   ├─ Account 2: bob@company.com
│   │   └─ Added to campaign at 2024-01-15 11:30 AM
│   │   └─ Status: Active, ReputationScore: 87
│   │
│   └─ Account 3: bob-secondary@company.com
│       └─ Added to campaign at 2024-01-16 09:00 AM
│       └─ Status: Active, ReputationScore: 72
│
└─ Participant 3: user-charlie (Max 50/day)
    └─ Account 4: charlie@company.com
        └─ Added to campaign at 2024-01-16 14:00 PM
        └─ Status: Active, ReputationScore: 91
```

### Database State

**RelayEmailAccount table:**
```
Id | EmailAddress              | UserId      | IsActive | DailySendLimit | SentToday
───┼───────────────────────────┼─────────────┼──────────┼────────────────┼─────────
1  | alice@company.com         | user-alice  | true     | 100            | 32
2  | bob@company.com           | user-bob    | true     | 100            | 45
3  | bob-secondary@company.com | user-bob    | true     | 100            | 0
4  | charlie@company.com       | user-charlie| true     | 50             | 18
```

**RelayCampaignEmailAccount table:**
```
Id | CampaignId | EmailAccountId | UserId       | IsActive | AddedAt              | SentTodayInCampaign
───┼────────────┼────────────────┼──────────────┼──────────┼──────────────────────┼──────────────────
1  | 5          | 1              | user-alice   | true     | 2024-01-15 10:00 AM  | 32
2  | 5          | 2              | user-bob     | true     | 2024-01-15 11:30 AM  | 28
3  | 5          | 3              | user-bob     | true     | 2024-01-16 09:00 AM  | 0
4  | 5          | 4              | user-charlie | true     | 2024-01-16 14:00 PM  | 18
```

**RelayCampaign table:**
```
Id | Name                      | Status | DailySendLimit | SentToday | UserId            
───┼───────────────────────────┼────────┼────────────────┼───────────┼──────────────────
5  | Sales Q1 Outreach         | Active | 300            | 78        | system-user-1
```

---

## Permission Model

### Who can add accounts to a campaign?

**Current Implementation:**
- Any authenticated user can add their own account to any active campaign
- User is identified by the account's UserId

**Future Enhancement (Not Yet Implemented):**
- Admin could restrict which users can add accounts to specific campaigns
- Role-based permissions (admin, contributor, viewer)

### Who can remove accounts from a campaign?

**Current Implementation:**
- The user who added the account (UserId match) or campaign admin
- Removes only their own account(s)

**Account Removal Effect:**
```
When user-bob removes account 3 from the campaign:
├─ RelayCampaignEmailAccount record deleted
├─ Scheduled emails from account 3 → Status = Cancelled
│  Reason: "Email account has been removed from this campaign"
├─ Already sent emails → Preserved for reporting
└─ Future emails → Will not be scheduled from account 3
```

---

## Email Sending with Multi-User Accounts

### Scheduling Phase
```csharp
var emailAccounts = campaign.CampaignEmailAccounts
    .Select(cea => cea.EmailAccount)
    .Where(a => a.IsActive && a.SentToday < a.DailySendLimit)
    .OrderBy(a => a.SentToday)  // Round-robin: prioritize lower count
    .ToList();

// For campaign "Sales Q1 Outreach":
// ├─ Account 1 (alice): 32/100 used ✓
// ├─ Account 2 (bob):   45/100 used ✓
// ├─ Account 3 (bob):   0/100 used  ✓
// └─ Account 4 (charlie): 18/50 used ✓
//
// All 4 accounts available for distribution
```

### Distribution Strategy
```
1000 leads to email
  │
  ├─ Round-robin across 4 accounts
  ├─ Prioritize accounts with lower SentToday
  │
  ├─ Account 3 (0 sent):   250 emails → 0 + 250 = 250 (saturated, still has 100 limit left)
  ├─ Account 1 (32 sent):  169 emails → 32 + 169 = 201 (near limit)
  ├─ Account 4 (18 sent):  32 emails  → 18 + 32 = 50 (at max limit)
  └─ Account 2 (45 sent):  55 emails  → 45 + 55 = 100 (at max limit)

Total: 250 + 169 + 32 + 55 = 506 emails scheduled today
Remaining: 1000 - 506 = 494 leads queued for tomorrow
```

### Sending Phase with Account Validation
```csharp
foreach (var email in pendingEmails)
{
    // NEW: Validate account is still in campaign
    var isAccountInCampaign = await _context.RelayCampaignEmailAccounts
        .AnyAsync(cea => cea.CampaignId == email.CampaignId 
                    && cea.EmailAccountId == email.EmailAccountId 
                    && cea.IsActive);

    if (!isAccountInCampaign)
    {
        // User removed account since this email was scheduled
        email.Status = EmailStatus.Cancelled;
        email.ErrorMessage = "Email account has been removed from this campaign";
        continue;  // Don't send
    }

    // All validations pass... SEND EMAIL
}
```

---

## Audit Trail

### What gets recorded?

**When user adds account to campaign:**
- RelayCampaignEmailAccount.UserId = "user-alice"
- RelayCampaignEmailAccount.AddedAt = [timestamp]
- Campaign.UpdatedAt = [timestamp]

**When user removes account from campaign:**
- RelayCampaignEmailAccount deleted
- Pending RelayEmail records cancelled with reason
- Campaign.UpdatedAt = [timestamp]

**When email is sent:**
- RelayEmail.UserId = campaign.UserId (who owns the campaign)
- RelayEmail.EmailAccountId = [account used]
- RelayEmail.SentAt = [timestamp]
- RelayEmail.Status = "Sent"

**When email fails due to removed account:**
- RelayEmail.Status = "Cancelled"
- RelayEmail.ErrorMessage = "Email account has been removed from this campaign"
- RelayEmail.UpdatedAt = [timestamp]

---

## Security Boundaries

### ✅ Account Isolation
- User Alice's accounts CANNOT be used without her explicitly adding them
- User Alice CANNOT add User Bob's account without User Bob's cooperation

### ✅ Campaign Isolation
- Accounts in Campaign 1 are NOT automatically in Campaign 2
- Each campaign maintains its own pre-selected account list

### ✅ Usage Tracking
- All emails track which account was used (EmailAccountId)
- Campaign-level stats show contribution from each account
- Can generate per-user reports

### ✅ Removal Safety
- When account is removed, pending emails are explicitly cancelled (not silently dropped)
- Already-sent emails preserved for reporting and compliance

---

## Business Logic Examples

### Example 1: User wants to test account in campaign
```
1. User adds their Gmail account to campaign
2. System validates: active, healthy, not duplicate
3. RelayCampaignEmailAccount created
4. Campaign's email scheduler includes this account
5. Emails are scheduled and sent

Result: User sees if their account works well
```

### Example 2: User's account gets compromised
```
1. User removes compromised account from all campaigns
2. RelayCampaignEmailAccount records deleted
3. Pending emails using this account → Status = Cancelled
4. User can delete the RelayEmailAccount record
5. Account's emails are cancelled but historical data preserved

Result: No more emails sent from compromised account
```

### Example 3: User reaches daily limit mid-day
```
1. At 2:00 PM, User Bob's account has sent 100 emails (at limit)
2. System tries to schedule more emails for Bob's account
3. Filter: SentToday < DailySendLimit → FALSE
4. Account not included in available accounts
5. Other accounts pick up the work

Result: Automatic failover to other accounts, no system error
```

### Example 4: Campaign admin wants to pause one user's participation
```
1. Admin sets RelayCampaignEmailAccount.IsActive = false
2. ProcessPendingEmailsAsync() checks IsActive
3. Account not included in pre-selection validation
4. Pending emails from this account → Cancelled
5. Other accounts handle campaign traffic

Result: User's account paused, campaign continues with other accounts
```

---

## Future Enhancements

### Recommended Additions:
1. **Permission Levels**: Admin, Contributor, Viewer roles per campaign
2. **Daily Quotas**: Admin can set max emails per user per campaign
3. **Account Priority**: Admin can prioritize certain accounts for better reputation
4. **Usage Reports**: Per-user breakdowns of emails sent, opens, clicks, replies
5. **Rate Limiting**: Enforce max email frequency per account
6. **Account Sharing**: Allow users to trust others with their accounts (with permission)
