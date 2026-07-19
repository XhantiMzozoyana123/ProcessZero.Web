# ProcessZero - Environment Variables Security Setup

## ✅ What Was Done

This setup implements the **recommended security practice** for ASP.NET Core APIs:

1. **Removed all secrets from `appsettings.json`** - Secrets are replaced with empty strings
2. **Created `.env.example`** - Template showing all required environment variables
3. **Updated documentation** - `ENVIRONMENT_VARIABLES_SETUP.md` and `DOCKER.md`
4. **Verified .gitignore** - `.processzero.env` is already excluded from Git

## 🔐 Security Principles Applied

### Before (Insecure)
```json
{
  "Twilio": {
    "AccountSid": "ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
    "AuthToken": "your-secret-token"
  }
}
```

**❌ Problems:**
- Secrets committed to version control
- Visible to anyone with repo access
- Cannot deploy to production without exposing secrets

### After (Secure)
```json
{
  "Twilio": {
    "AccountSid": "",
    "AuthToken": ""
  }
}
```

**✅ Benefits:**
- No secrets in source code
- Safe to commit to Git
- Environment-specific configuration
- Secrets managed separately per environment

## 📋 Complete Deployment Workflow

### Development (Local Machine)

```powershell
# 1. Edit appsettings.json for local development (empty values OK)
# 2. Set environment variables for testing (optional)
$env:Twilio__AccountSid = "ACxxxxx"
$env:Twilio__AuthToken = "your-token"

# 3. Run locally
dotnet run

# Or with Docker
docker compose up --build
```

### Production (Linux VPS)

```bash
# 1. SSH to VPS
ssh user@your-vps-ip

# 2. Navigate to project
cd /home/xhanti/ProcessZero.Web

# 3. One-time setup: Create .processzero.env
sudo bash setup-env-vars.sh

# 4. Deploy (safe to run multiple times)
git pull origin master
docker compose down
docker compose up -d --build

# 5. Verify
docker compose logs -f | grep -i "twilio\|starting"
```

**Important Notes:**
- `setup-env-vars.sh` creates `.processzero.env` with your secrets
- `.processzero.env` is in `.gitignore` - never committed
- Each deployment overwrites app code but preserves `.processzero.env`
- No need to re-run `setup-env-vars.sh` unless secrets change

## 📁 File Structure

```
ProcessZero.Web/
├── .gitignore                      # ✅ .processzero.env ignored
├── .env.example                    # ✅ Template for secrets
├── setup-env-vars.sh               # ✅ Interactive setup script
├── docker-compose.yml              # ✅ Uses .processzero.env
├── ProcessZero.Web/
│   └── appsettings.json            # ✅ No secrets (updated)
│   ├── appsettings.Development.json # ✅ Only logging config
│   └── appsettings.Production.json  # ✅ Only logging config
└── ENVIRONMENT_VARIABLES_SETUP.md  # ✅ Complete guide
```

## 🔑 Environment Variable Reference

### Required for Production

| Environment Variable | Purpose | Where to Get |
|---------------------|---------|--------------|
| `ConnectionStrings__DefaultConnection` | MySQL database | Your VPS/database provider |
| `Jwt__Key` | JWT authentication secret | Generate: `openssl rand -base64 32` |
| `Jwt__Issuer` | JWT issuer | Use: `https://api.processzero.xyz` |
| `Jwt__Audience` | JWT audience | Use: `https://processzero.xyz` |
| `Twilio__AccountSid` | SMS service | https://console.twilio.com |
| `Twilio__AuthToken` | SMS authentication | https://console.twilio.com |
| `Twilio__PhoneNumber` | SMS sender number | Your Twilio phone number |

### Optional Integrations

| Environment Variable | Purpose |
|---------------------|---------|
| `CalOptions__BaseUrl` | Cal.com API URL |
| `CalOptions__ApiKey` | Cal.com API key |
| `Paystack__SecretKey` | Paystack payments |
| `PayFast__MerchantId` | PayFast payments |
| `PayFast__MerchantKey` | PayFast payments |
| `PayFast__Passphrase` | PayFast payments |
| `GoogleOAuth__ClientId` | Google OAuth |
| `GoogleOAuth__ClientSecret` | Google OAuth |

## 🚨 Security Checklist

- [x] `.processzero.env` in `.gitignore`
- [x] All secrets removed from `appsettings.json`
- [x] Secrets only in environment variables
- [x] `.env.example` created (no real secrets)
- [x] Documentation updated
- [ ] **TODO:** Rotate any existing hardcoded secrets in production
- [ ] **TODO:** Verify .processzero.env permissions on VPS (`chmod 600`)
- [ ] **TODO:** Backup current .processzero.env securely

## 🔄 How It Works

### ASP.NET Core Configuration Order

ASP.NET Core reads configuration in this order (later overrides earlier):

1. `appsettings.json` (defaults, committed to Git)
2. `appsettings.Production.json` (environment-specific defaults)
3. **Environment variables** (secrets, NOT in Git) ← **YOU ARE HERE**

