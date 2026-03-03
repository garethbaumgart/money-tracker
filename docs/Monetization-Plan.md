# Monetization Plan (Locked v1)

## Summary
1. Launch a couples/families money tracker in Australia and New Zealand.
2. Ship MVP in 8-12 weeks with monetization enabled from first release.
3. Use a freemium model with one premium subscription tier and trial-to-paid conversion as the main KPI.

## Product and Market Decisions
1. Primary audience: families/couples.
2. Launch markets: Australia and New Zealand first.
3. Mobile strategy: cross-platform app.
4. Collaboration scope in MVP: core sharing only.

## Revenue Strategy
1. Model: freemium with a single premium tier.
2. Billing platform: RevenueCat.
3. North-star metric: paid conversion.
4. Retention guardrail: optimize conversion without degrading retained active users.

## Value Packaging
1. Free tier focus: shared visibility and basic budgeting.
2. Premium tier focus: automation, richer syncing capability, and deeper insights.
3. Monetization trigger: users see clear value in shared financial coordination before paywall pressure.

## Banking Integration Strategy and Risk
1. Primary provider path: Basiq for AU + NZ.
2. Contingency: Akahu fallback if NZ pilot quality targets are not met.
3. Pilot gate: validate link success, sync reliability, and support burden before full scale.

## Monetization Acceptance Criteria
1. Subscription purchase and entitlement flow works end to end.
2. Trial, renewal, restore purchase, and cancellation states are handled correctly.
3. Paywall behavior is consistent across iOS and Android.
4. KPI instrumentation exists for funnel stages from activation to paid.

## Assumptions and Defaults
1. Team size remains 1-2 builders.
2. RevenueCat remains billing source-of-truth abstraction.
3. Provider onboarding and commercial terms are feasible inside the MVP timeline.
