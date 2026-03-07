# UX Exploration Option Packs

Generated: 2026-03-07
Covers: Issues #65, #67, #74, #78, #79, #82, #86

---

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ISSUE #65: Bank Account Linking Flow
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

OPTION A: Dashboard CTA Card

  ┌──────────────────────────────┐
  │  MoneyTracker    [=] [Bell]  │
  ├──────────────────────────────┤
  │  Budget: $2,400 / $3,000     │
  │  ████████████░░░░  80%       │
  │                              │
  │  ┌──────────────────────┐    │
  │  │ 🏦 Link Your Bank    │    │
  │  │ Auto-sync your       │    │
  │  │ transactions          │    │
  │  │  [ Connect Now >>> ]  │    │
  │  └──────────────────────┘    │
  │                              │
  │  Recent Transactions         │
  │  > Groceries     -$45.20     │
  │  > Uber          -$12.00     │
  └──────────────────────────────┘

  User taps "Connect Now":
  ┌──────────────────────────────┐
  │  < Back    Link Bank         │
  ├──────────────────────────────┤
  │                              │
  │     🏦                       │
  │  We'll open your bank's     │
  │  secure login page.         │
  │                              │
  │  ✓ Read-only access          │
  │  ✓ 256-bit encryption        │
  │  ✓ Revoke anytime            │
  │                              │
  │  [ Continue to Bank  >>> ]   │
  │                              │
  │  Powered by Basiq            │
  └──────────────────────────────┘

  Success state:
  ┌──────────────────────────────┐
  │        Link Bank             │
  ├──────────────────────────────┤
  │                              │
  │          ✓                   │
  │   Bank Connected!            │
  │                              │
  │   ANZ Everyday Account       │
  │   ****4821                   │
  │                              │
  │  Transactions will sync      │
  │  within a few minutes.       │
  │                              │
  │  [ Back to Dashboard >>> ]   │
  └──────────────────────────────┘

✅ Pros: Minimal friction -- CTA is visible on the most-visited
   screen. One-tap entry. Card is dismissible after linking.
⚠️ Cons: Dashboard may feel cluttered for users who don't
   want bank linking. Card competes with budget summary
   for visual priority.


OPTION B: Dedicated Bank Screen + Bottom Nav

  ┌──────────────────────────────┐
  │  Bank Accounts       [+Add]  │
  ├──────────────────────────────┤
  │                              │
  │  ┌─────────────────────┐     │
  │  │  No banks linked     │    │
  │  │                      │    │
  │  │  Connect your bank   │    │
  │  │  to auto-sync txns   │    │
  │  │                      │    │
  │  │  [ + Link Bank ]     │    │
  │  └─────────────────────┘     │
  │                              │
  ├──────────────────────────────┤
  │ Home  Budget  🏦Bank  More   │
  └──────────────────────────────┘

  After linking:
  ┌──────────────────────────────┐
  │  Bank Accounts       [+Add]  │
  ├──────────────────────────────┤
  │  ┌──────────────────────┐    │
  │  │ ANZ Everyday  ● Live │    │
  │  │ ****4821             │    │
  │  │ Last sync: 2 min ago │    │
  │  └──────────────────────┘    │
  │                              │
  │  ┌──────────────────────┐    │
  │  │ + Link another bank  │    │
  │  └──────────────────────┘    │
  ├──────────────────────────────┤
  │ Home  Budget  🏦Bank  More   │
  └──────────────────────────────┘

✅ Pros: Clean separation of concerns. Scales well for
   multi-institution management. Easy to show connection
   health per bank. Non-intrusive on main dashboard.
⚠️ Cons: Lower discoverability -- user must find Bank tab.
   Extra navigation step. May delay activation if users
   don't explore all tabs.


OPTION C: Onboarding Step + Settings Fallback

  ┌──────────────────────────────┐
  │       Welcome!     2 of 3    │
  ├──────────────────────────────┤
  │                              │
  │     🏦                       │
  │  Link your bank for         │
  │  automatic tracking         │
  │                              │
  │  Your data stays private.    │
  │  We use read-only access.    │
  │                              │
  │  [ Connect Bank Account ]    │
  │                              │
  │  Skip for now                │
  │  ·  ●  ·                     │
  └──────────────────────────────┘

  If skipped, accessible from Settings:
  ┌──────────────────────────────┐
  │  < Back       Settings       │
  ├──────────────────────────────┤
  │  Account                     │
  │  ─────────────────────────── │
  │  > Bank Connections   None   │
  │  > Notifications             │
  │  > Export Data               │
  │  ─────────────────────────── │
  │  Subscription                │
  │  > Manage Plan       Free    │
  └──────────────────────────────┘

✅ Pros: Catches users at peak intent (first launch). High
   activation rate for users who complete onboarding.
   Settings fallback is natural and expected.
⚠️ Cons: Users may skip during onboarding and forget.
   Settings is low-traffic -- hard to re-engage skippers.
   Doesn't handle "link later" flow well.


OPTION D: Hybrid -- Dashboard Banner + Dedicated Screen

  ┌──────────────────────────────┐
  │  MoneyTracker    [=] [Bell]  │
  ├──────────────────────────────┤
  │ ┌────────────────────────┐   │
  │ │ 🏦 Connect bank → Auto │   │
  │ │ sync transactions  [>] │   │
  │ └────────────────────────┘   │
  │  Budget: $2,400 / $3,000     │
  │  ████████████░░░░  80%       │
  │                              │
  │  Recent Transactions         │
  │  > Groceries     -$45.20     │
  ├──────────────────────────────┤
  │ Home  Budget  🏦Bank  More   │
  └──────────────────────────────┘

  Banner is thin, dismissible. Tapping leads
  to the dedicated Bank screen (Option B).

