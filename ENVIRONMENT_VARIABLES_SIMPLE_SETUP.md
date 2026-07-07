# ✅ Switched to Environment Variables - Simple Setup

## 🎯 What Changed

You've moved from complex **Kubernetes Secrets** to simple **Environment Variables** - much simpler and still production-ready!

---

## 🚀 Your New Deployment Workflow

### One-Time Setup (Linux VPS)

```bash
ssh user@your-vps-ip

# Download and run setup script
curl -O https://raw.githubusercontent.com/XhantiMzozoyana123/ProcessZero.Web/master/setup-env-vars.sh
sudo bash setup-env-vars.sh

# This prompts for your credentials and saves them to:
# /etc/environment.d/processzero.conf (persistent)
```

### Every Deployment

```bash
ssh user@your-vps-ip
cd /path/to/ProcessZero.Web
git pull origin master
docker compose down
docker compose up -d --build
```

---

## ✨ Why Environment Variables are Better

| Feature | Kubernetes Secrets | Environment Variables |
|---------|---|---|
| **Setup Time** | Complex | 5 minutes |
| **Learning Curve** | Steep | Easy |
| **Works with Docker Compose** | ❌ No | ✅ Yes |
| **Works with Kubernetes** | ✅ Yes | ✅ Yes |
| **Works with VPS** | ⚠️ Complicated | ✅ Simple |
| **Persistence** | etcd | systemd/os |
| **Debugging** | Hard | Easy |

---

## 📋 How It Works

Your app reads credentials via `IConfiguration`:

```csharp
// In TwilioService.cs (already in your code)
var accountSid = _configuration["Twilio:AccountSid"];
var authToken = _configuration["Twilio:AuthToken"];

// This works with:
// 1. appsettings.json (dev)
// 2. Environment variables (production) ✅
// 3. User Secrets (dev)
// 4. Any .NET configuration source
```

---

## 🔄 Quick Commands Reference

### Set up credentials (one-time)
```bash
sudo bash setup-env-vars.sh
```

### Deploy
```bash
cd ProcessZero.Web
git pull
docker compose down && docker compose up -d --build
```

### View logs
```bash
docker compose logs -f
```

### Verify Twilio is loaded
```bash
docker compose exec web env | grep Twilio
```

### Check app is running
```bash
curl http://localhost:8080
```

---

## 📝 Environment Variables Set

After running `setup-env-vars.sh`, these are persisted in `/etc/environment.d/processzero.conf`:

```
Twilio__AccountSid         → Your Twilio SID
Twilio__AuthToken          → Your Twilio token
Twilio__PhoneNumber        → Your Twilio number
CalOptions__ApiKey         → Cal.com API key
Paystack__SecretKey        → Paystack key
PayFast__MerchantId        → PayFast merchant ID
PayFast__MerchantKey       → PayFast merchant key
PayFast__Passphrase        → PayFast passphrase
GoogleOAuth__ClientId      → Google Client ID
GoogleOAuth__ClientSecret  → Google Client Secret
Jwt__Key                   → JWT signing key
```

---

## ✅ Verify It's Working

### Method 1: Check logs
```bash
docker compose logs -f | grep -i twilio
```

### Method 2: Check inside container
```bash
docker compose exec web env | grep Twilio
```

### Method 3: Test Twilio SMS endpoint
```bash
curl -X POST http://localhost:8080/api/twilio/send-sms \
  -H "Content-Type: application/json" \
  -d '{"phoneNumber":"+1234567890","message":"Test"}'
```

---

## 🔐 Security

✅ Credentials are:
- Stored in `/etc/environment.d/processzero.conf` (file permissions 600)
- Loaded into environment (not visible in Docker image)
- Not committed to Git
- Encrypted in transit (if using HTTPS)

---

## 📚 Documentation

Read these for more details:

1. **[ENVIRONMENT_VARIABLES_SETUP.md](ENVIRONMENT_VARIABLES_SETUP.md)** ← Detailed guide
2. **[docker-compose.yml](docker-compose.yml)** ← Shows env var examples
3. **[setup-env-vars.sh](setup-env-vars.sh)** ← Automated setup script

---

## 🎯 Your Complete Deployment Steps

### First Time (Dev Machine)
```powershell
cd D:\Users\Xhanti\source\repos\ProcessZero.Web
git pull origin master  # Get latest including new setup
git push origin master  # Push any local changes
```

### First Time (Linux VPS)
```bash
ssh user@your-vps-ip
cd /path/to/ProcessZero.Web
git pull origin master
sudo bash setup-env-vars.sh  # Set credentials once
docker compose down
docker compose up -d --build
```

### Subsequent Deployments (Anytime)
```bash
ssh user@your-vps-ip
cd /path/to/ProcessZero.Web
git pull origin master
docker compose down && docker compose up -d --build
```

---

## 💡 Pro Tips

### Auto-Reload Environment
```bash
# If you update /etc/environment.d/processzero.conf
sudo systemctl daemon-reload
docker compose down && docker compose up -d --build
```

### Create an Alias
Add to `~/.bashrc` on Linux VPS:
```bash
alias deploy-pz='cd /path/to/ProcessZero.Web && git pull && docker compose down && docker compose up -d --build && docker compose logs -f'
```

Then just:
```bash
deploy-pz
```

### Monitor in Real-Time
```bash
docker compose logs -f --tail=100
```

---

## 🆘 If Something Goes Wrong

### Credentials not loading
```bash
# Check if file exists
ls -la /etc/environment.d/processzero.conf

# Reload systemd
sudo systemctl daemon-reload

# Restart Docker
sudo systemctl restart docker
sudo docker compose down && docker compose up -d --build
```

### Check what's actually set
```bash
# In the running container
docker compose exec web env | grep -i twilio

# Should show your actual credentials
```

### Reset everything
```bash
# Stop the app
docker compose down

# Re-run setup
sudo bash setup-env-vars.sh

# Restart
docker compose up -d --build
```

---

## ✨ Summary

You now have:
- ✅ Simple environment variable setup
- ✅ Works with Docker Compose
- ✅ Works with Kubernetes too (if you switch back)
- ✅ One-time setup script
- ✅ Persistent credentials
- ✅ Secure storage
- ✅ Easy to debug
- ✅ Production-ready

**Much simpler than Kubernetes Secrets! 🚀**

---

## 🎊 Next Steps

1. **On your Linux VPS:**
   ```bash
   sudo bash setup-env-vars.sh
   ```

2. **Deploy:**
   ```bash
   cd ProcessZero.Web
   git pull && docker compose down && docker compose up -d --build
   ```

3. **Verify:**
   ```bash
   docker compose logs -f | head -20
   docker compose exec web env | grep Twilio
   ```

**Done! Your app is now running with environment variables. 🎉**
