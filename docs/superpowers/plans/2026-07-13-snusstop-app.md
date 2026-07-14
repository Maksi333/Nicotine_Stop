# SnusStop App Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship a production-ready, no-placeholders SnusStop quit-nicotine Android app (Play Store) recreating all 25 hi-fi design frames with live counters, local persistence, four playable games, notifications, and home-screen widgets.

**Architecture:** Three projects — `SnusStop.Core` (net10.0 class library: models + all pure logic, fully unit-tested), `Nicotine_Stop` (net10.0-android MAUI app: XAML views, MVVM view models, drawn controls, platform services), and `SnusStop.Core.Tests` (xUnit). Custom-root navigation (onboarding gate → `MainTabsPage` with a custom bottom nav + overhanging SOS button; SOS/games/settings as full-screen modals). SQLite is the single local source of truth; all display stats are derived at runtime.

**Tech Stack:** .NET 10 MAUI (Android), CommunityToolkit.Mvvm, CommunityToolkit.Maui, sqlite-net-pcl, Plugin.LocalNotification, Android AppWidgetProvider (RemoteViews), xUnit.

## Global Constraints

- Package id **`dk.snusstop.app`**; display name **SnusStop**; version 1.0 (code 1).
- Target framework **net10.0-android only** (remove ios/maccatalyst/windows TFMs). Min SDK **24**, Target SDK latest available (36).
- Font **Nunito** weights 400/600/700/800/900 (OFL); remove OpenSans.
- **No placeholders** in shipped app: 4 games playable; mascot = drawn bubble-face component; widgets real.
- **Local-only** — no network for user data. SQLite single source of truth; derived stats never stored.
- Both **light + dark** themes, following the system setting; all colors via `AppThemeBinding` from the token dictionary.
- Money format da-DK (`1.234,56`); currency symbol `EUR → €` else `kr`.
- Slip resets **current streak only** — money, XP, badges, total clean days untouched.
- Pixel spec = the named dc.html frame id per screen; tokens = spec §6. Min hit target 44px; CTA height 56px.
- Chunky `0 4px 0 <darker>` bottom-shadows on primary actions (no blurred shadows); press = depress 2–4px.

---

## Phase 0 — Solution restructure & toolchain

### Task 0.1: Create Core library + test project, wire the solution

**Files:**
- Create: `SnusStop.Core/SnusStop.Core.csproj` (net10.0 classlib, Nullable enable, ImplicitUsings enable)
- Create: `SnusStop.Core.Tests/SnusStop.Core.Tests.csproj` (net10.0, xUnit, references Core)
- Modify: `Nicotine_Stop.slnx` — add both projects
- Modify: `Nicotine_Stop.csproj` — add `<ProjectReference Include="..\SnusStop.Core\SnusStop.Core.csproj" />`

**Interfaces:**
- Produces: `SnusStop.Core` namespace root for all model/service tasks; `SnusStop.Core.Tests` for all logic tests.

- [ ] Step 1: `dotnet new classlib -n SnusStop.Core -f net10.0` ; delete `Class1.cs`.
- [ ] Step 2: `dotnet new xunit -n SnusStop.Core.Tests -f net10.0` ; `dotnet add SnusStop.Core.Tests reference SnusStop.Core`.
- [ ] Step 3: Add both to `Nicotine_Stop.slnx`; add the ProjectReference from the app to Core.
- [ ] Step 4: `dotnet build SnusStop.Core.Tests` → Expected: PASS (0 tests).
- [ ] Step 5: Commit `chore: add Core library and test project`.

### Task 0.2: Trim MAUI csproj to Android + set identity + Nunito fonts + NuGet packages

**Files:** Modify `Nicotine_Stop.csproj`; Modify `MauiProgram.cs`; Add `Resources/Fonts/Nunito-Regular.ttf` … `Nunito-Black.ttf`; Remove `Resources/Fonts/OpenSans-*.ttf`.

