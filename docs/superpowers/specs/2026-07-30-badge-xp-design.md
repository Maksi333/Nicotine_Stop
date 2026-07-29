# SnusStop — 100 XP per badge (design)

**Date:** 2026-07-30
**Status:** Approved (design), pending implementation plan
**Scope:** Badge XP awards, their persistence, and the in-app unlock toast. No changes to badge
definitions, badge unlock conditions, level thresholds, or existing XP amounts.

## Goal

Every badge the user unlocks pays **100 XP**, once. Today badges pay nothing: `BadgeService`
derives them from stats + events on every read, and no XP ever changes hands.

## Decisions

| Question | Decision |
|---|---|
| Which achievements pay? | All badges — whatever `BadgeService.Earned` returns. Not the Home-ring milestones separately (they overlap the time badges). |
| Retroactive XP? | **No.** Badges already earned before this ships pay nothing. |
| Backdated quit date on a fresh install? | **Pays.** Someone who sets a quit date 3 weeks back earns 1 day / 3 days / 1 week / 2 weeks at once and is paid for all four. |
| Feedback | An in-app toast per unlock; several at once collapse into one combined toast. |
| Amount | 100 XP flat, every badge. No tiering by difficulty. |

## Known gap (not fixed here)

Three of the 24 badges are unreachable today, independent of XP:

- `goal1` / `goal3` count `EventType.GoalFunded` events. **Nothing in the app ever writes one** —
  `GoalsViewModel` derives funded state from savings allocation on the fly.
- `weekly1` — its own comment in `BadgeService` says the platform grants it when the first weekly
  summary is delivered. Nothing does.

So 21 badges can pay out. Making the other three reachable is separate work; this design neither
depends on it nor blocks it (once they become earnable, they pay 100 XP with no further changes).

## Architecture

### Record of payment: the event log

A `BadgeEarned` event in the existing log is the record that a badge has already paid.
`EventType.BadgeEarned` is already declared and unused; `EventLog.Source` carries the badge key.

Chosen because total XP is already `Events.Sum(e => e.XpDelta)`, so the XP bar, level, level-up
copy, `BadgeService`'s own `level5` check, and the CSV export all pick badge XP up with **no
schema change and no second source of truth**. Idempotency is structural: a key present in the log
never pays again.

Rejected alternatives:

- **Granted keys in `Preferences`** — splits truth between prefs and the log, and badge unlocks
  disappear from history and the CSV export.
- **Purely derived (`100 × earnedCount`, nothing stored)** — no writes and trivially idempotent,
  but it is retroactive by construction (contradicts the decision above) and XP would *drop* when
  a slip revokes the day badges, so a user could lose a level for slipping once.

### Core (`SnusStop.Core`, unit-testable, no MAUI dependency)

- `XpService.BadgeXp = 100` — alongside the existing award constants.
- `BadgeAwardService.Pending(Profile p, IEnumerable<EventLog> events, Stats s) : IReadOnlyList<Badge>`
  — badges that are currently earned but have no `BadgeEarned` event yet, matched on
  `e.Type == BadgeEarned && e.Source == badge.Key`. Pure function, canonical badge order.

### App layer

- `EventLog.BadgeEarned(DateTime nowUtc, string key, int xp)` factory, matching the existing
  `CheckIn` / `CravingWon` / `Slip` factories.
- `AppState.SyncBadgesAsync()`:
  1. Guard against re-entrancy (`AddEventAsync` raises `Changed`, which is itself a trigger).
  2. Loop up to 3 passes: compute `Pending`, stop when empty, else append a `BadgeEarned` event per
     badge at `XpService.BadgeXp` and collect them. The loop exists because badge XP can push the
     user over a level threshold and unlock `level5`, which then pays in the next pass.
  3. Raise `event EventHandler<IReadOnlyList<Badge>>? BadgesEarned` with everything granted.

  Awarding goes through the existing `AddEventAsync`, so `TotalXp` and `Changed` stay correct.