✅ Pros: Best of both -- dashboard nudge for discoverability
   plus dedicated screen for management. Banner disappears
   after linking. Scales for multi-bank.
⚠️ Cons: Two UI surfaces to build and maintain. Banner may
   still feel like clutter to some users. Slightly more
   complex implementation.

💡 RECOMMENDATION: Option D because it maximizes activation
   (dashboard visibility) while providing a scalable home
   for connection management. The thin banner is less
   intrusive than Option A's card, and the dedicated screen
   solves Option B's discoverability problem.


━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ISSUE #67: Consent Failure & Re-consent
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

OPTION A: Inline Status Badges + Banner Warning

  ┌──────────────────────────────┐
  │  Bank Accounts       [+Add]  │
  ├──────────────────────────────┤
  │ ⚠ 1 connection needs action  │
  ├──────────────────────────────┤
  │  ┌──────────────────────┐    │
  │  │ ANZ Everyday         │    │
  │  │ ● Active  Exp: 82d  │    │
  │  │ Last sync: 5 min ago │    │
  │  └──────────────────────┘    │
  │                              │
  │  ┌──────────────────────┐    │
  │  │ Westpac Savings      │    │
  │  │ 🔴 Expired           │    │
  │  │ Consent expired 3d   │    │
  │  │ [ Reconnect >>> ]    │    │
  │  └──────────────────────┘    │
  ├──────────────────────────────┤
  │ Home  Budget  🏦Bank  More   │
  └──────────────────────────────┘

  Expiring soon state (7-day warning):
  ┌──────────────────────────────┐
  │  ┌──────────────────────┐    │
  │  │ ANZ Everyday         │    │
  │  │ 🟡 Expiring in 5d    │    │
  │  │ Renew to keep sync   │    │
  │  │ [ Renew Now ]        │    │
  │  └──────────────────────┘    │

✅ Pros: Status is always visible on bank screen. Color
   badges (green/amber/red) are universally understood.
   Banner draws attention to urgent items. Reconnect CTA
   is right where the user sees the problem.
⚠️ Cons: User must navigate to Bank screen to see issues.
   Passive -- relies on user checking the screen. May
   miss users who rarely visit Bank screen.


OPTION B: Push Notification + Full-Screen Blocker

  Notification (system tray):
  ┌──────────────────────────────┐
  │ MoneyTracker                 │
  │ ⚠ Your Westpac connection   │
  │ expires in 3 days. Tap to   │
  │ renew.                       │
  └──────────────────────────────┘

  Tapping opens blocking screen:
  ┌──────────────────────────────┐
  │      Connection Issue        │
  ├──────────────────────────────┤
  │                              │
  │       ⚠                      │
  │  Westpac access expired      │
  │                              │
  │  Your bank consent has       │
  │  expired. Transactions are   │
  │  no longer syncing.          │
  │                              │
  │  [ Reconnect Westpac >>> ]   │
  │                              │
  │  Remind me later             │
  └──────────────────────────────┘

✅ Pros: Proactive -- reaches user even when not in app.
   Full-screen makes the issue impossible to miss. Clear
   single action. "Remind me later" respects user choice.
⚠️ Cons: Full-screen blocker may feel aggressive for a
   non-critical issue. Notification fatigue risk. May
   annoy users who don't care about that account.


OPTION C: Dashboard Health Strip + Contextual Sheet

  ┌──────────────────────────────┐
  │  MoneyTracker    [=] [Bell]  │
  ├──────────────────────────────┤
  │  Sync Health:                │
  │  ANZ ●  Westpac 🔴  CBA ●   │
  ├──────────────────────────────┤
  │  Budget: $2,400 / $3,000     │
  │  ████████████░░░░  80%       │
  │                              │
  │  Recent Transactions         │
  │  > Groceries     -$45.20     │
  └──────────────────────────────┘

  Tapping red dot opens bottom sheet:
  ┌──────────────────────────────┐
  │  ──────                      │
  │  Westpac Savings             │
  │  Status: Consent Expired     │
  │  Expired: 3 days ago         │
  │                              │
  │  Your transactions are not   │
  │  syncing from this account.  │
  │                              │
  │  [ Reconnect >>> ]           │
  │  [ Dismiss ]                 │
  └──────────────────────────────┘

✅ Pros: At-a-glance health on the primary screen. Compact
   strip doesn't take much space. Bottom sheet gives
   detail on demand. Users see issues during normal use.
⚠️ Cons: Health strip may confuse new users who have no
   bank connections. Dots are small -- easy to miss on
   phones. Doesn't scale well beyond 4-5 banks.


💡 RECOMMENDATION: Option A combined with push notifications
   from Option B. The inline badge approach is the most
   natural location (on the Bank screen where users manage
   connections), while push notifications ensure proactive
   reach for expiry warnings. Avoid the full-screen blocker
   as it is too aggressive for a non-critical state.


━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ISSUE #74: Premium Insights Dashboard
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

