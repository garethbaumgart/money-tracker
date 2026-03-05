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

if ! command -v yq &>/dev/null; then
  echo "yq is required but not installed. See: https://github.com/mikefarah/yq#install" >&2
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

workflow_required_checks=()
while IFS= read -r line; do
  workflow_required_checks+=("$line")
done < <(
  sed -n '/^# required-checks:start$/,/^# required-checks:end$/p' "$workflow_path" \
    | grep -E '^# - [A-Za-z0-9_-]+$' \
    | sed -E 's/^# - //'
)

if [[ "${#workflow_required_checks[@]}" -eq 0 ]]; then
  echo "No required checks found between workflow required-check markers in $workflow_path" >&2
  exit 1
fi

duplicate_workflow_required_check="$(
  printf '%s\n' "${workflow_required_checks[@]}" | sort | uniq -d | head -n 1
)"

if [[ -n "$duplicate_workflow_required_check" ]]; then
  echo "Duplicate required check in workflow markers: $duplicate_workflow_required_check" >&2
  exit 1
fi

missing_from_workflow_markers=()
for documented_check in "${documented_checks[@]}"; do
  if ! printf '%s\n' "${workflow_required_checks[@]}" | grep -qx "$documented_check"; then
    missing_from_workflow_markers+=("$documented_check")
  fi
done

if [[ "${#missing_from_workflow_markers[@]}" -gt 0 ]]; then
  echo "The following documented checks are missing from workflow required-check markers:" >&2
  printf '  - %s\n' "${missing_from_workflow_markers[@]}" >&2
  exit 1
fi

missing_from_docs=()
for workflow_required_check in "${workflow_required_checks[@]}"; do
  if ! printf '%s\n' "${documented_checks[@]}" | grep -qx "$workflow_required_check"; then
    missing_from_docs+=("$workflow_required_check")
  fi
done

if [[ "${#missing_from_docs[@]}" -gt 0 ]]; then
  echo "The following workflow required checks are missing from docs:" >&2
  printf '  - %s\n' "${missing_from_docs[@]}" >&2
  exit 1
fi

workflow_jobs=()
while IFS= read -r line; do
  workflow_jobs+=("$line")
done < <(yq '.jobs | keys | .[]' "$workflow_path")

if [[ "${#workflow_jobs[@]}" -eq 0 ]]; then
  echo "No workflow jobs found in $workflow_path" >&2
  exit 1
fi

missing_from_jobs=()
for workflow_required_check in "${workflow_required_checks[@]}"; do
  if ! printf '%s\n' "${workflow_jobs[@]}" | grep -qx "$workflow_required_check"; then
    missing_from_jobs+=("$workflow_required_check")
  fi
done

if [[ "${#missing_from_jobs[@]}" -gt 0 ]]; then
  echo "The following required checks are missing from workflow jobs:" >&2
  printf '  - %s\n' "${missing_from_jobs[@]}" >&2
  exit 1
fi

echo "Required checks are synchronized across docs, workflow markers, and workflow job IDs."
