#!/usr/bin/env bash
# Brings up the local SQL Server container and applies the schema so the DB-backed tests pass.
# Idempotent. Secrets never enter the repository: the SA password lives in
# ${XDG_CONFIG_HOME:-~/.config}/hotshot-logistics/sa-password (or $SQL_SA_PASSWORD) and the
# application connection string lives in .NET user secrets (ConnectionStrings:DefaultConnection),
# which every git worktree of this repo shares.
#
# Exit codes: 0 ready | 2 Docker unavailable | 3 SQL Server not ready in time
#             4 provisioning or migration failed | 5 configuration problem
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
COMPOSE_FILE="$ROOT/7-Deployment/docker-compose.yml"
API_PROJECT="$ROOT/1-Presentation/HotshotLogistics.Api/HotshotLogistics.Api.csproj"
DBSETUP_PROJECT="$ROOT/7-Deployment/DbSetup/HotshotLogistics.DbSetup/HotshotLogistics.DbSetup.csproj"
MIGRATION_PROJECT="$ROOT/4-Persistence/MigrationRunner/MigrationRunner.csproj"
CONTAINER="hotshot_sqlserver"
SERVER="${HOTSHOT_DEV_SQL_SERVER:-localhost,1433}"
DOCKER_WAIT="${DEV_ENV_DOCKER_WAIT_SECONDS:-180}"
SQL_WAIT="${DEV_ENV_SQL_WAIT_SECONDS:-180}"
SECRET_DIR="${XDG_CONFIG_HOME:-$HOME/.config}/hotshot-logistics"
SA_FILE="$SECRET_DIR/sa-password"

log() { printf '[dev-env-up] %s\n' "$*"; }
die() { local code="$1"; shift; printf '[dev-env-up] ERROR: %s\n' "$*" >&2; exit "$code"; }
START=$SECONDS

# 1. Docker daemon (start Docker Desktop on macOS), bounded.
if ! docker info >/dev/null 2>&1; then
  if [[ "$(uname)" == "Darwin" ]]; then
    log "starting Docker Desktop"
    open -a Docker || die 2 "cannot launch Docker Desktop"
  fi
  waited=0
  until docker info >/dev/null 2>&1; do
    ((waited >= DOCKER_WAIT)) && die 2 "Docker daemon not ready after ${DOCKER_WAIT}s"
    sleep 3
    waited=$((waited + 3))
  done
fi
log "docker daemon ready"

# 2. SA password: env var, then the per-user file, then the existing container, else generate.
if [[ -z "${SQL_SA_PASSWORD:-}" && -r "$SA_FILE" ]]; then
  SQL_SA_PASSWORD="$(<"$SA_FILE")"
fi
if [[ -z "${SQL_SA_PASSWORD:-}" ]]; then
  SQL_SA_PASSWORD="$(docker inspect "$CONTAINER" --format '{{range .Config.Env}}{{println .}}{{end}}' 2>/dev/null |
    sed -n 's/^MSSQL_SA_PASSWORD=//p' | head -n1 || true)"
fi
if [[ -z "${SQL_SA_PASSWORD:-}" ]]; then
  if docker volume inspect hotshot_sqlserver_data >/dev/null 2>&1; then
    die 5 "volume hotshot_sqlserver_data exists but its SA password is unknown; set SQL_SA_PASSWORD or run dev-env-down.sh --purge"
  fi
  SQL_SA_PASSWORD="Aa1!$(openssl rand -hex 12)"
fi
if [[ ! -r "$SA_FILE" ]]; then
  umask 077
  mkdir -p "$SECRET_DIR"
  printf '%s' "$SQL_SA_PASSWORD" >"$SA_FILE"
fi
export SQL_SA_PASSWORD

# 3. Application connection string from user secrets; generate it once if absent.
secret_get() {
  dotnet user-secrets list --project "$API_PROJECT" 2>/dev/null |
    sed -n "s/^$1 = //p" | head -n1
}
APP_CS="$(secret_get 'ConnectionStrings:DefaultConnection')"
if [[ -z "$APP_CS" ]]; then
  gen="Aa1!$(openssl rand -hex 12)"
  APP_CS="Server=$SERVER;Database=hotshot_logistics_test;User Id=hotshot_app;Password=$gen;TrustServerCertificate=True;MultipleActiveResultSets=True"
  dotnet user-secrets set --project "$API_PROJECT" "ConnectionStrings:DefaultConnection" "$APP_CS" >/dev/null ||
    die 5 "could not store ConnectionStrings:DefaultConnection in user secrets"
  log "generated application connection string into user secrets"
fi
cs_field() { # first value of a connection-string key (case-insensitive aliases)
  tr ';' '\n' <<<"$APP_CS" | sed -n -E "s/^[[:space:]]*($1)[[:space:]]*=[[:space:]]*//Ip" | head -n1
}
DB_NAME="$(cs_field 'Database|Initial Catalog')"
APP_USER="$(cs_field 'User Id|UID|User')"
APP_PASSWORD="$(cs_field 'Password|PWD')"
[[ -n "$DB_NAME" && -n "$APP_USER" && -n "$APP_PASSWORD" ]] ||
  die 5 "ConnectionStrings:DefaultConnection must contain Database, User Id and Password"

# 4. Container.
log "docker compose up"
docker compose -f "$COMPOSE_FILE" up -d sqlserver >/dev/null 2>&1 || die 2 "docker compose up failed"

sql_ready() {
  docker exec -e SQLCMDPASSWORD="$SQL_SA_PASSWORD" "$CONTAINER" \
    /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -l 3 -Q "SELECT 1" >/dev/null 2>&1
}
waited=0
until sql_ready; do
  ((waited >= SQL_WAIT)) && die 3 "SQL Server not ready after ${SQL_WAIT}s (docker logs $CONTAINER)"
  sleep 3
  waited=$((waited + 3))
done
log "SQL Server accepting connections (${waited}s)"

# 5. Database and application login (DbSetup CLI), then schema (FluentMigrator runner).
SA_CS="Server=$SERVER;Database=master;User Id=sa;Password=$SQL_SA_PASSWORD;TrustServerCertificate=True"
log "provisioning database '$DB_NAME' and login '$APP_USER'"
CI_SA_CONNECTION_STRING="$SA_CS" HOTSHOT_DB_PASSWORD="$APP_PASSWORD" \
  dotnet run -c Release --project "$DBSETUP_PROJECT" -- \
  --server "$SERVER" --db-name "$DB_NAME" --app-user "$APP_USER" --non-interactive >/dev/null ||
  die 4 "DbSetup failed (re-run with output: dotnet run --project $DBSETUP_PROJECT)"

# Migrations need DDL rights, which the least-privilege application login does not have.
log "applying migrations"
MIGRATION_CS="Server=$SERVER;Database=$DB_NAME;User Id=sa;Password=$SQL_SA_PASSWORD;TrustServerCertificate=True"
ConnectionStrings__DefaultConnection="$MIGRATION_CS" \
  dotnet run -c Release --project "$MIGRATION_PROJECT" >/dev/null ||
  die 4 "MigrationRunner failed (re-run with output: dotnet run --project $MIGRATION_PROJECT)"

log "ready in $((SECONDS - START))s: $SERVER / $DB_NAME. Tests read ConnectionStrings:DefaultConnection from user secrets."
