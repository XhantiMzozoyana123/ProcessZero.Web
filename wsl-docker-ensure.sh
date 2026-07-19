#!/usr/bin/env bash
# Ensures Docker is running (via systemd if available) and the container is up,
# then tests the HTTP endpoint.
set +e

echo "==== init process (PID 1) ===="
ps -p 1 -o comm=

echo ""
echo "==== ensuring docker is running ===="
if command -v systemctl >/dev/null 2>&1 && systemctl is-system-running >/dev/null 2>&1; then
  systemctl enable docker >/dev/null 2>&1
  systemctl start docker
  sleep 3
  echo "systemd docker active: $(systemctl is-active docker)"
fi

if ! docker info >/dev/null 2>&1; then
  echo "falling back to manual dockerd..."
  nohup dockerd >/var/log/dockerd.log 2>&1 &
  for i in $(seq 1 30); do sleep 2; docker info >/dev/null 2>&1 && break; done
fi

if ! docker info >/dev/null 2>&1; then
  echo "ERROR: docker daemon not available"
  tail -n 30 /var/log/dockerd.log 2>/dev/null
  exit 1
fi

echo ""
echo "==== ensuring container is running ===="
if docker ps --format '{{.Names}}' | grep -q '^processzero-web$'; then
  echo "container already running"
elif docker ps -a --format '{{.Names}}' | grep -q '^processzero-web$'; then
  echo "starting existing container"
  docker start processzero-web
else
  echo "no container found; creating from image"
  docker run -d --name processzero-web \
    -p 8080:8080 \
    -e ASPNETCORE_ENVIRONMENT=Production \
    -e ASPNETCORE_URLS=http://+:8080 \
    --restart unless-stopped \
    processzero-web:latest
fi

sleep 5
echo ""
echo "==== docker ps ===="
docker ps

echo ""
echo "==== HTTP test (inside WSL) ===="
code=$(curl -s -o /dev/null -w '%{http_code}' --max-time 15 http://localhost:8080/ 2>/dev/null)
echo "HTTP status: $code"
