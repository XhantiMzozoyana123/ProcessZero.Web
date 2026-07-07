# Environment Variables - Simple Production Setup

## 🚀 Quick Start (3 Steps)

### Step 1: Set Environment Variables on Linux VPS

```bash
# SSH into your Linux VPS
ssh user@your-vps-ip

# Run the setup script (one-time)
sudo bash ~/setup-env-vars.sh

# This will prompt for your credentials and save them to:
# /etc/environment.d/processzero.conf
```

### Step 2: Update & Deploy Code

```bash
cd /path/to/ProcessZero.Web
git pull origin master
docker compose down
docker compose up -d --build
```

### Step 3: Verify It's Working

```bash
# Check logs
docker compose logs -f

# Test Twilio is loaded
docker compose exec web env | grep TWILIO
```

---

## 📝 Manual Setup (Alternative)

If you prefer to set environment variables manually:

### Windows (Not recommended for production)

```powershell
# Temporary (only for current session)
$env:Twilio__AccountSid = "ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
$env:Twilio__AuthToken = "your_auth_token"
$env:Twilio__PhoneNumber = "+1234567890"

# Permanent (requires admin)
[System.Environment]::SetEnvironmentVariable("Twilio__AccountSid", "ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx", "Machine")
[System.Environment]::SetEnvironmentVariable("Twilio__AuthToken", "your_auth_token", "Machine")
[System.Environment]::SetEnvironmentVariable("Twilio__PhoneNumber", "+1234567890", "Machine")
```

### Linux (Best for Production)

```bash
# Method 1: Export in .bashrc (persistent for this user)
echo 'export Twilio__AccountSid="ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"' >> ~/.bashrc
echo 'export Twilio__AuthToken="your_auth_token"' >> ~/.bashrc
echo 'export Twilio__PhoneNumber="+1234567890"' >> ~/.bashrc
source ~/.bashrc

# Method 2: Systemd environment file (persistent system-wide)
sudo tee /etc/environment.d/processzero.conf > /dev/null << EOF
Twilio__AccountSid="ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
Twilio__AuthToken="your_auth_token"
Twilio__PhoneNumber="+1234567890"
EOF

# Method 3: Docker environment file (.env)
cat > .env << EOF
Twilio__AccountSid=ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
Twilio__AuthToken=your_auth_token
Twilio__PhoneNumber=+1234567890
EOF

# Then use with docker compose
docker compose --env-file .env up -d
```

---

## 🔄 Your Complete Workflow

### On Visual Studio (Dev Machine)
```powershell
# Make changes...
git add .
git commit -m "Your changes"
git push origin master
```

### On Linux VPS (Production)
```bash
ssh user@your-vps-ip
cd /path/to/ProcessZero.Web
git pull origin master
docker compose down
docker compose up -d --build

# Verify
docker compose logs -f | grep -i "twilio\|runtime\|error"
```

---

## ✅ How to Verify Credentials Are Loaded

### Method 1: Check Logs

```bash
docker compose logs processzero-web | grep -i "twilio"

# Should show your credentials were loaded successfully
```

### Method 2: Execute in Container

```bash
# Check environment variables inside container
docker compose exec web env | grep Twilio

# Expected output:
# Twilio__AccountSid=ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
# Twilio__AuthToken=your_auth_token
# Twilio__PhoneNumber=+1234567890
```

### Method 3: Test the API

```bash
# Send a test SMS
curl -X POST http://localhost:8080/api/twilio/send-sms \
  -H "Content-Type: application/json" \
  -d '{"phoneNumber":"+1234567890","message":"Test"}'
```

---

## 🔐 Security Best Practices

✅ **DO:**
- Store credentials in `/etc/environment.d/` (systemd) - persistent
- Use `.env` files with Docker Compose - easy to manage
- Restrict file permissions: `chmod 600 /etc/environment.d/processzero.conf`
- Rotate credentials every 90 days
- Never commit `.env` or credentials to Git

❌ **DON'T:**
- Hardcode credentials in source code
- Commit `.env` to Git
- Store credentials in Docker images
- Share credentials in chat/email
- Use same credentials for dev and production

---

## 📊 Comparison: Your Options

| Method | Easy? | Persistent? | Secure? | Best For |
|--------|-------|-------------|---------|----------|
| Export in shell | ❌ No | ❌ No | ⚠️ Medium | Testing |
| .bashrc | ✅ Yes | ✅ Yes | ⚠️ Medium | Single user |
| /etc/environment.d | ✅ Yes | ✅ Yes | ✅ Good | System-wide |
| .env file | ✅ Yes | ✅ Yes (with Compose) | ⚠️ Medium | Docker Compose |
| Kubernetes Secrets | ⚠️ Moderate | ✅ Yes | ✅ Good | Kubernetes |

**Recommendation for your setup:** Use `/etc/environment.d/` with the `setup-env-vars.sh` script (one-time setup, persistent)

---

## 🆘 Troubleshooting

### Credentials Not Loading
```bash
# Check if environment file exists
ls -la /etc/environment.d/processzero.conf

# Verify content
cat /etc/environment.d/processzero.conf | grep Twilio

# Source it manually
source /etc/environment.d/processzero.conf

# Verify it was loaded
echo $Twilio__AccountSid
```

### Docker Can't See Environment Variables
```bash
# Restart Docker daemon
sudo systemctl restart docker

# Or reload environment
sudo systemctl daemon-reload

# Then redeploy
docker compose down
docker compose up -d --build
```

### Twilio Integration Still Failing

1. Check container logs:
```bash
docker compose logs -f web
```

2. Verify variables in container:
```bash
docker compose exec web env | grep -i twilio
```

3. Check app error message for specific issue

---

## 📞 Common Commands

```bash
# View all environment variables
env

# Filter Twilio variables
env | grep Twilio

# Set temporary variable (current session only)
export Twilio__AccountSid="ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"

# View systemd environment
systemctl show-environment | grep -i twilio

# Reload systemd environment
sudo systemctl daemon-reload

# Verify Docker has access to variables
docker run -it --env-file /etc/environment.d/processzero.conf ubuntu bash
env | grep Twilio
```

---

## 🎯 One-Time Setup

Run this **once** on your Linux VPS:

```bash
# 1. Create setup script
cat > ~/setup-env-vars.sh << 'EOF'
#!/bin/bash
# ... (script content from setup-env-vars.sh)
EOF

# 2. Make executable
chmod +x ~/setup-env-vars.sh

# 3. Run it
sudo ~/setup-env-vars.sh

# 4. Verify
source /etc/environment.d/processzero.conf
echo $Twilio__AccountSid
```

Then every time you deploy:

```bash
cd /path/to/ProcessZero.Web
git pull origin master
docker compose down && docker compose up -d --build
```

**That's it! Simple and clean. 🚀**
