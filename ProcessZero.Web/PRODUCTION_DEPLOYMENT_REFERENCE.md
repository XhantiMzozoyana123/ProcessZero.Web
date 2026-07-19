# Production Deployment - Environment Variables Reference

## ✅ Current Configuration Summary

Your ProcessZero.Web project is now configured for secure production deployment:

- **Secrets removed from:** `appsettings.json` (empty strings for sensitive values)
- **Secrets stored in:** `.processzero.env` (gitignored, never committed)
- **Docker Compose:** Already configured to load `.processzero.env`

## 🚀 First-Time VPS Setup

```bash
# 1. SSH into your Linux VPS
ssh user@your-vps-ip

# 2. Navigate to project directory
cd /home/xhanti/ProcessZero.Web

# 3. Run setup script (interactive)
sudo bash setup-env-vars.sh

# 4. Deploy
docker compose down
docker compose up -d --build

# 5. Verify
docker compose logs -f web
```

## 🔄 Regular Deployments (Updates)

```bash
ssh user@your-vps-ip
cd /home/xhanti/ProcessZero.Web
git pull origin master
docker compose down
docker compose up -d --build
docker compose logs -f web
```

## 🔐 Environment Variables Reference

### Database
```env
ConnectionStrings__DefaultConnection=Server=localhost;Port=3306;Database=processzero;User=root;Password=password;
```

### JWT Authentication
```env
Jwt__Key=SuperSecretKey-min-32-chars
Jwt__Issuer=https://api.processzero.xyz
Jwt__Audience=https://processzero.xyz
```

### Twilio SMS (Production)
```env
Twilio__AccountSid=ACxxxxxxxxxxxxxxxx
Twilio__AuthToken=xxxxxxxxxxxxxxxx
Twilio__PhoneNumber=+1234567890
```

### Google OAuth
```env
GoogleOAuth__ClientId=your-client-id.apps.googleusercontent.com
GoogleOAuth__ClientSecret=your-client-secret
GoogleOAuth__RedirectUri=https://api.processzero.xyz/api/googleauth/callback
```

### Payment Gateways
```env
# Paystack
Paystack__SecretKey=sk_live_xxxxxxxxxxxxxxxx

# PayFast
PayFast__MerchantId=xxxxxxxx
PayFast__MerchantKey=xxxxxxxx
PayFast__Passphrase=xxxxxxxx
PayFast__UseSandbox=false
```

### Cal.com Integration
```env
CalOptions__BaseUrl=https://api.cal.com/v2
CalOptions__ApiKey=your-calcom-api-key
```

## 🔍 Verification Commands

```bash
# Test 1: Check file exists and permissions
ls -la .processzero.env
# Should show: -rw------- (permissions 600)

# Test 2: View loaded environment variables
docker compose exec web env | grep -E "(TWILIO|JWT|ConnectionStrings)"

# Test 3: Check logs for errors
docker compose logs -f web | grep -i "error\|twilio\|starting"

# Test 4: Test SMS endpoint
curl -X POST http://localhost:8081/api/twilio/send-sms \
  -H "Content-Type: application/json" \
  -d '{"phoneNumber":"+1234567890","message":"Test"}'
```

## ⚠️ Security Reminders

1. **NEVER** commit `.processzero.env` to Git
2. **NEVER** hardcode secrets in `appsettings.json`
3. **ALWAYS** use HTTPS in production
4. **ROTATE** credentials every 90 days
5. **RESTRICT** file permissions: `chmod 600 .processzero.env`

## 📂 File Structure

```
ProcessZero.Web/
├── appsettings.json                 # Public config (no secrets)
├── appsettings.Development.json     # Dev overrides
├── appsettings.Production.json      # Production overrides
├── docker-compose.yml               # Docker config (uses .processzero.env)
├── .processzero.env                 # ⚠️ SECRETS - NOT in Git
├── .env.example                     # Template (safe to commit)
├── setup-env-vars.sh                # Setup script for VPS
└── ProcessZero.Web/
    └── Program.cs                   # Reads from IConfiguration
```

## 🆘 Troubleshooting

### Environment variables not loading?

```bash
# 1. Verify .processzero.env syntax (no quotes around values)
cat .processzero.env | head -10

# 2. Rebuild containers
docker compose down
docker compose up -d --build

# 3. Check runtime environment
docker compose exec web env | grep TWILIO
```

### Need to update a secret?

```bash
# Option 1: Re-run setup script
sudo bash setup-env-vars.sh

# Option 2: Edit directly
nano .processzero.env
docker compose down && docker compose up -d --build
```

## 📖 Full Documentation

- **Environment Setup Guide:** `ENVIRONMENT_VARIABLES_SETUP.md`
- **Deployment Security:** `DEPLOYMENT_SECURITY_SETUP.md`
- **Docker Guide:** `DOCKER.md`
- **Quick Reference:** `QUICK_REFERENCE.md`

---

**Remember:** ASP.NET Core automatically maps `ConnectionStrings__DefaultConnection` to `ConnectionStrings:DefaultConnection` in C# code. No code changes needed!