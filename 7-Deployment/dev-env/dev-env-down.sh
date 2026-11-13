#!/usr/bin/env bash
# Stops the local SQL Server container. Data is kept in the named volume unless --purge is given.
# Exit codes: 0 done | 2 Docker unavailable
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
COMPOSE_FILE="$ROOT/7-Deployment/docker-compose.yml"
PURGE=0
[[ "${1:-}" == "--purge" ]] && PURGE=1

docker info >/dev/null 2>&1 || { echo "[dev-env-down] Docker daemon is not running" >&2; exit 2; }

# The compose file requires SQL_SA_PASSWORD to be set even to stop; any value works for down.
export SQL_SA_PASSWORD="${SQL_SA_PASSWORD:-unused}"
if ((PURGE)); then
  docker compose -f "$COMPOSE_FILE" down --volumes
  echo "[dev-env-down] container and data volume removed"
else
  docker compose -f "$COMPOSE_FILE" down
  echo "[dev-env-down] container removed, data volume kept"
fi
