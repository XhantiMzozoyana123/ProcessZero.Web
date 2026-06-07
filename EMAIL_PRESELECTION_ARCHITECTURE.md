# Email Account Pre-Selection Architecture

## Database Schema (Pre-Selection Enforcement)

```
┌─────────────────────────────────────────────────────────────┐
│                    RELAY EMAIL SYSTEM                       │
└─────────────────────────────────────────────────────────────┘

┌──────────────────┐         ┌──────────────────────────────┐
│  RelayCampaign   │ 1:N     │ RelayCampaignEmailAccount    │ ← ENFORCEMENT POINT
├──────────────────┤         ├──────────────────────────────┤
│ Id               │────────→│ CampaignId                   │
│ Name             │         │ EmailAccountId (FK)          │
│ Status           │         │ UserId (who added it)        │
│ SentToday        │         │ IsActive                     │
│ DailySendLimit   │         │ Priority                     │
│ ...              │         │ AddedAt                      │
└──────────────────┘         │ SentTodayInCampaign          │
                             │ TotalSentInCampaign          │
                             └──────────────────────────────┘
                                          ↓
                             ALL ACCOUNTS MUST
                          BE IN THIS TABLE TO
                         BE USED BY CAMPAIGN

         N:1
         │
         ↓
┌──────────────────────────┐
│ RelayEmailAccount        │
├──────────────────────────┤
│ Id                       │
│ EmailAddress             │
│ UserId (owner)           │ ← Multi-user tracking
│ Provider (OAuth/SMTP)    │
│ AccessToken              │
│ IsActive                 │
│ HealthStatus             │
│ SentToday                │
│ DailySendLimit           │
└──────────────────────────┘
```

## Pre-Selection Enforcement Flow

```
USER ACTION: Add Account to Campaign
════════════════════════════════════════════════════════════

1. API Call: AddEmailAccountToCampaignAsync(campaignId=5, accountId=12)
   │
   ├─ Verify campaign exists
   ├─ Verify account exists
   ├─ Verify account is active & healthy
   ├─ Check for duplicates
   │
   └─ CREATE RelayCampaignEmailAccount
      ├─ CampaignId: 5
      ├─ EmailAccountId: 12
      ├─ UserId: "user-uuid-456"  ← Multi-user tracking
      └─ IsActive: true


EMAIL SCHEDULING: Only Pre-Selected Accounts
════════════════════════════════════════════════════════════

1. ScheduleCampaignEmailsAsync(campaignId=5)
   │
   ├─ LOAD: campaign.CampaignEmailAccounts
   │        └─ Query: WHERE CampaignId = 5
   │           └─ Result: [AccountId: 12, AccountId: 18] (from junction table ONLY)
   │
   ├─ FILTER: Active && SentToday < DailySendLimit
   │
   ├─ FOR EACH LEAD:
   │   ├─ Select from pre-loaded accounts ONLY
   │   └─ CREATE RelayEmail
   │      ├─ EmailAccountId: 12 (from pre-selected list)
   │      ├─ Status: Scheduled
   │      └─ ScheduledAt: [calculated time]
   │
   └─ RESULT: Emails queued for accounts 12 & 18 ONLY


EMAIL SENDING: Validate Account Still In Campaign
════════════════════════════════════════════════════════════

1. ProcessPendingEmailsAsync()
   │
   ├─ FOR EACH scheduled email:
   │
   ├─ VALIDATE: Is this account still in this campaign?
   │   │
   │   └─ Query: SELECT * FROM RelayCampaignEmailAccounts
   │            WHERE CampaignId = email.CampaignId
   │              AND EmailAccountId = email.EmailAccountId
   │              AND IsActive = true
   │
   │   IF NOT FOUND:
   │   └─ Email.Status = Cancelled
   │      Email.ErrorMessage = "Account removed from campaign"
   │      RETURN (don't send)
   │
   │   IF FOUND:
   │   └─ Continue with other validations...
   │      ├─ Campaign is active?
   │      ├─ Lead unsubscribed?
   │      ├─ Account daily limit?
   │      ├─ Campaign daily limit?
   │      ├─ Sending window?
   │      ├─ Minimum time between emails?
   │      │
   │      └─ IF ALL PASS: SEND EMAIL
   │
   └─ RESULT: Email sent or cancelled with reason


REMOVE ACCOUNT: Cleans Up Future Emails
════════════════════════════════════════════════════════════

1. RemoveEmailAccountFromCampaignAsync(campaignId=5, accountId=12)
   │
   ├─ DELETE: RelayCampaignEmailAccount
   │  WHERE CampaignId = 5 AND EmailAccountId = 12
   │
   ├─ EFFECT:
   │  ├─ New emails will NOT be scheduled from account 12
   │  ├─ Existing scheduled emails for account 12 REMAIN
   │  ├─ ProcessPendingEmailsAsync() will CANCEL them
   │     (Because account no longer in junction table)
   │
   └─ RESULT: Account cleanly removed, no orphaned emails
```