OPTION A: Stacked Cards -- Bar Chart + Ring Gauge

  ┌──────────────────────────────┐
  │  < Back       Insights       │
  ├──────────────────────────────┤
  │  Spending Trends (30d)       │
  │  ┌──────────────────────┐    │
  │  │  ██     This month   │    │
  │  │  ██ ░░  Last month   │    │
  │  │  ██ ░░               │    │
  │  │  ██ ░░ ██            │    │
  │  │  ██ ░░ ██ ░░ ██      │    │
  │  │  Gro  Trn  Din  Ent  │    │
  │  └──────────────────────┘    │
  │                              │
  │  Budget Health               │
  │  ┌──────────────────────┐    │
  │  │    ╭───────╮         │    │
  │  │    │  78   │  Good   │    │
  │  │    ╰───────╯         │    │
  │  │  Adh: 85  Vel: 72    │    │
  │  │  Bills: 80           │    │
  │  └──────────────────────┘    │
  │                              │
  │  ⚠ Anomalies                 │
  │  ┌──────────────────────┐    │
  │  │ Dining  ↑ 68%        │    │
  │  │ $340 vs $202 last mo │    │
  │  └──────────────────────┘    │
  └──────────────────────────────┘

  Paywall teaser (free user):
  ┌──────────────────────────────┐
  │  < Back       Insights       │
  ├──────────────────────────────┤
  │  ░░░░░░░░░░░░░░░░░░░░░░░░   │
  │  ░░ Spending Trends ░░░░░░   │
  │  ░░░░░░ (blurred) ░░░░░░░   │
  │  ░░░░░░░░░░░░░░░░░░░░░░░░   │
  │                              │
  │  🔒 Unlock Premium Insights  │
  │                              │
  │  ✓ Spending trend analysis   │
  │  ✓ Budget health scoring     │
  │  ✓ Anomaly detection         │
  │                              │
  │  [ Upgrade to Premium >>> ]  │
  └──────────────────────────────┘

✅ Pros: Bar chart is familiar and easy to compare periods.
   Ring gauge is compact and visually distinct. Stacked
   cards allow scrolling through insights naturally. Good
   visual hierarchy.
⚠️ Cons: Vertical scrolling required to see everything.
   Ring gauge may feel small for the main health score.
   Bar chart gets crowded with many categories.


OPTION B: Tab Layout -- Line Chart + Large Number Score

  ┌──────────────────────────────┐
  │  < Back       Insights       │
  ├──────────────────────────────┤
  │  [ Spending ] [ Health ]     │
  ├──────────────────────────────┤
  │  7d | 30d | 90d              │
  │  ┌──────────────────────┐    │
  │  │     ╱╲               │    │
  │  │    ╱  ╲    ╱╲        │    │
  │  │   ╱    ╲──╱  ╲╱      │    │
  │  │  ╱                   │    │
  │  │  --- this  ... last  │    │
  │  └──────────────────────┘    │
  │  Total: $1,847  (-12%)  ↓   │
  │                              │
  │  Top Categories              │
  │  Groceries    $420  (23%)    │
  │  Transport    $310  (17%)    │
  │  Dining       $340  (18%)↑   │
  └──────────────────────────────┘

  Health tab:
  ┌──────────────────────────────┐
  │  [ Spending ] [ Health ]     │
  ├──────────────────────────────┤
  │                              │
  │         78                   │
  │       / 100                  │
  │     Budget Health            │
  │     ───── Good ─────         │
  │                              │
  │  Adherence   ████████░  85   │
  │  Velocity    ███████░░  72   │
  │  Bills Paid  ████████░  80   │
  │                              │
  │  Categories                  │
  │  Groceries     ● On Track    │
  │  Dining        ⚠ At Risk     │
  │  Transport     ● On Track    │
  └──────────────────────────────┘

✅ Pros: Tabs reduce scroll depth. Line chart shows trends
   over time clearly. Large number score is bold and
   immediately readable. Period selector (7/30/90d) is
   prominent. Progress bars for sub-scores are intuitive.
⚠️ Cons: Tabs hide content -- user must tap to see health.
   Line chart is harder to compare individual categories.
   Two-screen layout may reduce engagement with health
   score (out of sight, out of mind).


OPTION C: Single-Scroll Dashboard -- Area Chart + Gauge Arc

  ┌──────────────────────────────┐
  │  < Back       Insights       │
  ├──────────────────────────────┤
  │  Budget Health        78/100 │
  │  ┌──────────────────────┐    │
  │  │     ╭─── Good ───╮   │    │
  │  │   ╱▓▓▓▓▓▓▓▓▓▓░░╲   │    │
  │  │  0       78      100 │    │
  │  └──────────────────────┘    │
  │                              │
  │  Spending (30d)   ▼ Period   │
  │  ┌──────────────────────┐    │
  │  │  ╱╲▒▒▒▒▒            │    │
  │  │ ╱▒▒╲▒▒▒▒╱▒▒╲        │    │
  │  │╱▒▒▒▒▒▒▒╱▒▒▒▒╲▒▒     │    │
  │  │  ▒=this  ░=last      │    │
  │  └──────────────────────┘    │
  │  $1,847 total  (-12%) ↓     │
  │                              │
  │  ⚠ Dining up 68%  [View>]   │
  │                              │
  │  Groceries     ● $420        │
  │  Transport     ● $310        │
  │  Dining        ⚠ $340        │
  └──────────────────────────────┘

✅ Pros: Health score at top = first thing user sees (most
   actionable). Semi-circle gauge is visually rich. Area
   chart fills space well and shows volume. Anomaly inline
   alert is attention-grabbing. Everything on one scroll.
