# Hangfire Integration with Scheduler Service

## Overview

Hangfire is integrated with the Scheduler Service to automatically process pending scheduled messages at their scheduled times. The system runs a recurring background job that checks for pending messages and sends them.

## How It Works

1. **Message Scheduling**: Users schedule messages via the Scheduler API
2. **Database Storage**: Messages are stored in the database with a scheduled delivery time
3. **Hangfire Job**: A recurring Hangfire job runs every minute to check for pending messages
4. **Message Processing**: Messages that are due are automatically sent
5. **Status Tracking**: Message status is updated (Sent, Failed, etc.)

## Configuration

### Automatic Setup

The Hangfire integration is automatically configured in `Program.cs`:

```csharp
// Hangfire is already configured with MySQL storage
builder.Services.AddHangfire(config => {...});
builder.Services.AddHangfireServer(options => {...});

// Recurring job for processing scheduled messages
RecurringJob.AddOrUpdate<ScheduledMessagesBackgroundJob>(
	"process-scheduled-messages",
	job => job.ProcessScheduledMessagesAsync(),
	Cron.MinuteInterval(1)); // Run every minute
```

### Hangfire Dashboard

Access the Hangfire dashboard at: **`http://localhost:5000/hangfire`**

The dashboard shows:
- ✅ Recurring jobs status
- ✅ Job history
- ✅ Failed jobs
- ✅ Success/failure rates
- ✅ Real-time job execution

## Job Configuration Options

### Change Job Frequency

To modify how often the job runs, edit the `Program.cs` file:

```csharp
// Run every minute (default)
Cron.MinuteInterval(1)

// Run every 5 minutes
Cron.MinuteInterval(5)

// Run every hour
Cron.Hourly()

// Run every hour at minute 0
"0 * * * *"

// Run every 30 minutes
Cron.MinuteInterval(30)

// Run every 2 hours
Cron.HourInterval(2)

// Run every day at 2:00 AM UTC
"0 2 * * *"

// Run every weekday at 9:00 AM UTC
"0 9 * * 1-5"
```

## Background Job Class

### ScheduledMessagesBackgroundJob

Located in: `ProcessZero.Infrastructure\BackgroundJobs\ScheduledMessagesBackgroundJob.cs`

```csharp
public class ScheduledMessagesBackgroundJob
{
	private readonly ISchedulerService _schedulerService;
	private readonly ILogger<ScheduledMessagesBackgroundJob> _logger;

	public async Task ProcessScheduledMessagesAsync()
	{
		// Processes all pending scheduled messages
		await _schedulerService.ProcessPendingMessagesAsync();
	}
}
```

**Features:**
- ✅ Error handling with logging
- ✅ Hangfire retry logic
- ✅ Execution time tracking
- ✅ Detailed logging for debugging

## Message Processing Flow

```
Hangfire Job Triggered
	↓
ScheduledMessagesBackgroundJob.ProcessScheduledMessagesAsync()
	↓
ISchedulerService.ProcessPendingMessagesAsync()
	↓
Query Database for Messages where ScheduledAt <= Now
	↓
For Each Message:
	- Extract message details
	- Send via Twilio/Email Service
	- Update Status to "Sent"
	- Record SentAt timestamp
	↓
If Error:
	- Update Status to "Failed"
	- Store error message
	- Retry according to Hangfire policy
	↓
Save Changes to Database
```

## Monitoring and Debugging

### View Job Execution

1. Open Hangfire Dashboard: `http://localhost:5000/hangfire`
2. Click on **Recurring Jobs**
3. Find **process-scheduled-messages**
4. Click to view execution history

### View Job Logs

Logs are written to the application's logging system. Check:

```
- Application logs (console or log file)
- Windows Event Viewer (if using event logging)
- Log aggregation service (if configured)
```

### Sample Log Output

```
[2024-12-20 10:00:00] Information: Starting scheduled messages processing job at 2024-12-20 10:00:00
[2024-12-20 10:00:05] Information: SMS sent successfully. SID: SM1234567890
[2024-12-20 10:00:10] Information: Email sent successfully
[2024-12-20 10:00:15] Information: Completed scheduled messages processing job at 2024-12-20 10:00:15
```

## Common Issues and Solutions

### Issue: Job Not Running

**Symptoms:**
- Scheduled messages not being sent
- Job doesn't appear in Hangfire dashboard

**Solutions:**

1. **Verify Hangfire Server is running**
   - Check `app.UseHangfireServer()` is called in Program.cs
   - Verify application is running (Hangfire server runs in-process)

2. **Check database connectivity**
   - Ensure MySQL database is running
   - Verify connection string in appsettings.json

3. **Restart the application**
   - Hangfire jobs are registered when the app starts
   - Stop and restart the application if the job is missing

4. **View Hangfire dashboard**
   - Go to http://localhost:5000/hangfire
   - Check if job appears under "Recurring Jobs"
   - Check "Succeeded" tab for completed executions

### Issue: Job Failing Repeatedly

**Symptoms:**
- Job appears in "Failed" tab
- Error messages in logs

