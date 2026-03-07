# Data Retention Policy

## Account Deletion

When a user requests account deletion via `DELETE /users/{id}`:

1. **Immediate**: User is marked as soft-deleted. All active sessions are invalidated.
2. **30-day grace period**: User data is retained but inaccessible. The user cannot log in.
3. **After 30 days**: Full data purge is executed across all modules via `IUserDeletionParticipant` implementations.

## Data Export

Users can request a full data export via `GET /users/{id}/data-export`. This returns all user-associated data in structured JSON, excluding sensitive fields such as `ExternalConnectionId` and `ConsentSessionId` from bank connections.

## Retention by Module

| Module | Retention | Purge Behavior |
|--------|-----------|----------------|
| Auth | Until account deletion + 30 days | Sessions invalidated immediately, user record purged after grace period |
| Households | Indefinite (household-scoped) | Membership removed; household data retained for other members |
| Transactions | Indefinite (household-scoped) | User attribution anonymized |
| Budgets | Indefinite (household-scoped) | No user-specific data to purge |
| BillReminders | Indefinite (household-scoped) | No user-specific data to purge |
| BankConnections | Until account deletion + 30 days | Connections revoked and records purged |
| Notifications | Until account deletion + 30 days | Device tokens removed |
| Subscriptions | Indefinite (household-scoped) | No user-specific data to purge |
| Analytics | Until account deletion + 30 days | Events anonymized or deleted |
| Insights | N/A | Computed data; no persistent user-specific records |

## Session Data

- Access tokens: 15-minute lifetime
- Refresh tokens: 7-day lifetime
- Challenge tokens: 10-minute lifetime

## Audit Trail

Deletion requests are logged with the scheduled purge timestamp for compliance tracking.