⚠️ Cons: Area chart is harder to read precise values. Gauge
   takes significant vertical space. Dense layout on
   smaller phones. May feel overwhelming for new users.


💡 RECOMMENDATION: Option B because tabs cleanly separate
   spending analysis from health scoring, reducing cognitive
   load. The line chart with period selector gives users
   control over their view. The large number score on the
   health tab is the most impactful way to communicate
   budget health at a glance. The tab structure also maps
   cleanly to the two API endpoints.


━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ISSUE #78: Experiment Variants
         (Onboarding + Paywall)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

OPTION A: Guided Wizard Onboarding + Early Paywall

  Onboarding variant "guided":
  ┌──────────────────────────────┐
  │         Step 1 of 4          │
  ├──────────────────────────────┤
  │                              │
  │     👋 Welcome!              │
  │                              │
  │  What's your top money       │
  │  goal?                       │
  │                              │
  │  ( ) Save more               │
  │  ( ) Track spending          │
  │  ( ) Pay off debt            │
  │  ( ) Budget as a couple      │
  │                              │
  │       [ Next >>> ]           │
  │  ·  ·  ·  ·                  │
  └──────────────────────────────┘

  Step 2: Set first budget
  ┌──────────────────────────────┐
  │         Step 2 of 4          │
  ├──────────────────────────────┤
  │                              │
  │  Set your monthly budget     │
  │                              │
  │  Total:  [ $________ ]       │
  │                              │
  │  Quick picks:                │
  │  [ $2,000 ] [ $3,000 ]      │
  │  [ $4,000 ] [ Custom  ]     │
  │                              │
  │       [ Next >>> ]           │
  │  ·  ●  ·  ·                  │
  └──────────────────────────────┘

  Paywall at step 4 (early):
  ┌──────────────────────────────┐
  │         Step 4 of 4          │
  ├──────────────────────────────┤
  │  Unlock the full experience  │
  │                              │
  │  ┌──────────────────┐        │
  │  │ Premium  $59/yr  │        │
  │  │ ✓ Bank sync      │        │
  │  │ ✓ Insights       │        │
  │  │ ✓ Anomaly alerts │        │
  │  └──────────────────┘        │
  │                              │
  │  [ Start Free Trial >>> ]    │
  │  Continue with Free          │
  │  ·  ·  ·  ●                  │
  └──────────────────────────────┘

✅ Pros: Structured guidance builds user commitment. Data
   collected early (goal, budget) personalizes experience.
   Paywall at end of wizard capitalizes on engagement peak.
   Progress dots show momentum.
⚠️ Cons: 4 steps may feel long -- drop-off risk at each
   step. Users eager to explore may abandon the wizard.
   Early paywall may feel pushy before user sees value.


OPTION B: Self-Serve Onboarding + Deferred Paywall

  Onboarding variant "self-serve":
  ┌──────────────────────────────┐
  │     Welcome to MoneyTracker  │
  ├──────────────────────────────┤
  │                              │
  │  You're all set! Here's      │
  │  what you can do:            │
  │                              │
  │  ┌────────┐  ┌────────┐     │
  │  │ + Add  │  │ 🏦Link │     │
  │  │ Budget │  │  Bank  │     │
  │  └────────┘  └────────┘     │
  │  ┌────────┐  ┌────────┐     │
  │  │ + Add  │  │ 👥Join │     │
  │  │ Txn    │  │ House  │     │
  │  └────────┘  └────────┘     │
  │                              │
  │  [ Go to Dashboard >>> ]     │
  └──────────────────────────────┘

  Paywall triggers after 3 days:
  (User opens Insights or hits feature gate)
  ┌──────────────────────────────┐
  │  🔒 Premium Feature          │
  ├──────────────────────────────┤
  │                              │
  │  You've been using           │
  │  MoneyTracker for 3 days!    │
  │                              │
  │  Upgrade to unlock:          │
  │  ✓ Spending insights         │
  │  ✓ Budget health score       │
  │  ✓ Anomaly alerts            │
  │                              │
  │  [ See Plans >>> ]           │
  │  Maybe later                 │
  └──────────────────────────────┘

✅ Pros: Minimal friction -- user gets to dashboard fast.
   Action cards let user self-select what matters. Deferred
   paywall lets user experience value first. Higher
   onboarding completion rate.
⚠️ Cons: Less data collected upfront -- no personalization.
   User may miss key setup steps. Deferred paywall means
   lower trial conversion in first session. Action cards
   may overwhelm new users with choices.


OPTION C: Progressive Disclosure + Contextual Paywall

  First launch -- single CTA:
  ┌──────────────────────────────┐
  │                              │
  │     💰                       │
  │  MoneyTracker                │
  │                              │
  │  Track spending, budget      │
  │  smarter, together.          │
  │                              │
  │  [ Get Started >>> ]         │
  │                              │
  └──────────────────────────────┘

  Dashboard with coaching tips:
  ┌──────────────────────────────┐
  │  MoneyTracker    [=] [Bell]  │
  ├──────────────────────────────┤
  │ ┌────────────────────────┐   │
  │ │ 💡 Tip: Add your first │   │
  │ │ budget to get started  │   │
  │ │ [ Create Budget > ] [x]│   │
  │ └────────────────────────┘   │
  │                              │
  │  No budget yet               │
  │                              │
  │  Recent Transactions         │
  │  (empty state)               │
  │                              │
  │  + Add Transaction           │
  └──────────────────────────────┘

  Contextual paywall (on feature tap):
  ┌──────────────────────────────┐
  │  ──────                      │
  │  🔒 Insights is Premium      │
  │                              │
  │  See where your money goes   │
  │  with spending trends and    │
  │  budget health scores.       │
  │                              │
  │  [ Unlock with Premium ]     │
  │  [ Start 14-Day Trial  ]    │
  │                              │
  └──────────────────────────────┘

