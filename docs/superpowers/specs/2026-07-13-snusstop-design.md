# SnusStop — Design Spec

**Date:** 2026-07-13
**Target:** Production Android app for Google Play (.NET 10 MAUI)
**Source of visual truth:** `SnusStop UI.dc.html` (25 hi-fi frames) + `README.md` handoff.

> This spec captures architecture and decisions. For pixel-exact layout of any screen,
> the referenced dc.html frame id (e.g. `1h`, `3a`, `2b`) is authoritative.

---

## 1. Goals & constraints

- **Production-ready, no placeholders.** Everything ships real: all four SOS games playable,
  the "Puff" mascot shipped as a polished drawn component, widgets built.
- **Local-only persistence.** No backend, no network calls for user data. SQLite on-device.
- **Both themes**, following the system setting.
- **High-fidelity** recreation of the design (colors, type, spacing, radii, copy are final).

## 2. Decisions (locked)

| # | Decision | Choice |
|---|----------|--------|
| 1 | Package id | `dk.snusstop.app`, display name **SnusStop** |
| 2 | Mini-games | **All four** fully playable (Minesweeper, Pouch Pop, Memory Match, Reflex Tap) |
| 3 | Mascot "Puff" | Ship the **bubble-face** as the real mascot (drawn, animated, swappable component) |
| 4 | Home-screen widgets | **Build in v1** (native Android AppWidgetProvider, 2×2 + 4×2, light/dark) |
| 5 | Platform targets | **Android only** — drop iOS/Mac/Windows TFMs (Play Store deliverable) |
| 6 | Min / Target SDK | Min **API 24** (Android 7.0); Target = latest for Play compliance |
| 7 | Persistence | **SQLite** (`sqlite-net-pcl`) as single source of truth |
| 8 | MVVM | `CommunityToolkit.Mvvm` + `CommunityToolkit.Maui` |
| 9 | Notifications | `Plugin.LocalNotification` |
| 10 | SOS direction | Frame **1p** (calm-dark takeover); 1q is not shipped |

## 3. Architecture