**Possible Causes:**

1. **Twilio credentials not configured**
   ```json
   // Add to appsettings.json
   "Twilio": {
	 "AccountSid": "ACxxxxxx...",
	 "AuthToken": "auth_token...",
	 "PhoneNumber": "+1234567890",
	 "WhatsAppNumber": "whatsapp:+1234567890",
	 "FacebookMessengerId": "messenger:123456"
   }
   ```

2. **Email service not configured**
   ```json
   "EmailSettings": {
	 "SmtpHost": "smtp.example.com",
	 "SmtpPort": 587
   }
   ```

3. **Database schema not migrated**
   ```bash
   dotnet ef migrations add AddSchedulerTables --project ProcessZero.Domain
   dotnet ef database update
   ```

### Issue: Job Running But Not Sending Messages

**Symptoms:**
- Job executes successfully
- No messages are sent
- No errors in logs

**Possible Causes:**

1. **No scheduled messages in database**
   - Verify messages are being scheduled via the API
   - Check `ScheduledSmsMessages` table in database

2. **Scheduled time is in the future**
   - Job only processes messages where `ScheduledAt <= DateTime.UtcNow`
   - Verify scheduled times are not too far in the future

3. **Message status is not Pending/Scheduled**
   - Check message status in database
   - Only "Pending" and "Scheduled" statuses are processed

## Performance Optimization

### For High-Volume Message Delivery

If you're sending thousands of messages, consider:

1. **Increase job frequency** (be careful with database load)
   ```csharp
   Cron.MinuteInterval(30) // Run every 30 seconds instead
   ```

2. **Implement batching**
   - Process messages in batches instead of one-by-one
   - Reduces database round-trips

3. **Use async/await properly**
   - Ensure all I/O operations are truly asynchronous
   - Avoid blocking calls

4. **Add database indexes**
   ```sql
   CREATE INDEX IX_ScheduledMessages_Status_ScheduledAt 
   ON ScheduledSmsMessages(Status, ScheduledAt);
   ```

## Deployment Considerations

### Single-Server Deployment

```
┌─────────────────────────┐
│  ASP.NET Core App       │
├─────────────────────────┤
│  Hangfire Server        │
│  (in-process)           │
├─────────────────────────┤
│  Processes messages     │
└─────────────────────────┘
```

**Best for:** Small to medium deployments

### Multi-Server Deployment (Recommended)

```
┌─────────────────────┐
│  Web Server 1       │
│  (no Hangfire)      │
└─────────────────────┘

┌─────────────────────┐
│  Web Server 2       │
│  (no Hangfire)      │
└─────────────────────┘

┌─────────────────────┐
│  Background Server  │
│  (Hangfire only)    │
└─────────────────────┘
	 ↓
┌──────────────────────┐
│  Shared MySQL DB     │
│  (Hangfire storage)  │
└──────────────────────┘
```

**Configuration for multiple servers:**

```csharp
// Disable Hangfire server on web servers
builder.Services.AddHangfire(config => {...});
// Don't call builder.Services.AddHangfireServer() on web servers

// Enable Hangfire server only on background server
if (IsBackgroundServer) // Read from configuration
{
	builder.Services.AddHangfireServer(options => {...});
}
```

## Advanced Monitoring

### Set Up Application Insights Integration

```csharp
builder.Services.AddApplicationInsights();

// Track Hangfire jobs in Application Insights
GlobalJobFilters.Filters.Add(new ApplicationInsightsJobFilter());
```

### Custom Job Failure Notifications

```csharp
GlobalJobFilters.Filters.Add(new JobFailureFilter());

public class JobFailureFilter : IApplyStateFilter
{
	public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
	{
		if (context.NewState is FailedState failedState)
		{
			// Send notification about job failure
			NotifyAdmins($"Job failed: {context.BackgroundJob.Id}");
		}
	}

	public void OnStateUnapplied(UnapplyStateContext context, IWriteOnlyTransaction transaction) { }
}
```

## Testing

### Unit Test Example

```csharp
[Fact]
public async Task ProcessScheduledMessagesAsync_ShouldSendDueMessages()
{
	// Arrange
	var mockSchedulerService = new Mock<ISchedulerService>();
	var mockLogger = new Mock<ILogger<ScheduledMessagesBackgroundJob>>();
	var backgroundJob = new ScheduledMessagesBackgroundJob(mockSchedulerService.Object, mockLogger.Object);

	// Act
	await backgroundJob.ProcessScheduledMessagesAsync();

	// Assert
	mockSchedulerService.Verify(s => s.ProcessPendingMessagesAsync(), Times.Once);
}
```

## References

- [Hangfire Documentation](https://docs.hangfire.io/)
- [Cron Expressions](https://crontab.guru/)
- [Hangfire Best Practices](https://docs.hangfire.io/en/latest/best-practices/)
- [MySQL Storage for Hangfire](https://www.nuget.org/packages/Hangfire.MySqlStorage/)

---

**Status**: ✅ Fully Integrated and Ready to Use