✅ Pros: Zero-friction onboarding (one tap). Coaching tips
   guide progressively without blocking. Contextual paywall
   appears exactly when user wants the feature (highest
   intent). Bottom sheet is non-intrusive.
⚠️ Cons: No structured data collection. Users may ignore
   coaching tips. Contextual paywall may feel like many
   small blocks rather than one clear upgrade offer.
   Harder to measure single conversion funnel.


💡 RECOMMENDATION: Option C for onboarding (progressive
   disclosure has the highest completion rate) combined
   with Option A's structured paywall timing for the
   experiment control group. The experiment framework
   should test: C-onboarding + contextual-paywall vs
   A-wizard + early-paywall. This gives the clearest
   signal on whether guided structure or low-friction
   exploration converts better.


━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ISSUE #79: Trial & Restore Purchases
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

OPTION A: Banner Countdown + Settings Restore

  Dashboard with trial banner:
  ┌──────────────────────────────┐
  │  MoneyTracker    [=] [Bell]  │
  ├──────────────────────────────┤
  │ ┌────────────────────────┐   │
  │ │ ⭐ Premium Trial        │   │
  │ │ 11 days left           │   │
  │ │ [ Subscribe Now > ]    │   │
  │ └────────────────────────┘   │
  │                              │
  │  Budget: $2,400 / $3,000     │
  │  ████████████░░░░  80%       │
  │                              │
  │  Recent Transactions         │
  │  > Groceries     -$45.20     │
  └──────────────────────────────┘

  Trial expiring (3 days left):
  ┌──────────────────────────────┐
  │ ┌────────────────────────┐   │
  │ │ ⚠ Trial ends in 3 days │   │
  │ │ Don't lose Premium     │   │
  │ │ features!              │   │
  │ │ [ Subscribe >>> ]      │   │
  │ └────────────────────────┘   │

  Restore in Settings:
  ┌──────────────────────────────┐
  │  < Back       Settings       │
  ├──────────────────────────────┤
  │  Subscription                │
  │  ─────────────────────────── │
  │  Plan: Premium Trial (11d)   │
  │  ─────────────────────────── │
  │  > Manage Subscription       │
  │  > Restore Purchases         │
  │                              │
  │  Restoring...                │
  │  ┌────────────────────┐      │
  │  │  Checking stores...│      │
  │  │  ████████░░░░░░░░  │      │
  │  └────────────────────┘      │
  └──────────────────────────────┘

  Restore success:
  ┌──────────────────────────────┐
  │  ──────                      │
  │  ✓ Purchase Restored!        │
  │                              │
  │  Plan: Premium Annual        │
  │  Renews: Apr 15, 2026        │
  │                              │
  │  [ Done ]                    │
  └──────────────────────────────┘

✅ Pros: Dashboard banner keeps trial awareness persistent.
   Countdown creates gentle urgency without blocking.
   Restore in Settings is standard iOS/Android pattern
   (App Store review requirement). Clear success feedback.
⚠️ Cons: Banner takes space every session during trial.
   Settings-based restore is low discoverability -- users
   may contact support instead. No grace period messaging.


OPTION B: Trial Badge + Onboarding Restore + Grace Modal

  Dashboard with subtle badge:
  ┌──────────────────────────────┐
  │  MoneyTracker  [TRIAL] [=]   │
  ├──────────────────────────────┤
  │  Budget: $2,400 / $3,000     │
  │  ████████████░░░░  80%       │
  │                              │
  │  Recent Transactions         │
  │  > Groceries     -$45.20     │
  └──────────────────────────────┘

  Tapping "TRIAL" badge:
  ┌──────────────────────────────┐
  │  ──────                      │
  │  ⭐ Premium Trial             │
  │  11 days remaining           │
  │  ─────────────────────────── │
  │  You're enjoying:            │
  │  ✓ Bank sync                 │
  │  ✓ Spending insights         │
  │  ✓ Budget health score       │
  │  ─────────────────────────── │
  │  [ Subscribe Now     >>> ]   │
  │  [ Restore Purchases     ]   │
  └──────────────────────────────┘

  Grace period modal (after expiry):
  ┌──────────────────────────────┐
  │      Trial Ended             │
  ├──────────────────────────────┤
  │                              │
  │  Your 14-day trial has       │
  │  ended. You still have       │
  │  access for 3 more days.     │
  │                              │
  │  After that, you'll lose:    │
  │  ✗ Bank sync                 │
  │  ✗ Spending insights         │
  │  ✗ Budget health score       │
  │                              │
  │  [ Subscribe Now >>> ]       │
  │  Continue on Free            │
  └──────────────────────────────┘

✅ Pros: Badge is subtle -- less intrusive than banner.
   Tapping badge gives full context + both CTAs (subscribe
   and restore). Grace period modal is clear about what
   user loses. Restore is prominent (not buried in
   Settings).
⚠️ Cons: Badge may be too subtle -- users may not notice
   trial is active. Badge text changes needed for each
   state (TRIAL/EXPIRING/EXPIRED). Grace period adds
   complexity to state management.


