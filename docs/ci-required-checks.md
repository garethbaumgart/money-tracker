# CI Required Checks

This repository's quality gate workflow lives in `.github/workflows/ci.yml`.

## Required Status Checks for `main`

Configure branch protection to require the following checks:

<!-- required-checks:start -->
- backend-quality
- mobile-quality
- security-baseline
- openapi-contract-check
<!-- required-checks:end -->

`required-checks-doc-parity` is a documentation parity guard and can be enabled as an additional required check.

## Branch Protection Setup

1. Open GitHub repository settings.
2. Navigate to **Branches** and add or edit a rule for `main`.
3. Enable **Require a pull request before merging**.
4. Enable **Require status checks to pass before merging**.
5. Add each required check listed above by exact name.
6. Save the branch protection rule.

## Local Command Parity

Run the same CI scripts locally from repository root:

```bash
./scripts/ci/backend-quality.sh
./scripts/ci/mobile-quality.sh
./scripts/ci/security-baseline.sh
./scripts/ci/validate-required-checks.sh
```

Security baseline prerequisites:

- `gitleaks`
- `osv-scanner`
- `jq`
