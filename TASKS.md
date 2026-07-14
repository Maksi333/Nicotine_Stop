# SnusStop — Task List

Changes to implement. Tasks are ordered so shared infrastructure comes before the
tasks that depend on it. **Task 0 must be done before Tasks 6 and 7.**

Stack reminder: .NET MAUI, Android, MVVM. When a task says "the X service", find the
existing equivalent in the codebase first — don't assume names.

---

## Task 0 — Shared: once-per-day XP claim system (prerequisite for Tasks 6 & 7)

**Why:** Multiple activities (mini-games, "Remind me why") should each grant XP only
once per calendar day, while still letting the user do the activity as often as they
like. Build this once, reuse it.

**Do:**
- Add a small service (e.g. `IDailyXpService`) that answers `CanClaimXp(activityId)`
  and `ClaimXp(activityId, amount)`.
- Persist the last-claimed **local date** per `activityId` (e.g. in preferences /
  local storage). Compare against today's local date, not UTC.
- `activityId` should be a stable key per activity: `"game.minesweeper"`,
  `"game.memory"`, `"game.reflex"`, `"remind_me_why"`, etc.
- When an activity finishes, it calls `ClaimXp`. If already claimed today, no XP is
  added and no XP toast/animation fires — but the activity itself still completes
  normally.

**Acceptance criteria:**
- [ ] First completion of an activity on a given local day awards XP.
- [ ] Every later completion of that same activity that day awards **0 XP** and shows
      no XP reward animation.
- [ ] A different activity on the same day is unaffected (independent per `activityId`).
- [ ] Crossing local midnight re-enables the XP for that activity.
- [ ] Survives app restart (persisted).

---

## Task 1 — Goals: money saved is counted incorrectly before a goal is added

**Why:** Home shows `25,18 kr` saved but the Goals page header shows `25 kr`, and the
running total on Goals looks wrong before any goal exists. Goals is using a different
(truncated / stale) value instead of the real saved total.

**Do:**
- Make the Goals page read the **same money-saved source** the Home page uses (same
  service / calculation), not a separate or rounded copy.
- Verify the "X kr saved" header on Goals matches Home exactly, including decimals and
  locale formatting (comma decimal separator, e.g. `25,18 kr`).
- Confirm the total is correct **when there are zero goals** — this is the reported
  broken case.
- Check that goal funding percentages ("5% funded", "475 kr to go") are computed from
  the correct saved total once goals exist.

**Acceptance criteria:**
- [ ] With no goals added, the Goals header equals the Home saved figure exactly.
- [ ] Decimals and locale formatting match Home.
- [ ] After adding a goal, funded % and "to go" amounts are derived from the correct
      saved total.

---

## Task 2 — Let the user choose their addiction: Snus or Cigarettes

**Why:** The app is currently snus-only. Copy, units, and the health timeline are all
snus-specific and don't fit cigarettes.

**Do:**
- Add an **addiction type** choice (Snus / Cigarettes) in setup / onboarding, and make
  it changeable later in **Settings → My Quit Plan**.
- Drive all user-facing terminology off this setting:
  - **Units:** Snus → "per can", "pouches"; Cigarettes → "per pack", "cigarettes".
    - Settings "Usage before quitting" and "Price per can" labels update accordingly
      ("Price per pack").
  - **Home card:** "pouches skipped" → "cigarettes not smoked" (or equivalent).
  - **Health timeline (Task-relevant copy):** the snus copy references gums/pouches
    ("Circulation and gum blood flow improving where pouches sat"). Provide a
    cigarette variant (lungs, breathing, circulation, taste/smell) for each milestone.
  - Any milestone/badge copy that names snus specifically.
- Keep the two copy sets data-driven (e.g. a resource/strings set keyed by addiction
  type) rather than sprinkling `if` checks through the UI.
- Existing users default to **Snus** so nothing breaks on upgrade.

**Acceptance criteria:**
- [ ] User can pick Snus or Cigarettes in setup and change it later in Settings.
- [ ] Units and labels (usage, price, skipped-count) reflect the choice everywhere.
- [ ] Health timeline shows addiction-appropriate copy for every milestone.
- [ ] Existing installs default to Snus with no data loss.

