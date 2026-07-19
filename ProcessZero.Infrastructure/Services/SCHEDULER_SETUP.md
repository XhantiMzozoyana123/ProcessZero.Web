# Scheduler Service Configuration Guide

## Overview
The `SchedulerService` allows users to schedule messages (SMS, WhatsApp, Facebook, and Email) for future delivery. Messages are stored in the database and processed at their scheduled time by a background job.

## Key Features

✅ **Schedule Messages** - Schedule SMS, WhatsApp, Facebook, and Email messages  
✅ **Reschedule Messages** - Change the delivery time of pending messages  
✅ **Cancel Messages** - Cancel scheduled messages before they are sent  
✅ **Track Status** - Monitor message status (Pending, Sent, Failed, Cancelled)  
✅ **Background Processing** - Automatic message delivery at scheduled time  
✅ **Error Handling** - Failed messages are marked with error details  

## Database Entities

### ScheduledSmsMessage
```csharp
public class ScheduledSmsMessage
{
	public int Id { get; set; }
	public string PhoneNumber { get; set; }
	public string Message { get; set; }
	public DateTime ScheduledAt { get; set; }
	public DateTime? SentAt { get; set; }
	public MessageStatus Status { get; set; }
	public string? ErrorMessage { get; set; }
	public string? TwilioSid { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }
}
```

### ScheduledWhatsAppMessage
```csharp
public class ScheduledWhatsAppMessage
{
	public int Id { get; set; }
	public string PhoneNumber { get; set; }
	public string Message { get; set; }
	public DateTime ScheduledAt { get; set; }
	public DateTime? SentAt { get; set; }
	public MessageStatus Status { get; set; }
	public string? ErrorMessage { get; set; }
	public string? TwilioSid { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }
}
```

### ScheduledFacebookMessage
```csharp
public class ScheduledFacebookMessage
{
	public int Id { get; set; }
	public string RecipientId { get; set; }
	public string Message { get; set; }
	public DateTime ScheduledAt { get; set; }
	public DateTime? SentAt { get; set; }
	public MessageStatus Status { get; set; }
	public string? ErrorMessage { get; set; }
	public string? TwilioSid { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }
}
```

### ScheduledEmailMessage
```csharp
public class ScheduledEmailMessage
{
	public int Id { get; set; }
	public string RecipientEmail { get; set; }
	public string? RecipientName { get; set; }
	public string Subject { get; set; }
	public string Body { get; set; }
	public DateTime ScheduledAt { get; set; }
	public DateTime? SentAt { get; set; }
	public MessageStatus Status { get; set; }
	public string? ErrorMessage { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }
}
```

## Message Status Enum

```csharp
public enum MessageStatus
{
	Pending = 0,      // Message is pending (initial state)
	Sent = 1,         // Message has been successfully sent
	Failed = 2,       // Message failed to send
	Cancelled = 3,    // Message was cancelled by user
	Scheduled = 4     // Message is scheduled for future delivery
}
```

## API Endpoints

### Schedule Endpoints

#### Schedule SMS
```
POST /api/scheduler/schedule-sms
Content-Type: application/json

{
  "phoneNumber": "+27123456789",
  "message": "Hello! This is a scheduled SMS",
  "scheduledAt": "2024-12-25T10:00:00Z"
}

Response:
{
  "message": "SMS scheduled successfully.",
  "id": 1
}
```

#### Schedule WhatsApp
```
POST /api/scheduler/schedule-whatsapp
Content-Type: application/json

{
  "phoneNumber": "+27123456789",
  "message": "Hello! This is a scheduled WhatsApp message",
  "scheduledAt": "2024-12-25T10:00:00Z"
}

Response:
{
  "message": "WhatsApp message scheduled successfully.",
  "id": 1
}
```

#### Schedule Facebook
```
POST /api/scheduler/schedule-facebook
Content-Type: application/json

{
  "recipientId": "1234567890",
  "message": "Hello! This is a scheduled Facebook message",
  "scheduledAt": "2024-12-25T10:00:00Z"
}

Response:
{
  "message": "Facebook message scheduled successfully.",
  "id": 1
}
```

