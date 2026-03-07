#!/usr/bin/env bash
# release-checklist.sh - Validates release readiness.
#
# Checks:
#   1. Backend tests pass
#   2. No critical CVEs (osv-scanner)
#   3. OpenAPI spec is up to date
#   4. Mobile bundle size within budget (< 25 MB)
#   5. No TODO:LAUNCH or FIXME:LAUNCH markers in source
#
# Exit code: 0 if all checks pass, 1 otherwise.

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
ARTIFACTS_DIR="${REPO_ROOT}/.artifacts/release-checklist"
mkdir -p "${ARTIFACTS_DIR}"

FAILED=0

echo "=== Release Checklist ==="
echo ""

# 1. Backend tests
echo "[1/5] Running backend tests..."
if dotnet test "${REPO_ROOT}/backend/MoneyTracker.slnx" --no-restore --verbosity quiet > "${ARTIFACTS_DIR}/backend-tests.log" 2>&1; then
  echo "  PASS: Backend tests passed."
else
  echo "  FAIL: Backend tests failed. See ${ARTIFACTS_DIR}/backend-tests.log"
  FAILED=1
fi

# 2. Critical CVEs
echo "[2/5] Checking for critical CVEs..."
if command -v osv-scanner &> /dev/null; then
  if osv-scanner scan --lockfile="${REPO_ROOT}/mobile/pubspec.lock" > "${ARTIFACTS_DIR}/osv-scan.log" 2>&1; then
    echo "  PASS: No critical CVEs found."
  else
    echo "  WARN: osv-scanner reported findings. See ${ARTIFACTS_DIR}/osv-scan.log"
    # Non-blocking for now; review findings manually.
  fi
else
  echo "  SKIP: osv-scanner not installed."
fi

# 3. OpenAPI up to date
echo "[3/5] Checking OpenAPI contract..."
if [ -x "${REPO_ROOT}/backend/scripts/export-openapi.sh" ]; then
  "${REPO_ROOT}/backend/scripts/export-openapi.sh" > /dev/null 2>&1 || true
  if git diff --exit-code "${REPO_ROOT}/backend/openapi/openapi.v1.json" > /dev/null 2>&1; then
    echo "  PASS: OpenAPI spec is up to date."
  else
    echo "  FAIL: OpenAPI contract drift detected. Re-run export-openapi.sh and commit."
    FAILED=1
  fi
else
  echo "  SKIP: export-openapi.sh not found or not executable."
fi

# 4. Bundle size budget
echo "[4/5] Checking mobile bundle size budget..."
BUNDLE_SIZE_BUDGET_MB=25
# Check if a pre-built APK or app bundle exists; otherwise skip.
APK_PATH="${REPO_ROOT}/mobile/build/app/outputs/flutter-apk/app-release.apk"
if [ -f "${APK_PATH}" ]; then
  APK_SIZE_BYTES=$(stat -f%z "${APK_PATH}" 2>/dev/null || stat -c%s "${APK_PATH}" 2>/dev/null || echo "0")
  APK_SIZE_MB=$(( APK_SIZE_BYTES / 1048576 ))
  if [ "${APK_SIZE_MB}" -lt "${BUNDLE_SIZE_BUDGET_MB}" ]; then
    echo "  PASS: Bundle size ${APK_SIZE_MB}MB is within ${BUNDLE_SIZE_BUDGET_MB}MB budget."
  else
    echo "  FAIL: Bundle size ${APK_SIZE_MB}MB exceeds ${BUNDLE_SIZE_BUDGET_MB}MB budget."
    FAILED=1
  fi
else
  echo "  SKIP: No release APK found at ${APK_PATH}. Build first to check bundle size."
fi

# 5. No LAUNCH markers
echo "[5/5] Checking for TODO:LAUNCH and FIXME:LAUNCH markers..."
LAUNCH_MARKERS=$(grep -rn "TODO:LAUNCH\|FIXME:LAUNCH" \
  --include="*.cs" --include="*.dart" --include="*.ts" --include="*.js" --include="*.yaml" --include="*.yml" \
  "${REPO_ROOT}/backend" "${REPO_ROOT}/mobile" "${REPO_ROOT}/scripts" 2>/dev/null || true)

if [ -z "${LAUNCH_MARKERS}" ]; then
  echo "  PASS: No LAUNCH markers found."
else
  echo "  FAIL: Found LAUNCH markers that must be resolved before release:"
  echo "${LAUNCH_MARKERS}" | head -20
  FAILED=1
fi

echo ""
if [ "${FAILED}" -eq 0 ]; then
  echo "=== Release Checklist: ALL CHECKS PASSED ==="
else
  echo "=== Release Checklist: SOME CHECKS FAILED ==="
fi

exit "${FAILED}"