---

## Task 3 — Minesweeper: add press-and-hold as a second way to flag

**Why:** There's already a way to place flags. This adds an **additional**, faster
gesture — a short press-and-hold on a cell — without changing the existing method.

**Do:**
- Reuse the **existing** flag toggle logic — do not add a separate/parallel flag
  state, counter, or visuals. Long-press should call the same code path the current
  flag action uses.
- Add a **long-press** gesture on each unrevealed cell that toggles the flag on/off.
  Tune the hold duration to feel quick (roughly ~300–500 ms) — short enough to feel
  responsive, long enough not to fire on a normal tap.
- A long-press must **not** also trigger a reveal tap on the same cell (consume the
  gesture so tap and long-press don't both fire).
- Leave the existing flag method fully intact and working.

**Acceptance criteria:**
- [ ] The current flag method still works unchanged.
- [ ] A short press-and-hold on an unrevealed cell toggles its flag via the same
      existing logic (same visuals, same counter).
- [ ] A long-press does not also reveal the cell.
- [ ] Long-press on an already-flagged cell removes the flag.

---

## Task 4 — Memory Match: preview + selectable board sizes

**Why:** Game is too quick/easy; users should get a preview and a difficulty choice.

**Do:**
- **Preview:** at the start of a round, show all cards face-up for **5 seconds**, then
  flip them all face-down and start play. Ideally show a short countdown during the
  preview.
- **Board sizes:** add a size chooser before the round starts with (at least) three
  options — keep the current size as **Small**, and add **Medium** and **Large** with
  more pairs so rounds take longer.
  - Ensure the grid layout scales cleanly to the larger sizes (responsive columns/rows,
    no overflow on smaller devices).

**Acceptance criteria:**
- [ ] Every round begins with a 5-second all-cards-visible preview, then hides them.
- [ ] User can choose Small / Medium / Large before starting.
- [ ] Medium and Large have more pairs than Small and lay out correctly on device.

---

## Task 5 — Reflex Tap: high score + celebration when beaten

**Why:** No persistence or reward for improvement.

**Do:**
- Persist a **high score** for Reflex Tap (best result — confirm whether higher or
  lower is better for this game and store accordingly; e.g. fastest reaction time or
  most taps).
- Display the current high score on the game screen.
- When the user **beats** their high score, trigger a celebration (animation /
  confetti / sound consistent with the app's existing reward style) and update the
  stored value.

**Acceptance criteria:**
- [ ] High score persists across app restarts.
- [ ] High score is visible on the Reflex Tap screen.
- [ ] Beating it fires a celebration and saves the new best; matching or missing it
      does not.

---

## Task 6 — Mini-games award XP only once per day

**Depends on Task 0.**

**Why:** Prevent XP farming while still letting people play freely.

**Do:**
- Wire each mini-game's completion into `IDailyXpService` using its `activityId`
  (`game.minesweeper`, `game.memory`, `game.reflex`, and any others).
- On first completion that day: award XP as normal.
- On subsequent completions that day: game plays normally, **no XP**, no XP reward
  animation.

**Acceptance criteria:**
- [ ] Each game awards XP on its first completion per local day only.
- [ ] Replays that day give 0 XP but are otherwise fully playable.
- [ ] Games are independent of each other and of "Remind me why".

---

## Task 7 — "Remind me why" awards XP only once per day

**Depends on Task 0.**

**Why:** Same reasoning as Task 6.

**Do:**
- Route the "Remind me why" XP reward through `IDailyXpService` with
  `activityId = "remind_me_why"`.
- First use per local day awards XP; later uses that day award none but still work.

**Acceptance criteria:**
- [ ] First "Remind me why" of the local day awards XP.
- [ ] Later uses that day award 0 XP with no reward animation.
- [ ] Independent from the mini-games' daily XP.

---

## Suggested order

1. Task 0 (shared XP infra)
2. Tasks 6 & 7 (depend on 0)
3. Task 1 (self-contained bug fix)
4. Task 2 (largest surface area — do when you have a clean run)
5. Tasks 3, 4, 5 (independent game features, any order)