- [ ] Step 1: Set `<TargetFrameworks>net10.0-android</TargetFrameworks>` (single line, remove conditionals), `ApplicationTitle=SnusStop`, `ApplicationId=dk.snusstop.app`, android `SupportedOSPlatformVersion=24`.
- [ ] Step 2: Add PackageReferences: `CommunityToolkit.Mvvm`, `CommunityToolkit.Maui`, `sqlite-net-pcl`, `SQLitePCLRaw.bundle_green`, `Plugin.LocalNotification`.
- [ ] Step 3: Download Nunito TTFs (weights 400/600/700/800/900) into `Resources/Fonts/` (see §Fonts note); register each in `MauiProgram` with aliases `Nunito`, `NunitoSemiBold`, `NunitoBold`, `NunitoExtraBold`, `NunitoBlack`; call `.UseMauiCommunityToolkit()` and `LocalNotificationCenter.CreateNotificationChannel`.
- [ ] Step 4: `dotnet build -f net10.0-android` → Expected: PASS.
- [ ] Step 5: Commit `chore: android-only target, app identity, Nunito, core packages`.

**Fonts note:** Download from Google Fonts static TTFs (`https://github.com/google/fonts/raw/main/ofl/nunito/static/Nunito-Regular.ttf` etc.) via `Invoke-WebRequest`. If offline, fall back to the variable font `Nunito[wght].ttf` registered once. OFL license permits bundling.

---

## Phase 1 — Core models & logic (TDD)

### Task 1.1: Domain models & enums

**Files:** Create `SnusStop.Core/Models/Currency.cs`, `EventType.cs`, `Trigger.cs`, `Profile.cs`, `GoalItem.cs`, `EventLog.cs`, `Milestone.cs`, `Badge.cs`.

**Interfaces — Produces:**
- `enum Currency { DKK, SEK, NOK, EUR }` with `static string Symbol(Currency c) => c==EUR?"€":"kr";`
- `enum EventType { CheckIn, CravingWon, Slip, BadgeEarned, XpDelta, GoalFunded }`
- `enum Trigger { None, Party, Stress, Coffee, AfterMeal, Boredom, FriendsUsing }`
- `class Profile { string Name; DateTime QuitUtc; int PouchesPerDay; int PouchesPerCan; decimal CanPrice; Currency Currency; List<string> Motivations; bool SplitSavings; bool NotifyMilestone; bool NotifyDaily; bool NotifyWeekly; bool OnboardingComplete; }`
- `class GoalItem { int Id; string Name; decimal Price; string? PhotoPath; int SortOrder; bool Funded; int? FundedOnDay; int? FundedByPouches; }`
- `class EventLog { int Id; DateTime TimestampUtc; EventType Type; string? Source; Trigger Trigger; string? Note; int XpDelta; }`
- `record Milestone(string Key, string Title, TimeSpan At, string Body);` + `static IReadOnlyList<Milestone> Milestone.All` = {20min,72h,1w,2w,1m,3m,1y} with the exact body copy from frame `1n`.
- `record Badge(string Key, string Emoji, string Label);` + `static IReadOnlyList<Badge> Badge.All` (24 badges; first 6 = frame `1o`).

- [ ] Step 1: Write model classes exactly as above (plain properties, no logic).
- [ ] Step 2: `dotnet build SnusStop.Core` → PASS. Commit `feat: core domain models`.

### Task 1.2: `StatsCalculator` — derived math (TDD)

**Files:** Create `SnusStop.Core/Services/StatsCalculator.cs`; Test `SnusStop.Core.Tests/StatsCalculatorTests.cs`.

**Interfaces — Produces:** `static class StatsCalculator` with:
```
record Stats(int Days,int Hours,int Min,int Sec,double DaysFloat,decimal Money,int PouchesAvoided,
             int CurrentStreak,Milestone? Next,double RingProgress,string RingDash);
static Stats Compute(Profile p, DateTime nowUtc, DateTime? lastSlipUtc);
static decimal PerPouch(Profile p);           // CanPrice / PouchesPerCan
static decimal PerDayCost(Profile p);         // PouchesPerDay * PerPouch
static decimal WeekSave/MonthSave/YearSave(Profile p);   // *7, *30.4, *365
static string FormatMoney(decimal v, bool withDecimals); // da-DK "1.234,56" / "1.234"
```

