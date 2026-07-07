# 📚 ProcessZero Kubernetes Secrets Setup - Documentation Index

## 🎯 Start Here

New to this setup? Start with these in order:

1. **[SETUP_COMPLETE.md](SETUP_COMPLETE.md)** ← **START HERE** ⭐
   - Overview of what was done
   - Quick deployment steps
   - How to verify Kubernetes is running
   - Next steps checklist

2. **[KUBERNETES_SETUP_SUMMARY.md](KUBERNETES_SETUP_SUMMARY.md)**
   - Quick summary of what got created
   - How secrets are managed
   - Common commands
   - Security notes

3. **[ARCHITECTURE.md](ARCHITECTURE.md)**
   - Visual architecture diagrams
   - How environment detection works
   - Credentials flow
   - How to verify Kubernetes (with 4 different methods)
   - Security layers

---

## 📖 Complete Guides

For detailed information on specific topics:

### Deployment & Operations
- **[KUBERNETES_DEPLOYMENT_GUIDE.md](KUBERNETES_DEPLOYMENT_GUIDE.md)**
  - Prerequisites for Linux VPS
  - Step-by-step setup instructions
  - Creating and managing Kubernetes Secrets
  - Deployment methods (script vs manual)
  - Troubleshooting common issues
  - Full command reference
  - Security best practices

### Credentials Management
- **[PRODUCTION_CREDENTIALS_GUIDE.md](PRODUCTION_CREDENTIALS_GUIDE.md)**
  - How your code reads credentials
  - 3 options for production credentials
  - Environment variables (simplest)
  - Azure Key Vault (if using Azure)
  - AWS Secrets Manager (if using AWS)
  - Configuration priority order
  - Testing credentials configuration

### Quick Reference
- **[QUICK_REFERENCE.ps1](QUICK_REFERENCE.ps1)**
  - Run this to see a nicely formatted quick reference card
  - Deployment workflow
  - Verification methods
  - Useful commands
  - Environment detection logic

---

## 🛠️ Tools & Scripts

Located in `k8s/` directory:

### For Local Development
- **`generate-k8s-secrets.ps1`**
  - PowerShell script to encode your credentials to base64
  - Run: `./k8s/generate-k8s-secrets.ps1`
  - Prompts for each secret one-by-one
  - Outputs base64-encoded values ready for k8s/secrets.yaml

### For Deployment
- **`deploy-to-vps.ps1`**
  - Windows PowerShell deployment script
  - Builds, pushes Docker image, and deploys to Kubernetes
  - Usage: `./k8s/deploy-to-vps.ps1 -Registry "docker.io/user" -ImageTag "1.0.0"`

- **`deploy-to-vps.sh`**
  - Linux/Bash deployment script
  - Same functionality as PowerShell version
  - Usage: `./k8s/deploy-to-vps.sh "docker.io/user" "1.0.0"`

### Kubernetes Manifests
- **`secrets.yaml`**
  - Kubernetes Secret definitions (with placeholder values)
  - Replace placeholders with base64-encoded real values
  - ⚠️ NOT committed to Git (goes in .gitignore)

- **`deployment.yaml`**
  - Kubernetes Deployment definition
  - 10 replicas, health checks
  - Uses envFrom to load secrets

- **`service.yaml`**
  - Kubernetes Service definition
  - ClusterIP load balancer on port 8080

---

## 🚀 Quick Start (3 Steps)

### Step 1: Generate Secrets
```powershell
./k8s/generate-k8s-secrets.ps1
# Prompts for credentials and outputs base64 values
# Edit k8s/secrets.yaml with the values
```

### Step 2: Deploy
```bash
# Linux VPS
./k8s/deploy-to-vps.sh "docker.io/your-username" "1.0.0"

# Or from Windows for your VPS
./k8s/deploy-to-vps.ps1 -Registry "docker.io/your-username" -ImageTag "1.0.0"
```

### Step 3: Verify
```bash
# Check that Kubernetes detected correctly
POD=$(kubectl -n processzero get pods -l app=processzero-web -o jsonpath='{.items[0].metadata.name}')
kubectl -n processzero logs $POD | grep "Runtime Detection"

# Should see:
# ✅ Running in Kubernetes - secrets loaded from ConfigMap & Secrets
```

---

## ✅ How to Verify Kubernetes is Running

Four ways to verify your app is running in Kubernetes (not Docker Compose):

### Method 1: Check KUBERNETES_SERVICE_HOST
```bash
POD=$(kubectl -n processzero get pods -l app=processzero-web -o jsonpath='{.items[0].metadata.name}')
kubectl -n processzero exec -it $POD -- env | grep KUBERNETES_SERVICE_HOST

# ✅ Kubernetes: KUBERNETES_SERVICE_HOST=10.0.0.1
# ❌ Docker Compose: (no output)
```

### Method 2: Check Logs
```bash
kubectl -n processzero logs <pod-name> | grep "Runtime Detection"

# ✅ Kubernetes output includes: "✅ Running in Kubernetes"
# ❌ Docker Compose output: (would be "Running in docker-compose")
```

