#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd -- "$script_dir/../.." && pwd)"

solution_path="${BACKEND_SOLUTION_PATH:-$repo_root/backend/MoneyTracker.slnx}"
artifacts_dir="${ARTIFACTS_DIR:-$repo_root/.artifacts/backend-quality}"
test_results_dir="$artifacts_dir/test-results"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet CLI is required to run backend quality checks." >&2
  exit 1
fi

mkdir -p "$artifacts_dir" "$test_results_dir"

echo "Running backend restore..."
dotnet restore "$solution_path"

echo "Running backend build..."
dotnet build "$solution_path" \
  --configuration Release \
  --no-restore \
  2>&1 | tee "$artifacts_dir/dotnet-build.log"

echo "Running backend analyzer/format gate..."
dotnet format "$solution_path" \
  --verify-no-changes \
  --no-restore \
  2>&1 | tee "$artifacts_dir/dotnet-format-verify.log"

echo "Running backend tests..."
dotnet test "$solution_path" \
  --configuration Release \
  --no-build \
  --logger "trx;LogFileName=backend-tests.trx" \
  --results-directory "$test_results_dir" \
  2>&1 | tee "$artifacts_dir/dotnet-test.log"

echo "Backend quality checks passed."