- [ ] Step 1: Write failing tests:
```csharp
public class StatsCalculatorTests {
  static Profile P(int ppd=15,int ppc=20,decimal price=45m,int quitDaysAgo=12)
    => new(){ Name="M", QuitUtc=new DateTime(2026,7,1,0,0,0,DateTimeKind.Utc).AddDays(-(quitDaysAgo-12)),
              PouchesPerDay=ppd, PouchesPerCan=ppc, CanPrice=price, Currency=Currency.DKK };

  [Fact] public void PerPouch_is_price_over_can() =>
    Assert.Equal(2.25m, StatsCalculator.PerPouch(P()));                 // 45/20
  [Fact] public void Money_grows_with_elapsed_days() {
    var p = P(); var now = p.QuitUtc.AddDays(10);
    var s = StatsCalculator.Compute(p, now, null);
    Assert.Equal(10, s.Days);
    Assert.Equal(337.5m, Math.Round(s.Money,1));                        // 10*15*2.25
    Assert.Equal(150, s.PouchesAvoided);                               // floor(10*15)
  }
  [Fact] public void Next_milestone_after_10_days_is_two_weeks() {
    var p=P(); var s=StatsCalculator.Compute(p,p.QuitUtc.AddDays(10),null);
    Assert.Equal("2w", s.Next!.Key);
  }
  [Fact] public void Streak_resets_from_last_slip() {
    var p=P(); var now=p.QuitUtc.AddDays(20); var slip=now.AddDays(-3);
    var s=StatsCalculator.Compute(p,now,slip);
    Assert.Equal(3, s.CurrentStreak);
  }
  [Fact] public void FormatMoney_daDK() =>
    Assert.Equal("1.234,56", StatsCalculator.FormatMoney(1234.56m, true));
}
```
- [ ] Step 2: Run `dotnet test SnusStop.Core.Tests` → FAIL (not implemented).
- [ ] Step 3: Implement `StatsCalculator` per spec §5 (use `CultureInfo("da-DK")`; ring circumference C=628.3, dash = `$"{(prog*C):F1} {C:F1}"`).
- [ ] Step 4: `dotnet test` → PASS.
- [ ] Step 5: Commit `feat: StatsCalculator with tests`.

### Task 1.3: `XpService` + `BadgeService` (TDD)

**Files:** Create `SnusStop.Core/Services/XpService.cs`, `BadgeService.cs`; Tests `XpServiceTests.cs`.

**Interfaces — Produces:**
- `static class XpService { record Level(int Number,string Name,int Floor,int Ceil); static Level ForXp(int xp); static int Award(EventType t, string? source); }` — awards: CheckIn 10, CravingWon 15, game 10–15, checklist 20, milestone 50; level names incl. "Fresh Air"=4; level 4 spans 340/500 as in frame `1h` (thresholds: L1 0, L2 100, L3 250, L4 500 cumulative → within-level 340/500 shown).
- `static class BadgeService { static IReadOnlyList<Badge> Earned(Profile p, IEnumerable<EventLog> events, StatsCalculator.Stats s); }`

- [ ] Step 1: Tests: `Award(CravingWon,null)==15`; `ForXp(x)` returns level "Fresh Air" for the sample; `Earned` returns 6 badges for the sample persona (day≥12, 250 kr saved, 10 cravings, goal funded, 1w, 3d, 1d). Run → FAIL.
- [ ] Step 2: Implement; `dotnet test` → PASS. Commit `feat: XP + badge services with tests`.

### Task 1.4: Game engines — Minesweeper & Memory (TDD)

