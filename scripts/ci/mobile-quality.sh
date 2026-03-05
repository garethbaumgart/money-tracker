#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd -- "$script_dir/../.." && pwd)"

mobile_dir="${MOBILE_DIR:-$repo_root/mobile}"
artifacts_dir="${ARTIFACTS_DIR:-$repo_root/.artifacts/mobile-quality}"

if ! command -v flutter >/dev/null 2>&1; then
  echo "flutter CLI is required to run mobile quality checks." >&2
  exit 1
fi

mkdir -p "$artifacts_dir"

echo "Running Flutter dependency restore..."
(
  cd "$mobile_dir"
  flutter pub get
) 2>&1 | tee "$artifacts_dir/flutter-pub-get.log"

echo "Running Flutter analyzer..."
(
  cd "$mobile_dir"
  flutter analyze --no-pub
) 2>&1 | tee "$artifacts_dir/flutter-analyze.log"

echo "Running Flutter tests..."
(
  cd "$mobile_dir"
  flutter test --no-pub --reporter expanded
) 2>&1 | tee "$artifacts_dir/flutter-test.log"

echo "Mobile quality checks passed."