## Security Guarantees

### ✅ No Account Leakage
- Accounts from other campaigns CANNOT be used
- All accounts must be explicitly added via AddEmailAccountToCampaignAsync()
- Query path: Campaign.CampaignEmailAccounts → RelayEmailAccounts (filtered only)

### ✅ Multi-User Safety
- Each account tracked to owner (RelayEmailAccount.UserId)
- Each campaign-account association tracked (RelayCampaignEmailAccount.UserId)
- Can implement user-level filtering in future

### ✅ Account Removal Safety
- When account removed from campaign, pending emails are cancelled
- Not deleted silently, but explicitly cancelled with error reason
- Audit trail preserved for reporting

### ✅ Deactivation Safety
- Inactive accounts won't be scheduled
- ProcessPendingEmailsAsync() validates account is still active
- Deactivated accounts already sent from won't be used for new emails

## Validation Layers

```
Layer 1: Pre-Selection (Most Important)
─────────────────────────────────────
Only accounts in RelayCampaignEmailAccount for this campaign
are ever considered for email sending

Layer 2: Account Status
─────────────────────────────────────
Account must be active and healthy

Layer 3: Daily Limits
─────────────────────────────────────
Campaign and account-level daily limits respected

Layer 4: Campaign Status
─────────────────────────────────────
Only active campaigns schedule and send emails

Layer 5: Lead Validation
─────────────────────────────────────
Unsubscribed or invalid leads not emailed

Layer 6: Sending Window
─────────────────────────────────────
Respect campaign's time zone and sending hours

Layer 7: Rate Limiting
─────────────────────────────────────
Minimum time between emails per account
```

## Critical Code Paths

### ✅ SAFE: Scheduling Emails
```csharp
var emailAccounts = campaign.CampaignEmailAccounts  // FROM JUNCTION TABLE ONLY
    .Select(cea => cea.EmailAccount)
    .Where(a => a.IsActive && a.SentToday < a.DailySendLimit)
    .ToList();

// emailAccounts contains ONLY accounts explicitly added to campaign
// NO DATABASE QUERY of all accounts
// NO FALLBACK to other campaigns' accounts
```

### ✅ SAFE: Sending Emails
```csharp
// VALIDATE: Is this account still in this campaign?
var isAccountInCampaign = await _context.RelayCampaignEmailAccounts
    .AnyAsync(cea => cea.CampaignId == email.CampaignId 
                && cea.EmailAccountId == email.EmailAccountId 
                && cea.IsActive);

if (!isAccountInCampaign)
{
    email.Status = EmailStatus.Cancelled;
    email.ErrorMessage = "Email account has been removed from this campaign";
    continue;
}
```

### ✅ SAFE: Adding Accounts
```csharp
// THIS IS THE ONLY WAY TO ADD ACCOUNTS TO CAMPAIGNS
var campaignEmailAccount = new RelayCampaignEmailAccount
{
    CampaignId = campaignId,
    EmailAccountId = accountId,
    UserId = emailAccount.UserId,  // Multi-user tracking
    IsActive = true
};
// Creates the pre-selection junction record
```
