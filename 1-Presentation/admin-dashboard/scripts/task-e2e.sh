#!/usr/bin/env bash
# Per-task Playwright runner for the squad's dashboard gates. Runs, in chromium only:
#   - every changed spec (tests/**/*.spec.ts), and
#   - the specs that cover a changed source file by naming convention: tests/<kebab-case stem>.spec.ts
#     (DriversManagement.tsx -> drivers-management.spec.ts) and tests/<area>*.spec.ts, where <area> is
#     the folder under src/components or src/app (src/components/jobs/JobForm.tsx -> jobs-management.spec.ts).
#   - the whole suite when a shared test helper (tests/fixtures, tests/utils) or playwright.config.ts changed.
# A changed source file no spec covers is reported as NOT VERIFIED (exit 0): it has no e2e evidence, and the
# lint, typecheck and build gates still judge it. Paths arrive repository-relative.
set -uo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.." || exit 2
PREFIX="1-Presentation/admin-dashboard/"

kebab() { sed -E 's/([a-z0-9])([A-Z])/\1-\2/g' <<<"$1" | tr '[:upper:]' '[:lower:]'; }

specs=()
uncovered=()
full=0
add() { local s; for s in "${specs[@]:-}"; do [[ "$s" == "$1" ]] && return; done; specs+=("$1"); }

for raw in "$@"; do
  path="${raw#"$PREFIX"}"
  case "$path" in
    tests/*.spec.ts | tests/**/*.spec.ts) [[ -f "$path" ]] && add "$path" ;;
    tests/fixtures/* | tests/utils/* | playwright.config.ts) full=1 ;;
    src/*)
      found=0
      stem="$(basename "${path%.*}")"
      if [[ -f "tests/$(kebab "$stem").spec.ts" ]]; then add "tests/$(kebab "$stem").spec.ts"; found=1; fi
      area="$(sed -nE 's#^src/(components|app)/(\([^/]*\)/)?([^/]+)/.*#\3#p' <<<"$path")"
      if [[ -n "$area" ]]; then
        for f in tests/"$(kebab "$area")"*.spec.ts; do [[ -f "$f" ]] && { add "$f"; found=1; }; done
      fi
      ((found)) || uncovered+=("$raw")
      ;;
  esac
done

for f in "${uncovered[@]:-}"; do
  [[ -n "$f" ]] && echo "NOT VERIFIED: no Playwright spec covers $f (by naming convention); lint, typecheck and build still ran"
done

if ((full == 0 && ${#specs[@]} == 0)); then
  echo "[task-e2e] no spec to run for this change"
  exit 0
fi

(test -d node_modules || npm ci --no-audit --no-fund) || exit 1
# Type errors first, with repository-relative paths: a spec that imports something that does not exist
# yet then fails here with the compiler's TS2307/TS2339 (what a failing run, --red, reads) rather than
# as a Playwright load error.
(cd ../.. && 1-Presentation/admin-dashboard/node_modules/.bin/tsc --noEmit --pretty false -p 1-Presentation/admin-dashboard) || exit 1
npx --no-install playwright install chromium || exit 1
if ((full)); then
  echo "[task-e2e] shared test helper or config changed: running the whole chromium suite"
  exec npx --no-install playwright test --project=chromium
fi
echo "[task-e2e] running: ${specs[*]}"
exec npx --no-install playwright test --project=chromium "${specs[@]}"