- **Baseline (implements "no retroactive XP")**: a `Preferences` flag, `badge_xp_baseline_v1`.
  On the first sync after the update, *if the event log is non-empty* (an install with history
  predating this feature), currently-earned badges are recorded as `BadgeEarned` with
  `XpDelta = 0` and no toast — they are marked paid without paying. Set the flag either way.

  A fresh install reaches its first sync with an empty log, so the baseline is a no-op and every
  badge pays as it lands — including the ones a backdated quit date grants immediately.

- **Trigger points** for `SyncBadgesAsync`:
  1. `MainTabsPage.OnAppearing` — app start (which runs right after the state is loaded) and every
     return to the tabs, including a dismissed modal.
  2. Every `AppState.Changed` — covers check-in, craving won, slip and profile edits in one hook.
     The sync's own writes raise `Changed` too, which the re-entrancy guard absorbs.
  4. `ClockService.Tick`, throttled to at most once per minute — so "1 day free" lands while the
     app sits open. A minute is the coarsest granularity that still feels immediate for a
     day-boundary event.

### UI

`Controls/BadgeToast.cs`, styled after the existing rounded card controls (`StatCard`):
badge emoji, badge label, `+100 XP`. Slides/fades in at the top of the content area,
auto-dismisses after ~3.5 s, tap dismisses early.

Hosted as an overlay in `MainTabsPage`'s root `Grid` (top row, `VerticalOptions="Start"`) so it
shows on any tab. `MainTabsPage` subscribes to `AppState.BadgesEarned`.

- Multiple badges queue and show one after another.
- **3 or more at once collapse into a single toast** — `🏆 5 badges unlocked · +500 XP` — instead
  of a 20-second parade. This is the normal case for a backdated quit date.
- While a modal is on the stack (SOS takeover, Settings), toasts are held and flushed on the next
  `OnAppearing`, so a badge never fires behind a craving takeover.

## Behavior at the edges

- **Slip revokes a badge.** Day-based badges are computed from the last slip, so a slip can make
  "1 week" un-earned again. The 100 XP is **kept** (no clawback) and the badge **does not pay
  again** when re-earned — the log entry stands. The Journey grid re-locking the badge while the
  XP remains is pre-existing behavior and unchanged.
- **Level cascade.** Badge XP counts toward the level, so it can unlock `level5`, which pays 100
  XP itself. The bounded loop settles this in one extra pass; it cannot run away because
  `level5` can only be granted once.
- **Reinstall / data wipe.** A cleared database is a fresh install: badges are re-earned from the
  restored profile and pay again. Acceptable — there is no cloud state to reconcile against.
- **Toast while onboarding.** Sync only runs once `MainTabsPage` exists, so no toast can appear
  over the onboarding flow.

## Testing

**Core unit tests** (`SnusStop.Core.Tests/BadgeAwardServiceTests.cs`):

- No badges earned → nothing pending.
- Badge earned, no `BadgeEarned` event → pending.
- Badge earned and already recorded → not pending (no double pay).
- Badge recorded at `XpDelta = 0` (baselined) → not pending.
- Slip-revoked badge that was already paid → not pending when re-earned.
- `XpService.BadgeXp == 100`, and 100 XP from a badge moves the level as expected.

**Manual verification** (emulator, per the project's usual `-t:Install` flow):

- Fresh install → onboarding with a quit date ~3 weeks back → one combined toast, XP bar and
  level jump by the expected amount.
- Force-stop and relaunch → no toast, XP unchanged (no double pay).
- Win a craving to cross `cravings10`, confirm a single-badge toast reads the badge label + 100 XP.

## Non-goals

- No change to badge definitions, unlock thresholds, or the Journey grid layout.
- No use of the unused `MilestoneXp` / `ChecklistXp` constants.
- No notification (system tray) on badge unlock — in-app toast only.
- No per-badge XP tiering.
