#!/usr/bin/env bash
# Runs inside the WSL2 Ubuntu distro (as root) to install Docker Engine
# (no Docker Desktop) and start the ProcessZero.Web container.
set -e
export DEBIAN_FRONTEND=noninteractive

PROJECT_DIR="/mnt/c/Users/Xhanti/source/repos/ProcessZero.Web"

echo "==> Installing Docker Engine if missing..."
if ! command -v docker >/dev/null 2>&1; then
  apt-get update -y
  apt-get install -y docker.io
fi

echo "==> Ensuring Docker daemon is running..."
if ! docker info >/dev/null 2>&1; then
  service docker start 2>/dev/null || true
  sleep 3
fi
if ! docker info >/dev/null 2>&1; then
  echo "    starting dockerd manually..."
  nohup dockerd >/var/log/dockerd.log 2>&1 &
  for i in $(seq 1 30); do
    sleep 2
    if docker info >/dev/null 2>&1; then break; fi
  done
fi

if ! docker info >/dev/null 2>&1; then
  echo "ERROR: Docker daemon failed to start. Last log lines:" >&2
  tail -n 40 /var/log/dockerd.log 2>/dev/null || true
  exit 1
fi
echo "    Docker daemon is up."

cd "$PROJECT_DIR"

echo "==> Building and starting the container..."
if docker compose version >/dev/null 2>&1; then
  docker compose up --build -d
  echo "----"
  docker compose ps
else
  docker build -t processzero-web:latest .
  docker rm -f processzero-web >/dev/null 2>&1 || true
  docker run -d --name processzero-web \
    -p 8080:8080 \
    -e ASPNETCORE_ENVIRONMENT=Production \
    -e ASPNETCORE_URLS=http://+:8080 \
    --restart unless-stopped \
    processzero-web:latest
  echo "----"
  docker ps
fi

echo ""
echo "Done. The app should be available at http://localhost:8080"