When you set `Twilio__AccountSid=ACxxxxx`, ASP.NET Core:
1. Reads `"Twilio": { "AccountSid": "" }` from `appsettings.json`
2. Your code calls `_configuration["Twilio:AccountSid"]`
3. ASP.NET Core uses the environment variable value instead

### The `__` Separator

The double underscore (`__`) maps to the colon (`:`) in configuration:

```
Jwt__Key           →  Jwt:Key
Twilio__AccountSid →  Twilio:AccountSid
```

This works for nested objects and arrays:

```
Cors__AllowedOrigins__0  →  Cors:AllowedOrigins[0]
```

## 📊 Verification Steps

### 1. Check appsettings.json has no secrets
```bash
grep -r "ACxxxxxxxx\|your-secret\|set-this" appsettings.json
# Should return nothing
```

### 2. Verify .gitignore blocks secrets
```bash
cat .gitignore | grep -A 2 -B 2 processzero
# Should show: .processzero.env
```

### 3. Test Docker Compose loads env
```bash
docker compose config | grep -A 3 env_file
# Should show: ./.processzero.env
```

### 4. Verify in running container
```bash
docker compose exec web env | grep -E "ConnectionStrings|Twilio|Jwt"
# Should show your environment variables
```

## 🛠️ Maintenance

### Updating Secrets

```bash
# Scenario: Twilio credentials need rotation

# 1. Edit .processzero.env
nano .processzero.env
# Update Twilio__AccountSid and Twilio__AuthToken

# 2. Restart containers
docker compose down
docker compose up -d --build

# 3. Verify
docker compose logs -f | grep -i "twilio\|starting"
```

### Emergency Recovery

If `.processzero.env` is lost:

```bash
# Re-run setup script
sudo bash setup-env-vars.sh

# Enter all values again
# Redeploy
docker compose up -d --build
```

## 📚 Related Documentation

- `ENVIRONMENT_VARIABLES_SETUP.md` - Detailed setup guide
- `DOCKER.md` - Docker deployment instructions
- `.env.example` - All environment variables listed
- `setup-env-vars.sh` - Automated setup script

## 🆘 Common Issues

### Issue: "NullReferenceException" on configuration value

**Cause:** Environment variable not set in `.processzero.env`

**Fix:**
```bash
# Check appsettings.json - shows the key name
grep -A 5 "Twilio" appsettings.json

# Check .processzero.env - should have matching variable
grep Twilio .processzero.env

# If missing, re-run setup
sudo bash setup-env-vars.sh
```

### Issue: Container starts but secrets still empty

**Cause:** `.processzero.env` not loaded or syntax error

**Fix:**
```bash
# Verify file exists
ls -la .processzero.env

# Check syntax (no quotes around values)
cat .processzero.env | grep Twilio__AccountSid

# Should be: Twilio__AccountSid=ACxxxxx
# NOT: Twilio__AccountSid="ACxxxxx"

# Restart with fresh env
docker compose down
docker compose up -d --build
```

### Issue: `setup-env-vars.sh` fails

**Cause:** File permissions or running without sudo

**Fix:**
```bash
# Make executable
chmod +x setup-env-vars.sh

# Run with sudo
sudo bash setup-env-vars.sh
```

## 🎯 Next Steps

1. **On Development Machine:**
   - Commit these changes to Git
   - Push to GitHub

2. **On VPS (Production):**
   ```bash
   git pull origin master
   sudo bash setup-env-vars.sh
   docker compose up -d --build
   ```

3. **Verify:**
   ```bash
   docker compose logs -f
   ```

## 📖 Quick Reference

### Environment Variables in Code

```csharp
// No code changes needed! ASP.NET Core handles this automatically.

// Reading from appsettings.json
var accountSid = _configuration["Twilio:AccountSid"];

// Reading from environment variable (same code!)
var authToken = _configuration["Twilio:AuthToken"];

// Connection strings
var connStr = _configuration.GetConnectionString("DefaultConnection");
```

### Local Development

```powershell
# Option 1: Use appsettings.json (for local/testing)
{
  "Twilio": {
    "AccountSid": "AC-test-account",
    "AuthToken": "test-token"
  }
}

# Option 2: Environment variables (PowerShell)
$env:Twilio__AccountSid = "AC-test"
$env:Twilio__AuthToken = "test"

# Option 3: Docker with .env file
# Create .env with your dev values
cat .env
# Twilio__AccountSid=AC-test
# Twilio__AuthToken=test
```

## ✨ Summary

Your ProcessZero API now follows **production-grade security practices**:

- ✅ **Secrets in environment variables** - Not in Git
- ✅ **ASP.NET Core best practices** - Uses built-in configuration hierarchy
- ✅ **Easy deployment** - `setup-env-vars.sh` manages everything
- ✅ **Safe to commit** - `appsettings.json` has no secrets
- ✅ **Well documented** - Multiple guides available
- ✅ **Maintainable** - Easy to update and rotate secrets

**No code changes required** - ASP.NET Core automatically loads environment variables and uses them to override `appsettings.json` values.