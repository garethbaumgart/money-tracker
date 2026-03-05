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
gitleaks dir "$repo_root" \
  --no-banner \
  --redact \
  --exit-code 1 \
  --report-format sarif \
  --report-path "$gitleaks_report"

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
  jq '[
    def normalized_score:
      (.max_severity // "") as $raw
      | ($raw | tonumber?) as $numeric
      | if $numeric != null then
          $numeric
        else
          ($raw | ascii_upcase) as $label
          | if $label == "CRITICAL" then 9
            elif $label == "HIGH" then 7
            elif $label == "MODERATE" or $label == "MEDIUM" then 4
            elif $label == "LOW" then 0.1
            else 0
            end
        end;

    .results[]?.packages[]? as $pkg
    | $pkg.groups[]?
    | (normalized_score) as $score
    | select($score >= 7)
    | {
      package: $pkg.package.name,
      version: $pkg.package.version,
      ecosystem: $pkg.package.ecosystem,
      ids: (.ids // []),
      score: $score,
      severity: (if $score >= 9 then "CRITICAL" else "HIGH" end)
    }
  ] | length' "$osv_report"
)"

total_vulnerability_count="$(
  jq '[.results[]?.packages[]?.vulnerabilities[]?] | length' "$osv_report"
)"

jq -r '
  def normalized_score:
    (.max_severity // "") as $raw
    | ($raw | tonumber?) as $numeric
    | if $numeric != null then
        $numeric
      else
        ($raw | ascii_upcase) as $label
        | if $label == "CRITICAL" then 9
          elif $label == "HIGH" then 7
          elif $label == "MODERATE" or $label == "MEDIUM" then 4
          elif $label == "LOW" then 0.1
          else 0
          end
      end;

  .results[]?.packages[]? as $pkg
  | $pkg.groups[]?
  | (normalized_score) as $score
  | select($score >= 7)
  | (if $score >= 9 then "CRITICAL" else "HIGH" end) as $severity
  | "\($severity) (CVSS \($score), raw=\(.max_severity // "unknown")): \((.ids // [] | join(", "))) \($pkg.package.name)@\($pkg.package.version) [\($pkg.package.ecosystem)]"
' "$osv_report" > "$osv_summary"

if [[ "$high_critical_count" -gt 0 ]]; then
  echo "Found $high_critical_count HIGH/CRITICAL dependency vulnerabilities." >&2
  cat "$osv_summary" >&2
  exit 1
fi

if [[ "$osv_scan_exit_code" -gt 1 ]]; then
  echo "osv-scanner failed with exit code $osv_scan_exit_code." >&2
  exit 1
fi

if [[ "$osv_scan_exit_code" -eq 1 && "$total_vulnerability_count" -eq 0 ]]; then
  echo "osv-scanner returned exit code 1 but no vulnerabilities were present in the report." >&2
  exit 1
fi

if [[ "$osv_scan_exit_code" -eq 1 ]]; then
  echo "osv-scanner reported vulnerabilities below HIGH severity only." >&2
fi

echo "Security baseline checks passed."
