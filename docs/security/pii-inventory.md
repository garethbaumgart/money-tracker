# PII Inventory

This document lists all personally identifiable information (PII) fields stored per module.

## Auth Module

| Field | Type | Storage | Notes |
|-------|------|---------|-------|
| Email | string | auth_users | Normalized, case-insensitive |
| UserId | GUID | auth_users | Primary identifier |
| AccessToken | string | auth_sessions | Short-lived |
| RefreshToken | string | auth_sessions | Rotated on refresh |

## Households Module

| Field | Type | Storage | Notes |
|-------|------|---------|-------|
| UserId | GUID | household_members | Foreign key to auth_users |
| InviteeEmail | string | household_invitations | Used for invitation matching |

## Transactions Module

| Field | Type | Storage | Notes |
|-------|------|---------|-------|
| CreatedByUserId | GUID | transactions | Attribution only |

## Budgets Module

No user-specific PII. Budget data is household-scoped.

## BillReminders Module

No user-specific PII. Bill reminders are household-scoped.

## BankConnections Module

| Field | Type | Storage | Notes |
|-------|------|---------|-------|
| CreatedByUserId | GUID | bank_connections | Attribution |
| ExternalUserId | string | bank_connections | Basiq user identifier |
| ExternalConnectionId | string | bank_connections | Basiq connection identifier |
| ConsentSessionId | string | bank_connections | Basiq consent session |
| InstitutionName | string | bank_connections | Bank name |

## Notifications Module

| Field | Type | Storage | Notes |
|-------|------|---------|-------|
| UserId | GUID | device_tokens | Token owner |
| DeviceId | string | device_tokens | Device identifier |
| Token | string | device_tokens | Push notification token |
| Platform | string | device_tokens | ios/android |

## Subscriptions Module

| Field | Type | Storage | Notes |
|-------|------|---------|-------|
| RevenueCatAppUserId | string | subscriptions | RevenueCat identifier |

## Analytics Module

| Field | Type | Storage | Notes |
|-------|------|---------|-------|
| UserId | GUID | activation_events | Event attribution |
| Platform | string | activation_events | Device platform |
| Region | string | activation_events | Optional geographic region |

## Insights Module

No user-specific PII. Insights are computed from transaction data.
