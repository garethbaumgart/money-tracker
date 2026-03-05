#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd -- "$script_dir/../.." && pwd)"

workflow_path="${WORKFLOW_PATH:-$repo_root/.github/workflows/ci.yml}"
docs_path="${DOCS_PATH:-$repo_root/docs/ci-required-checks.md}"

if [[ ! -f "$workflow_path" ]]; then
  echo "Workflow file not found: $workflow_path" >&2
  exit 1
fi

if [[ ! -f "$docs_path" ]]; then
  echo "Documentation file not found: $docs_path" >&2
  exit 1
fi

documented_checks=()
while IFS= read -r line; do
  documented_checks+=("$line")
done < <(
  sed -n '/<!-- required-checks:start -->/,/<!-- required-checks:end -->/p' "$docs_path" \
    | grep -E '^- [A-Za-z0-9_-]+$' \
    | sed -E 's/^- //'
)

if [[ "${#documented_checks[@]}" -eq 0 ]]; then
  echo "No documented checks found between required-check markers in $docs_path" >&2
  exit 1
fi

duplicate_documented_check="$(
  printf '%s\n' "${documented_checks[@]}" | sort | uniq -d | head -n 1
)"

if [[ -n "$duplicate_documented_check" ]]; then
  echo "Duplicate required check in docs: $duplicate_documented_check" >&2
  exit 1
fi

workflow_jobs=()
while IFS= read -r line; do
  workflow_jobs+=("$line")
done < <(
  awk '
    /^jobs:/ { in_jobs=1; next }
    in_jobs && /^[^[:space:]]/ { in_jobs=0 }
    in_jobs && /^[[:space:]]{2}[A-Za-z0-9_-]+:/ {
      name=$1
      sub(":", "", name)
      print name
    }
  ' "$workflow_path"
)

if [[ "${#workflow_jobs[@]}" -eq 0 ]]; then
  echo "No workflow jobs found in $workflow_path" >&2
  exit 1
fi

missing_from_workflow=()
for documented_check in "${documented_checks[@]}"; do
  if ! printf '%s\n' "${workflow_jobs[@]}" | grep -qx "$documented_check"; then
    missing_from_workflow+=("$documented_check")
  fi
done

if [[ "${#missing_from_workflow[@]}" -gt 0 ]]; then
  echo "The following documented required checks are missing from workflow jobs:" >&2
  printf '  - %s\n' "${missing_from_workflow[@]}" >&2
  exit 1
fi

echo "Required check documentation matches workflow job names."
