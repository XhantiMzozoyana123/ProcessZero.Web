#!/usr/bin/env bash
# Ensures dockerd is up, prints container status/logs, and tests the HTTP endpoint.
set +e

if ! docker info >/dev/null 2>&1; then
  service docker start 2>/dev/null || true
  sleep 2
  if ! docker info >/dev/null 2>&1; then
    nohup dockerd >/var/log/dockerd.log 2>&1 &
    for i in $(seq 1 30); do sleep 2; docker info >/dev/null 2>&1 && break; done
  fi
fi

echo "==== docker ps -a ===="
docker ps -a

echo ""
echo "==== inspect ===="
docker inspect -f 'RestartCount={{.RestartCount}} Status={{.State.Status}} ExitCode={{.State.ExitCode}} Error={{.State.Error}}' processzero-web

echo ""
echo "==== last 60 log lines ===="
docker logs --tail 60 processzero-web 2>&1

echo ""
echo "==== HTTP test (inside WSL) ===="
sleep 2
code=$(curl -s -o /dev/null -w '%{http_code}' --max-time 10 http://localhost:8080/ 2>/dev/null)
echo "HTTP status from inside WSL: $code"
