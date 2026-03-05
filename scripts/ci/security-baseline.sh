#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd -- "$script_dir/../.." && pwd)"

artifacts_dir="${ARTIFACTS_DIR:-$repo_root/.artifacts/security-baseline}"
gitleaks_report="$artifacts_dir/gitleaks.sarif"
osv_report="$artifacts_dir/osv-scan.json"
osv_summary="$artifacts_dir/osv-high-critical-summary.txt"

for command_name in gitleaks osv-scanner jq; do
  if ! command -v "$command_name" >/dev/null 2>&1; then
    echo "$command_name is required to run security baseline checks." >&2
    exit 1
  fi
done

mkdir -p "$artifacts_dir"

echo "Running secret scan with gitleaks..."
gitleaks_exit_code=0
gitleaks dir "$repo_root" \
  --no-banner \
  --redact \
  --exit-code 1 \
  --report-format sarif \
  --report-path "$gitleaks_report" \
  || gitleaks_exit_code=$?

echo "Running dependency vulnerability scan with osv-scanner..."
osv_scan_exit_code=0
osv-scanner scan source "$repo_root" \
  --recursive \
  --format json \
  --output "$osv_report" \
  --verbosity error \
  --experimental-exclude ".git" \
  --experimental-exclude ".artifacts" \
  --experimental-exclude "mobile/.dart_tool" \
  --experimental-exclude "mobile/build" \
  --experimental-exclude "mobile/ios/Flutter/ephemeral" \
  --experimental-exclude "backend/src/MoneyTracker.Api/bin" \
  --experimental-exclude "backend/src/MoneyTracker.Api/obj" \
  --experimental-exclude "backend/tests/MoneyTracker.Api.Tests/bin" \
  --experimental-exclude "backend/tests/MoneyTracker.Api.Tests/obj" \
  || osv_scan_exit_code=$?

if [[ ! -s "$osv_report" ]]; then
  echo "osv-scanner did not produce a JSON report." >&2
  exit 1
fi

high_critical_count="$(
  jq '[.results[]?.packages[]? as $pkg | $pkg.vulnerabilities[]? | (.database_specific.severity // "" | ascii_upcase) as $severity | select($severity == "HIGH" or $severity == "CRITICAL") | {package: $pkg.package.name, version: $pkg.package.version, ecosystem: $pkg.package.ecosystem, id: .id, severity: $severity, summary: (.summary // "")}] | length' "$osv_report"
)"

jq -r '.results[]?.packages[]? as $pkg | $pkg.vulnerabilities[]? | (.database_specific.severity // "" | ascii_upcase) as $severity | select($severity == "HIGH" or $severity == "CRITICAL") | "\($severity): \(.id) \($pkg.package.name)@\($pkg.package.version) [\($pkg.package.ecosystem)] \(.summary // "no summary")"' \
  "$osv_report" > "$osv_summary"

security_failed=0

if [[ "$gitleaks_exit_code" -ne 0 ]]; then
  echo "gitleaks found potential secrets (exit code $gitleaks_exit_code). See report: $gitleaks_report" >&2
  security_failed=1
fi

if [[ "$high_critical_count" -gt 0 ]]; then
  echo "Found $high_critical_count HIGH/CRITICAL dependency vulnerabilities." >&2
  cat "$osv_summary" >&2
  security_failed=1
fi

if [[ "$osv_scan_exit_code" -ne 0 ]]; then
  echo "osv-scanner reported non-high/critical vulnerabilities only." >&2
fi

if [[ "$security_failed" -ne 0 ]]; then
  exit 1
fi

echo "Security baseline checks passed."
