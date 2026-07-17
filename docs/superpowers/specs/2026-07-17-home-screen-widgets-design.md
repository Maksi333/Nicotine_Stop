# SnusStop — Home-screen widgets (design)

**Date:** 2026-07-17
**Status:** Approved (design), pending implementation plan
**Scope:** Android home-screen widgets only. No changes to `SnusStop.Core` logic, view models, or in-app screens beyond the SOS deep-link plumbing described below.

## Goal

Replace the current widget offering with three production-ready home-screen widgets that
follow the finalized design canvas (frames 5a/5b/5c). All values are live and theme-aware.

Final set:

| Provider (class) | Cells | Design | Picker label |
|---|---|---|---|
| `SnusWidgetCompact` (restyle of existing 2×2) | 2×2 | 5a-style compact | `SnusStop · Compact` |
| `SnusWidgetRing` (repurpose of existing 4×2) | 4×2 | 5b Mini ring hero | `SnusStop · Progress ring` |
| `SnusWidgetSos` (new) | 4×2 | 5c Puff + SOS shortcut | `SnusStop · Puff + SOS` |

The old 4×2 "split-stats" layout is superseded by 5b + 5c and removed. The 2×2 becomes the
5a-flavored Compact widget. All three providers get clear semantic class names. Renaming a
provider class changes its Android component identity, so any already-placed instance of the
old `SnusWidget2x2`/`SnusWidget4x2` would need re-adding — acceptable because the app is
pre-release (not yet published), so there are no live installs to migrate.

## Non-goals (YAGNI)