OPTION C: Notification-Led + Paywall Restore

  Push notification (7 days into trial):
  ┌──────────────────────────────┐
  │ MoneyTracker                 │
  │ Your trial has 7 days left.  │
  │ Keep Premium features --     │
  │ subscribe now.               │
  └──────────────────────────────┘

  No persistent dashboard indicator.
  Paywall includes restore:
  ┌──────────────────────────────┐
  │  < Back       Upgrade        │
  ├──────────────────────────────┤
  │  ┌──────────────────────┐    │
  │  │ Annual    $59.99/yr  │    │
  │  │ BEST VALUE  Save 50% │    │
  │  │ $4.99/mo equivalent  │    │
  │  └──────────────────────┘    │
  │  ┌──────────────────────┐    │
  │  │ Monthly   $9.99/mo   │    │
  │  └──────────────────────┘    │
  │                              │
  │  [ Subscribe >>> ]           │
  │                              │
  │  Already subscribed?         │
  │  [ Restore Purchases ]       │
  │                              │
  │  Terms | Privacy             │
  └──────────────────────────────┘

✅ Pros: Clean dashboard -- no trial clutter. Notifications
   reach user at the right time. Restore on paywall is
   discoverable when user is considering subscribing.
   Matches Apple/Google best practices.
⚠️ Cons: No in-app trial awareness -- user forgets they
   have Premium. Notifications can be dismissed or
   disabled. No countdown creates less urgency. User
   may not know they're on a trial at all.


💡 RECOMMENDATION: Option B because the badge provides
   persistent but subtle trial awareness, and tapping it
   reveals both subscribe and restore CTAs in one sheet.
   The grace period modal from Option B should also be
   implemented as it clearly communicates the downgrade
   timeline. Supplement with push notifications from
   Option C at 3-day and 1-day marks.


━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ISSUE #82: Paywall Screen (Annual-First)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

OPTION A: Full-Screen Stacked Cards

  ┌──────────────────────────────┐
  │  [X]     Go Premium          │
  ├──────────────────────────────┤
  │                              │
  │  Unlock everything:          │
  │  ✓ Automatic bank sync      │
  │  ✓ Spending insights         │
  │  ✓ Budget health score       │
  │  ✓ Anomaly detection         │
  │                              │
  │  ┌──────────────────────┐    │
  │  │ ⭐ BEST VALUE         │    │
  │  │ Annual    $59.99/yr  │    │
  │  │ Just $4.99/month     │    │
  │  │ ██████████████████   │    │
  │  │ Save 58% vs monthly  │    │
  │  └──────────────────────┘    │
  │                              │
  │  ┌──────────────────────┐    │
  │  │ Monthly   $9.99/mo   │    │
  │  └──────────────────────┘    │
  │                              │
  │  [ Start 14-Day Trial >>> ]  │
  │                              │
  │  Restore  |  Terms  |  Priv  │
  └──────────────────────────────┘

✅ Pros: Feature list at top establishes value before price.
   Annual card is visually dominant (border, star, size).
   Monthly is de-emphasized. Clear savings callout. Single
   CTA button. Standard full-screen paywall pattern.
⚠️ Cons: Requires scrolling on shorter phones. Monthly
   option is almost hidden -- some users prefer it and
   may feel tricked. Feature list is generic text.


OPTION B: Toggle Switch -- Annual/Monthly Selector

  ┌──────────────────────────────┐
  │  [X]     Go Premium          │
  ├──────────────────────────────┤
  │                              │
  │    ┌───────────────────┐     │
  │    │ Annual | Monthly  │     │
  │    │ ██████   ░░░░░░░  │     │
  │    └───────────────────┘     │
  │                              │
  │  ┌──────────────────────┐    │
  │  │                      │    │
  │  │    $59.99 / year     │    │
  │  │    $4.99/mo          │    │
  │  │                      │    │
  │  │  🏷 Save 58%          │    │
  │  │                      │    │
  │  └──────────────────────┘    │
  │                              │
  │  ✓ Automatic bank sync      │
  │  ✓ Spending insights         │
  │  ✓ Budget health score       │
  │  ✓ Anomaly detection         │
  │                              │
  │  [ Start Free Trial >>> ]    │
  │  Restore Purchases           │
  └──────────────────────────────┘

  Toggle to monthly:
  ┌──────────────────────────────┐
  │    ┌───────────────────┐     │
  │    │ Annual | Monthly  │     │
  │    │ ░░░░░░   ██████   │     │
  │    └───────────────────┘     │
  │  ┌──────────────────────┐    │
  │  │    $9.99 / month     │    │
  │  └──────────────────────┘    │

✅ Pros: Toggle gives user agency -- feels less manipulative.
   Clean, focused layout with one price visible at a time.
   Annual is pre-selected (annual-first requirement met).
   Savings badge incentivizes staying on annual.
⚠️ Cons: Toggle interaction adds a step. User may toggle to
   monthly and stay there. Price comparison is less
   immediate since only one is visible. Some users may
   not realize they can toggle.