#### Schedule Email
```
POST /api/scheduler/schedule-email
Content-Type: application/json

{
  "recipientEmail": "user@example.com",
  "recipientName": "John Doe",
  "subject": "Scheduled Email Subject",
  "body": "This is the email body content",
  "scheduledAt": "2024-12-25T10:00:00Z"
}

Response:
{
  "message": "Email scheduled successfully.",
  "id": 1
}
```

### Reschedule Endpoints

#### Reschedule SMS
```
PUT /api/scheduler/reschedule-sms/{id}
Content-Type: application/json

{
  "newScheduledAt": "2024-12-26T10:00:00Z"
}

Response:
{
  "message": "SMS rescheduled successfully."
}
```

#### Reschedule WhatsApp
```
PUT /api/scheduler/reschedule-whatsapp/{id}
Content-Type: application/json

{
  "newScheduledAt": "2024-12-26T10:00:00Z"
}

Response:
{
  "message": "WhatsApp message rescheduled successfully."
}
```

#### Reschedule Facebook
```
PUT /api/scheduler/reschedule-facebook/{id}
Content-Type: application/json

{
  "newScheduledAt": "2024-12-26T10:00:00Z"
}

Response:
{
  "message": "Facebook message rescheduled successfully."
}
```

#### Reschedule Email
```
PUT /api/scheduler/reschedule-email/{id}
Content-Type: application/json

{
  "newScheduledAt": "2024-12-26T10:00:00Z"
}

Response:
{
  "message": "Email rescheduled successfully."
}
```

### Cancel Endpoints

#### Cancel Scheduled SMS
```
DELETE /api/scheduler/cancel-sms/{id}

Response:
{
  "message": "SMS cancelled successfully."
}
```

#### Cancel Scheduled WhatsApp
```
DELETE /api/scheduler/cancel-whatsapp/{id}

Response:
{
  "message": "WhatsApp message cancelled successfully."
}
```

#### Cancel Scheduled Facebook
```
DELETE /api/scheduler/cancel-facebook/{id}

Response:
{
  "message": "Facebook message cancelled successfully."
}
```

#### Cancel Scheduled Email
```
DELETE /api/scheduler/cancel-email/{id}

Response:
{
  "message": "Email cancelled successfully."
}
```

### Get Endpoints

#### Get Pending SMS Messages
```
GET /api/scheduler/pending-sms

Response:
{
  "count": 2,
  "messages": [
	{
	  "id": 1,
	  "content": "Hello!",
	  "scheduledAt": "2024-12-25T10:00:00Z",
	  "sentAt": null,
	  "status": 4,
	  "errorMessage": null,
	  "createdAt": "2024-12-20T10:00:00Z",
	  "updatedAt": "2024-12-20T10:00:00Z"
	}
  ]
}
```

#### Get Pending WhatsApp Messages
```
GET /api/scheduler/pending-whatsapp

Response: Similar to pending SMS
```

#### Get Pending Facebook Messages
```
GET /api/scheduler/pending-facebook

Response: Similar to pending SMS
```

#### Get Pending Emails
```
GET /api/scheduler/pending-emails

Response: Similar to pending SMS
```

#### Get Specific Scheduled SMS
```
GET /api/scheduler/sms/{id}

Response:
{
  "id": 1,
  "content": "Hello!",
  "scheduledAt": "2024-12-25T10:00:00Z",
  "sentAt": null,
  "status": 4,
  "errorMessage": null,
  "createdAt": "2024-12-20T10:00:00Z",
  "updatedAt": "2024-12-20T10:00:00Z"
}
```

#### Get Specific Scheduled WhatsApp
```
GET /api/scheduler/whatsapp/{id}

Response: Similar to SMS
```

#### Get Specific Scheduled Facebook
```
GET /api/scheduler/facebook/{id}

Response: Similar to SMS
```

#### Get Specific Scheduled Email
```
GET /api/scheduler/email/{id}

Response: Similar to SMS
```

## Background Job Setup

To process pending scheduled messages, you need to set up a Hangfire background job:

