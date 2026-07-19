# Scheduler System Implementation Summary

## What Was Created

A complete **Scheduler Service** for scheduling and managing outbound messages (SMS, WhatsApp, Facebook, and Email) for future delivery.

## Files Created

### 1. Domain Entities
- **`ProcessZero.Domain\Entities\ScheduledMessages.cs`**
  - `ScheduledSmsMessage` - SMS scheduling entity
  - `ScheduledWhatsAppMessage` - WhatsApp scheduling entity
  - `ScheduledFacebookMessage` - Facebook scheduling entity
  - `ScheduledEmailMessage` - Email scheduling entity
  - `MessageStatus` enum - Status tracking (Pending, Sent, Failed, Cancelled, Scheduled)

### 2. DTOs (Data Transfer Objects)
- **`ProcessZero.Application\Dtos\ScheduledMessageDtos.cs`**
  - `ScheduleSmsDto` - For scheduling SMS
  - `ScheduleWhatsAppDto` - For scheduling WhatsApp
  - `ScheduleFacebookDto` - For scheduling Facebook
  - `ScheduleEmailDto` - For scheduling Email
  - `ScheduledMessageDetailsDto` - For retrieving scheduled message details

### 3. Application Layer
- **`ProcessZero.Application\Interfaces\ISchedulerService.cs`**
  - Interface defining all scheduler operations
  - Methods for scheduling, rescheduling, cancelling, retrieving messages
  - Background processing method for automated message delivery

### 4. Infrastructure Layer
- **`ProcessZero.Infrastructure\Services\SchedulerService.cs`**
  - Complete implementation of ISchedulerService
  - Database operations for storing scheduled messages
  - Background job processing for scheduled message delivery
  - Error handling and logging

### 5. API Controller
- **`ProcessZero.Web\Controllers\SchedulerController.cs`**
  - RESTful API endpoints for all scheduler operations
  - Admin authorization required
  - Comprehensive input validation
  - Error handling with appropriate HTTP status codes

### 6. Documentation
- **`ProcessZero.Infrastructure\Services\SCHEDULER_SETUP.md`**
  - Complete setup and usage guide
  - API endpoint documentation
  - Database entity descriptions
  - C# usage examples
  - Migration instructions

## Configuration Changes

### Updated Files

1. **`ProcessZero.Domain\ApplicationDbContext.cs`**
   - Added DbSet properties for all scheduled message entities

2. **`ProcessZero.Web\Program.cs`**
   - Registered `ISchedulerService` in dependency injection

## API Endpoints

### Schedule Endpoints
- `POST /api/scheduler/schedule-sms` - Schedule SMS message
- `POST /api/scheduler/schedule-whatsapp` - Schedule WhatsApp message
- `POST /api/scheduler/schedule-facebook` - Schedule Facebook message
- `POST /api/scheduler/schedule-email` - Schedule email message

### Reschedule Endpoints
- `PUT /api/scheduler/reschedule-sms/{id}` - Reschedule SMS
- `PUT /api/scheduler/reschedule-whatsapp/{id}` - Reschedule WhatsApp
- `PUT /api/scheduler/reschedule-facebook/{id}` - Reschedule Facebook
- `PUT /api/scheduler/reschedule-email/{id}` - Reschedule email

### Cancel Endpoints
- `DELETE /api/scheduler/cancel-sms/{id}` - Cancel SMS
- `DELETE /api/scheduler/cancel-whatsapp/{id}` - Cancel WhatsApp
- `DELETE /api/scheduler/cancel-facebook/{id}` - Cancel Facebook
- `DELETE /api/scheduler/cancel-email/{id}` - Cancel email

### Get Endpoints
- `GET /api/scheduler/pending-sms` - Get pending SMS for user
- `GET /api/scheduler/pending-whatsapp` - Get pending WhatsApp for user
- `GET /api/scheduler/pending-facebook` - Get pending Facebook for user
- `GET /api/scheduler/pending-emails` - Get pending emails for user
- `GET /api/scheduler/sms/{id}` - Get specific SMS details
- `GET /api/scheduler/whatsapp/{id}` - Get specific WhatsApp details
- `GET /api/scheduler/facebook/{id}` - Get specific Facebook details
- `GET /api/scheduler/email/{id}` - Get specific email details

## Key Features

✅ **Schedule Messages** - Schedule SMS, WhatsApp, Facebook, and Email  
✅ **Reschedule** - Change delivery time of pending messages  
✅ **Cancel** - Cancel scheduled messages before sending  
✅ **Track Status** - Monitor message status (Pending, Sent, Failed, Cancelled, Scheduled)  
✅ **Error Handling** - Failed messages logged with error details  
✅ **Background Processing** - Automatic delivery at scheduled time  
✅ **User Isolation** - Messages tracked by UserId  
✅ **Comprehensive Logging** - All operations logged for debugging  
✅ **Admin Only** - Authorization policy enforced on all endpoints  

## Next Steps

### 1. Create Database Migration
```bash
dotnet ef migrations add AddSchedulerTables --project ProcessZero.Domain
dotnet ef database update --project ProcessZero.Domain
```

### 2. Configure Background Job (in Program.cs)
```csharp
// Add after Hangfire configuration
RecurringJob.AddOrUpdate<ISchedulerService>(
	"process-scheduled-messages",
	schedulerService => schedulerService.ProcessPendingMessagesAsync(),
	Cron.MinuteInterval(1)); // Run every minute
```

### 3. Ensure Twilio Configuration
Make sure your `appsettings.json` has Twilio credentials configured:
```json
{
  "Twilio": {
	"AccountSid": "your_account_sid",
	"AuthToken": "your_auth_token",
	"PhoneNumber": "+1234567890",
	"WhatsAppNumber": "whatsapp:+1234567890",
	"FacebookMessengerId": "messenger:1234567890"
  }
}
```

## Usage Example

### Schedule an SMS for Later
```csharp
var dto = new ScheduleSmsDto
{
	PhoneNumber = "+27123456789",
	Message = "Hello! This is a scheduled message",
	ScheduledAt = DateTime.UtcNow.AddHours(2)
};

int messageId = await _schedulerService.ScheduleSmsAsync(dto);
```

### Reschedule a Message
```csharp
await _schedulerService.RescheduleSmsAsync(messageId, DateTime.UtcNow.AddDays(1));
```

### Cancel a Scheduled Message
```csharp
await _schedulerService.CancelScheduledSmsAsync(messageId);
```

### Get Pending Messages
```csharp
var pendingMessages = await _schedulerService.GetPendingSmsByUserAsync(userId);
```

## Build Status

✅ **Build Successful** - All code compiles without errors
✅ **Dependencies Resolved** - All required services are injected
✅ **Integration Complete** - Scheduler service integrated with Twilio and Email services

## Architecture

The Scheduler system follows the same layered architecture as the rest of the application:

```
API Layer (SchedulerController)
	↓
Application Layer (ISchedulerService interface)
	↓
Infrastructure Layer (SchedulerService implementation)
	↓
Domain Layer (Scheduled Message Entities)
	↓
Database (MySQL via Entity Framework Core)
```

The service integrates with:
- **TwilioService** - For sending SMS, WhatsApp, and Facebook messages
- **EmailService** - For sending scheduled emails
- **ApplicationDbContext** - For database persistence
- **Hangfire** - For background job processing (recommended)

---

**Status**: ✅ Ready to use after database migration and background job configuration
