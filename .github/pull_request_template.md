## Summary

- [ ] OpenAPI artifact is up to date for this API change.

## Contracts

- [ ] When API endpoint contracts change, run:
  - `./backend/scripts/export-openapi.sh`
  - Verify `backend/openapi/openapi.v1.json` changed intentionally.

Before merge, ensure the CI check `openapi-contract-check` is present and passing.

## Notes

- If contract changes are not committed, CI OpenAPI drift check should fail and block merge.
