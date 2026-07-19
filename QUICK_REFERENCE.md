# Email Account Pre-Selection: Quick Reference Guide

## Problem
❌ **Before**: All email accounts in database were used by all campaigns by default
✅ **After**: Only explicitly-added accounts (via AddEmailAccountToCampaignAsync) are used

---

## How Pre-Selection Works

### Adding Account to Campaign
```
User calls: AddEmailAccountToCampaignAsync(campaignId=5, accountId=12)
            ↓
System creates: RelayCampaignEmailAccount record
                ├─ CampaignId: 5
                ├─ EmailAccountId: 12
                ├─ UserId: "user-alice"
                └─ IsActive: true
            ↓
Result: Account 12 is NOW available for Campaign 5 (and ONLY Campaign 5)
```

### Scheduling Emails
```
Campaign 5 starts:
├─ Get ALL pre-selected accounts: RelayCampaignEmailAccount WHERE CampaignId=5
├─ For Campaign 5: [AccountId: 12, 18, 24] (ONLY these)
├─ Filter by active & daily limit
├─ Use ONLY these accounts (never query all accounts in database)
└─ Other accounts are NOT available to Campaign 5
```

### Sending Emails
```
Before sending each email:
├─ Verify account is STILL in campaign (RelayCampaignEmailAccount check)
├─ Verify account is still active
├─ If NO → Cancel email with reason
└─ If YES → Proceed with other validations
```

---

## Key Methods

### `AddEmailAccountToCampaignAsync(campaignId, accountId)`
**Purpose**: Pre-select an account for a campaign
```csharp
await service.AddEmailAccountToCampaignAsync(campaignId: 5, accountId: 12);
// ✅ Account 12 now available for Campaign 5 only
// ❌ Throws if account not active/healthy or already added
```

### `RemoveEmailAccountFromCampaignAsync(campaignId, accountId)`
**Purpose**: Remove account from campaign
```csharp
await service.RemoveEmailAccountFromCampaignAsync(campaignId: 5, accountId: 12);
// ✅ Account 12 no longer available for Campaign 5
// ✅ Pending emails from Account 12 → Cancelled
// ✅ Already-sent emails → Preserved
```

### `GetCampaignEmailAccountsAsync(campaignId)`
**Purpose**: Get all pre-selected accounts for a campaign
```csharp
var accounts = await service.GetCampaignEmailAccountsAsync(campaignId: 5);
// Returns: [Account 12, Account 18] (active only)
// These are THE ONLY accounts used for Campaign 5
```

### `ScheduleCampaignEmailsAsync(campaignId)`
**Purpose**: Schedule emails using pre-selected accounts
```csharp
await service.ScheduleCampaignEmailsAsync(campaignId: 5);
// ✅ Uses ONLY accounts from GetCampaignEmailAccountsAsync()
// ✅ No fallback to all accounts
// ✅ Respects daily limits per account
```

### `ProcessPendingEmailsAsync()`
**Purpose**: Send pending emails with validation
```csharp
await service.ProcessPendingEmailsAsync();
// ✅ Validates each email's account is still in campaign (NEW)
// ✅ Cancels email if account was removed from campaign (NEW)
// ✅ Respects all other validations (campaign status, daily limits, etc.)
```

---

## Validation Points

```
┌─ Add Account to Campaign
│  ├─ Campaign exists?
│  ├─ Account exists?
│  ├─ Account active?
│  ├─ Account healthy?
│  └─ Not already added?
│
├─ Schedule Emails
│  ├─ Campaign active?
│  ├─ Has sequences?
│  ├─ Has pre-selected accounts?
│  ├─ Account active & under daily limit?
│  └─ Lead not unsubscribed?
│
└─ Send Email (NEW VALIDATIONS)
   ├─ Account still in pre-selected list?  ← NEW
   ├─ Account still active?               ← NEW
   ├─ Campaign still active?
   ├─ Lead not unsubscribed?
   ├─ Daily limits not exceeded?
   ├─ Sending window valid?
   └─ Rate limiting satisfied?
```

---

## Error Messages

### Account Removed From Campaign
```
Email.Status = Cancelled
Email.ErrorMessage = "Email account has been removed from this campaign"

Why: Account was pre-selected but user removed it before email was sent
Action: Check if account removal was intentional, consider re-adding
```

### Account Deactivated
```
Email.Status = Cancelled
Email.ErrorMessage = "Email account has been deactivated"

Why: Account was deactivated after email was scheduled
Action: Re-activate account or use different account
```

### Account Not Found in Campaign
```
AddEmailAccountToCampaignAsync() throws:
"Email account '{emailAddress}' is already added to this campaign"

Why: Trying to add an account that's already pre-selected
Action: Use GetCampaignEmailAccountsAsync() to check existing accounts
```

---

## Database Schema

```sql
-- Email account owned by user
SELECT * FROM RelayEmailAccounts WHERE UserId = 'user-alice'
-- Returns: accounts user-alice owns

-- Accounts pre-selected for campaign
SELECT cea.*, ea.EmailAddress 
FROM RelayCampaignEmailAccounts cea
JOIN RelayEmailAccounts ea ON cea.EmailAccountId = ea.Id
WHERE cea.CampaignId = 5
-- Returns: accounts pre-selected for Campaign 5

-- Accounts added by specific user to campaign
SELECT * FROM RelayCampaignEmailAccounts 
WHERE CampaignId = 5 AND UserId = 'user-alice'
-- Returns: accounts user-alice added to Campaign 5
```