Custom-root navigation (Shell's TabBar cannot render the overhanging SOS button cleanly):

- **Launch gate** (`App.CreateWindow`): `Profile` exists & onboarded → `MainTabsPage`; else → `OnboardingPage`.
- **`MainTabsPage`** — Grid: swappable content region hosting `HomeView` / `GoalsView` / `HealthView` /
  `JourneyView` (ContentViews bound to a `MainShellViewModel.CurrentTab`) + a custom **`BottomNavBar`**
  with the pulsing, overhanging coral SOS button.
- **Full-screen modal takeovers** via `Navigation.PushModalAsync`: SOS flow, the 4 games, Settings,
  Add/Edit-goal. Gives correct Android hardware-back behavior.
- **Settings entry point:** Home header avatar + name is tappable → Settings (design has no gear icon).

### 3.1 Project structure

```
/Models         Profile, GoalItem, EventLog, EventType, Badge, Milestone, Currency, Trigger, Motivation
/Data           AppDatabase (SQLite), IRepository<T>, ProfileRepository, GoalRepository, EventRepository
/Services       ClockService, StatsCalculator, XpService, BadgeService, NotificationService,
                WidgetUpdateService, CsvExportService, ThemeService, HapticsService
/ViewModels     OnboardingViewModel, HomeViewModel, GoalsViewModel, HealthViewModel, JourneyViewModel,
                SosViewModel, MinesweeperViewModel, PouchPopViewModel, MemoryMatchViewModel,
                ReflexTapViewModel, BreatheViewModel, RemindWhyViewModel, SlipViewModel, SettingsViewModel,
                MainShellViewModel
/Views          Onboarding/*, MainTabsPage, Tabs/HomeView|GoalsView|HealthView|JourneyView,
                Sos/*, Games/*, SettingsPage, GoalEditPage
/Controls       PuffMascot, ProgressRing, ChunkyButton, BottomNavBar, PillToggle, SegmentedControl,
                CalendarGrid, StatCard, StepperCard, CurrencyChip, ConfettiView
/Resources      Styles/Colors.xaml, Styles/Styles.xaml, Fonts/Nunito*, Images/
/Platforms/Android  MainApplication, MainActivity, Widgets/(SnusWidgetProvider + RemoteViews layouts),
                notification channel setup
```

### 3.2 App-wide services (DI singletons registered in `MauiProgram`)

- **`ClockService`** — a 1s `IDispatcher` timer exposing `Now`; view models subscribe for live tickers.
- **`StatsCalculator`** — pure functions for all derived values (see §5). No stored derived state.
- **`XpService`** — XP totals, level thresholds, level names ("Fresh Air" = level 4), award events.
- **`BadgeService`** — badge definitions + earned evaluation (24 badges, 6 earned for sample persona).
- **`NotificationService`** — schedule/cancel local notifications; request POST_NOTIFICATIONS on Android 13+.
- **`WidgetUpdateService`** — push current stats to the Android widget (on app resume + relevant events).
- **`CsvExportService`** — export events/goals/profile to a CSV file + share sheet.
- **`ThemeService`** — expose current app theme; follow system, allow manual override later.

## 4. Data model

**Profile** (single row): `Name`, `QuitDateTimeUtc`, `PouchesPerDay`, `PouchesPerCan`, `CanPrice`,
`Currency` (DKK/SEK/NOK/EUR), `Motivations` (list incl. free text), `SplitSavings` (bool),
`NotifyMilestone`/`NotifyDaily`/`NotifyWeekly` (bools), `OnboardingComplete` (bool).

**GoalItem**: `Id`, `Name`, `Price`, `PhotoPath` (nullable), `SortOrder`, `Funded` (bool),
`FundedOnDay` (nullable), `FundedByPouches` (nullable).

**EventLog**: `Id`, `TimestampUtc`, `Type` (CheckIn | CravingWon | Slip | BadgeEarned | XpDelta | GoalFunded),
`Source` (breathe/game/reasons/…), `Trigger` (nullable, for slips), `Note` (nullable), `XpDelta` (int).

**Derived (never stored)** — computed by `StatsCalculator`:
`elapsed`, `days/hh/mm/ss`, `moneySaved`, `pouchesAvoided`, `currentStreak`, `bestStreak`,
`totalCleanDays`, milestone progress + ring dash, recovery %, per-goal funding.

## 5. Derived math (from dc.html logic block — authoritative)

```
perPouch      = canPrice / pouchesPerCan            (design uses pouchesPerCan = 20)
perDayCost    = pouchesPerDay * perPouch
daysFloat     = elapsedSeconds / 86400
moneySaved    = daysFloat * pouchesPerDay * perPouch
pouchesAvoided= floor(daysFloat * pouchesPerDay)
day/hh/mm/ss  = split of elapsedSeconds
nextMilestone = next unreached in {1d,3d,1w,2w,1m,3m,1y}; ring = min(1, daysFloat/target)
weekSave      = perDayCost * 7 ;  monthSave = perDayCost * 30.4 ;  yearSave = perDayCost * 365
```
Currency symbol: `EUR → €`, else `kr`. Number format: da-DK (`1.234,56`).
`currentStreak` resets to 0 on a slip and restarts immediately; money/XP/badges/totalCleanDays untouched.

## 6. Design tokens (from README — final)

**Type:** Nunito 400/600/700/800/900. Body 14–15, H1 26–32/900, overlines 11–13/900 (1–2px tracking),
stat numbers 900 tabular-nums.

**Light:** bg `#F7FAF7` · card `#FFFFFF` · border `#E3EEE7` · hairline `#F1F5F2`/`#E9EFEA` ·
ink `#17322B` · muted `#5E7A6F` · faint `#8AA398` · disabled `#A9BBB0` ·
primary `#14B36B` (pressed `#0E8A50`) · mint `#EAF7F0`/`#BDF0D4` · track `#E9F3EC` ·
coral `#FF6B5E` (shadow `#D94F44`) tints `#FFF4F2`/`#FFD1CC`/`#FDEEEC` ·
gold `#FFB627` (shadow `#D8940D`) tints `#FFF6E3`/`#F2EBDA` text `#B07E14`/`#7A5A16` ·
lavender `#8B7CF6` (shadow `#6C5CE0`) tint `#F0EDFC` · blue `#4C9EF5` (shadow `#2E75C4`) tint `#EAF3FD` ·
cheeks `#FFB1A6` · flame `#E8590C`.

**Dark:** bg `#10201B` · card `#1A2E27` · border/track `#23453A` · nav `#152922` ·
ink `#ECF6F0` · muted `#92AC9F` · disabled `#6F8A7D` · primary text/active `#2BD98A` (fills stay `#14B36B`) ·
mint chip `#15382B` / clean-day `#1F5C41` · gold tint `#2E2A18` text `#FFD98A`/`#D8A93C` ·
coral shadow `#B23A31` · 38% text `#FF8A7E` · SOS bg `#232140` (cards = white 8–16% overlays,
muted `#B9B4E3`) · reasons bg `#0E3B2A`.

**Shape/depth:** cards 16–26px radius; buttons/pills 12–18px; chunky `0 4px 0 <darker>` bottom-shadows
on primary actions (no blurred drop shadows); CTA height 56px; min hit target 44px.
**Spacing:** gutter 20–24; card padding 14–20; stack gaps 8–14.

## 7. Screen inventory (frame → implementation)

- **Onboarding** `1a`–`1g`: Welcome, Name (keyboard), Habits (steppers), Cost+currency, Quit date
  (already-quit / future-date toggle → future path shows countdown + get-ready checklist, +20 XP each),
  Motivation grid, Summary (confetti, "Start my quit").
- **Home** `1h`/`1j`: ring hero + live ticker, milestone chip, 3 stat cards, XP card, check-in chip.
- **Goals** `1l`/`3a` + empty `1m`: saved pill, split toggle, goal cards (photo/progress/% funded),
  add-goal, trophy shelf.
- **Health** `1n`/`3b`: recovery donut + vertical milestone timeline (done / in-progress / locked).
- **Journey** `1o`/`3c`: "6 of 24 badges" grid + month clean-day calendar + insight line.
- **SOS** `1p` home → picker `2a` → games → Breathe `1r` → Remind why `1s` → Defeated `1t`
  → Slip `1u` → Post-slip `1v`.
- **Widgets** `1w` (2×2, 4×2, light+dark). **Settings** `1x`. **Notifications** `1y`.

## 8. Custom controls

`PuffMascot` (GraphicsView; props: `Size`, `Expression` = Normal/Happy/Thinking, `Cheeks`, `Float`) ·
`ProgressRing` (GraphicsView; `Progress`, `Stroke`, `TrackColor`, `FillColor`, round caps; 232px & 74px) ·
`ChunkyButton` (Border + colored bottom-shadow layer + press-depress animation) ·
`BottomNavBar` (5 slots, center overhang, pulse) · `PillToggle` · `SegmentedControl` ·
`CalendarGrid` (clean/craving/slip legend) · `StatCard` · `StepperCard` (−/+ 44×44) · `CurrencyChip` ·
`ConfettiView` (celebration dots). All bind colors from the token dictionary via `AppThemeBinding`.

## 9. Games (all fully playable)

- **Minesweeper** — 8×8, 10 mines, flood-fill reveal, Dig/Flag mode toggle, mines-left + timer,
  loss = reveal + retry, win → +15 XP → Craving-defeated.
- **Pouch Pop** — endless bubble-wrap grid, tap-to-pop (scale/opacity anim + haptic), +10 XP after a run.
- **Memory Match** — 4×4 (8 pairs), flip-to-match, 2-min round, +10 XP on clear.
- **Reflex Tap** — dots appear/vanish at random, tap before timeout, 60s, score, +10 XP.

Each win logs a `CravingWon` event (with `Source`) and routes into celebration `1t`.

## 10. Notifications (`Plugin.LocalNotification`)

- **Milestone unlocked** — scheduled at each upcoming milestone time; CLAIM BADGE / LATER (`1y`).
- **Daily encouragement** — repeating 08:00 (toggle in Settings).
- **Weekly summary** — repeating Sun 18:00 (toggle; off by default per `1x`).
- Android 13+ POST_NOTIFICATIONS permission requested after onboarding.

## 11. Widgets (native Android)

`SnusWidgetProvider : AppWidgetProvider` + RemoteViews XML in `Platforms/Android/Resources/layout`
(`widget_2x2`, `widget_4x2`) with light/dark drawables. Values (day+hour, money, streak, milestone bar)
read from persisted profile; refreshed via WorkManager (~30 min) + on app events through
`WidgetUpdateService`. Per-second ticking is not possible in widgets — day+hour granularity matches design.

## 12. Build & release

- Trim csproj to `net10.0-android`; set package id, display name, versions, min/target SDK.
- App icon + splash from the SnusStop mark (mint rounded square + Puff), replacing the .NET defaults.
- Release build produces a signed **AAB**; document the keystore/signing steps for the user (they hold the key).

## 13. Implementation phases

1. **Foundation** — trim csproj, package id, Nunito fonts, token dictionaries (Colors/Styles),
   SQLite DB + repositories, DI wiring, `PuffMascot` + `ProgressRing` + `ChunkyButton` + `BottomNavBar`.
2. **Onboarding** — 6-step wizard + summary, writes Profile, future-date path.
3. **Main tabs** — Home (+ live tickers), Goals (+ empty/add/edit), Health, Journey; both themes.
4. **SOS + games** — takeover, picker, all 4 games, Breathe, Remind why, Defeated, Slip, Post-slip.
5. **System surfaces** — Settings, notifications (real scheduling), CSV export.
6. **Widgets** — native 2×2 + 4×2, light/dark, scheduled refresh.
7. **Polish & release** — icon/splash, animations pass, accessibility labels, release AAB + signing docs.

## 14. Out of scope (v1)

iOS/Mac/Windows builds · cloud sync / account · illustrated mascot art (bubble-face ships instead) ·
in-app purchases · localization beyond the shipped English/Danish copy in the frames.