**Files:** Create `SnusStop.Core/Games/MinesweeperEngine.cs`, `MemoryMatchEngine.cs`; Tests `MinesweeperEngineTests.cs`, `MemoryMatchEngineTests.cs`.

**Interfaces — Produces:**
- `class MinesweeperEngine { MinesweeperEngine(int size=8,int mines=10,int? seed=null); Cell[,] Grid; enum CellState{Hidden,Revealed,Flagged}; int MinesLeft; bool Won; bool Lost; void Reveal(int r,int c); void ToggleFlag(int r,int c); }` with flood-fill on 0-neighbour reveal, first-click-safe.
- `class MemoryMatchEngine { MemoryMatchEngine(int pairs=8,int? seed=null); IReadOnlyList<Card> Cards; bool Flip(int i); bool Won; }` (Flip returns true on a completed match).

- [ ] Step 1: Tests: seeded board has exactly `mines` mines; revealing a 0-cell flood-fills; flagging decrements `MinesLeft`; revealing all non-mines → `Won`; Memory: 8 pairs = 16 cards, two matching flips → matched & `Won` when all matched. Run → FAIL.
- [ ] Step 2: Implement engines; `dotnet test` → PASS. Commit `feat: minesweeper + memory engines with tests`.

### Task 1.5: `CsvExporter` (TDD)

**Files:** Create `SnusStop.Core/Services/CsvExporter.cs`; Test `CsvExporterTests.cs`.

**Interfaces — Produces:** `static class CsvExporter { static string Build(Profile p, IEnumerable<GoalItem> goals, IEnumerable<EventLog> events); }` — RFC-4180 CSV, one section per entity, header rows, quoted fields.

- [ ] Step 1: Test: output contains profile header, a row per event, commas/quotes escaped. Run → FAIL.
- [ ] Step 2: Implement; `dotnet test` → PASS. Commit `feat: CSV exporter with tests`.

---

## Phase 2 — App foundation (theming, data, DI, base controls)

### Task 2.1: Design-token resource dictionaries

**Files:** Rewrite `Resources/Styles/Colors.xaml` (every token in spec §6 as `<Color>` + `AppThemeBinding` pairs where light/dark differ); Rewrite `Resources/Styles/Styles.xaml` (implicit styles: `Label` default Nunito; named styles `H1`,`Overline`,`StatNumber`,`CardBorder`,`CtaLabel`; `VisualStateManager` not needed).

- [ ] Step 1: Define color resources keyed `Bg,Card,Border,Hairline,Ink,Muted,Faint,Disabled,Primary,PrimaryPressed,Mint,MintTint,Track,Coral,CoralShadow,Gold,GoldText,Lavender,LavenderShadow,Blue,BlueShadow,Cheeks,Flame,SosBg,ReasonsBg,NavBg` using `AppThemeBinding Light=… Dark=…`.
- [ ] Step 2: `dotnet build -f net10.0-android` → PASS. Commit `feat: design token dictionaries`.

### Task 2.2: SQLite data layer + repositories

**Files:** Create `Data/AppDatabase.cs`, `Data/ProfileRepository.cs`, `Data/GoalRepository.cs`, `Data/EventRepository.cs`; SQLite-annotated partial models in `Data/Records/*` (map Core models ↔ table rows to keep Core persistence-free).

**Interfaces — Produces:** `class AppDatabase { Task InitAsync(); }`; `interface IProfileRepository { Task<Profile?> GetAsync(); Task SaveAsync(Profile p); }`; `IGoalRepository { Task<List<GoalItem>> AllAsync(); Task UpsertAsync(GoalItem g); Task DeleteAsync(int id); }`; `IEventRepository { Task<List<EventLog>> AllAsync(); Task AddAsync(EventLog e); Task<DateTime?> LastSlipAsync(); Task ResetAsync(); }`.

- [ ] Step 1: Implement SQLite tables (async connection at `FileSystem.AppDataDirectory/snusstop.db3`); JSON-serialize `Motivations`.
- [ ] Step 2: Register repositories + `AppDatabase` as singletons in `MauiProgram`.
- [ ] Step 3: `dotnet build -f net10.0-android` → PASS. Commit `feat: SQLite data layer`.