```csharp
// In Program.cs after configuring Hangfire
RecurringJob.AddOrUpdate<ISchedulerService>(
	"process-scheduled-messages",
	schedulerService => schedulerService.ProcessPendingMessagesAsync(),
	Cron.MinuteInterval(1)); // Run every minute
```

Or with a custom interval:

```csharp
// Run every 5 minutes
RecurringJob.AddOrUpdate<ISchedulerService>(
	"process-scheduled-messages",
	schedulerService => schedulerService.ProcessPendingMessagesAsync(),
	Cron.MinuteInterval(5));

// Run every hour
RecurringJob.AddOrUpdate<ISchedulerService>(
	"process-scheduled-messages",
	schedulerService => schedulerService.ProcessPendingMessagesAsync(),
	"0 * * * *"); // Cron expression for hourly
```

## C# Usage Examples

### Schedule an SMS Message
```csharp
public class MyService
{
	private readonly ISchedulerService _schedulerService;

	public MyService(ISchedulerService schedulerService)
	{
		_schedulerService = schedulerService;
	}

	public async Task ScheduleWelcomeSms()
	{
		var dto = new ScheduleSmsDto
		{
			PhoneNumber = "+27123456789",
			Message = "Welcome to ProcessZero!",
			ScheduledAt = DateTime.UtcNow.AddHours(2) // Schedule for 2 hours from now
		};

		int messageId = await _schedulerService.ScheduleSmsAsync(dto);
		Console.WriteLine($"SMS scheduled with ID: {messageId}");
	}
}
```

### Reschedule a Message
```csharp
public async Task RescheduleMessage(int messageId)
{
	var newTime = DateTime.UtcNow.AddDays(1); // Reschedule for tomorrow
	bool success = await _schedulerService.RescheduleSmsAsync(messageId, newTime);

	if (success)
		Console.WriteLine("Message rescheduled successfully");
	else
		Console.WriteLine("Message not found");
}
```

### Cancel a Scheduled Message
```csharp
public async Task CancelMessage(int messageId)
{
	bool success = await _schedulerService.CancelScheduledSmsAsync(messageId);

	if (success)
		Console.WriteLine("Message cancelled successfully");
	else
		Console.WriteLine("Message not found");
}
```

### Get Pending Messages
```csharp
public async Task GetUserPendingMessages(string userId)
{
	var messages = await _schedulerService.GetPendingSmsByUserAsync(userId);

	foreach (var msg in messages)
	{
		Console.WriteLine($"ID: {msg.Id}, Status: {msg.Status}, ScheduledAt: {msg.ScheduledAt}");
	}
}
```

## Database Migrations

After adding the scheduler service, you need to create and apply a database migration:

```bash
# Add a new migration
dotnet ef migrations add AddSchedulerTables --project ProcessZero.Domain

# Apply the migration
dotnet ef database update --project ProcessZero.Domain
```

## Important Notes

1. **UTC Time**: All scheduled times should be in UTC to avoid timezone issues
2. **Future Scheduling**: Scheduled times must be in the future (greater than current UTC time)
3. **Cannot Reschedule Sent Messages**: Only messages with "Scheduled" or "Pending" status can be rescheduled
4. **Cannot Cancel Sent Messages**: Only messages that haven't been sent can be cancelled
5. **Background Processing**: The `ProcessPendingMessagesAsync()` method must be called periodically via a background job scheduler (Hangfire)
6. **Error Tracking**: Failed messages are marked with error details for debugging
7. **User Isolation**: Each message is associated with a UserId for proper authorization and tracking

## Troubleshooting

### Messages Not Being Sent
- Check if the background job is running in Hangfire dashboard
- Verify Twilio/Email credentials are configured correctly
- Check application logs for error details

### "Scheduled time must be in the future" Error
- Ensure the ScheduledAt time is set to a future time
- Check that client and server timezones are properly synchronized (use UTC)

### "Cannot reschedule a message that has already been sent" Error
- Only messages with Scheduled or Pending status can be rescheduled
- Check the message status before attempting to reschedule