- No user-configurable widget options (color/content pickers).
- No new persisted data; widgets derive everything from the existing plan snapshot.
- Puff stays a primitive placeholder asset — swappable, not final brand art.
- iOS/Mac/Windows widgets are out of scope (Android only, matching the app's target).

## Data & update mechanism (reuse existing, already refactored)

`WidgetUpdateService.Update(profile, lastSlipUtc, cravingsWon)` writes the **raw plan** to
shared preferences (`snusstop_widget`): quit ticks, per-day, per-container, price, currency,
addiction, last-slip ticks, cravings-won. It is called on every `AppState.Changed` and on
app foreground.

`WidgetData.Read(prefs)` recomputes current stats via `StatsCalculator.Compute(...)` against
`DateTime.UtcNow`, so each provider's `OnUpdate` renders live numbers even while the app is
closed. Providers also refresh on their own `updatePeriodMillis` (3600000 = hourly).

Fields each widget consumes (all already available from `WidgetData` / to be surfaced there):

- `Day` — `"{Days}d {Hours}h"` (compact, 5a) / `Days` integer (ring center, 5b) / `Days` (5c message).
- `Money` — `StatsCalculator.FormatMoney(s.Money, currency, withDecimals:false)` (symbol placed per currency).
- `MoneyInt` — grouped amount **without** symbol, for the tight 5c chip (`FormatNumber`).
- `Streak` — `s.CurrentStreak`.
- `Avoided` — `s.PouchesAvoided`.
- `SkippedCaption` — `AddictionCopy.For(addiction).SkippedCaption` ("pouches skipped" / "cigarettes not smoked").
- `MilestoneTitle` — `s.Next?.Title ?? "1 year"` (e.g. "2 weeks").
- `MilestonePct` — `(int)(s.RingProgress * 100)`.
- `RingProgress` — `s.RingProgress` (0..1, for the drawn ring arc).
- `DaysLeft` — `StatsCalculator.DaysUntilNextLabel(s)` (e.g. "2 days").

"No plan yet" state (widget added before onboarding completes, `quitTicks <= 0`): neutral
zeros, empty milestone caption, no crash. Already handled in `WidgetData.Read`.

## Theming

Light/dark follow the **system** theme through Android resource qualifiers
(`values/colors.xml` + `values-night/colors.xml`), the existing pattern. New color tokens
are added to both files. The bitmap-drawn ring (5b) resolves its colors from these resources
in code (`ContextCompat.GetColor`), so it matches the current theme.

Color tokens (add any missing to both light/night):

- Light: card `#FDFEFD` · ink `#17322B` · muted `#8AA398` · money/primary `#0E8A50` ·
  ring/bar fill `#14B36B` · ring track `#E3EEE7` · bar track `#E9F3EC` · mint chip `#EAF7F0` ·
  coral chip `#FDEEEC` / text `#D94F44` · flame `#E8590C` · SOS `#FF6B5E` / shadow `#D94F44`.
- Dark: card `#1A2E27` · ink `#ECF6F0` · muted `#92AC9F` · money/green fill `#2BD98A` ·
  track/bar `#23453A` · mint chip `#15382B` · coral chip `#3A2320` / text `#FF9D94` ·
  flame `#FF9D66` · SOS `#FF6B5E` / shadow `#B23A31`.

## Constraint: RemoteViews inflation

Widget layouts may only contain `@RemoteView`-annotated classes: `LinearLayout`,
`FrameLayout`, `RelativeLayout`, `GridLayout`, `TextView`, `ImageView`, `ProgressBar`,
`Button`, `Chronometer`, `AnalogClock`, `ImageButton`, `ViewStub`, `AdapterViewFlipper`,
`ListView`, `GridView`, `StackView`, `ViewFlipper`. A plain `<View>` throws
"Class not allowed to be inflated". (Established in the existing layout comments.) All new
layouts obey this — the 1px dividers and dots use `ImageView`, not `View`.

## Widget 1 — 2×2 Compact (5a flavor)

Layout (`layout/widget_compact.xml`), vertical `LinearLayout`, `@drawable/widget_bg`, 16dp padding:

```
┌──────────────────────┐
│ ◕ SNUSSTOP      🔥 12 │   row: mini-Puff (18dp) + overline + spacer + flame streak
│  12d 4h              │   day count, ~26sp/900 ink
│  nicotine-free       │   caption, 10sp muted
│  156 kr   saved      │   money (primary) + "saved" (muted)
│  ▓▓▓▓▓▓▓▓▓░  2 wks    │   milestone ProgressBar + "N wks" label
└──────────────────────┘
```

- Mini Puff = `@drawable/widget_puff` (18dp `ImageView`).
- Streak = flame color, `🔥 {streak}`.
- Bottom: `ProgressBar` (fill `widget_primary`, track `widget_bar_track`) + short milestone label.
- Whole widget taps → open app Home.

## Widget 2 — 4×2 Progress ring (5b)

Layout (`layout/widget_ring.xml`), horizontal `LinearLayout`, `@drawable/widget_bg`, 16dp/20dp padding:

```
┌────────────────────────────────────────┐
│   ╭──────╮    💰 156 kr   saved          │
│  │   12   │   🚫 180      pouches skipped │
│  │  days  │   🏆 2 days   until 2 wks free│
│   ╰──────╯                               │
└────────────────────────────────────────┘
```

- **Ring** = an `ImageView` whose bitmap is drawn in `OnUpdate`:
  - Canvas size 118dp→px; circle r=50dp, stroke 10dp, round cap.
  - Track arc = full circle in `widget_ring_track`; fill arc = `RingProgress` sweep in
    `widget_ring_fill`, starting at 12 o'clock (`-90°`).
  - Centered text drawn on the same canvas: day integer (~34sp/900 ink) + "days free"
    (10sp muted). Text uses the same theme colors.
  - Drawn with `DisplayMetrics.Density` scaling so it is crisp at the device DPI.
- **Right column** = three icon+stat rows (emoji `TextView` + value + caption):
  - 💰 `Money` (primary, tabular) / "saved"
  - 🚫 `Avoided` (ink) / `SkippedCaption` (addiction-aware)
  - 🏆 `DaysLeft` (ink) / "until {MilestoneTitle} free"
- Whole widget taps → open app Home.

## Widget 3 — 4×2 Puff + SOS (5c)

Layout (`layout/widget_sos.xml`), horizontal `LinearLayout`, `@drawable/widget_bg`, 16dp/18dp padding:

```
┌────────────────────────────────────────┐
│  ╭────╮  Day 12 — Puff's proud of you.  ╭────╮│
│ │ ◕  ◕ │ [💰156] [🔥12d]                │ SOS ││
│  ╰────╯  ▓▓▓▓▓▓▓░░                       ╰────╯│
│          🏆 2 days until 2 weeks free          │
└────────────────────────────────────────┘
```

- **Left**: 78dp Puff = `@drawable/widget_puff` (same asset, larger). The inset bottom-shadow
  from the mock is decorative; approximated within the drawable, not required pixel-exact.
- **Middle** (`weight=1`): message "Day {Days} — Puff's proud of you." (13.5sp/900 ink);
  a row of two chips — 💰 `MoneyInt` on `@drawable/widget_chip_mint`, 🔥 `{Streak}d` on
  `@drawable/widget_chip_coral` (coral text); a milestone `ProgressBar`; caption
  "🏆 {DaysLeft} until {MilestoneTitle} free" (9.5sp muted). Chip drawables = 9dp-radius rounded rects.
- **Right**: 64dp coral SOS circle. `@drawable/widget_sos_circle` (coral fill; the `0 4px 0`
  bottom-shadow approximated with a 4dp darker underlay via a layer-list) + "SOS" 14sp/900 white.
- **Taps**: SOS circle → deep-link into Craving SOS (see below). Everywhere else → app Home.

## SOS deep-link plumbing

The one piece of app-side work.

1. Widget: the SOS `ImageView`/container gets `SetOnClickPendingIntent` → an `Intent` for
   `MainActivity` with `PutExtra("navigate", "sos")`, `FLAG_ACTIVITY_SINGLE_TOP` (activity is
   already `LaunchMode.SingleTop`). The root container gets a separate PendingIntent → plain
   `MainActivity` launch (Home). Per-view pending intents mean the SOS tap wins on the circle.
2. `MainActivity`: override `OnCreate` (cold start) and `OnNewIntent` (warm) to inspect the
   intent for `navigate == "sos"`; hand it to a small static bridge
   (`WidgetNavigation.RequestSos()` raising an event / setting a pending flag).
3. `MainTabsPage`: subscribes to the bridge. If the page is already loaded, it invokes the
   existing `OnSos` path (`PushModalAsync(new NavigationPage(SosTakeoverPage))`). If the
   request arrives during cold start (before load), a pending flag is consumed at the end of
   `OnAppearing` after `LoadAsync`, so SOS still opens once ready. Guard against double-open.
4. Distinct `PendingIntent` request codes per widget/target so they do not collide/overwrite.

This reuses the existing SOS navigation entirely; no new SOS UI.

## Files

New/changed (Android head project unless noted):

- `Platforms/Android/Widgets/SnusWidgets.cs` — replace 2×2/4×2 providers with
  `SnusWidgetCompact`, `SnusWidgetRing`, `SnusWidgetSos`; add ring-bitmap drawing helper;
  add PendingIntent wiring (Home + SOS). Keep `WidgetPrefs`, `WidgetData`, `WidgetView`.
- `Platforms/Android/Resources/layout/widget_compact.xml` — new (replaces `widget_2x2.xml`).
- `Platforms/Android/Resources/layout/widget_ring.xml` — new (replaces `widget_4x2.xml`).
- `Platforms/Android/Resources/layout/widget_sos.xml` — new.
- `Platforms/Android/Resources/xml/widget_compact_info.xml` / `widget_ring_info.xml` /
  `widget_sos_info.xml` — provider metadata (target cells, preview, resize, hourly update).
- `Platforms/Android/Resources/drawable/` — `widget_puff.xml` (vector), `widget_chip_mint.xml`,
  `widget_chip_coral.xml`, `widget_sos_circle.xml`; keep/extend `widget_bg.xml`.
- `Platforms/Android/Resources/values/colors.xml` + `values-night/colors.xml` — add ring
  track/fill, chip, coral, SOS tokens.
- `Platforms/Android/MainActivity.cs` — intent inspection for the SOS deep-link.
- `Platforms/Android/Widgets/WidgetNavigation.cs` — new static bridge (or fold into MainActivity).
- `Views/MainTabsPage.cs` — subscribe to the bridge; pending-SOS handling in `OnAppearing`.
- `Services/WidgetUpdateService.cs` / `WidgetData` — surface any not-yet-exposed field
  (`MoneyInt`, `SkippedCaption`) needed by the new layouts.

Removed: `layout/widget_2x2.xml`, `layout/widget_4x2.xml`, `xml/widget_2x2_info.xml`,
`xml/widget_4x2_info.xml` (superseded by the renamed set).

## Testing / "production-ready" bar

- Builds for `net10.0-android`; app deploys to emulator/device.
- Each widget renders correctly in **light and dark**, at the launcher's default and (where
  resizable) stretched sizes, with no text clipping.
- Values match the in-app Home for the same profile (day count, money incl. currency symbol,
  streak, pouches, milestone).
- Taps: all three open Home; 5c's SOS circle opens the Craving SOS takeover from both a cold
  and warm app start.
- "No plan yet" placeholder renders without crashing.
- `SnusStop.Core` unit tests still pass (untouched, but run to confirm no regressions).
- Manual verification via the `run` skill on an Android target after implementation.