### Task 2.3: `ClockService` + DI registration of Core services

**Files:** Create `Services/ClockService.cs` (INotifyPropertyChanged `Now`, 1s `IDispatcherTimer`, Start/Stop); Modify `MauiProgram.cs` (register ClockService singleton, all VMs & pages transient).

- [ ] Step 1: Implement; register. Step 2: build → PASS. Commit `feat: clock service + DI`.

### Task 2.4: `PuffMascot` control (drawn)

**Files:** Create `Controls/PuffMascot.cs` (`GraphicsView` subclass + `IDrawable`), `Controls/PuffDrawable.cs`.

**Interfaces — Produces:** `class PuffMascot : GraphicsView` bindable props `double Diameter`, `PuffExpression Expression {Normal,Happy,Thinking}`, `bool Cheeks`, `bool Float`. Draws: mint `#BDF0D4` circle, ink eyes (dots for Normal/Thinking, upward arcs for Happy), smile arc (down-open), optional `#FFB1A6` cheeks, optional 💭 for Thinking, inset bottom highlight. `Float=true` runs a 3.2s translateY loop.

- [ ] Step 1: Implement drawable for all three expressions scaling with `Diameter` (match frames `1a` 120px, `1h` 40px w/ cheeks, `1m` thinking, `1t` happy).
- [ ] Step 2: Add to a temporary harness page, build → PASS. Commit `feat: PuffMascot drawn control`.

### Task 2.5: `ProgressRing` + `ChunkyButton` + `ConfettiView` controls

**Files:** Create `Controls/ProgressRing.cs` (+drawable), `Controls/ChunkyButton.cs`, `Controls/ConfettiView.cs`.

**Interfaces — Produces:**
- `ProgressRing : GraphicsView` props `double Progress(0..1)`, `double StrokeWidth`, `Color TrackColor`, `Color FillColor`, round caps, start at −90°. Sizes: 232px (Home), 74px (Health donut).
- `ChunkyButton : ContentView` props `string Text`, `Color Fill`, `Color Shadow`, `Color TextColor`, `ICommand Command`, `double CornerRadius=18`, height 56 default; press animates content down 3px and collapses the shadow.
- `ConfettiView : GraphicsView` static scatter of colored dots/rects (frames `1g`,`1t`).

- [ ] Step 1: Implement all three; Step 2: build → PASS. Commit `feat: ring, chunky button, confetti controls`.

---

## Phase 3 — Onboarding wizard

### Task 3.1: OnboardingViewModel + host page

**Files:** Create `ViewModels/OnboardingViewModel.cs`, `Views/Onboarding/OnboardingPage.xaml(.cs)`.

**Interfaces — Consumes:** repositories, `NotificationService` (Phase 5 — guard null). **Produces:** writes `Profile{OnboardingComplete=true}`, navigates to `MainTabsPage`.

- [ ] Step 1: VM holds Step (0–5), Name, PouchesPerDay(15), PouchesPerCan(20), CanPrice(45), Currency, Motivations, AlreadyQuit(bool), QuitDate/Time; commands Next/Back/Finish; live-computed `CansPerWeek`,`PerPouch`,`PerDay`,`DayNum`,`YearSave` via `StatsCalculator`.
- [ ] Step 2: Host page = progress header (back chip + track + `n/6`) + a swappable step content region bound to `Step`. Build → PASS. Commit `feat: onboarding shell + VM`.

### Task 3.2: Onboarding step views 1a–1g

**Files:** Create `Views/Onboarding/Step*View.xaml` — `WelcomeView`(1a), `NameView`(1b), `HabitsView`(1c), `CostView`(1d), `QuitDateView`(1e), `MotivationView`(1f), `SummaryView`(1g).