---

## Common Scenarios

### Scenario 1: Multi-User Campaign
```
Campaign: "Q1 Sales"
├─ Alice adds her account 1 → Alice can use Account 1 in this campaign
├─ Bob adds his account 2 → Bob can use Account 2 in this campaign
├─ Charlie adds his account 3 → Charlie can use Account 3 in this campaign
└─ System sends emails round-robin across Accounts 1, 2, 3

✅ Accounts are isolated: Account 1 can ONLY be used if Alice adds it
✅ Alice cannot use Bob's Account 2 without Bob's cooperation
```

### Scenario 2: Account Gets Compromised
```
1. Alice discovers Account 1 was compromised
2. Alice calls: RemoveEmailAccountFromCampaignAsync(campaignId, accountId)
3. System: 
   ├─ Deletes RelayCampaignEmailAccount record
   ├─ Cancels all pending emails from Account 1
   └─ Campaign continues with Accounts 2, 3
4. Result: No more emails sent from compromised account
```

### Scenario 3: Account Reaches Daily Limit
```
1. Account 1 limit: 100 emails/day (already sent 100)
2. ScheduleCampaignEmailsAsync() filters: SentToday < DailySendLimit
3. Account 1: FALSE (100 < 100 is false)
4. Account 1 not included in available accounts
5. Other accounts handle the emails
6. Next day: Account 1 back in rotation (SentToday resets)
```

### Scenario 4: Remove Account Mid-Campaign
```
1. Campaign has Accounts 1, 2, 3 pre-selected
2. 500 emails scheduled (150 from Account 1)
3. User removes Account 1
4. ProcessPendingEmailsAsync():
   ├─ Email from Account 1 → Check RelayCampaignEmailAccounts
   ├─ NOT FOUND (just removed)
   ├─ Status = Cancelled
   └─ Reason: "Email account has been removed from this campaign"
5. Emails from Accounts 2, 3 → Continue normally
6. Result: 150 emails cancelled, 350 continue
```

---

## Testing Checklist

```
✅ Add account to campaign (creates junction record)
✅ Remove account from campaign (cleans up pending emails)
✅ Get campaign accounts (returns pre-selected only)
✅ Schedule emails (uses pre-selected accounts)
✅ Send emails (validates account still in campaign)
✅ Deactivate account (prevents use in new emails)
✅ Multi-user scenario (accounts isolated)
✅ Account removal (pending emails cancelled)
✅ Account at daily limit (skipped in rotation)
✅ Non-existent account (error handling)
```

---

## Troubleshooting

### Problem: "Email not sent, account not found in campaign"
**Check**:
- [ ] Was account explicitly added via AddEmailAccountToCampaignAsync()?
- [ ] Is account still active?
- [ ] Was account removed after email was scheduled?
- [ ] Check RelayCampaignEmailAccounts table for the record

### Problem: "Cannot add account to campaign"
**Check**:
- [ ] Is account active?
- [ ] Is account healthy (not Critical/Disabled)?
- [ ] Is account already added to this campaign?
- [ ] Call GetCampaignEmailAccountsAsync() to see what's already added

### Problem: "Emails cancelled when account removed"
**This is CORRECT behavior**:
- ✅ When you remove an account, pending emails from it are cancelled
- ✅ Already-sent emails are preserved
- ✅ Other accounts' emails continue
- ✅ This is by design for security

---

## Migration

### Before Deploying:
1. [ ] Backup database
2. [ ] Run migration: `Update-Database`
3. [ ] Verify column added: `UserId` on `RelayCampaignEmailAccounts`
4. [ ] Verify index created: `IX_RelayCampaignEmailAccounts_UserId`

### If Migration Fails:
```powershell
# Rollback
Update-Database -TargetMigration "AddRelayEntities"

# Check current state
Get-Migration | where { $_.Name -like "*RelayCampaignEmailAccount*" }

# Verify schema
SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME='RelayCampaignEmailAccounts'
```

---

## Performance Impact

### New Query Added
```csharp
// Check in ProcessPendingEmailsAsync()
var isAccountInCampaign = await _context.RelayCampaignEmailAccounts
    .AnyAsync(cea => cea.CampaignId == email.CampaignId 
                && cea.EmailAccountId == email.EmailAccountId 
                && cea.IsActive);
```

**Performance**:
- ✅ Simple query (2 integer columns + 1 boolean)
- ✅ Indexed by CampaignId, EmailAccountId
- ✅ Returns boolean (fast)
- ✅ Called per pending email (not per lead)
- ✅ Minimal impact (~1-2ms per email)

### Benefits
- ✅ Prevents sending from removed accounts
- ✅ Detects account deactivation immediately
- ✅ Maintains consistency across campaigns
- ✅ No orphaned emails

---

## Support

**Questions?**
- Review: `EMAIL_PRESELECTION_ENFORCEMENT.md`
- Review: `EMAIL_PRESELECTION_ARCHITECTURE.md`  
- Review: `MULTIUSER_ACCOUNT_OWNERSHIP.md`

**Issues?**
- Check error message in RelayEmail.ErrorMessage
- Verify RelayCampaignEmailAccounts has account record
- Verify account is active (RelayEmailAccount.IsActive = true)
- Check campaign status (should be Active)
