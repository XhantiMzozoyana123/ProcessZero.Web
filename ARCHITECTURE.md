# ProcessZero Kubernetes Secrets - Visual Architecture

```
╔════════════════════════════════════════════════════════════════════════════╗
║                          DEVELOPMENT WORKFLOW                              ║
╚════════════════════════════════════════════════════════════════════════════╝

┌─────────────────────────────────────────────────────────────────────────────┐
│                         YOUR DEVELOPMENT MACHINE                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ProcessZero.Web/                                                            │
│  ├── appsettings.json              ← Placeholder values only                │
│  │   ├── Twilio__AccountSid: "your_account_sid_here"                        │
│  │   ├── Twilio__AuthToken: "your_auth_token_here"                          │
│  │   └── ... (all placeholders)                                             │
│  │                                                                           │
│  ├── docker-compose.yml             ← Dev environment                       │
│  │   └── ENVIRONMENT_TYPE: docker-compose                                   │
│  │       (safe to run locally)                                              │
│  │                                                                           │
│  ├── k8s/secrets.yaml               ← FOR YOU ONLY - Don't commit!          │
│  │   (with real base64-encoded credentials)                                 │
│  │                                                                           │
│  └── k8s/generate-k8s-secrets.ps1   ← Run to encode your secrets            │
│      (prompts for credentials, outputs base64)                              │
│                                                                              │
│  Workflow:                                                                   │
│  1. Run: ./k8s/generate-k8s-secrets.ps1                                     │
│  2. Edit: k8s/secrets.yaml (paste base64 values)                            │
│  3. Run: git add . && git commit && git push                                │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
									  ↓ git push
							┌──────────────────────┐
							│     GitHub Repo      │
							│  ProcessZero.Web     │
							└──────────────────────┘
									  ↓ git pull
┌─────────────────────────────────────────────────────────────────────────────┐
│                    YOUR LINUX VPS (Kubernetes Cluster)                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ProcessZero Namespace                                                       │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │ Kubernetes Objects                                                     │ │
│  │ ┌─────────────────────────────────────────────────────────────────┐  │ │
│  │ │ ConfigMap: processzero-config                                  │  │ │
│  │ │ ├── ASPNETCORE_ENVIRONMENT: "Production"                       │  │ │
│  │ │ ├── ASPNETCORE_URLS: "http://+:8080"                           │  │ │
│  │ │ └── ENVIRONMENT_TYPE: "kubernetes"  ← KEY!                     │  │ │
│  │ └─────────────────────────────────────────────────────────────────┘  │ │
│  │                                                                        │ │
│  │ ┌─────────────────────────────────────────────────────────────────┐  │ │
│  │ │ Secret: processzero-secrets (base64-encoded)                    │  │ │
│  │ │ ├── Twilio__AccountSid: "QUN4eHh4eHh4eHh4..." (base64)         │  │ │
│  │ │ ├── Twilio__AuthToken: "YXV0aF90b2tlbl9o..." (base64)          │  │ │
│  │ │ ├── CalOptions__ApiKey: "Y2FsX2xpdmVfZ..." (base64)            │  │ │
│  │ │ ├── Paystack__SecretKey: "c2tfdGVzdF8x..." (base64)            │  │ │
│  │ │ └── ... (all credentials)                                      │  │ │
│  │ └─────────────────────────────────────────────────────────────────┘  │ │
│  │                                                                        │ │
│  │ ┌─────────────────────────────────────────────────────────────────┐  │ │
│  │ │ Service: processzero-web (ClusterIP)                            │  │ │
│  │ │ └── Port 8080 → Pods                                            │  │ │
│  │ └─────────────────────────────────────────────────────────────────┘  │ │
│  │                                                                        │ │
│  │ ┌─────────────────────────────────────────────────────────────────┐  │ │
│  │ │ Deployment: processzero-web (10 replicas)                       │  │ │
│  │ │                                                                  │  │ │
│  │ │  ┌──────────────────────────────────────────────────────────┐  │  │ │
│  │ │  │ Pod 1: processzero-web-abc123                            │  │  │ │
│  │ │  │ ├── KUBERNETES_SERVICE_HOST: "10.0.0.1" (auto-set)      │  │  │ │
│  │ │  │ ├── ENVIRONMENT_TYPE: "kubernetes" (from ConfigMap)     │  │  │ │
│  │ │  │ ├── Twilio__AccountSid: "AC..." (from Secret)           │  │  │ │
│  │ │  │ └── Container Status: Running ✅                         │  │  │ │
│  │ │  └──────────────────────────────────────────────────────────┘  │  │ │
│  │ │                                                                  │  │ │
│  │ │  ┌──────────────────────────────────────────────────────────┐  │  │ │
│  │ │  │ Pod 2: processzero-web-def456                            │  │  │ │
│  │ │  │ ├── KUBERNETES_SERVICE_HOST: "10.0.0.1" (auto-set)      │  │  │ │
│  │ │  │ ├── ENVIRONMENT_TYPE: "kubernetes" (from ConfigMap)     │  │  │ │
│  │ │  │ ├── Twilio__AccountSid: "AC..." (from Secret)           │  │  │ │
│  │ │  │ └── Container Status: Running ✅                         │  │  │ │
│  │ │  └──────────────────────────────────────────────────────────┘  │  │ │
│  │ │                                                                  │  │ │
│  │ │  ... (8 more replicas) ...                                      │  │ │
│  │ │                                                                  │  │ │
│  │ └─────────────────────────────────────────────────────────────────┘  │ │
│  │                                                                        │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘


╔════════════════════════════════════════════════════════════════════════════╗
║                       RUNTIME ENVIRONMENT DETECTION                         ║
╚════════════════════════════════════════════════════════════════════════════╝

Program.cs Logic:
┌──────────────────────────────────────────────────────────────────────────────┐
│                                                                              │
│  IsRunningInKubernetes()                                                    │
│  {                                                                          │
│      if (ENVIRONMENT_TYPE == "kubernetes")                                 │
│          → ✅ Kubernetes detected                                           │
│      else if (KUBERNETES_SERVICE_HOST is set)                              │
│          → ✅ Kubernetes detected (auto-set by K8s)                        │
│      else if (/var/run/secrets/kubernetes.io/serviceaccount/namespace)     │
│          → ✅ Kubernetes detected (K8s mounts this)                        │
│      else                                                                   │
│          → ❌ Docker Compose or Local development                          │
│  }                                                                          │
│                                                                              │
│  Output:                                                                    │
│  ✅ KUBERNETES:                                                             │
│     🔍 Runtime Detection:                                                   │
│        - Kubernetes Service Host: 10.0.0.1                                 │
│        - Environment Type: kubernetes                                       │
│        - Is Kubernetes: True                                               │
│     ✅ Running in Kubernetes - secrets loaded from ConfigMap & Secrets     │
│                                                                              │
│  ❌ DOCKER COMPOSE:                                                         │
│     🔍 Runtime Detection:                                                   │
│        - Kubernetes Service Host: Not found                                │
│        - Environment Type: docker-compose                                  │
│        - Is Kubernetes: False                                              │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘


╔════════════════════════════════════════════════════════════════════════════╗
║                         CREDENTIALS FLOW                                    ║
╚════════════════════════════════════════════════════════════════════════════╝

LOCAL DEVELOPMENT:
  appsettings.json / User Secrets
		 ↓
  (Twilio__AccountSid: "your_account_sid_here")
		 ↓
  IConfiguration
		 ↓
  TwilioService reads via _configuration["Twilio:AccountSid"]
		 ↓
  ✅ Safe - only placeholder values in repo

DOCKER COMPOSE:
  docker-compose.yml
		 ↓
  Environment Variables (passed to container)
		 ↓
  (Twilio__AccountSid: "your_development_account_sid")
		 ↓
  IConfiguration reads from environment
		 ↓
  TwilioService
		 ↓
  ✅ Safe - dev values, not production

KUBERNETES (PRODUCTION):
  k8s/secrets.yaml (stored separately from repo)
		 ↓
  kubectl apply -f k8s/secrets.yaml
		 ↓
  Kubernetes Secret object (base64-encoded in etcd)
		 ↓
  Deployment uses envFrom: [secretRef]
		 ↓
  Pod environment variables (auto-injected)
		 ↓
  (KUBERNETES_SERVICE_HOST, Twilio__AccountSid, etc.)
		 ↓
  IConfiguration reads from environment
		 ↓
  TwilioService reads via _configuration["Twilio:AccountSid"]
		 ↓
  ✅ Secure - real credentials in K8s Secrets only


╔════════════════════════════════════════════════════════════════════════════╗
║                       HOW TO VERIFY KUBERNETES                             ║
╚════════════════════════════════════════════════════════════════════════════╝

CHECK 1: KUBERNETES_SERVICE_HOST Environment Variable
────────────────────────────────────────────────────────

In Kubernetes Pod:
  $ env | grep KUBERNETES_SERVICE_HOST
  KUBERNETES_SERVICE_HOST=10.0.0.1
  ✅ RUNNING IN KUBERNETES

In Docker Compose Container:
  $ env | grep KUBERNETES_SERVICE_HOST
  (no output)
  ❌ NOT IN KUBERNETES


CHECK 2: Application Startup Logs
────────────────────────────────────

In Kubernetes:
  kubectl logs pod/processzero-web-abc123 | grep "Runtime Detection"

  Output:
  🔍 Runtime Detection:
	 - Kubernetes Service Host: 10.0.0.1
	 - Environment Type: kubernetes
	 - Is Kubernetes: True
  ✅ Running in Kubernetes - secrets loaded from ConfigMap & Secrets

  ✅ CONFIRMED KUBERNETES

In Docker Compose:
  docker logs processzero-web | grep "Runtime Detection"

  Output:
  🔍 Runtime Detection:
	 - Kubernetes Service Host: Not found
	 - Environment Type: docker-compose
	 - Is Kubernetes: False

  ❌ CONFIRMED NOT KUBERNETES


CHECK 3: ENVIRONMENT_TYPE Variable
────────────────────────────────────

In Kubernetes:
  $ env | grep ENVIRONMENT_TYPE
  ENVIRONMENT_TYPE=kubernetes
  ✅ KUBERNETES

In Docker Compose:
  $ env | grep ENVIRONMENT_TYPE
  ENVIRONMENT_TYPE=docker-compose
  ❌ NOT KUBERNETES


CHECK 4: Kubernetes Namespace File
────────────────────────────────────

In Kubernetes Pod:
  $ test -f /var/run/secrets/kubernetes.io/serviceaccount/namespace && echo "✅ Kubernetes" || echo "❌ Not Kubernetes"
  ✅ Kubernetes

In Docker Compose Container:
  $ test -f /var/run/secrets/kubernetes.io/serviceaccount/namespace && echo "✅ Kubernetes" || echo "❌ Not Kubernetes"
  ❌ Not Kubernetes


╔════════════════════════════════════════════════════════════════════════════╗
║                           SECURITY LAYERS                                   ║
╚════════════════════════════════════════════════════════════════════════════╝

1. Source Control Security
   ├── k8s/secrets.yaml is NOT committed to Git
   ├── .gitignore contains k8s/secrets.yaml
   └── Real secrets never appear in repository

2. Kubernetes Security
   ├── Secrets stored in Kubernetes etcd (base64)
   ├── RBAC: Only pods with proper SA can access secrets
   ├── Namespace isolation: Secrets only accessible in processzero namespace
   └── Optional: Enable encryption at rest in etcd

3. Pod Security
   ├── Secrets injected as environment variables via envFrom
   ├── Application reads via IConfiguration
   ├── Memory-only (never written to disk)
   └── No credentials visible in pod manifests

4. Audit Trail
   ├── Kubernetes audit logging (if enabled)
   ├── Secret access can be tracked
   ├── Credentials rotation logged
   └── All changes in git history

```

---

## 📖 To View This Architecture:

```bash
cat ARCHITECTURE.md
```

## 🚀 To Deploy:

```bash
./k8s/deploy-to-vps.ps1 -Registry "docker.io/your-username" -ImageTag "1.0.0"
```

## ✅ To Verify:

```bash
# Get pod name
POD=$(kubectl -n processzero get pods -l app=processzero-web -o jsonpath='{.items[0].metadata.name}')

# Check all 4 indicators
kubectl -n processzero exec -it $POD -- env | grep KUBERNETES_SERVICE_HOST
kubectl -n processzero logs $POD | grep "Runtime Detection"
kubectl -n processzero exec -it $POD -- env | grep ENVIRONMENT_TYPE
kubectl -n processzero exec -it $POD -- test -f /var/run/secrets/kubernetes.io/serviceaccount/namespace && echo "✅ Kubernetes"
```
