#!/bin/bash
# ProcessZero Kubernetes Deployment Script for Linux VPS
# This script deploys the application to your Kubernetes cluster on your Linux VPS

set -e

REGISTRY="${1:-your-docker-registry}"
IMAGE_TAG="${2:-latest}"
NAMESPACE="processzero"

echo "╔════════════════════════════════════════════════════════════╗"
echo "║  ProcessZero Kubernetes Deployment for Linux VPS           ║"
echo "╚════════════════════════════════════════════════════════════╝"
echo ""
echo "📦 Deployment Configuration:"
echo "   Registry: $REGISTRY"
echo "   Image Tag: $IMAGE_TAG"
echo "   Namespace: $NAMESPACE"
echo ""

# Function to check prerequisites
check_prerequisites() {
	echo "🔍 Checking prerequisites..."

	# Check kubectl
	if ! command -v kubectl &> /dev/null; then
		echo "❌ kubectl not found. Install it first:"
		echo "   Linux: curl -LO 'https://dl.k8s.io/release/$(curl -L -s https://dl.k8s.io/release/stable.txt)/bin/linux/amd64/kubectl'"
		exit 1
	fi

	# Check Docker
	if ! command -v docker &> /dev/null; then
		echo "❌ Docker not found. Install Docker first."
		exit 1
	fi

	# Check kubeconfig
	if ! kubectl cluster-info &> /dev/null; then
		echo "❌ Cannot connect to Kubernetes cluster."
		echo "   Check your kubeconfig: $KUBECONFIG"
		exit 1
	fi

	echo "✅ All prerequisites met"
	echo ""
}

# Function to build and push Docker image
build_and_push_image() {
	echo "🐳 Building Docker image..."
	docker build -t "$REGISTRY/processzero-web:$IMAGE_TAG" .

	echo "📤 Pushing image to registry..."
	docker push "$REGISTRY/processzero-web:$IMAGE_TAG"

	echo "✅ Image pushed: $REGISTRY/processzero-web:$IMAGE_TAG"
	echo ""
}

# Function to update deployment image
update_deployment_image() {
	local image="$REGISTRY/processzero-web:$IMAGE_TAG"

	echo "🔄 Updating deployment image to: $image"

	# Create temporary deployment file with updated image
	sed "s|image: processzero-web:latest|image: $image|g" k8s/deployment.yaml > /tmp/deployment-updated.yaml

	kubectl apply -f /tmp/deployment-updated.yaml
	rm -f /tmp/deployment-updated.yaml

	echo "✅ Deployment updated"
	echo ""
}

# Function to deploy Kubernetes manifests
deploy_manifests() {
	echo "📋 Creating Kubernetes namespace..."
	kubectl create namespace $NAMESPACE --dry-run=client -o yaml | kubectl apply -f -

	echo "🔐 Deploying ConfigMap and Secrets..."
	kubectl apply -f k8s/secrets.yaml

	echo "📦 Deploying application..."
	kubectl apply -f k8s/service.yaml
	update_deployment_image

	echo "✅ Manifests deployed"
	echo ""
}

# Function to wait for deployment to be ready
wait_for_deployment() {
	echo "⏳ Waiting for deployment to be ready..."
	kubectl -n $NAMESPACE rollout status deployment/processzero-web --timeout=5m
	echo "✅ Deployment ready!"
	echo ""
}

# Function to show deployment status
show_status() {
	echo "📊 Deployment Status:"
	echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

	echo ""
	echo "🔹 Pods:"
	kubectl -n $NAMESPACE get pods -o wide

	echo ""
	echo "🔹 Services:"
	kubectl -n $NAMESPACE get services

	echo ""
	echo "🔹 Recent Events:"
	kubectl -n $NAMESPACE get events --sort-by='.lastTimestamp' | tail -10

	echo ""
	echo "🔹 Pod Logs (latest):"
	local pod=$(kubectl -n $NAMESPACE get pods -l app=processzero-web -o jsonpath='{.items[0].metadata.name}' 2>/dev/null)
	if [ ! -z "$pod" ]; then
		echo "Pod: $pod"
		kubectl -n $NAMESPACE logs "$pod" --tail=20
	fi

	echo ""
}

# Main execution
echo "📍 Step 1/5: Checking prerequisites"
check_prerequisites

echo "📍 Step 2/5: Building and pushing Docker image"
read -p "Push image to registry? (y/n) " -n 1 -r
echo ""
if [[ $REPLY =~ ^[Yy]$ ]]; then
	build_and_push_image
else
	echo "⏭️  Skipping image build"
fi

echo "📍 Step 3/5: Deploying to Kubernetes"
deploy_manifests

echo "📍 Step 4/5: Waiting for deployment to be ready"
wait_for_deployment

echo "📍 Step 5/5: Showing deployment status"
show_status

echo ""
echo "╔════════════════════════════════════════════════════════════╗"
echo "║  ✅ Deployment Complete!                                   ║"
echo "╚════════════════════════════════════════════════════════════╝"
echo ""
echo "🚀 Next steps:"
echo "   1. Verify the app is running:"
echo "      kubectl -n $NAMESPACE port-forward svc/processzero-web 8080:8080"
echo "      Then visit: http://localhost:8080"
echo ""
echo "   2. View logs:"
echo "      kubectl -n $NAMESPACE logs -f deployment/processzero-web"
echo ""
echo "   3. Scale the deployment:"
echo "      kubectl -n $NAMESPACE scale deployment processzero-web --replicas=3"
echo ""
echo "   4. Verify Kubernetes is running (not Docker Compose):"
echo "      kubectl -n $NAMESPACE exec -it <pod-name> -- env | grep KUBERNETES_SERVICE_HOST"
echo ""
echo "   5. Check which environment is detected:"
echo "      kubectl -n $NAMESPACE logs <pod-name> | grep 'Runtime Detection\\|Environment Type'"
echo ""
