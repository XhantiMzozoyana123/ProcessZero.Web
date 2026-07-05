# Twilio Service Configuration Guide

## Overview
The `TwilioService` implementation provides support for sending:
- **SMS Messages** - Traditional text messages
- **WhatsApp Messages** - Messages via WhatsApp
- **Facebook Messages** - Messages via Facebook Messenger

## Configuration

Add the following to your `appsettings.json` file:

```json
{
  "Twilio": {
	"AccountSid": "your_account_sid_here",
	"AuthToken": "your_auth_token_here",
	"PhoneNumber": "+1234567890",
	"WhatsAppNumber": "whatsapp:+1234567890",
	"FacebookMessengerId": "messenger:your_facebook_messenger_id"
  }
}
```

### Configuration Parameters

| Parameter | Description | Example |
|-----------|-------------|---------|
| **AccountSid** | Your Twilio Account SID from [Twilio Console](https://www.twilio.com/console) | `ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx` |
| **AuthToken** | Your Twilio Auth Token from [Twilio Console](https://www.twilio.com/console) | `your_auth_token` |
| **PhoneNumber** | Your Twilio SMS phone number in E.164 format | `+1234567890` |
| **WhatsAppNumber** | Your Twilio WhatsApp number (must include `whatsapp:` prefix) | `whatsapp:+1234567890` |
| **FacebookMessengerId** | Your Twilio Facebook Messenger ID (must include `messenger:` prefix) | `messenger:1234567890` |

## Usage Examples

### Sending SMS
```csharp
public class MyController : ControllerBase
{
	private readonly ITwilioService _twilioService;

	public MyController(ITwilioService twilioService)
	{
		_twilioService = twilioService;
	}

	public async Task SendSms()
	{
		var smsDto = new TwilioSmsDto
		{
			PhoneNumber = "+27123456789",
			Message = "Hello! This is a test SMS from your application."
		};

		var result = await _twilioService.SendSmsAsync(smsDto);
		// result will be true if successful
	}
}
```

### Sending WhatsApp Message
```csharp
public async Task SendWhatsApp()
{
	var whatsAppDto = new TwilioWhatsAppDto
	{
		PhoneNumber = "+27123456789", // Phone number can be with or without whatsapp: prefix
		Message = "Hello! This is a test WhatsApp message from your application."
	};

	var result = await _twilioService.SendWhatsAppAsync(whatsAppDto);
	// result will be true if successful
}
```

### Sending Facebook Message
```csharp
public async Task SendFacebookMessage()
{
	var facebookDto = new TwilioFacebookDto
	{
		RecipientId = "1234567890", // Facebook recipient ID
		Message = "Hello! This is a test Facebook message from your application."
	};

	var result = await _twilioService.SendFacebookMessageAsync(facebookDto);
	// result will be true if successful
}
```

## Setting Up Twilio

1. **Create a Twilio Account**
   - Go to [Twilio.com](https://www.twilio.com)
   - Sign up for a free trial account

2. **Get Your Credentials**
   - Visit the [Twilio Console](https://www.twilio.com/console)
   - Copy your `Account SID` and `Auth Token`
   - These will be used in the configuration

3. **Set Up Phone Numbers**
   - For SMS: Get a Twilio phone number from the console
   - For WhatsApp: Enable WhatsApp on your Twilio account and get a WhatsApp number
   - For Facebook: Connect your Facebook Business Account to Twilio

4. **Update appsettings.json**
   - Add the Twilio configuration section with your credentials and phone numbers

## Error Handling

The service throws exceptions if:
- Required parameters are null or empty
- Twilio credentials are not configured
- The Twilio API call fails

Always wrap calls in try-catch blocks:

```csharp
try
{
	var result = await _twilioService.SendSmsAsync(smsDto);
}
catch (ArgumentNullException ex)
{
	// Handle null parameter
}
catch (ArgumentException ex)
{
	// Handle empty/invalid parameter
}
catch (InvalidOperationException ex)
{
	// Handle missing configuration
}
catch (Exception ex)
{
	// Handle other Twilio API errors
}
```

## Logging

The service uses `ILogger<TwilioService>` to log:
- Successful message sends (includes Twilio Message SID)
- Error messages with details

Check your application logs to monitor message delivery.

## DTOs

### TwilioSmsDto
```csharp
public class TwilioSmsDto
{
	public string PhoneNumber { get; set; } // Required: Phone number in E.164 format
	public string Message { get; set; }      // Required: SMS message content
}
```

### TwilioWhatsAppDto
```csharp
public class TwilioWhatsAppDto
{
	public string PhoneNumber { get; set; } // Required: Phone number (with or without whatsapp: prefix)
	public string Message { get; set; }      // Required: WhatsApp message content
}
```

### TwilioFacebookDto
```csharp
public class TwilioFacebookDto
{
	public string RecipientId { get; set; } // Required: Facebook recipient ID
	public string Message { get; set; }      // Required: Facebook message content
}
```

## Additional Resources

- [Twilio Documentation](https://www.twilio.com/docs)
- [Twilio SMS API](https://www.twilio.com/docs/sms)
- [Twilio WhatsApp API](https://www.twilio.com/docs/whatsapp)
- [Twilio Facebook Messenger API](https://www.twilio.com/docs/messaging/channels/facebook)