- [ ] Step 1: Recreate each frame pixel-faithfully from dc.html using token styles + controls (`PuffMascot`, `ChunkyButton`, `StepperCard`, `CurrencyChip`, `SegmentedControl`, `CalendarGrid`, `ConfettiView`). Wire to VM. QuitDate: "already quit" (past date) vs "future date" → future path sets a get-ready countdown flag.
- [ ] Step 2: Build → PASS; launch to onboarding, walk all 6 steps (see Verification). Commit `feat: onboarding step views 1a-1g`.

---

## Phase 4 — Main tabs

### Task 4.1: `MainShellViewModel` + `MainTabsPage` + `BottomNavBar`

**Files:** Create `ViewModels/MainShellViewModel.cs`, `Views/MainTabsPage.xaml(.cs)`, `Controls/BottomNavBar.cs`.

**Interfaces — Produces:** `MainShellViewModel.CurrentTab (enum Tab{Home,Goals,Health,Journey})`, `OpenSosCommand`. `BottomNavBar`: 5 slots (Home,Goals,SOS,Health,Journey), center overhang −24px, `0 4px 0` coral shadow, 2.4s pulse, active label primary/900, inactive grayscale+50%; raises `TabSelected` and `SosTapped`.

- [ ] Step 1: `MainTabsPage` = Grid(content region + BottomNavBar). Content swaps the 4 tab views on `CurrentTab`. SOS → `PushModalAsync(SosTakeoverPage)`.
- [ ] Step 2: Build → PASS. Commit `feat: main tabs shell + bottom nav`.

### Task 4.2: Home view (1h/1j)

**Files:** Create `Views/Tabs/HomeView.xaml(.cs)`, `ViewModels/HomeViewModel.cs`, `Controls/StatCard.cs`.

- [ ] Step 1: HomeVM subscribes to `ClockService.Now`; exposes `dd/hh/mm/ss`, `MoneyStr`, `PouchesAvoided`, `Streak`, `RingProgress`, `MsLeftLabel`, XP (`Level 4 · Fresh Air`, 340/500), check-in command (+10 XP, once/day). Recreate frame 1h: header (PuffMascot 40 + cheeks, "Hey {name} 👋", LVL pill) tappable→Settings; 232px `ProgressRing` hero with centered ticker; milestone chip; 3 `StatCard`s; XP card; check-in chip.
- [ ] Step 2: Build → PASS; run, confirm ticker increments each second and money rises. Commit `feat: home dashboard`.

### Task 4.3: Goals view + empty state + add/edit (1l/3a/1m)

**Files:** Create `Views/Tabs/GoalsView.xaml(.cs)`, `Views/GoalEditPage.xaml(.cs)`, `ViewModels/GoalsViewModel.cs`, `Controls/PillToggle.cs`.

- [ ] Step 1: GoalsVM: saved pill, `SplitSavings` toggle, goal cards (photo slot via `MediaPicker`, name/price, progress bar, % funded / kr-to-go computed from money + allocation order), dashed add button, trophy shelf (funded goals). Empty state (1m) when no goals. Add/Edit modal writes `GoalItem`.
- [ ] Step 2: Build → PASS; add a goal, see funding progress. Commit `feat: goals tab + editor`.

### Task 4.4: Health view (1n/3b)

**Files:** Create `Views/Tabs/HealthView.xaml(.cs)`, `ViewModels/HealthViewModel.cs`.

- [ ] Step 1: Recovery donut (74px `ProgressRing`, coral, % = weighted milestone progress); vertical timeline from `Milestone.All` with done/in-progress(2px primary border + own bar)/locked(🔒, 62% opacity) states computed from elapsed. Body copy verbatim from frame 1n.
- [ ] Step 2: Build → PASS. Commit `feat: health recovery timeline`.

### Task 4.5: Journey view (1o/3c) + `CalendarGrid`

**Files:** Create `Views/Tabs/JourneyView.xaml(.cs)`, `ViewModels/JourneyViewModel.cs`, `Controls/CalendarGrid.cs`.