OPTION C: Side-by-Side Comparison + Feature Table

  ┌──────────────────────────────┐
  │  [X]     Choose Your Plan    │
  ├──────────────────────────────┤
  │                              │
  │  ┌───────┐    ┌───────┐     │
  │  │ Annual│    │Monthly│     │
  │  │⭐BEST │    │       │     │
  │  │$59.99 │    │ $9.99 │     │
  │  │ /year │    │  /mo  │     │
  │  │       │    │       │     │
  │  │$4.99  │    │       │     │
  │  │ /mo   │    │       │     │
  │  └───────┘    └───────┘     │
  │                              │
  │         Feature     Free Pro │
  │  ───────────────────────────  │
  │  Manual txns     ✓    ✓     │
  │  Basic budget    ✓    ✓     │
  │  Bank sync       ✗    ✓     │
  │  Insights        ✗    ✓     │
  │  Health score    ✗    ✓     │
  │  Anomaly alerts  ✗    ✓     │
  │                              │
  │  [ Start Free Trial >>> ]    │
  │  Restore  |  Terms           │
  └──────────────────────────────┘

✅ Pros: Side-by-side makes price comparison immediate.
   Feature table shows exactly what free users are missing.
   The "X vs checkmark" pattern is proven in SaaS paywalls.
   Both options visible -- feels transparent and honest.
⚠️ Cons: Side-by-side cards are small on mobile screens.
   Feature table adds length. Neither card feels dominant
   enough to steer toward annual. More visual noise.


OPTION D: Bottom Sheet Paywall (Compact)

  Feature gate triggers bottom sheet:
  ┌──────────────────────────────┐
  │                              │
  │  (current screen behind      │
  │   semi-transparent overlay)  │
  │                              │
  ├──────────────────────────────┤
  │  ──────                      │
  │  ⭐ Go Premium                │
  │                              │
  │  $59.99/yr  ($4.99/mo)      │
  │  Save 58% vs monthly         │
  │                              │
  │  [ Start Free Trial >>> ]    │
  │                              │
  │  or $9.99/month              │
  │                              │
  │  Restore | Terms | Privacy   │
  └──────────────────────────────┘

✅ Pros: Least disruptive -- user stays in context. Quick
   decision point without full screen takeover. Annual
   price is prominent. Monthly is text-only (de-emphasized).
   Fast to dismiss if not interested.
⚠️ Cons: Less room for feature list or comparison table.
   May feel too small for such an important decision.
   Easy to dismiss without engaging. Less "premium" feel
   for a premium purchase moment.


💡 RECOMMENDATION: Option A because the full-screen stacked
   card layout is the industry-proven pattern for mobile
   subscription paywalls. It gives maximum space for value
   proposition and creates clear visual hierarchy with
   annual as the hero. Option D (bottom sheet) should be
   the A/B test variant to test whether lower friction
   outperforms higher immersion.


━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ISSUE #86: In-App Feedback & NPS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

OPTION A: Settings Entry + Multi-Step Form

  Settings screen:
  ┌──────────────────────────────┐
  │  < Back       Settings       │
  ├──────────────────────────────┤
  │  Support                     │
  │  ─────────────────────────── │
  │  > Send Feedback             │
  │  > Rate Us on App Store      │
  │  > Help Center               │
  │  ─────────────────────────── │
  └──────────────────────────────┘

  Feedback form (step 1 -- category):
  ┌──────────────────────────────┐
  │  < Back     Send Feedback    │
  ├──────────────────────────────┤
  │  What's on your mind?        │
  │                              │
  │  ┌──────────────────────┐    │
  │  │  🐛 Bug Report        │    │
  │  └──────────────────────┘    │
  │  ┌──────────────────────┐    │
  │  │  💡 Feature Request   │    │
  │  └──────────────────────┘    │
  │  ┌──────────────────────┐    │
  │  │  💬 General Feedback  │    │
  │  └──────────────────────┘    │
  └──────────────────────────────┘

  Step 2 -- detail:
  ┌──────────────────────────────┐
  │  < Back   Bug Report         │
  ├──────────────────────────────┤
  │  Tell us what happened:      │
  │  ┌──────────────────────┐    │
  │  │                      │    │
  │  │                      │    │
  │  │                      │    │
  │  └──────────────────────┘    │
  │  How would you rate your     │
  │  experience?                 │
  │  ☆ ☆ ☆ ☆ ☆                  │
  │                              │
  │  [ Submit Feedback >>> ]     │
  └──────────────────────────────┘

  NPS prompt (dialog):
  ┌──────────────────────────────┐
  │  ┌──────────────────────┐    │
  │  │ How likely are you   │    │
  │  │ to recommend us?     │    │
  │  │                      │    │
  │  │ 0 1 2 3 4 5 6 7 8 9 10  │
  │  │ Not        Very      │    │
  │  │ likely     likely    │    │
  │  │                      │    │
  │  │ [ Submit ]  [Skip]   │    │
  │  └──────────────────────┘    │
  └──────────────────────────────┘

✅ Pros: Multi-step keeps each screen simple. Category
   selection upfront routes feedback correctly. Star
   rating is universally understood. Settings entry is
   expected location. NPS dialog is standard pattern.
⚠️ Cons: Multi-step means more taps to submit. Settings
   entry is low discoverability. NPS numbered scale
   (0-10) is small and hard to tap on mobile. May get
   low submission rates due to friction.


