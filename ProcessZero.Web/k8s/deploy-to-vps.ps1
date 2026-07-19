#!/usr/bin/env pwsh
<#
.SYNOPSIS
ProcessZero Kubernetes Deployment Script for Linux VPS

.DESCRIPTION
Deploys the ProcessZero application to your Kubernetes cluster on your Linux VPS

.PARAMETER Registry
Docker registry URL (default: your-docker-registry)

.PARAMETER ImageTag
Docker image tag (default: latest)

.EXAMPLE
./deploy-to-vps.ps1 -Registry "gcr.io/my-project" -ImageTag "1.0"
#>

param(
	[string]$Registry = "your-docker-registry",
	[string]$ImageTag = "latest"
)

$Namespace = "processzero"
$ErrorActionPreference = "Stop"

Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  ProcessZero Kubernetes Deployment for Linux VPS           ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

Write-Host "📦 Deployment Configuration:" -ForegroundColor Yellow
Write-Host "   Registry: $Registry"
Write-Host "   Image Tag: $ImageTag"
Write-Host "   Namespace: $Namespace"
Write-Host ""

# Function to check prerequisites
function Test-Prerequisites {
	Write-Host "🔍 Checking prerequisites..." -ForegroundColor Blue

	# Check kubectl
	if (-not (Get-Command kubectl -ErrorAction SilentlyContinue)) {
		Write-Host "❌ kubectl not found. Install it first." -ForegroundColor Red
		exit 1
	}

	# Check Docker
	if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
		Write-Host "❌ Docker not found. Install Docker Desktop first." -ForegroundColor Red
		exit 1
	}

	# Check kubeconfig
	try {
		kubectl cluster-info | Out-Null
	} catch {
		Write-Host "❌ Cannot connect to Kubernetes cluster." -ForegroundColor Red
		Write-Host "   Check your kubeconfig" -ForegroundColor Red
		exit 1
	}

	Write-Host "✅ All prerequisites met" -ForegroundColor Green
	Write-Host ""
}

# Function to build and push image
function Build-AndPush-Image {
	Write-Host "🐳 Building Docker image..." -ForegroundColor Blue
	docker build -t "$Registry/processzero-web:$ImageTag" .

	Write-Host "📤 Pushing image to registry..." -ForegroundColor Blue
	docker push "$Registry/processzero-web:$ImageTag"

	Write-Host "✅ Image pushed: $Registry/processzero-web:$ImageTag" -ForegroundColor Green
	Write-Host ""
}

# Function to update deployment image
function Update-DeploymentImage {
	$image = "$Registry/processzero-web:$ImageTag"

	Write-Host "🔄 Updating deployment image to: $image" -ForegroundColor Blue

	# Read deployment file and replace image
	$deploymentContent = Get-Content k8s/deployment.yaml -Raw
	$updatedContent = $deploymentContent -replace "image: processzero-web:latest", "image: $image"
	$updatedContent | Set-Content -Path /tmp/deployment-updated.yaml

	kubectl apply -f /tmp/deployment-updated.yaml
	Remove-Item -Path /tmp/deployment-updated.yaml -Force

	Write-Host "✅ Deployment updated" -ForegroundColor Green
	Write-Host ""
}

# Function to deploy manifests
function Deploy-Manifests {
	Write-Host "📋 Creating Kubernetes namespace..." -ForegroundColor Blue
	kubectl create namespace $Namespace --dry-run=client -o yaml | kubectl apply -f -

	Write-Host "🔐 Deploying ConfigMap and Secrets..." -ForegroundColor Blue
	kubectl apply -f k8s/secrets.yaml

	Write-Host "📦 Deploying application..." -ForegroundColor Blue
	kubectl apply -f k8s/service.yaml
	Update-DeploymentImage

	Write-Host "✅ Manifests deployed" -ForegroundColor Green
	Write-Host ""
}

# Function to wait for deployment
function Wait-ForDeployment {
	Write-Host "⏳ Waiting for deployment to be ready..." -ForegroundColor Blue
	kubectl -n $Namespace rollout status deployment/processzero-web --timeout=5m
	Write-Host "✅ Deployment ready!" -ForegroundColor Green
	Write-Host ""
}

# Function to show status
function Show-Status {
	Write-Host "📊 Deployment Status:" -ForegroundColor Yellow
	Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray

	Write-Host ""
	Write-Host "🔹 Pods:" -ForegroundColor Cyan
	kubectl -n $Namespace get pods -o wide

	Write-Host ""
	Write-Host "🔹 Services:" -ForegroundColor Cyan
	kubectl -n $Namespace get services

	Write-Host ""
	Write-Host "🔹 Recent Events:" -ForegroundColor Cyan
	kubectl -n $Namespace get events --sort-by='.lastTimestamp' | Select-Object -Last 10

	Write-Host ""
	Write-Host "🔹 Pod Logs (latest):" -ForegroundColor Cyan
	$pod = kubectl -n $Namespace get pods -l app=processzero-web -o jsonpath='{.items[0].metadata.name}' 2>$null
	if ($pod) {
		Write-Host "Pod: $pod" -ForegroundColor Gray
		kubectl -n $Namespace logs $pod --tail=20
	}

	Write-Host ""
}

# Main execution
Write-Host "📍 Step 1/5: Checking prerequisites" -ForegroundColor Magenta
Test-Prerequisites

Write-Host "📍 Step 2/5: Building and pushing Docker image" -ForegroundColor Magenta
$response = Read-Host "Push image to registry? (y/n)"
if ($response -eq "y") {
	Build-AndPush-Image
} else {
	Write-Host "⏭️  Skipping image build" -ForegroundColor Yellow
}

Write-Host "📍 Step 3/5: Deploying to Kubernetes" -ForegroundColor Magenta
Deploy-Manifests

Write-Host "📍 Step 4/5: Waiting for deployment to be ready" -ForegroundColor Magenta
Wait-ForDeployment

Write-Host "📍 Step 5/5: Showing deployment status" -ForegroundColor Magenta
Show-Status

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║  ✅ Deployment Complete!                                   ║" -ForegroundColor Green
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""

Write-Host "🚀 Next steps:" -ForegroundColor Yellow
Write-Host "   1. Verify the app is running:"
Write-Host "      kubectl -n $Namespace port-forward svc/processzero-web 8080:8080" -ForegroundColor Cyan
Write-Host "      Then visit: http://localhost:8080"
Write-Host ""
Write-Host "   2. View logs:"
Write-Host "      kubectl -n $Namespace logs -f deployment/processzero-web" -ForegroundColor Cyan
Write-Host ""
Write-Host "   3. Verify Kubernetes is running (not Docker Compose):"
Write-Host "      kubectl -n $Namespace exec -it <pod-name> -- env | grep KUBERNETES_SERVICE_HOST" -ForegroundColor Cyan
Write-Host ""
Write-Host "   4. Check which environment is detected:"
Write-Host "      kubectl -n $Namespace logs <pod-name> | findstr 'Runtime Detection' " -ForegroundColor Cyan
Write-Host ""
