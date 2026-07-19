# Quick Reference - Messaging System Cheat Sheet

## 🚀 Getting Started (5 Minutes)

### 1. Database Migration
```bash
dotnet ef migrations add AddSchedulerTables --project ProcessZero.Domain
dotnet ef database update
```

### 2. Configure Twilio
Add to `appsettings.json`:
```json
{
  "Twilio": {
	"AccountSid": "AC_YOUR_SID",
	"AuthToken": "YOUR_TOKEN",
	"PhoneNumber": "+1234567890",
	"WhatsAppNumber": "whatsapp:+1234567890",
	"FacebookMessengerId": "messenger:123456"
  }
}
```

### 3. Start & Monitor
```bash
dotnet run
# Open http://localhost:5000/hangfire
```

## 📱 API Examples (cURL)

### Send SMS Now
```bash
POST /api/blaster/send-bulk-sms
[{"phoneNumber": "+27123456789", "message": "Hello!"}]
```

### Schedule SMS for Later
```bash
POST /api/scheduler/schedule-sms
{
  "phoneNumber": "+27123456789",
  "message": "Hello in 2 hours!",
  "scheduledAt": "2024-12-20T16:00:00Z"
}
```

### Get Pending Messages
```bash
GET /api/scheduler/pending-sms
```

### Reschedule
```bash
PUT /api/scheduler/reschedule-sms/1
{"newScheduledAt": "2024-12-21T10:00:00Z"}
```

### Cancel
```bash
DELETE /api/scheduler/cancel-sms/1
```

## 📊 All Endpoints

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/blaster/send-bulk-emails` | POST | Send bulk email now |
| `/api/blaster/send-bulk-sms` | POST | Send bulk SMS now |
| `/api/blaster/send-bulk-whatsapp` | POST | Send bulk WhatsApp now |
| `/api/blaster/send-bulk-facebook` | POST | Send bulk Facebook now |
| `/api/scheduler/schedule-sms` | POST | Schedule SMS |
| `/api/scheduler/schedule-whatsapp` | POST | Schedule WhatsApp |
| `/api/scheduler/schedule-facebook` | POST | Schedule Facebook |
| `/api/scheduler/schedule-email` | POST | Schedule email |
| `/api/scheduler/reschedule-sms/{id}` | PUT | Reschedule SMS |
| `/api/scheduler/cancel-sms/{id}` | DELETE | Cancel SMS |
| `/api/scheduler/pending-sms` | GET | Get pending SMS |
| `/api/scheduler/sms/{id}` | GET | Get SMS details |

Plus WhatsApp, Facebook, Email variants.

## 💾 C# Usage

```csharp
// Inject services
public MyController(IBlasterService blaster, ISchedulerService scheduler)

// Send bulk SMS now
await _blaster.SendBulkSmsAsync(new List<TwilioSmsDto> {...});

// Schedule SMS for later
int id = await _scheduler.ScheduleSmsAsync(new ScheduleSmsDto 
{
	PhoneNumber = "+27123456789",
	Message = "Hello",
	ScheduledAt = DateTime.UtcNow.AddHours(2)
});

// Reschedule
await _scheduler.RescheduleSmsAsync(id, DateTime.UtcNow.AddDays(1));

// Cancel
await _scheduler.CancelScheduledSmsAsync(id);

// Get pending
var pending = await _scheduler.GetPendingSmsByUserAsync(userId);
```

## 🔍 Monitor

- **Hangfire Dashboard**: http://localhost:5000/hangfire
- **Check Jobs**: Recurring Jobs → process-scheduled-messages
- **View History**: Succeeded/Failed tabs
- **Logs**: Application logs show job execution

## 🐛 Troubleshooting

| Issue | Solution |
|-------|----------|
| Messages not sending | Check Hangfire dashboard, verify Twilio credentials |
| Job not running | Restart app, check database connection |
| API returns 401 | Add Bearer token, verify Admin role |
| Database error | Run migration: `dotnet ef database update` |

## 📋 DTOs

**Blaster (Send Now):**
- `EmailDto` - email now
- `TwilioSmsDto` - SMS now
- `TwilioWhatsAppDto` - WhatsApp now
- `TwilioFacebookDto` - Facebook now

**Scheduler (Send Later):**
- `ScheduleEmailDto` - schedule email
- `ScheduleSmsDto` - schedule SMS
- `ScheduleWhatsAppDto` - schedule WhatsApp
- `ScheduleFacebookDto` - schedule Facebook

## ✅ Checklist

- [ ] Migration applied
- [ ] Twilio configured
- [ ] App running
- [ ] Hangfire dashboard accessible
- [ ] Test message scheduled
- [ ] Message sent successfully

## 🎯 Common Tasks

```csharp
// Schedule SMS for 2 hours from now
await _scheduler.ScheduleSmsAsync(new ScheduleSmsDto 
{
	PhoneNumber = "+27123456789",
	Message = "Reminder",
	ScheduledAt = DateTime.UtcNow.AddHours(2)
});

// Schedule email for tomorrow
await _scheduler.ScheduleEmailAsync(new ScheduleEmailDto
{
	RecipientEmail = "user@example.com",
	Subject = "Daily Report",
	Body = "Here's your report",
	ScheduledAt = DateTime.UtcNow.AddDays(1)
});

// Send SMS to multiple now
await _blaster.SendBulkSmsAsync(new[] {
	new TwilioSmsDto { PhoneNumber = "+27123456789", Message = "Hi 1" },
	new TwilioSmsDto { PhoneNumber = "+27987654321", Message = "Hi 2" }
});
```

## 📚 Full Docs

- `COMPLETE_MESSAGING_SYSTEM_SUMMARY.md` - Overview
- `SCHEDULER_HANGFIRE_COMPLETE_GUIDE.md` - Detailed guide
- `HANGFIRE_INTEGRATION.md` - Hangfire details
- `SCHEDULER_SETUP.md` - Scheduler details
- `TWILIO_SETUP.md` - Twilio details

---

**Status**: ✅ Ready to Use | Build: ✅ Successful
