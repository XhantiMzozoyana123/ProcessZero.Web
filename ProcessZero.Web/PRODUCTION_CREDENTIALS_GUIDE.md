# Production Credentials Management Guide

## How Your Code Works
Your `TwilioService.cs` reads from `IConfiguration`, which means it automatically supports multiple configuration sources:
1. **appsettings.json** (local development)
2. **User Secrets** (local development)
3. **Environment Variables** (anywhere)
4. **Azure Key Vault** (Azure production)

---

## Option 1: Environment Variables (Simplest & Universal)

### Windows Server:
```powershell
# Run as Administrator
setx Twilio__AccountSid "ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
setx Twilio__AuthToken "your_auth_token_here"
setx Twilio__PhoneNumber "+1234567890"

# Then restart your application for changes to take effect
```

### Linux/Docker:
```bash
export Twilio__AccountSid="ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
export Twilio__AuthToken="your_auth_token_here"
export Twilio__PhoneNumber="+1234567890"
```

### Docker Compose:
```yaml
services:
  processzero-api:
	environment:
	  - Twilio__AccountSid=ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
	  - Twilio__AuthToken=your_auth_token_here
	  - Twilio__PhoneNumber=+1234567890
```

**Advantages:** Works everywhere, no extra services needed
**Disadvantages:** Hard to manage many secrets, visible in process list

---

## Option 2: Azure Key Vault (Recommended for Azure)

### Prerequisites:
1. Azure subscription
2. Created Key Vault in Azure Portal

### Step 1: Add NuGet Package
```powershell
dotnet add package Azure.Identity
dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets
```

### Step 2: Update Program.cs
Already done! The code now checks for Azure Key Vault in production.

### Step 3: Create Azure Key Vault
In Azure Portal:
1. Create a new Key Vault
2. Add secrets with these names (replace `:` with `-` in portal):
   - `Twilio-AccountSid`
   - `Twilio-AuthToken`
   - `Twilio-PhoneNumber`

### Step 4: Update appsettings.Production.json
Edit `ProcessZero.Web/appsettings.Production.json`:
```json
{
  "Logging": {
	"LogLevel": {
	  "Default": "Information",
	  "Microsoft.AspNetCore": "Warning"
	}
  },
  "KeyVault": {
	"Url": "https://your-keyvault-name.vault.azure.net/"
  }
}
```

### Step 5: Assign Managed Identity (if using App Service)
1. Go to your App Service → Identity → Turn on System Managed Identity
2. In Key Vault → Access Policies → Add your App Service's managed identity
3. Grant: Get, List permissions for Secrets

**Advantages:** Secure, centralized, audit trail, integrates with Azure RBAC
**Disadvantages:** Azure-only, requires setup

---

## Option 3: AWS Secrets Manager (If using AWS)

### Install NuGet:
```powershell
dotnet add package Amazon.Extensions.Configuration.SystemsManager
```

### Update Program.cs:
```csharp
if (builder.Environment.IsProduction())
{
	builder.Configuration.AddSystemsManager(
		path: "/processzero/",
		credentials: new Amazon.Runtime.BasicAWSCredentials(accessKey, secretKey));
}
```

---

## Configuration Priority (Highest to Lowest)
.NET Configuration automatically looks for values in this order:
1. **Command-line arguments** (if using)
2. **Environment variables** (best for production)
3. **Azure Key Vault** (if configured)
4. **appsettings.{Environment}.json** (like appsettings.Production.json)
5. **appsettings.json** (default, has placeholders now)
6. **User Secrets** (development only)

---

## Quick Production Checklist

- [ ] Remove all secrets from `appsettings.json` ✅ (Already done!)
- [ ] Set environment variables OR use Azure Key Vault
- [ ] Test locally with User Secrets: `dotnet user-secrets`
- [ ] Verify `appsettings.Production.json` is configured
- [ ] For Azure: Enable Managed Identity on App Service
- [ ] Deploy to production
- [ ] Test that credentials are loaded correctly

---

## Test Your Configuration

Create a simple test endpoint to verify credentials load:

```csharp
[ApiController]
[Route("api/[controller]")]
public class ConfigTestController : ControllerBase
{
	private readonly IConfiguration _config;

	public ConfigTestController(IConfiguration config) => _config = config;

	[HttpGet("check-config")]
	[Authorize(Roles = "Admin")] // Only admins can check
	public IActionResult CheckConfig()
	{
		var twilioSid = _config["Twilio:AccountSid"];
		var hasToken = !string.IsNullOrEmpty(_config["Twilio:AuthToken"]);

		return Ok(new
		{
			twilioConfigured = !string.IsNullOrEmpty(twilioSid),
			twilioSidPreview = string.IsNullOrEmpty(twilioSid) ? null : twilioSid[..4] + "***",
			twilioTokenPresent = hasToken
		});
	}
}
```

---

## Recommended Setup for Your Project

**Development:**
- Use `appsettings.json` (with placeholders) ✅
- Use User Secrets for actual values: `dotnet user-secrets set "Twilio:AccountSid" "..."`

**Staging:**
- Use environment variables on the server
- Or Azure Key Vault if on Azure

**Production:**
- **Azure:** Use Azure Key Vault with Managed Identity
- **AWS:** Use AWS Secrets Manager
- **On-Premises/Other:** Use environment variables

This approach keeps credentials secure and out of your repository! 🔒
