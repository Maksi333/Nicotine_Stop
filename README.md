# Handoff: SnusStop — Quit Snus App (Android)

## Overview
SnusStop is a gamified quit-snus/nicotine-pouch app for Android (.NET MAUI target). It tracks time since quitting, money saved, and pouches avoided; funds user-defined goals with the savings; shows a health-recovery timeline; awards XP/levels/badges; and provides an always-reachable "Craving SOS" takeover with breathing, distraction games (incl. Minesweeper), and motivation reminders. Slips are logged without shame, restart the clean-day counter and streak, and deduct any entered spending from savings. Earned badges and XP are kept.

## About the Design Files
The files in this bundle are **design references created in HTML** — prototypes showing intended look and behavior, not production code to copy directly. The task is to **recreate these designs in the target codebase's environment** (.NET MAUI, or whatever framework the team chooses if none exists yet) using its established patterns and libraries.

`SnusStop UI.dc.html` is a design-canvas document containing all 25 frames side by side. Each frame is a 412×892 Android screen inside a device bezel (the bezel itself — `android-frame.jsx` — is scaffolding, not part of the app). Frames are identified by badge ids referenced throughout this README (1a, 1h, 3a, …).

## Fidelity
**High-fidelity.** Colors, typography, spacing, radii, and copy are final unless noted. Recreate pixel-perfectly, adapting only to platform conventions (system status bar, back navigation, etc.). The mascot "Puff" is a **placeholder** (simple bubble face drawn with primitives) — final brand art will replace it, so implement it as a swappable image/component. Goal photos use striped placeholder boxes — real user photos at runtime.

## Canonical screens (build exactly one Home, in two themes)
Duplicates were removed by design. The canonical set:

### Onboarding wizard (6 steps, progress bar top: back chip 40px circle + 12px track + "n/6")
- **1a Welcome** — Puff mascot 120px (floating anim), "SNUSSTOP" overline 13px/900/2px tracking in primary green, H1 32px/900 "Quit snus. Keep the money.", sub 16px muted, page dots, primary CTA "Let's do this".
- **1b Name** — H1 28px/900, text field 60px, 18px radius, 2.5px primary border, keyboard visible.
- **1c Usage habits** — stepper cards (white, 20px radius, 1.5px #E3EEE7 border): pouches/day and pouches/can. −/+ buttons 44×44, 14px radius (minus: #E9F3EC bg + #D0E2D6 bottom-shadow; plus: primary + #0E8A50 bottom-shadow). Live mascot tip banner #EAF7F0.
- **1d Cost** — price input 72px tall, currency chip row (DKK/SEK/NOK/EUR, selected = primary bg + bottom-shadow). Gold info banner #FFF6E3 with per-pouch/per-day math.
- **1e Quit date** — segmented toggle "I already quit / Pick a future date" (#E9F3EC track, white selected pill), calendar card (selected day = primary pill), time field + "THAT MAKES TODAY Day N 🎉" card.
- **1f Motivation** — 2-col grid of selectable cards (selected: 2.5px primary border + ✓ badge top-right; icons in 40px tinted squares).
- **1g Summary** — full primary-green screen, confetti dots, big "IN ONE YEAR {amount}" card (white 14% overlay, 24px radius), per-week/per-month cards, white CTA "Start my quit 🎉". Note: "Slips won't erase your progress."

### Main app — 4 tabs + docked SOS
Bottom nav: white bar, 1.5px #E9EFEA top border; tabs Home 🏠, Goals 🎯, Health ❤️, Journey 🏅 (inactive: grayscale + 50% opacity, label #8AA398; active: label primary 900). Center: **SOS button** — 58px coral circle, `margin-top:-24px` so it overhangs, 4px border in page bg color, `0 4px 0 #D94F44` bottom-shadow, slow pulse animation. Opens the SOS takeover from anywhere.

- **1h Home (light)** — header: Puff avatar 40px **with pink cheeks** + "Hey Mikkel 👋" + LVL pill (gold). Hero: 232px SVG progress ring (17px stroke, track #E3EEE7, fill primary, round caps) showing progress toward next milestone; center = "NICOTINE-FREE" overline, day count 52px/900, live hh:mm:ss ticker in primary. Milestone chip below ("N days until 2 weeks free"). Three stat cards: money saved (live, green), pouches skipped, day streak (🔥 #E8590C). XP card: "Level 4 · Fresh Air", 340/500 XP, 14px gold bar (#FFB627 on #F2EBDA), "✓ Daily check-in done · +10 XP" chip.
- **1j Home (dark)** — same layout. Dark palette below.
- **1l Goals (light) / 3a (dark)** — header + "{saved} kr saved" pill; "Split savings between goals" toggle row; goal cards (74px photo slot, name + price, 16px progress bar, "% funded" / "N kr to go"); dashed "+ Add a goal" button; TROPHY SHELF: completed goal in gold card ("Funded on day 9. Paid for by 156 skipped pouches.").
- **1m Goals empty state** (light only) — mascot + 💭, "What will freedom buy?", suggestion chips, CTA "Add my first goal".
- **1n Health (light) / 3b (dark)** — header with overall recovery donut (38%, coral); vertical timeline: 20 min ✓, 72 h ✓, 1 week ✓ (green check nodes + green connector), 2 weeks IN PROGRESS (highlighted card, 2px primary border + own progress bar), then locked 🔒 rows at 62% opacity (1 month, 3 months, 1 year 🏆).
- **1o Journey (light) / 3c (dark)** — "6 of 24 badges"; badge grid 64px circles (earned: #EAF7F0 bg + 3px primary ring; newest: gold + ✨ pulse; locked: grayscale 45%); month calendar card (clean day = mint pill, today = primary pill, craving-won = gold dot under date, slip = #FFD1CC per legend); footer insight line.

### Craving SOS (chosen direction: **1p calm dark takeover**, bg #232140)
- **1p SOS home** — ✕ close, "You've got this.", "Cravings feel huge but usually pass in 3–5 minutes." Three big option cards with bottom-shadows: **Breathe** (lavender #8B7CF6/#6C5CE0, pulsing dot), **Distract me** (blue #4C9EF5/#2E75C4, 🎲), **Remind me why** (green). Quiet footer link "I slipped — log it, no shame".
- **2a Game picker** — "Give your hands something to do." Cards: **Minesweeper** (blue, "Classic logic sweep · 3–5 min", +15 XP), **Pouch Pop** (lavender, endless bubble wrap, +10 XP), **Memory Match** (green, +10 XP), **Reflex Tap** (gold, 60 s, +10 XP). XP chips top-right of each card.
- **2b Minesweeper** — stat chips (💣 mines left, ⏱️ timer labeled "CRAVING FADING…"); 8×8 board in dark inset panel (20px radius): hidden cells lavender checkerboard (#8B7CF6/#7C6CEC + inset bottom-shadow), revealed cells #2E2B52/#2A2749 with colored numbers (1 #8FB8F8, 2 #7EE0A9, 3 #FF9D94), 🚩 flags; Dig/Flag mode toggle buttons; footer "The craving passed — I'm okay ›".
- **1r Breathe** — 4-7-8 exercise: three concentric lavender circles scaling with a 19s keyframe cycle (in 4s → hold 7s → out 8s), phase label + count, cycle dots "1 of 4", exit button.
- **1s Remind me why** — deep green bg #0E3B2A; user's own written reasons as quote cards; white goal card showing nearest goal + "One can skipped = 45 kr closer. This craving is worth money."
- **1t Craving defeated** — full green celebration, confetti, happy-eyes Puff, "That's the 23rd craving you've beaten", +15 XP and total-wins chips, white CTA.
- **1u I slipped** — light, judgment-free. "Slips happen." Two-column keep/reset cards (KEEP: net savings after spending, badges/XP — green; RESETS: clean-day counter and streak — coral). Optional amount spent in the selected currency (blank = zero, comma or dot decimals), with a savings preview. Optional trigger chips (Party/Stress/Coffee/After a meal/Boredom/Friends using; selected = dark ink pill), note field, dark CTA "Log it & keep going", escape link "Never mind".
- **1v Post-slip** — "Day 1 of your next streak.", stats retained, trigger-pattern bars (count per trigger, worst = coral), actionable tip banner, CTA "Start day 1 💪".

### Widgets & system surfaces
- **1w Home-screen widgets** — 2×2 (day count + money pill) and 4×2 (day count | money | milestone bar + streak) in light and dark. Values update live.
- **1x Settings** — grouped lists (white cards, rows with emoji icon, chevrons): MY QUIT PLAN (quit date/time, usage, price/can, currency), NOTIFICATIONS (milestone, daily encouragement @08:00, weekly summary — toggles), DATA (CSV export, destructive "Reset all progress" in coral). Footer "SnusStop 1.0 · Made with 💚 in Denmark".
- **1y Notifications** — milestone unlocked (CLAIM BADGE / LATER actions), morning check-in nudge, weekly summary. Tone: warm, concrete numbers, never guilt.

## Interactions & Behavior
- **Live counters**: day/hh:mm:ss ticker (1 s interval) starts from the latest slip or the original quit date, whichever is later. Money saved = days_since_original_quit × pouches/day × (can_price ÷ pouches_per_can) − total_slip_spending; negative balances are allowed. Pouches avoided = floor(days_since_original_quit × pouches/day). Shown on Home, Goals pill, widgets, SOS "Remind me why".
- **SOS is global**: docked center nav button, opens full-screen takeover; every SOS sub-screen has an "I'm okay / craving passed" exit that logs a craving-won event (+XP, celebration 1t).
- **Slip flow**: clean days, ticker, and streak → 0 and restart immediately; time-based progress and health timeline restart too. Savings continue accruing from the original quit date, less cumulative spending recorded on slips. Existing slips have zero spending. XP and earned badges stay intact. Milestone notifications and widgets follow the restarted timer. Trigger chips optional. Post-slip screen shows net savings, trigger patterns, and one concrete tip.
- **Future quit date** (onboarding 1e): countdown state with get-ready checklist (+20 XP each) until day one. (Frame removed from canvas by request; behavior remains in spec.)
- **XP**: daily check-in +10, craving beaten +15, games +10–15, checklist +20, milestones +50. Levels have names ("Fresh Air" = level 4).
- **Animations**: mascot float 3.2s ease-in-out loop; SOS pulse 2.4s box-shadow ring; breathe 19s cycle; new-badge shimmer 1.6s. Button presses should depress the 0 4px 0 bottom-shadow (translate down 2–4px).
- **Theme**: light + dark, follows system setting.

## State Management
- Profile: name, quit datetime, pouches/day, pouches/can, can price, currency (DKK/SEK/NOK/EUR), motivations (list, incl. free text).
- Derived (never stored): elapsed time, money saved, pouches avoided, milestone progress.
- Events log: check-ins, cravings won (with source: breathe/game/reasons), slips (timestamp, trigger, note, amount spent), badges earned, XP total, current + best streak, clean days. CSV exports include `AmountSpent` on each event.
- Goals: name, price, photo, order, funded flag; savings allocate top-down when "split" toggle off.
- All data local-first; CSV export.

## Design Tokens
Type: **Nunito** (400/600/700/800/900). Body 14–15px, H1 26–32px/900, overlines 11–13px/900 with 1–2px tracking, stat numbers 900 with `font-variant-numeric:tabular-nums`.

Light theme:
- Background #F7FAF7 · card #FFFFFF · border #E3EEE7 · hairline #F1F5F2 / #E9EFEA
- Ink #17322B · muted #5E7A6F · faint #8AA398 · disabled #A9BBB0
- Primary green #14B36B (pressed/shadow #0E8A50) · mint tint #EAF7F0 / #BDF0D4 · progress track #E9F3EC
- Coral (SOS/destructive) #FF6B5E (shadow #D94F44) · tints #FFF4F2 / #FFD1CC / #FDEEEC
- Gold (XP/streak/trophy) #FFB627 (shadow #D8940D) · tints #FFF6E3 / #F2EBDA · text #B07E14 / #7A5A16
- Lavender (calm) #8B7CF6 (shadow #6C5CE0) · tint #F0EDFC · Blue #4C9EF5 (shadow #2E75C4) · tint #EAF3FD
- Cheeks #FFB1A6 · streak flame text #E8590C

Dark theme:
- Background #10201B · card #1A2E27 · border/track #23453A · nav #152922
- Ink #ECF6F0 · muted #92AC9F · disabled #6F8A7D
- Primary #2BD98A (text/active) — #14B36B stays for fills · mint chip #15382B / clean-day #1F5C41
- Gold tint #2E2A18, text #FFD98A/#D8A93C · coral shadow #B23A31 · 38% text #FF8A7E
- SOS takeover bg #232140 (cards = white 8–16% overlays, muted #B9B4E3) · reasons bg #0E3B2A

Shape & depth: cards 16–26px radius; buttons/pills 12–18px; chunky `0 4px 0 <darker>` bottom-shadows on all primary actions (game-like, no blurred drop shadows on buttons). CTA height 56px. Min hit target 44px.

Spacing: screen gutter 20–24px; card padding 14–20px; stack gaps 8–14px.

## Assets
None bundled. Mascot "Puff" = placeholder (bubble face: mint circle, ink eyes/smile, #FFB1A6 cheeks; happy variant = arc eyes). Goal photos = user-supplied. Emoji used as icon placeholders throughout — replace with the app's icon set if desired.

## Files
- `SnusStop UI.dc.html` — all 25 frames (design canvas; open in a browser)
- `android-frame.jsx` — device bezel scaffolding used by the canvas (not app UI)
- `support.js` — canvas runtime (not app UI)