OPTION B: FAB (Floating Action Button) + Single-Screen Form

  Any screen with FAB:
  ┌──────────────────────────────┐
  │  MoneyTracker    [=] [Bell]  │
  ├──────────────────────────────┤
  │  Budget: $2,400 / $3,000     │
  │  ████████████░░░░  80%       │
  │                              │
  │  Recent Transactions         │
  │  > Groceries     -$45.20     │
  │  > Uber          -$12.00     │
  │                              │
  │                      [💬]    │
  └──────────────────────────────┘

  Tapping FAB opens form sheet:
  ┌──────────────────────────────┐
  │  ──────                      │
  │  Send Feedback               │
  │                              │
  │  [Bug] [Feature] [General]   │
  │                              │
  │  ┌──────────────────────┐    │
  │  │ Describe...          │    │
  │  │                      │    │
  │  └──────────────────────┘    │
  │                              │
  │  Rate:  ★ ★ ★ ☆ ☆           │
  │                              │
  │  [ Submit >>> ]              │
  └──────────────────────────────┘

  NPS as bottom sheet with slider:
  ┌──────────────────────────────┐
  │  ──────                      │
  │  Quick question...           │
  │                              │
  │  How likely to recommend?    │
  │                              │
  │  0──────●──────────────10    │
  │         7                    │
  │                              │
  │  Tell us more (optional):    │
  │  ┌──────────────────────┐    │
  │  │                      │    │
  │  └──────────────────────┘    │
  │  [ Submit ]    [ Not now ]   │
  └──────────────────────────────┘

✅ Pros: FAB is accessible from any screen -- maximum
   discoverability. Single-screen form reduces friction.
   Chip selector for category is fast. Slider for NPS is
   easier to use than 11 small buttons. Everything in
   bottom sheets keeps user in context.
⚠️ Cons: FAB competes with other floating elements. Always-
   visible FAB may feel intrusive. Bottom sheet limits
   vertical space for description. Slider value is harder
   to set precisely than tapping a number.


OPTION C: Contextual Prompt + Minimal Star Rating

  After completing an action (e.g., adding transaction):
  ┌──────────────────────────────┐
  │  ──────                      │
  │  How was that?               │
  │                              │
  │  ★ ★ ★ ★ ★                  │
  │                              │
  │  [ Tell us more ]   [ Done ] │
  └──────────────────────────────┘

  "Tell us more" expands:
  ┌──────────────────────────────┐
  │  < Back     Feedback         │
  ├──────────────────────────────┤
  │  You rated: ★ ★ ★ ☆ ☆       │
  │                              │
  │  Category:                   │
  │  [Bug] [Feature] [General]   │
  │                              │
  │  ┌──────────────────────┐    │
  │  │ What could be better?│    │
  │  │                      │    │
  │  └──────────────────────┘    │
  │                              │
  │  [ Submit >>> ]              │
  └──────────────────────────────┘

  NPS as full-width number row:
  ┌──────────────────────────────┐
  │  ──────                      │
  │  How likely to recommend     │
  │  MoneyTracker to a friend?   │
  │                              │
  │  ┌─┬─┬─┬─┬─┬─┬─┬─┬─┬─┬──┐  │
  │  │0│1│2│3│4│5│6│7│8│9│10│  │
  │  └─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴──┘  │
  │  Unlikely       Very likely  │
  │                              │
  │  Why? (optional)             │
  │  ┌──────────────────────┐    │
  │  │                      │    │
  │  └──────────────────────┘    │
  │  [ Submit ]     [ Skip ]     │
  └──────────────────────────────┘

✅ Pros: Contextual prompt captures sentiment at the moment
   of experience -- higher response quality. Minimal star
   rating is fast (one tap). "Tell us more" is optional,
   reducing friction. Full-width NPS numbers are large
   enough to tap. Two-step progressive disclosure.
⚠️ Cons: Contextual prompts may interrupt flow if poorly
   timed. Requires logic to decide when to show prompt
   (not every action). Star rating alone lacks detail.
   Users may tap "Done" and never give written feedback.


OPTION D: Shake-to-Feedback + Emoji NPS

  Shake device or long-press in Settings:
  ┌──────────────────────────────┐
  │  ──────                      │
  │  📸 Screenshot captured      │
  │                              │
  │  What's happening?           │
  │  [Bug] [Feature] [General]   │
  │                              │
  │  ┌──────────────────────┐    │
  │  │ Describe the issue...│    │
  │  └──────────────────────┘    │
  │                              │
  │  [ Send >>> ]   [ Cancel ]   │
  └──────────────────────────────┘

  Emoji-based NPS:
  ┌──────────────────────────────┐
  │  ──────                      │
  │  How's MoneyTracker          │
  │  working for you?            │
  │                              │
  │  😡  😕  😐  🙂  😍          │
  │                              │
  │  Why? (optional)             │
  │  ┌──────────────────────┐    │
  │  │                      │    │
  │  └──────────────────────┘    │
  │  [ Submit ]     [ Skip ]     │
  └──────────────────────────────┘

✅ Pros: Shake-to-report is delightful and captures context
   (screenshot). Emoji scale is fun, fast, and requires
   zero explanation. Very low friction -- even a single
   emoji tap is useful data. Feels modern and playful.
⚠️ Cons: Shake gesture is not discoverable -- users won't
   know about it. Emoji scale is not a true NPS (0-10)
   and can't be benchmarked against industry standards.
   5-point emoji scale loses NPS granularity. Screenshot
   capture adds technical complexity.


💡 RECOMMENDATION: Option C because contextual prompts
   capture feedback at the moment of highest relevance,
   leading to higher quality responses. The progressive
   disclosure (star rating first, detail optional) balances
   low friction with rich data collection. The full-width
   number row for NPS maintains industry-standard 0-10
   scale while being tappable on mobile. Add the Settings
   entry from Option A as a fallback for deliberate
   feedback.
```
