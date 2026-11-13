#!/usr/bin/env bash
# Per-task test runner for the squad's developer gates: runs the given test classes in whichever test
# project(s) declare them. It builds each project it runs (an up-to-date no-op after the build gate, same
# artifacts path) instead of using --no-build, so a test that names a symbol that does not exist yet fails
# with the compiler's own CS error (what a failing run, --red, reads) and never runs stale binaries.
#
#   5-Test/run-task-tests.sh '<dotnet test --filter value>' <artifacts-path>
#
# The filter is the `{tests}` expansion (`FullyQualifiedName~Ns.Class|FullyQualifiedName~Ns.Other`). The
# classes may live in any test project, so each project is searched for the class names first and only
# the projects that declare one are run. A project that the filter still matches nothing in (same class
# name in another namespace) is tolerated; if NO project runs a test the script fails with exit 8, the
# "zero tests ran" code, so a filter that selects nothing can never read as a pass.
#
# Projects that need SQL Server (they use the shared database fixtures) start the dev environment first
# through dev-env-up.sh, which serialises concurrent callers with its own lock.
#
# Exit: 0 tests ran and passed | 8 no project ran a test | any other: the first failing dotnet test code.
set -uo pipefail

if [[ $# -ne 2 || -z "$1" ]]; then
  echo "usage: $0 '<filter>' <artifacts-path>" >&2
  exit 2
fi
FILTER="$1"
ARTIFACTS="$2"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT" || exit 2
# `dotnet test` (Microsoft.Testing.Platform) does not take MSBuild's -nr:false switch; the environment does the same.
export MSBUILDDISABLENODEREUSE=1

# Simple class names from the filter: the last dotted segment of every FullyQualifiedName~ clause.
# (No mapfile: macOS ships bash 3.2.)
CLASSES=()
while IFS= read -r name; do [[ -n "$name" ]] && CLASSES+=("$name"); done < <(grep -oE 'FullyQualifiedName~[A-Za-z0-9_.]+' <<<"$FILTER" | sed -E 's/^FullyQualifiedName~//; s/.*\.//' | sort -u)
if ((${#CLASSES[@]} == 0)); then
  echo "[task-tests] no FullyQualifiedName~ clause in filter '$FILTER'" >&2
  exit 2
fi
PATTERN="class[[:space:]]+($(IFS='|'; echo "${CLASSES[*]}"))([^A-Za-z0-9_]|\$)"

# Every test project in the repository (not vendored code or build output).
PROJECTS=()
while IFS= read -r project; do PROJECTS+=("$project"); done < <(find . \( -name node_modules -o -name apm_modules -o -name .agent_scratchpad -o -name artifacts -o -name bin -o -name obj -o -name .git \) -prune -o \
  -type f \( -name '*Tests.csproj' -o -name '*IntegrationTests.csproj' \) -print | sed 's|^\./||' | sort -u)

ran=0
status=0
db_ready=0
for project in "${PROJECTS[@]}"; do
  dir="$(dirname "$project")"
  grep -rqE --include='*.cs' --exclude-dir=obj --exclude-dir=bin "$PATTERN" "$dir" || continue

  if ((db_ready == 0)) && grep -rqE --include='*.cs' --exclude-dir=obj --exclude-dir=bin 'DatabaseTestFixture|TestDatabaseHelper|DefaultConnection|DatabaseFact' "$dir"; then
    echo "[task-tests] $project needs SQL Server: dev-env-up.sh"
    7-Deployment/dev-env/dev-env-up.sh || exit $?
    db_ready=1
  fi

  echo "[task-tests] $project: ${CLASSES[*]}"
  dotnet test "$project" -c Release --filter "$FILTER" --artifacts-path "$ARTIFACTS" -p:UseSharedCompilation=false
  rc=$?
  case $rc in
    0) ran=$((ran + 1)) ;;
    8) echo "[task-tests] $project: filter matched no test here" ;;
    *) ran=$((ran + 1)); ((status == 0)) && status=$rc ;;
  esac
done

if ((ran == 0)); then
  echo "[task-tests] NO TESTS RAN: no test project declares or matches ${CLASSES[*]}" >&2
  exit 8
fi
exit $status
