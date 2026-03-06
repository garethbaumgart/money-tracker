#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd -- "$script_dir/../.." && pwd)"
project_path="$repo_root/backend/src/MoneyTracker.Api/MoneyTracker.Api.csproj"
out_file="$repo_root/backend/openapi/openapi.v1.json"
port="${OPENAPI_EXPORT_PORT:-5065}"
pid=""
log_file="${OPENAPI_EXPORT_LOG:-$repo_root/.artifacts/openapi-export.log}"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet CLI is required to run export-openapi.sh" >&2
  exit 1
fi

if ! command -v jq >/dev/null 2>&1; then
  echo "jq is required to normalize OpenAPI JSON deterministically." >&2
  exit 1
fi

mkdir -p "$(dirname -- "$log_file")"
mkdir -p "$(dirname -- "$out_file")"

cleanup() {
  if [[ -n "$pid" ]]; then
    if kill -0 "$pid" >/dev/null 2>&1; then
      kill "$pid" >/dev/null 2>&1 || true
      wait "$pid" 2>/dev/null || true
    fi
    pid=""
  fi
}

wait_for_openapi() {
  local max_attempts=120
  local attempt=0
  local response_file
  response_file="$(mktemp)"
  set +e

  while ((attempt < max_attempts)); do
    if curl -fsS "http://127.0.0.1:${port}/openapi/v1.json" -o "$response_file"; then
      if jq -e '.openapi and .info and .paths' "$response_file" >/dev/null 2>&1; then
        jq -S . "$response_file" > "$out_file.tmp"
        rm -f "$response_file"
        return 0
      fi
    fi

    sleep 0.5
    ((attempt++))
  done
  set -e

  echo "Timed out waiting for local API at port ${port} to return a valid OpenAPI document." >&2
  echo "Last captured payload:" >&2
  sed -n '1,40p' "$response_file" >&2 || true
  if [[ -f "$log_file" ]]; then
    echo "OpenAPI export log (tail):" >&2
    tail -n 80 "$log_file" >&2 || true
  fi
  rm -f "$response_file"
  return 1
}

trap cleanup EXIT

ASPNETCORE_ENVIRONMENT=Development \
dotnet run --project "$project_path" --no-launch-profile --urls "http://127.0.0.1:${port}" \
  >"$log_file" 2>&1 &
pid=$!

wait_for_openapi

mv "$out_file.tmp" "$out_file"
cleanup
trap - EXIT

echo "Wrote normalized OpenAPI artifact to $out_file"
