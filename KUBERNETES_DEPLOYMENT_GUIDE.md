# Kubernetes Deployment Guide - ProcessZero on Linux VPS

## Table of Contents
1. [Prerequisites](#prerequisites)
2. [Step 1: Prepare Your Linux VPS](#step-1-prepare-your-linux-vps)
3. [Step 2: Create Kubernetes Secrets](#step-2-create-kubernetes-secrets)
4. [Step 3: Deploy to Kubernetes](#step-3-deploy-to-kubernetes)
5. [Step 4: Verify Kubernetes is Running (Not Docker Compose)](#step-4-verify-kubernetes-is-running-not-docker-compose)
6. [Troubleshooting](#troubleshooting)
7. [Commands Reference](#commands-reference)

---

## Prerequisites

You need:
- ✅ Linux VPS with Kubernetes cluster (K3s, kubeadm, or managed service)
- ✅ `kubectl` configured to access your cluster
- ✅ Docker registry (Docker Hub, GCR, ECR, or private registry)
- ✅ Your actual API credentials (Twilio, PayFast, etc.)

---

## Step 1: Prepare Your Linux VPS

### Option A: Use K3s (Lightweight, Recommended for VPS)

```bash
# SSH into your Linux VPS
ssh user@your-vps-ip

# Install K3s
curl -sfL https://get.k3s.io | sh -

# Verify K3s is running
sudo k3s kubectl cluster-info

# Make kubectl accessible without sudo (optional)
sudo cp /etc/rancher/k3s/k3s.yaml ~/.kube/config
sudo chown $(id -u):$(id -g) ~/.kube/config

# Test connection
kubectl cluster-info
```

### Option B: Use Existing Kubernetes Cluster

If you already have a cluster, ensure your `kubeconfig` is set up:

```bash
export KUBECONFIG=/path/to/your/kubeconfig.yaml
kubectl cluster-info
```

---

## Step 2: Create Kubernetes Secrets

### Method 1: Using the PowerShell Helper Script (Recommended)

On your development machine:

```powershell
cd ProcessZero.Web
./k8s/generate-k8s-secrets.ps1
```

This will:
1. Prompt you for each credential
2. Generate base64-encoded values
3. Copy them to your clipboard

Then update `k8s/secrets.yaml` with the encoded values.

### Method 2: Manual Base64 Encoding

```bash
# On Linux or macOS
echo -n "ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx" | base64

# On Windows PowerShell
[Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"))
```

Then update `k8s/secrets.yaml`:

```yaml
data:
  Twilio__AccountSid: "QUN4eHh4eHh4eHh4eHh4eHg="  # Your base64 value
  Twilio__AuthToken: "YXV0aF90b2tlbl9oZXJl"
  # ... other secrets
```

---

## Step 3: Deploy to Kubernetes

### Option A: Using the Deployment Script (Easiest)

**On Windows:**
```powershell
./k8s/deploy-to-vps.ps1 -Registry "docker.io/your-username" -ImageTag "1.0.0"
```

**On Linux/macOS:**
```bash
chmod +x k8s/deploy-to-vps.sh
./k8s/deploy-to-vps.sh "docker.io/your-username" "1.0.0"
```

The script will:
1. Check prerequisites (kubectl, Docker)
2. Build Docker image
3. Push to registry
4. Apply Kubernetes manifests
5. Wait for deployment to be ready
6. Show status

### Option B: Manual Deployment

1. **Build and push Docker image:**
   ```bash
   docker build -t docker.io/your-username/processzero-web:1.0.0 .
   docker push docker.io/your-username/processzero-web:1.0.0
   ```

2. **Update deployment image:**
   Edit `k8s/deployment.yaml`:
   ```yaml
   containers:
	 - name: processzero-web
	   image: docker.io/your-username/processzero-web:1.0.0
   ```

3. **Deploy to Kubernetes:**
   ```bash
   # Create namespace
   kubectl create namespace processzero

   # Deploy secrets and config
   kubectl apply -f k8s/secrets.yaml
   kubectl apply -f k8s/service.yaml
   kubectl apply -f k8s/deployment.yaml
   ```

4. **Monitor deployment:**
   ```bash
   kubectl -n processzero rollout status deployment/processzero-web --timeout=5m
   ```

---

## Step 4: Verify Kubernetes is Running (Not Docker Compose)

### Method 1: Check Environment Variables

```bash
# Get a pod name
POD=$(kubectl -n processzero get pods -l app=processzero-web -o jsonpath='{.items[0].metadata.name}')

# Check KUBERNETES_SERVICE_HOST (set only in K8s)
kubectl -n processzero exec -it $POD -- env | grep KUBERNETES_SERVICE_HOST

# Output should show something like:
# KUBERNETES_SERVICE_HOST=10.0.0.1
```

### Method 2: Check Application Logs

```bash
POD=$(kubectl -n processzero get pods -l app=processzero-web -o jsonpath='{.items[0].metadata.name}')
kubectl -n processzero logs $POD | grep "Runtime Detection"

# Expected output:
# 🔍 Runtime Detection:
#    - Kubernetes Service Host: 10.0.0.1
#    - Environment Type: kubernetes
#    - Is Kubernetes: True
# ✅ Running in Kubernetes - secrets loaded from ConfigMap & Secrets
```

### Method 3: Compare with Docker Compose

**In Docker Compose, you'd see:**
```
🔍 Runtime Detection:
   - Kubernetes Service Host: Not found
   - Environment Type: docker-compose
   - Is Kubernetes: False
```

**In Kubernetes, you should see:**
```
🔍 Runtime Detection:
   - Kubernetes Service Host: 10.0.0.1 (or similar)
   - Environment Type: kubernetes
   - Is Kubernetes: True
✅ Running in Kubernetes - secrets loaded from ConfigMap & Secrets
```

### Method 4: Direct Pod Inspection

```bash
# Verify the pod is running in Kubernetes (namespace isolation)
kubectl -n processzero get pods -o wide

# Check if namespace file exists (K8s only)
POD=$(kubectl -n processzero get pods -l app=processzero-web -o jsonpath='{.items[0].metadata.name}')
kubectl -n processzero exec $POD -- test -f /var/run/secrets/kubernetes.io/serviceaccount/namespace && echo "✅ Kubernetes pod" || echo "❌ Not Kubernetes"
```

---

## Troubleshooting

### Pod not starting

```bash
# Check pod status
kubectl -n processzero get pods
kubectl -n processzero describe pod <pod-name>

# Check logs
kubectl -n processzero logs <pod-name>
kubectl -n processzero logs <pod-name> --previous  # Previous container if crashed
```

### ImagePullBackOff

```bash
# Check if image exists in registry
docker pull docker.io/your-username/processzero-web:1.0.0

# If error, rebuild and push
docker build -t docker.io/your-username/processzero-web:1.0.0 .
docker push docker.io/your-username/processzero-web:1.0.0
```

### Secrets not loaded

```bash
# Verify secrets exist
kubectl -n processzero get secrets
kubectl -n processzero get secret processzero-secrets -o yaml

# Check if pod can read secrets
POD=$(kubectl -n processzero get pods -l app=processzero-web -o jsonpath='{.items[0].metadata.name}')
kubectl -n processzero exec $POD -- env | grep "Twilio"
```

### Connection timeout

```bash
# Check network policies
kubectl -n processzero get networkpolicies

# Check ingress/service
kubectl -n processzero get services
kubectl -n processzero get ingress
```

---

## Commands Reference

### Deployment Management

```bash
# View deployment
kubectl -n processzero get deployment

# Scale up/down
kubectl -n processzero scale deployment processzero-web --replicas=5

# Force restart
kubectl -n processzero rollout restart deployment/processzero-web

# View rollout history
kubectl -n processzero rollout history deployment/processzero-web

# Rollback to previous version
kubectl -n processzero rollout undo deployment/processzero-web
```

### Pod Management

```bash
# List all pods
kubectl -n processzero get pods -o wide

# Execute command in pod
kubectl -n processzero exec -it <pod-name> -- /bin/bash

# Port forward (access app locally)
kubectl -n processzero port-forward svc/processzero-web 8081:8081
# Then visit: http://localhost:8081

# Stream logs
kubectl -n processzero logs -f deployment/processzero-web

# View resource usage
kubectl -n processzero top pods
```

### Secrets Management

```bash
# List secrets
kubectl -n processzero get secrets

# View secret (base64 encoded)
kubectl -n processzero get secret processzero-secrets -o yaml

# Delete and recreate secret
kubectl -n processzero delete secret processzero-secrets
kubectl apply -f k8s/secrets.yaml

# Update a specific secret key
kubectl -n processzero patch secret processzero-secrets -p '{"data":{"Twilio__AccountSid":"'$(echo -n "NEW_VALUE" | base64)'"}}'
```

### Debugging

```bash
# Get detailed pod info
kubectl -n processzero describe pod <pod-name>

# Get events
kubectl -n processzero get events --sort-by='.lastTimestamp'

# Check node status
kubectl get nodes
kubectl describe node <node-name>

# Check resource requests vs available
kubectl -n processzero describe deployment processzero-web
```

---

## Summary: How to Verify Kubernetes (Not Docker Compose)

| Check | Kubernetes | Docker Compose |
|-------|-----------|-----------------|
| **KUBERNETES_SERVICE_HOST** | Set to cluster IP | Not set |
| **ENVIRONMENT_TYPE** | "kubernetes" | "docker-compose" |
| **Logs show** | "Running in Kubernetes" | "Running in docker-compose" |
| **Namespace isolation** | Yes, in `processzero` | No, single network |
| **Pod file exists** | `/var/run/secrets/kubernetes.io/serviceaccount/namespace` | No |
| **Secrets source** | K8s Secrets object | Environment variables from compose file |
| **Replica management** | `kubectl scale` | `docker-compose scale` |
| **Rolling updates** | Automatic via rollout | Manual restarts |

---

## Next Steps

1. ✅ Deploy to Kubernetes using `deploy-to-vps.ps1` or `deploy-to-vps.sh`
2. ✅ Verify Kubernetes is running using the verification commands above
3. ✅ Monitor logs: `kubectl -n processzero logs -f deployment/processzero-web`
4. ✅ Set up ingress for external access (if needed)
5. ✅ Configure certificate management (SSL/TLS)
6. ✅ Set up monitoring and alerting

---

## Security Best Practices

- 🔒 **Never commit secrets.yaml** - use `.gitignore`
- 🔐 **Use encrypted secrets** - Enable Kubernetes Secrets encryption at rest
- 🛡️ **Use RBAC** - Restrict pod permissions
- 🔑 **Rotate secrets regularly** - Update credentials every 90 days
- 📊 **Audit logs** - Enable Kubernetes audit logging
- 🚨 **Use network policies** - Restrict pod-to-pod communication

