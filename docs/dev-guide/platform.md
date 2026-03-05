# Platform and Tooling Guide

Platform tasks include CI pipelines, repository automation, and development tooling.

## CI Expectations

CI pipelines must enforce:

Build  
Static analysis / lint  
Tests

## Security

CI must include baseline security checks.

Secret scanning  
Dependency vulnerability scanning

High or critical vulnerabilities must fail CI.

## Branch Protection

Main branches should require CI checks before merge.

PR workflows should run the same commands locally and in CI.

## Scope Rules

Platform workers should operate primarily within:

`.github/**`  
`scripts/**`  
root tooling configuration

Backend and mobile code should not be modified unless explicitly required.
