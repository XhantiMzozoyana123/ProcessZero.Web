#!/usr/bin/env pwsh
# Quick reference card - ProcessZero Kubernetes Secrets

$guide = @"
╔════════════════════════════════════════════════════════════════════════════╗
║                                                                            ║
║         ProcessZero - Kubernetes Secrets Setup - QUICK REFERENCE           ║
║                                                                            ║
╚════════════════════════════════════════════════════════════════════════════╝

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📋 ARCHITECTURE
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

YOUR DEVELOPMENT MACHINE                   LINUX VPS (Production)
   │                                              │
   ├─ Local: appsettings.json              ├─ Kubernetes Cluster
   │  (dev placeholder values)             │
   ├─ Docker Compose                       ├─ Pod 1 ──┐
   │  (dev environment)                    │          ├─ Deployment (10 replicas)
   │                                        ├─ Pod 2 ──┤
   └─ git push ──────────────────────────>├─ Pod 3 ──┘
										   │
										   ├─ Kubernetes Secret
										   │  (your real credentials)
										   │
										   ├─ App detects:
										   │  KUBERNETES_SERVICE_HOST ✅
										   │  Loads secrets ✅

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🚀 DEPLOYMENT WORKFLOW
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Step 1: Generate Secrets
   $ ./k8s/generate-k8s-secrets.ps1
   ↓
   Prompts for: Twilio__AccountSid, Twilio__AuthToken, etc.
   Outputs: base64-encoded values
   ↓
   Edit: k8s/secrets.yaml (paste base64 values)

Step 2: Push to Git
   $ git add .
   $ git commit -m "Update k8s/secrets.yaml"
   $ git push origin master

Step 3: SSH to VPS
   $ ssh user@your-vps-ip
   $ cd /path/to/ProcessZero.Web
   $ git pull origin master

Step 4: Deploy to Kubernetes
   $ ./k8s/deploy-to-vps.sh "docker.io/your-username" "1.0.0"
   ↓
   Builds Docker image
   Pushes to registry
   Creates namespace & secrets
   Deploys pods
   ↓
   ✅ Done! App running in Kubernetes

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✅ VERIFY KUBERNETES IS RUNNING
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Method 1: Check Kubernetes Service Host
   $ POD=\$(kubectl -n processzero get pods -l app=processzero-web -o jsonpath='{.items[0].metadata.name}')
   $ kubectl -n processzero exec -it \$POD -- env | grep KUBERNETES_SERVICE_HOST

   ✅ Kubernetes: KUBERNETES_SERVICE_HOST=10.0.0.1
   ❌ Docker Compose: (no output)

Method 2: Check Application Logs
   $ POD=\$(kubectl -n processzero get pods -l app=processzero-web -o jsonpath='{.items[0].metadata.name}')
   $ kubectl -n processzero logs \$POD | grep "Runtime Detection"

   ✅ Kubernetes output:
	  🔍 Runtime Detection:
		 - Kubernetes Service Host: 10.0.0.1
		 - Environment Type: kubernetes
		 - Is Kubernetes: True
	  ✅ Running in Kubernetes - secrets loaded from ConfigMap & Secrets

Method 3: Check Environment Type Variable
   $ kubectl -n processzero exec -it \$POD -- env | grep ENVIRONMENT_TYPE

   ✅ Kubernetes: ENVIRONMENT_TYPE=kubernetes
   ❌ Docker Compose: ENVIRONMENT_TYPE=docker-compose

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🔐 CREDENTIALS MANAGEMENT
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

LOCATION              │ STORED           │ VISIBILITY    │ USE CASE
─────────────────────┼──────────────────┼───────────────┼──────────────────
appsettings.json     │ Repo (Git)       │ Public repo   │ ❌ NOT for secrets
User Secrets         │ Local machine    │ User only     │ ✅ Local development
Docker Compose       │ compose.yml      │ Local         │ ✅ Local dev (placeholders)
Kubernetes Secrets   │ Cluster (etcd)   │ Pods in NS    │ ✅ Production
Azure Key Vault      │ Azure            │ RBAC          │ ✅✅ Enterprise production
AWS Secrets Manager  │ AWS              │ IAM           │ ✅✅ Enterprise production

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📊 ENVIRONMENT DETECTION LOGIC
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

if (ENVIRONMENT_TYPE == "kubernetes")
   → Use Kubernetes Secrets (loaded via envFrom)
   → Logs: "Running in Kubernetes"

if (KUBERNETES_SERVICE_HOST is set)
   → Detected running in Kubernetes pod
   → Logs: "Kubernetes Service Host: 10.0.0.1"

if (/var/run/secrets/kubernetes.io/serviceaccount/namespace exists)
   → Running in Kubernetes pod
   → Kubernetes mounts this automatically

Otherwise:
   → Running in Docker Compose or local
   → Use environment variables from compose file or appsettings.json

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🛠️ USEFUL COMMANDS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

# View deployment
kubectl -n processzero get deployment

# View pods
kubectl -n processzero get pods -o wide

# View logs
kubectl -n processzero logs -f deployment/processzero-web

# Scale replicas
kubectl -n processzero scale deployment processzero-web --replicas=5

# Port forward
kubectl -n processzero port-forward svc/processzero-web 8080:8080

# Execute in pod
kubectl -n processzero exec -it <pod-name> -- bash

# Restart deployment
kubectl -n processzero rollout restart deployment/processzero-web

# View events
kubectl -n processzero get events --sort-by='.lastTimestamp'

# Delete everything
kubectl delete namespace processzero

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
⚠️ IMPORTANT REMINDERS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

❌ DON'T:
   • Commit real secrets to Git
   • Use plain text in config files
   • Use same credentials for dev and prod
   • Store base64 secrets without encryption at rest

✅ DO:
   • Use .gitignore for k8s/secrets.yaml
   • Rotate credentials every 90 days
   • Use separate credentials per environment
   • Enable Kubernetes secret encryption
   • Use RBAC to restrict access
   • Audit all secret access

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📚 DOCUMENTATION
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📖 Full Guide:               KUBERNETES_DEPLOYMENT_GUIDE.md
📖 Credentials Strategies:   PRODUCTION_CREDENTIALS_GUIDE.md
📖 This Summary:             KUBERNETES_SETUP_SUMMARY.md

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
"@

Write-Host $guide