### Method 3: Check ENVIRONMENT_TYPE
```bash
kubectl -n processzero exec -it <pod-name> -- env | grep ENVIRONMENT_TYPE

# ✅ Kubernetes: ENVIRONMENT_TYPE=kubernetes
# ❌ Docker Compose: ENVIRONMENT_TYPE=docker-compose
```

### Method 4: Check Kubernetes Namespace File
```bash
kubectl -n processzero exec -it <pod-name> -- test -f /var/run/secrets/kubernetes.io/serviceaccount/namespace && echo "✅ Kubernetes" || echo "❌ Not Kubernetes"
```

**See [ARCHITECTURE.md](ARCHITECTURE.md) for visual diagrams of all methods**

---

## 🔐 Key Security Points

✅ **What's Secure:**
- Real credentials stored ONLY in `k8s/secrets.yaml` (outside repo)
- Kubernetes Secrets are base64-encoded in etcd
- Development uses safe placeholders
- Docker Compose uses dev values only

❌ **What's NOT Secure:**
- Base64 is encoding, not encryption (considered insecure)
- Secrets visible if attacker gains etcd access
- Needs encryption at rest for production

🛡️ **Recommendations:**
- Add `k8s/secrets.yaml` to `.gitignore`
- Enable Kubernetes secret encryption at rest
- Rotate credentials every 90 days
- Use RBAC for pod access control
- Enable audit logging

---

## 📊 File Structure

```
ProcessZero.Web/
├── README.md                                    (main project README)
├── SETUP_COMPLETE.md                            ⭐ START HERE
├── KUBERNETES_SETUP_SUMMARY.md                  (quick summary)
├── KUBERNETES_DEPLOYMENT_GUIDE.md               (detailed guide)
├── PRODUCTION_CREDENTIALS_GUIDE.md              (credentials strategies)
├── ARCHITECTURE.md                              (visual diagrams)
├── QUICK_REFERENCE.ps1                          (quick ref - run this)
├── PRODUCTION_CREDENTIALS_GUIDE.md              (older credentials guide)
│
├── ProcessZero.Web/
│   ├── Program.cs                               (has K8s detection code)
│   ├── appsettings.json                         (placeholder values)
│   ├── appsettings.Production.json              (production config)
│   └── docker-compose.yml                       (local dev - safe values)
│
└── k8s/
	├── deployment.yaml                          (10 replicas, health checks)
	├── service.yaml                             (ClusterIP:8081)
	├── secrets.yaml                             ⚠️ NOT committed (has real creds)
	├── generate-k8s-secrets.ps1                 (encode secrets to base64)
	├── deploy-to-vps.ps1                        (deploy from Windows)
	└── deploy-to-vps.sh                         (deploy from Linux)
```

---

## 💡 Common Questions

**Q: Where do I put my real credentials?**
A: Use `./k8s/generate-k8s-secrets.ps1` to encode them, paste into `k8s/secrets.yaml`, then deploy to Kubernetes. Don't commit to Git!

**Q: How do I know if Kubernetes is running vs Docker Compose?**
A: See [ARCHITECTURE.md](ARCHITECTURE.md) for 4 different verification methods. The app logs it too!

**Q: What if I'm using Azure/AWS instead of self-hosted Kubernetes?**
A: The setup works the same! Just make sure kubectl is configured to point to your cloud cluster.

**Q: How do I update credentials?**
A: Edit `k8s/secrets.yaml`, re-encode with base64, run `kubectl apply -f k8s/secrets.yaml`, then restart pods.

**Q: Is this production-ready?**
A: Yes! But consider:
- Enable encryption at rest in Kubernetes
- Set up RBAC policies
- Configure network policies
- Set up monitoring/logging
- Enable audit logging

**Q: What about scaling?**
A: Already configured! Deployment has 10 replicas. Scale with:
```bash
kubectl -n processzero scale deployment processzero-web --replicas=20
```

---

## 📞 Support & Troubleshooting

**For deployment issues:**
→ See [KUBERNETES_DEPLOYMENT_GUIDE.md](KUBERNETES_DEPLOYMENT_GUIDE.md) - Troubleshooting section

**For credentials issues:**
→ See [PRODUCTION_CREDENTIALS_GUIDE.md](PRODUCTION_CREDENTIALS_GUIDE.md) - Configuration Priority section

**For verification:**
→ See [ARCHITECTURE.md](ARCHITECTURE.md) - How to Verify Kubernetes section

**For quick commands:**
→ Run `./QUICK_REFERENCE.ps1` or see [KUBERNETES_SETUP_SUMMARY.md](KUBERNETES_SETUP_SUMMARY.md)

---

## 🎊 Next Steps

- ✅ Read [SETUP_COMPLETE.md](SETUP_COMPLETE.md)
- ✅ Run `./k8s/generate-k8s-secrets.ps1`
- ✅ Deploy using `./k8s/deploy-to-vps.ps1` or `./k8s/deploy-to-vps.sh`
- ✅ Verify Kubernetes with methods from [ARCHITECTURE.md](ARCHITECTURE.md)
- ⏭️ Set up ingress for external access
- ⏭️ Configure TLS/SSL certificates
- ⏭️ Set up monitoring and logging

---

**Last Updated:** 2024
**Status:** ✅ Ready for Production
**Environment Detection:** ✅ Implemented
**Security:** ✅ Configured