- [ ] Step 1: "N of 24 badges" grid (earned = mint + 3px primary ring; newest = gold + ✨ shimmer 1.6s; locked = grayscale 45%) from `BadgeService`. `CalendarGrid`: current month, clean=mint pill, today=primary pill, craving-won=gold dot, slip=coral, with legend + insight line.
- [ ] Step 2: Build → PASS. Commit `feat: journey badges + calendar`.

---

## Phase 5 — SOS, games, notifications

### Task 5.1: SOS takeover + Breathe + Remind why + Defeated + Slip + Post-slip (1p,1r,1s,1t,1u,1v)

**Files:** Create `Views/Sos/SosTakeoverPage`, `BreathePage`, `RemindWhyPage`, `CravingDefeatedPage`, `SlipPage`, `PostSlipPage` (+ VMs); `SosViewModel`.

- [ ] Step 1: SOS home (1p, bg #232140): three option cards (Breathe/Distract/Remind) + "I slipped" link. Breathe (1r): three concentric lavender circles on the 19s 4-7-8 keyframe (scale animation loop 4→7→8), phase label + cycle dots. Remind why (1s, bg #0E3B2A): motivation quote cards + nearest-goal card. Defeated (1t): confetti + happy Puff + `+15 XP`/total-wins chips → logs `CravingWon`. Slip (1u): keep/reset two-column, trigger chips, note → logs `Slip` (streak→0, restart), CTA. Post-slip (1v): fresh streak + trigger-pattern bars + tip.
- [ ] Step 2: Build → PASS; open SOS from nav, beat a craving (celebration), log a slip (streak resets, money unchanged). Commit `feat: SOS flow + slip`.

### Task 5.2: Game picker + Minesweeper (2a, 2b)

**Files:** Create `Views/Games/GamePickerPage`, `MinesweeperPage(.xaml.cs)`, `ViewModels/MinesweeperViewModel.cs`.

- [ ] Step 1: Picker (2a): 4 cards (Minesweeper/Pouch Pop/Memory/Reflex) with XP chips → push game pages. Minesweeper (2b): bind `MinesweeperEngine` to an 8×8 grid in a dark inset panel; hidden = lavender checkerboard + inset shadow; revealed numbers colored (`1 #8FB8F8, 2 #7EE0A9, 3 #FF9D94`); 🚩; Dig/Flag toggle; mines-left + timer; win → +15 XP → CravingDefeated.
- [ ] Step 2: Build → PASS; play a full Minesweeper win. Commit `feat: game picker + minesweeper`.

### Task 5.3: Pouch Pop, Memory Match, Reflex Tap

**Files:** Create `Views/Games/PouchPopPage`, `MemoryMatchPage`, `ReflexTapPage` (+ VMs). Memory uses `MemoryMatchEngine`.

- [ ] Step 1: Pouch Pop — endless bubble grid, tap pops (scale/opacity + haptic), +10 XP after a run. Memory — 4×4 from engine, flip/match, 2-min timer, +10 XP on clear. Reflex — dots appear/vanish at random positions, tap before timeout, 60s, +10 XP. Each win → CravingDefeated + `CravingWon` event (source=game).
- [ ] Step 2: Build → PASS; play each to a win. Commit `feat: pouch pop, memory, reflex games`.

### Task 5.4: NotificationService (real scheduling)

**Files:** Create `Services/NotificationService.cs`; Modify onboarding finish + Settings toggles to (re)schedule.

**Interfaces — Produces:** `class NotificationService { Task RequestPermissionAsync(); void ScheduleMilestones(Profile,Stats); void ScheduleDaily(bool); void ScheduleWeekly(bool); }` via `Plugin.LocalNotification`. Copy from frame 1y (milestone CLAIM BADGE / daily 08:00 / weekly Sun 18:00).

- [ ] Step 1: Implement scheduling + Android-13 permission after onboarding.
- [ ] Step 2: Build → PASS; trigger a near-term test notification. Commit `feat: local notifications`.

---

## Phase 6 — Settings, CSV, widgets

### Task 6.1: Settings page (1x) + CSV export

**Files:** Create `Views/SettingsPage.xaml(.cs)`, `ViewModels/SettingsViewModel.cs`, `Services/CsvExportService.cs` (wraps Core `CsvExporter` + `Share`).

- [ ] Step 1: Grouped lists per 1x: MY QUIT PLAN (edit quit date/time, usage, price, currency → writes Profile, reschedules), NOTIFICATIONS (three toggles → NotificationService), DATA (CSV export via share sheet; destructive "Reset all progress" with confirm → `EventRepository.ResetAsync` + profile reset). Footer "SnusStop 1.0 · Made with 💚 in Denmark".
- [ ] Step 2: Build → PASS; export CSV, toggle a setting. Commit `feat: settings + CSV export`.

### Task 6.2: Native Android home-screen widgets (1w)

**Files:** Create `Platforms/Android/Widgets/SnusWidgetProvider.cs` (`AppWidgetProvider`), `Platforms/Android/Resources/layout/widget_2x2.xml`, `widget_4x2.xml`, `Platforms/Android/Resources/xml/widget_2x2_info.xml`, `widget_4x2_info.xml`, drawables (rounded bg light/dark); Modify `Platforms/Android/AndroidManifest.xml` (register receivers); Create `Services/WidgetUpdateService.cs` + `Platforms/Android` impl.

- [ ] Step 1: Persist a compact widget snapshot (day, hour, moneyInt, streak, milestone%) to shared prefs on app events. `SnusWidgetProvider.OnUpdate` builds RemoteViews for 2×2 (day+hour, money pill) and 4×2 (day | money | milestone bar + streak), light/dark via `-night` resources; schedule ~30-min refresh via `PeriodicWorkRequest`.
- [ ] Step 2: Build → PASS; add both widgets to the launcher, confirm live values. Commit `feat: android home-screen widgets`.

---

## Phase 7 — Release polish

### Task 7.1: App icon, splash, animation & a11y pass

**Files:** Replace `Resources/AppIcon/appicon.svg` + `appiconfg.svg` (mint rounded square + Puff), `Resources/Splash/splash.svg`; add `SemanticProperties` to interactive elements; verify float/pulse/breathe/shimmer/press animations across screens.

- [ ] Step 1: New icon/splash from the SnusStop mark; a11y labels; animation sweep. Build → PASS. Commit `feat: icon, splash, a11y, animation polish`.

### Task 7.2: Release AAB + signing docs

**Files:** Create `RELEASE.md` (keystore generation, `dotnet publish -f net10.0-android -c Release`, AAB location, Play upload checklist).

- [ ] Step 1: `dotnet publish -f net10.0-android -c Release -p:AndroidPackageFormat=aab` → produces signed-config AAB (user supplies keystore). Document steps.
- [ ] Step 2: Commit `docs: release + signing guide`.

---

## Verification (per phase)

- **Logic (Phase 1):** `dotnet test SnusStop.Core.Tests` all green.
- **Build gate (every UI task):** `dotnet build -f net10.0-android -c Debug` → 0 errors.
- **Runtime (where a device/emulator is available):** launch via `dotnet build -t:Run -f net10.0-android`; drive the affected flow and observe (ticker increments, money rises, slip resets streak only, a game win reaches the celebration, a widget shows live values). Use the `verify` skill before claiming a nontrivial change works.

## Self-review notes

- Spec coverage: onboarding (3.2), home/goals/health/journey (4.2–4.5), SOS+all frames (5.1), 4 games (5.2–5.3), notifications (5.4), settings/CSV (6.1), widgets (6.2), mascot (2.4), theming (2.1), persistence (2.2), release (7.2) — all mapped.
- Types consistent across tasks (`StatsCalculator.Stats`, `MinesweeperEngine`, repository interfaces reused verbatim).
- No TBDs; pixel markup delegated to named dc.html frames by design (frames are the exact spec, not a placeholder).
