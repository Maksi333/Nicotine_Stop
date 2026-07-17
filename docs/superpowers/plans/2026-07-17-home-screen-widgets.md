# Home-screen Widgets Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship three production-ready Android home-screen widgets — Compact 2×2 (5a style), Progress-ring 4×2 (5b), and Puff+SOS 4×2 (5c) — with live, theme-aware values and a widget-to-SOS deep link.

**Architecture:** Three `AppWidgetProvider`s render `RemoteViews` layouts. All values are recomputed live in the widget process by the existing `WidgetData.Read` (raw plan snapshot → `StatsCalculator.Compute` against `DateTime.UtcNow`). The 5b ring is drawn to a `Bitmap` (RemoteViews can't host custom views). The 5c SOS button fires a `PendingIntent` into `MainActivity`, which routes through a shared `WidgetNavigation` bridge to the existing in-app SOS takeover.

**Tech Stack:** .NET MAUI (`net10.0-android`, minSdk 24), Xamarin.Android RemoteViews / AppWidgetProvider, Android resource XML (`layout`, `xml`, `drawable`, `values`/`values-night`), `SnusStop.Core` (unchanged) for stats/formatting.

## Global Constraints

- **RemoteViews inflation:** widget layouts may contain ONLY `@RemoteView` classes — `LinearLayout`, `FrameLayout`, `RelativeLayout`, `TextView`, `ImageView`, `ProgressBar`, etc. A plain `<View>` throws "Class not allowed to be inflated". Use `ImageView` for 1px dividers/dots.
- **Live values, no frozen snapshots:** every provider reads `WidgetData.Read(prefs)` in `OnUpdate`; never store pre-computed day/money values. `updatePeriodMillis="3600000"` (hourly) on every widget.
- **Theme:** follow the system theme via `values/colors.xml` + `values-night/colors.xml`. The bitmap ring resolves colors from these resources at draw time.
- **Money formatting:** always `StatsCalculator.FormatMoney` (symbol) or `FormatNumber` (no symbol). Never hand-build a currency string.
- **Addiction-aware copy:** the "skipped" caption comes from `AddictionCopy.For(addiction).SkippedCaption` ("pouches skipped" / "cigarettes not smoked").
- **Puff is a swappable placeholder** — one vector drawable, scaled; not final brand art.
- **Pre-release:** no live installs; renaming widget provider classes is acceptable.
- **Testing reality:** `AppWidgetProvider`/`RemoteViews` cannot be unit-tested. Widget tasks are verified by (a) a clean `dotnet build`, (b) on-device render/interaction, and (c) the existing `SnusStop.Core` unit tests staying green (they cover the stats/formatting the widgets display). No new unit tests are added because no new pure logic is introduced.

**Colors (exact, both themes):**

| Token | Light | Dark | Notes |
|---|---|---|---|
| `widget_card` | `#FDFEFD` | `#1A2E27` | exists |
| `widget_ink` | `#17322B` | `#ECF6F0` | exists |
| `widget_muted` | `#8AA398` | `#92AC9F` | exists |
| `widget_primary` | `#0E8A50` | `#2BD98A` | exists (money text) |
| `widget_pill` | `#EAF7F0` | `#15382B` | exists (mint chip) |
| `widget_flame` | `#E8590C` | `#FF9D66` | exists |
| `widget_track` | `#E9F3EC` | `#23453A` | exists (bar track) |
| `widget_ring_track` | `#E3EEE7` | `#23453A` | NEW |
| `widget_fill` | `#14B36B` | `#2BD98A` | NEW (ring/bar fill) |
| `widget_chip_coral` | `#FDEEEC` | `#3A2320` | NEW |
| `widget_coral_text` | `#D94F44` | `#FF9D94` | NEW |
| `widget_sos` | `#FF6B5E` | `#FF6B5E` | NEW |
| `widget_sos_shadow` | `#D94F44` | `#B23A31` | NEW |

---

### Task 1: Color tokens + Puff vector drawable (foundation)

**Files:**
- Modify: `Platforms/Android/Resources/values/colors.xml`
- Modify: `Platforms/Android/Resources/values-night/colors.xml`
- Create: `Platforms/Android/Resources/drawable/widget_puff.xml`

**Interfaces:**
- Produces: color resources `widget_ring_track`, `widget_fill`, `widget_chip_coral`, `widget_coral_text`, `widget_sos`, `widget_sos_shadow`; drawable `@drawable/widget_puff`.

- [ ] **Step 1: Add light-theme tokens.** Insert before the closing `</resources>` in `Platforms/Android/Resources/values/colors.xml`:

```xml
    <!-- Widget: ring, bars, chips, SOS (added for 5a/5b/5c widgets) -->
    <color name="widget_ring_track">#E3EEE7</color>
    <color name="widget_fill">#14B36B</color>
    <color name="widget_chip_coral">#FDEEEC</color>
    <color name="widget_coral_text">#D94F44</color>
    <color name="widget_sos">#FF6B5E</color>
    <color name="widget_sos_shadow">#D94F44</color>
```

- [ ] **Step 2: Add dark-theme tokens.** Insert before the closing `</resources>` in `Platforms/Android/Resources/values-night/colors.xml`:

```xml
    <!-- Widget: ring, bars, chips, SOS (added for 5a/5b/5c widgets) -->
    <color name="widget_ring_track">#23453A</color>
    <color name="widget_fill">#2BD98A</color>
    <color name="widget_chip_coral">#3A2320</color>
    <color name="widget_coral_text">#FF9D94</color>
    <color name="widget_sos">#FF6B5E</color>
    <color name="widget_sos_shadow">#B23A31</color>
```

- [ ] **Step 3: Create the Puff vector drawable.** Create `Platforms/Android/Resources/drawable/widget_puff.xml`. Mint face, ink eyes, ink smile stroke, coral cheeks — the same primitive "Puff" used in-app, as a swappable asset:

```xml
<?xml version="1.0" encoding="utf-8"?>
<!-- Placeholder "Puff" mascot. Swap this single asset for final brand art later.
     100x100 viewport; scales to any dp size the layout requests (18dp compact, 78dp on 5c). -->
<vector xmlns:android="http://schemas.android.com/apk/res/android"
    android:width="78dp"
    android:height="78dp"
    android:viewportWidth="100"
    android:viewportHeight="100">
    <!-- Face -->
    <path android:fillColor="#BDF0D4"
        android:pathData="M50,2 C76.5,2 98,23.5 98,50 C98,76.5 76.5,98 50,98 C23.5,98 2,76.5 2,50 C2,23.5 23.5,2 50,2 Z" />
    <!-- Cheeks -->
    <path android:fillColor="#FFB1A6" android:fillAlpha="0.85"
        android:pathData="M18,54.5 a6,3.5 0 1,0 12,0 a6,3.5 0 1,0 -12,0 Z" />
    <path android:fillColor="#FFB1A6" android:fillAlpha="0.85"
        android:pathData="M70,54.5 a6,3.5 0 1,0 12,0 a6,3.5 0 1,0 -12,0 Z" />
    <!-- Eyes -->
    <path android:fillColor="#17322B"
        android:pathData="M34,39 a4,5 0 1,0 8,0 a4,5 0 1,0 -8,0 Z" />
    <path android:fillColor="#17322B"
        android:pathData="M58,39 a4,5 0 1,0 8,0 a4,5 0 1,0 -8,0 Z" />
    <!-- Smile -->
    <path android:strokeColor="#17322B" android:strokeWidth="3.5"
        android:strokeLineCap="round" android:fillColor="#00000000"
        android:pathData="M39,58 Q50,69 61,58" />
</vector>
```

- [ ] **Step 4: Verify the project still builds.**

Run: `dotnet build "Nicotine_Stop.csproj" -c Debug`
Expected: `Build succeeded` with 0 errors (resource compilation validates the new color XML and vector drawable).

- [ ] **Step 5: Commit.**

```bash
git add Platforms/Android/Resources/values/colors.xml Platforms/Android/Resources/values-night/colors.xml Platforms/Android/Resources/drawable/widget_puff.xml
git commit -m "feat(widgets): add ring/chip/SOS color tokens and Puff vector drawable"
```

---

### Task 2: Extend widget view-model (`WidgetView` + `WidgetData.Read`)

**Files:**
- Modify: `Platforms/Android/Widgets/SnusWidgets.cs` (the `WidgetView` record and `WidgetData.Read` only)

**Interfaces:**
- Consumes: `StatsCalculator.Compute`, `StatsCalculator.FormatMoney/FormatNumber/DaysUntilNextLabel`, `AddictionCopy.For(...).SkippedCaption/SkippedShort` (all existing in `SnusStop.Core`).
- Produces: extended `WidgetView` with new members `int Days`, `string MoneyInt`, `double RingProgress`, `string SkippedCaption` (in addition to existing `Day, Money, Streak, Milestone, MilestonePct, DaysLeft, Avoided, AvoidedCap, Wins`). Consumed by Tasks 3–5.

- [ ] **Step 1: Replace the `WidgetView` record struct.** In `Platforms/Android/Widgets/SnusWidgets.cs`, replace the existing declaration:

```csharp
/// <summary>The values a widget renders, recomputed for the current moment.</summary>
internal readonly record struct WidgetView(
    string Day, string Money, int Streak, string Milestone,
    int MilestonePct, string DaysLeft, int Avoided, string AvoidedCap, int Wins);
```

with (adds the four fields the new widgets need; keeps the old ones so nothing else breaks mid-refactor):

```csharp
/// <summary>The values a widget renders, recomputed for the current moment.</summary>
internal readonly record struct WidgetView(
    int Days,              // raw day count — ring centre (5b) and "Day N" message (5c)
    string Day,            // "12d 4h" — compact hero (5a)
    string Money,          // "156 kr" — amount with the user's currency symbol
    string MoneyInt,       // "156" — grouped amount, NO symbol, for the tight 5c chip
    int Streak,
    string Milestone,      // next-milestone title, e.g. "2 weeks"
    int MilestonePct,      // 0..100, progress toward the next milestone (bars)
    double RingProgress,   // 0..1, same progress for the drawn ring arc (5b)
    string DaysLeft,       // "2 days"
    int Avoided,
    string SkippedCaption, // addiction-aware: "pouches skipped" / "cigarettes not smoked"
    string AvoidedCap,     // addiction-aware short: "skipped" / "not smoked"
    int Wins);
```

- [ ] **Step 2: Update `WidgetData.Read` to populate the new fields.** Replace the two `return new WidgetView(...)` statements in `WidgetData.Read`:

Replace the no-plan branch:

```csharp
        // No plan stored yet (widget added before the app was opened). Show neutral zeros.
        if (quitTicks <= 0)
            return new WidgetView("0d 0h", "0", 0, "next milestone", 0, "", 0, "skipped", wins);
```

with:

```csharp
        // No plan stored yet (widget added before the app was opened). Show neutral zeros.
        if (quitTicks <= 0)
            return new WidgetView(
                Days: 0, Day: "0d 0h", Money: "0", MoneyInt: "0", Streak: 0,
                Milestone: "next milestone", MilestonePct: 0, RingProgress: 0d, DaysLeft: "",
                Avoided: 0, SkippedCaption: "skipped", AvoidedCap: "skipped", Wins: wins);
```

Replace the normal branch:

```csharp
        return new WidgetView(
            Day: $"{s.Days}d {s.Hours}h",
            Money: StatsCalculator.FormatMoney(s.Money, profile.Currency, false),
            Streak: s.CurrentStreak,
            Milestone: s.Next?.Title ?? "1 year",
            MilestonePct: (int)(s.RingProgress * 100),
            DaysLeft: StatsCalculator.DaysUntilNextLabel(s),
            Avoided: s.PouchesAvoided,
            AvoidedCap: AddictionCopy.For(profile.Addiction).SkippedShort,
            Wins: wins);
```

with:

```csharp
        var copy = AddictionCopy.For(profile.Addiction);
        return new WidgetView(
            Days: s.Days,
            Day: $"{s.Days}d {s.Hours}h",
            Money: StatsCalculator.FormatMoney(s.Money, profile.Currency, false),
            MoneyInt: StatsCalculator.FormatNumber(s.Money, profile.Currency),
            Streak: s.CurrentStreak,
            Milestone: s.Next?.Title ?? "1 year",
            MilestonePct: (int)(s.RingProgress * 100),
            RingProgress: s.RingProgress,
            DaysLeft: StatsCalculator.DaysUntilNextLabel(s),
            Avoided: s.PouchesAvoided,
            SkippedCaption: copy.SkippedCaption,
            AvoidedCap: copy.SkippedShort,
            Wins: wins);
```

- [ ] **Step 3: Verify the project builds.** (The existing `SnusWidget2x2`/`SnusWidget4x2` providers still reference only old fields, so they compile unchanged.)

Run: `dotnet build "Nicotine_Stop.csproj" -c Debug`
Expected: `Build succeeded`, 0 errors.

- [ ] **Step 4: Run the core unit tests** (confirm the stats/formatting the widget relies on are unaffected).

Run: `dotnet test "SnusStop.Core.Tests/SnusStop.Core.Tests.csproj"`
Expected: all tests pass.

- [ ] **Step 5: Commit.**

```bash
git add Platforms/Android/Widgets/SnusWidgets.cs
git commit -m "feat(widgets): surface Days, MoneyInt, RingProgress, SkippedCaption on WidgetView"
```

---

### Task 3: Compact 2×2 widget (design 5a)

**Files:**
- Create: `Platforms/Android/Resources/layout/widget_compact.xml`
- Create: `Platforms/Android/Resources/xml/widget_compact_info.xml`
- Create: `Platforms/Android/Widgets/WidgetIntents.cs`
- Modify: `Platforms/Android/Widgets/SnusWidgets.cs` (replace `SnusWidget2x2` class with `SnusWidgetCompact`)

**Interfaces:**
- Consumes: `WidgetView` (Task 2), `WidgetData.Read`, `@drawable/widget_puff` (Task 1), color tokens.
- Produces: `WidgetIntents.Home(Context, int)` → `PendingIntent` (reused by Tasks 4–5); provider `SnusWidgetCompact`.

- [ ] **Step 1: Create the Home-intent helper.** Create `Platforms/Android/Widgets/WidgetIntents.cs`:

```csharp
using Android.App;
using Android.Content;

namespace Nicotine_Stop.Platforms.Android.Widgets;

/// <summary>
/// Builds the PendingIntents widgets attach to their tap targets. Distinct request codes keep the
/// system from collapsing the "open Home" and (later) "open SOS" intents into one another.
/// </summary>
internal static class WidgetIntents
{
    // Distinct request codes per (widget, target) so PendingIntents stay independent.
    public const int CompactHome = 10;
    public const int RingHome = 20;
    public const int SosHome = 30;
    public const int SosButton = 31;

    /// <summary>Opens the app on its normal Home screen.</summary>
    public static PendingIntent Home(Context context, int requestCode)
    {
        var intent = new Intent(context, typeof(MainActivity));
        intent.SetFlags(ActivityFlags.SingleTop);
        return PendingIntent.GetActivity(
            context, requestCode, intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)!;
    }
}
```

- [ ] **Step 2: Create the compact layout.** Create `Platforms/Android/Resources/layout/widget_compact.xml`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<!-- 2x2 compact widget (design 5a): brand mark + streak, day hero, money, milestone bar.
     Only @RemoteView classes (LinearLayout/TextView/ImageView/ProgressBar) — no plain <View>. -->
<LinearLayout xmlns:android="http://schemas.android.com/apk/res/android"
    android:id="@+id/w_root"
    android:layout_width="match_parent"
    android:layout_height="match_parent"
    android:orientation="vertical"
    android:background="@drawable/widget_bg"
    android:padding="16dp">

    <LinearLayout
        android:layout_width="match_parent"
        android:layout_height="wrap_content"
        android:orientation="horizontal"
        android:gravity="center_vertical">
        <ImageView
            android:layout_width="18dp"
            android:layout_height="18dp"
            android:contentDescription="@null"
            android:src="@drawable/widget_puff" />
        <TextView
            android:layout_width="wrap_content"
            android:layout_height="wrap_content"
            android:layout_marginStart="6dp"
            android:text="SNUSSTOP"
            android:textSize="10sp"
            android:textStyle="bold"
            android:letterSpacing="0.08"
            android:textColor="@color/widget_muted" />
        <TextView
            android:layout_width="0dp"
            android:layout_height="wrap_content"
            android:layout_weight="1" />
        <TextView
            android:id="@+id/w_streak"
            android:layout_width="wrap_content"
            android:layout_height="wrap_content"
            android:text="🔥 0"
            android:textSize="13sp"
            android:textStyle="bold"
            android:textColor="@color/widget_flame" />
    </LinearLayout>

    <TextView
        android:id="@+id/w_day"
        android:layout_width="wrap_content"
        android:layout_height="0dp"
        android:layout_weight="1"
        android:gravity="center_vertical"
        android:text="0d 0h"
        android:textSize="26sp"
        android:textStyle="bold"
        android:maxLines="1"
        android:fontFamily="sans-serif-black"
        android:textColor="@color/widget_ink" />

    <LinearLayout
        android:layout_width="wrap_content"
        android:layout_height="wrap_content"
        android:orientation="horizontal"
        android:gravity="center_vertical">
        <TextView
            android:id="@+id/w_money"
            android:layout_width="wrap_content"
            android:layout_height="wrap_content"
            android:text="0 kr"
            android:textSize="17sp"
            android:textStyle="bold"
            android:maxLines="1"
            android:fontFamily="sans-serif-black"
            android:textColor="@color/widget_primary" />
        <TextView
            android:layout_width="wrap_content"
            android:layout_height="wrap_content"
            android:layout_marginStart="6dp"
            android:text="saved"
            android:textSize="10sp"
            android:textStyle="bold"
            android:textColor="@color/widget_muted" />
    </LinearLayout>

    <LinearLayout
        android:layout_width="match_parent"
        android:layout_height="wrap_content"
        android:orientation="horizontal"
        android:gravity="center_vertical"
        android:layout_marginTop="8dp">
        <ProgressBar
            android:id="@+id/w_progress"
            style="@android:style/Widget.ProgressBar.Horizontal"
            android:layout_width="0dp"
            android:layout_weight="1"
            android:layout_height="8dp"
            android:max="100"
            android:progress="0"
            android:progressTint="@color/widget_fill"
            android:progressBackgroundTint="@color/widget_track" />
        <TextView
            android:id="@+id/w_ms"
            android:layout_width="wrap_content"
            android:layout_height="wrap_content"
            android:layout_marginStart="8dp"
            android:text="2 weeks"
            android:textSize="10sp"
            android:textStyle="bold"
            android:maxLines="1"
            android:textColor="@color/widget_muted" />
    </LinearLayout>
</LinearLayout>
```

- [ ] **Step 3: Create the compact provider metadata.** Create `Platforms/Android/Resources/xml/widget_compact_info.xml`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<appwidget-provider xmlns:android="http://schemas.android.com/apk/res/android"
    android:minWidth="110dp"
    android:minHeight="110dp"
    android:targetCellWidth="2"
    android:targetCellHeight="2"
    android:updatePeriodMillis="3600000"
    android:initialLayout="@layout/widget_compact"
    android:previewLayout="@layout/widget_compact"
    android:resizeMode="none"
    android:widgetCategory="home_screen" />
```

- [ ] **Step 4: Replace the `SnusWidget2x2` provider with `SnusWidgetCompact`.** In `Platforms/Android/Widgets/SnusWidgets.cs`, replace the entire `SnusWidget2x2` class:

```csharp
[BroadcastReceiver(Label = "SnusStop · day & money", Exported = false)]
[IntentFilter(new[] { AppWidgetManager.ActionAppwidgetUpdate })]
[MetaData("android.appwidget.provider", Resource = "@xml/widget_2x2_info")]
public class SnusWidget2x2 : AppWidgetProvider
{
    public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
    {
        var d = WidgetData.Read(context.GetSharedPreferences(WidgetPrefs.Name, FileCreationMode.Private)!);
        foreach (var id in appWidgetIds)
        {
            var v = new RemoteViews(context.PackageName, Resource.Layout.widget_2x2);
            v.SetTextViewText(Resource.Id.w_day, d.Day);
            v.SetTextViewText(Resource.Id.w_money, d.Money);
            appWidgetManager.UpdateAppWidget(id, v);
        }
    }
}
```

with:

```csharp
[BroadcastReceiver(Label = "SnusStop · Compact", Exported = false)]
[IntentFilter(new[] { AppWidgetManager.ActionAppwidgetUpdate })]
[MetaData("android.appwidget.provider", Resource = "@xml/widget_compact_info")]
public class SnusWidgetCompact : AppWidgetProvider
{
    public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
    {
        var d = WidgetData.Read(context.GetSharedPreferences(WidgetPrefs.Name, FileCreationMode.Private)!);
        foreach (var id in appWidgetIds)
        {
            var v = new RemoteViews(context.PackageName, Resource.Layout.widget_compact);
            v.SetTextViewText(Resource.Id.w_day, d.Day);
            v.SetTextViewText(Resource.Id.w_money, d.Money);
            v.SetTextViewText(Resource.Id.w_streak, $"🔥 {d.Streak}");
            v.SetTextViewText(Resource.Id.w_ms, d.Milestone);
            v.SetProgressBar(Resource.Id.w_progress, 100, d.MilestonePct, false);
            v.SetOnClickPendingIntent(Resource.Id.w_root, WidgetIntents.Home(context, WidgetIntents.CompactHome));
            appWidgetManager.UpdateAppWidget(id, v);
        }
    }
}
```

- [ ] **Step 5: Verify the project builds.**

Run: `dotnet build "Nicotine_Stop.csproj" -c Debug`
Expected: `Build succeeded`. (The old `widget_2x2.xml` / `widget_2x2_info.xml` are now unreferenced but still valid — deleted in Task 7.)

- [ ] **Step 6: Commit.**

```bash
git add Platforms/Android/Widgets/WidgetIntents.cs Platforms/Android/Widgets/SnusWidgets.cs Platforms/Android/Resources/layout/widget_compact.xml Platforms/Android/Resources/xml/widget_compact_info.xml
git commit -m "feat(widgets): 2x2 Compact widget (5a) with brand mark, streak, milestone bar"
```

---

### Task 4: Progress-ring 4×2 widget (design 5b)

**Files:**
- Create: `Platforms/Android/Resources/layout/widget_ring.xml`
- Create: `Platforms/Android/Resources/xml/widget_ring_info.xml`
- Create: `Platforms/Android/Widgets/RingBitmap.cs`
- Modify: `Platforms/Android/Widgets/SnusWidgets.cs` (replace `SnusWidget4x2` class with `SnusWidgetRing`)

**Interfaces:**
- Consumes: `WidgetView` (Task 2), `WidgetIntents.Home` (Task 3), color tokens (Task 1).
- Produces: `RingBitmap.Draw(Context, int days, double progress)` → `Bitmap`; provider `SnusWidgetRing`.

- [ ] **Step 1: Create the ring bitmap renderer.** Create `Platforms/Android/Widgets/RingBitmap.cs`:

```csharp
using Android.Content;
using Android.Graphics;
using AndroidX.Core.Content;

namespace Nicotine_Stop.Platforms.Android.Widgets;

/// <summary>
/// Draws the 5b progress ring (track + round-capped fill arc) with the day count and "days free"
/// caption centred inside it. RemoteViews cannot host a custom view, so the whole thing is rendered
/// to a Bitmap and pushed via SetImageViewBitmap. Colours come from theme resources, so it matches
/// the current light/dark configuration.
/// </summary>
internal static class RingBitmap
{
    public static Bitmap Draw(Context context, int days, double progress)
    {
        var res = context.Resources!;
        float density = res.DisplayMetrics!.Density;
        int size = (int)(118 * density);
        float stroke = 10 * density;

        var bmp = Bitmap.CreateBitmap(size, size, Bitmap.Config.Argb8888!)!;
        var canvas = new Canvas(bmp);

        var trackColor = new Color(ContextCompat.GetColor(context, Resource.Color.widget_ring_track));
        var fillColor = new Color(ContextCompat.GetColor(context, Resource.Color.widget_fill));
        var inkColor = new Color(ContextCompat.GetColor(context, Resource.Color.widget_ink));
        var mutedColor = new Color(ContextCompat.GetColor(context, Resource.Color.widget_muted));

        float inset = stroke / 2f + density;
        var rect = new RectF(inset, inset, size - inset, size - inset);

        using var track = new Paint(PaintFlags.AntiAlias) { StrokeWidth = stroke, Color = trackColor };
        track.SetStyle(Paint.Style.Stroke);
        canvas.DrawArc(rect, 0f, 360f, false, track);

        using var fill = new Paint(PaintFlags.AntiAlias) { StrokeWidth = stroke, Color = fillColor };
        fill.SetStyle(Paint.Style.Stroke);
        fill.StrokeCap = Paint.Cap.Round;
        float sweep = (float)(System.Math.Clamp(progress, 0d, 1d) * 360d);
        canvas.DrawArc(rect, -90f, sweep, false, fill);

        float cx = size / 2f;
        using var num = new Paint(PaintFlags.AntiAlias) { Color = inkColor, TextAlign = Paint.Align.Center };
        num.SetTypeface(Typeface.Create("sans-serif-black", TypefaceStyle.Bold));
        num.TextSize = 34 * density;
        using var cap = new Paint(PaintFlags.AntiAlias) { Color = mutedColor, TextAlign = Paint.Align.Center };
        cap.SetTypeface(Typeface.Create("sans-serif", TypefaceStyle.Bold));
        cap.TextSize = 10 * density;

        // Day number baseline sits just above centre; caption below it.
        canvas.DrawText(days.ToString(), cx, cx + 6 * density, num);
        canvas.DrawText("days free", cx, cx + 22 * density, cap);

        return bmp;
    }
}
```

- [ ] **Step 2: Create the ring layout.** Create `Platforms/Android/Resources/layout/widget_ring.xml`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<!-- 4x2 progress-ring widget (design 5b): drawn ring on the left, three stat rows on the right. -->
<LinearLayout xmlns:android="http://schemas.android.com/apk/res/android"
    android:id="@+id/w_root"
    android:layout_width="match_parent"
    android:layout_height="match_parent"
    android:orientation="horizontal"
    android:gravity="center_vertical"
    android:background="@drawable/widget_bg"
    android:paddingLeft="20dp"
    android:paddingRight="20dp"
    android:paddingTop="16dp"
    android:paddingBottom="16dp">

    <ImageView
        android:id="@+id/w_ring"
        android:layout_width="118dp"
        android:layout_height="118dp"
        android:layout_marginEnd="18dp"
        android:contentDescription="@null" />

    <LinearLayout
        android:layout_width="0dp"
        android:layout_height="wrap_content"
        android:layout_weight="1"
        android:orientation="vertical">

        <!-- Money -->
        <LinearLayout
            android:layout_width="match_parent"
            android:layout_height="wrap_content"
            android:orientation="horizontal"
            android:gravity="center_vertical"
            android:baselineAligned="false">
            <TextView
                android:layout_width="wrap_content"
                android:layout_height="wrap_content"
                android:layout_marginEnd="8dp"
                android:text="💰"
                android:textSize="15sp" />
            <LinearLayout
                android:layout_width="wrap_content"
                android:layout_height="wrap_content"
                android:orientation="vertical">
                <TextView
                    android:id="@+id/w_money"
                    android:layout_width="wrap_content"
                    android:layout_height="wrap_content"
                    android:text="0 kr"
                    android:textSize="17sp"
                    android:textStyle="bold"
                    android:maxLines="1"
                    android:fontFamily="sans-serif-black"
                    android:textColor="@color/widget_primary" />
                <TextView
                    android:layout_width="wrap_content"
                    android:layout_height="wrap_content"
                    android:text="saved"
                    android:textSize="10sp"
                    android:textStyle="bold"
                    android:textColor="@color/widget_muted" />
            </LinearLayout>
        </LinearLayout>

        <!-- Pouches skipped -->
        <LinearLayout
            android:layout_width="match_parent"
            android:layout_height="wrap_content"
            android:orientation="horizontal"
            android:gravity="center_vertical"
            android:layout_marginTop="9dp"
            android:baselineAligned="false">
            <TextView
                android:layout_width="wrap_content"
                android:layout_height="wrap_content"
                android:layout_marginEnd="8dp"
                android:text="🚫"
                android:textSize="15sp" />
            <LinearLayout
                android:layout_width="wrap_content"
                android:layout_height="wrap_content"
                android:orientation="vertical">
                <TextView
                    android:id="@+id/w_avoided"
                    android:layout_width="wrap_content"
                    android:layout_height="wrap_content"
                    android:text="0"
                    android:textSize="17sp"
                    android:textStyle="bold"
                    android:maxLines="1"
                    android:fontFamily="sans-serif-black"
                    android:textColor="@color/widget_ink" />
                <TextView
                    android:id="@+id/w_skipcap"
                    android:layout_width="wrap_content"
                    android:layout_height="wrap_content"
                    android:text="pouches skipped"
                    android:textSize="10sp"
                    android:textStyle="bold"
                    android:textColor="@color/widget_muted" />
            </LinearLayout>
        </LinearLayout>

        <!-- Milestone countdown -->
        <LinearLayout
            android:layout_width="match_parent"
            android:layout_height="wrap_content"
            android:orientation="horizontal"
            android:gravity="center_vertical"
            android:layout_marginTop="9dp"
            android:baselineAligned="false">
            <TextView
                android:layout_width="wrap_content"
                android:layout_height="wrap_content"
                android:layout_marginEnd="8dp"
                android:text="🏆"
                android:textSize="15sp" />
            <LinearLayout
                android:layout_width="wrap_content"
                android:layout_height="wrap_content"
                android:orientation="vertical">
                <TextView
                    android:id="@+id/w_daysleft"
                    android:layout_width="wrap_content"
                    android:layout_height="wrap_content"
                    android:text="2 days"
                    android:textSize="13sp"
                    android:textStyle="bold"
                    android:maxLines="1"
                    android:fontFamily="sans-serif-black"
                    android:textColor="@color/widget_ink" />
                <TextView
                    android:id="@+id/w_mscap"
                    android:layout_width="wrap_content"
                    android:layout_height="wrap_content"
                    android:text="until 2 weeks free"
                    android:textSize="10sp"
                    android:textStyle="bold"
                    android:textColor="@color/widget_muted" />
            </LinearLayout>
        </LinearLayout>
    </LinearLayout>
</LinearLayout>
```

- [ ] **Step 3: Create the ring provider metadata.** Create `Platforms/Android/Resources/xml/widget_ring_info.xml`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<appwidget-provider xmlns:android="http://schemas.android.com/apk/res/android"
    android:minWidth="250dp"
    android:minHeight="110dp"
    android:targetCellWidth="4"
    android:targetCellHeight="2"
    android:updatePeriodMillis="3600000"
    android:initialLayout="@layout/widget_ring"
    android:previewLayout="@layout/widget_ring"
    android:resizeMode="horizontal"
    android:widgetCategory="home_screen" />
```

- [ ] **Step 4: Replace the `SnusWidget4x2` provider with `SnusWidgetRing`.** In `Platforms/Android/Widgets/SnusWidgets.cs`, replace the entire `SnusWidget4x2` class:

```csharp
[BroadcastReceiver(Label = "SnusStop · full stats", Exported = false)]
[IntentFilter(new[] { AppWidgetManager.ActionAppwidgetUpdate })]
[MetaData("android.appwidget.provider", Resource = "@xml/widget_4x2_info")]
public class SnusWidget4x2 : AppWidgetProvider
{
    public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
    {
        var d = WidgetData.Read(context.GetSharedPreferences(WidgetPrefs.Name, FileCreationMode.Private)!);
        foreach (var id in appWidgetIds)
        {
            var v = new RemoteViews(context.PackageName, Resource.Layout.widget_4x2);
            v.SetTextViewText(Resource.Id.w_day, d.Day);
            v.SetTextViewText(Resource.Id.w_money, d.Money);
            v.SetTextViewText(Resource.Id.w_avoided, d.Avoided.ToString());
            v.SetTextViewText(Resource.Id.w_avoided_cap, d.AvoidedCap);
            v.SetTextViewText(Resource.Id.w_wins, d.Wins.ToString());
            v.SetTextViewText(Resource.Id.w_streak, $"🔥 {d.Streak}");
            v.SetTextViewText(Resource.Id.w_milestone, $"🏆 {d.Milestone} free");
            v.SetTextViewText(Resource.Id.w_daysleft, $"{d.DaysLeft} to go");
            v.SetProgressBar(Resource.Id.w_progress, 100, d.MilestonePct, false);
            appWidgetManager.UpdateAppWidget(id, v);
        }
    }
}
```

with:

```csharp
[BroadcastReceiver(Label = "SnusStop · Progress ring", Exported = false)]
[IntentFilter(new[] { AppWidgetManager.ActionAppwidgetUpdate })]
[MetaData("android.appwidget.provider", Resource = "@xml/widget_ring_info")]
public class SnusWidgetRing : AppWidgetProvider
{
    public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
    {
        var d = WidgetData.Read(context.GetSharedPreferences(WidgetPrefs.Name, FileCreationMode.Private)!);
        var ring = RingBitmap.Draw(context, d.Days, d.RingProgress);
        foreach (var id in appWidgetIds)
        {
            var v = new RemoteViews(context.PackageName, Resource.Layout.widget_ring);
            v.SetImageViewBitmap(Resource.Id.w_ring, ring);
            v.SetTextViewText(Resource.Id.w_money, d.Money);
            v.SetTextViewText(Resource.Id.w_avoided, d.Avoided.ToString());
            v.SetTextViewText(Resource.Id.w_skipcap, d.SkippedCaption);
            v.SetTextViewText(Resource.Id.w_daysleft, d.DaysLeft);
            v.SetTextViewText(Resource.Id.w_mscap, $"until {d.Milestone} free");
            v.SetOnClickPendingIntent(Resource.Id.w_root, WidgetIntents.Home(context, WidgetIntents.RingHome));
            appWidgetManager.UpdateAppWidget(id, v);
        }
    }
}
```

- [ ] **Step 5: Verify the project builds.**

Run: `dotnet build "Nicotine_Stop.csproj" -c Debug`
Expected: `Build succeeded`. (If `AndroidX.Core.Content` is unresolved, it is provided transitively by MAUI; confirm the `using AndroidX.Core.Content;` compiles — it should on this stack.)

- [ ] **Step 6: Commit.**

```bash
git add Platforms/Android/Widgets/RingBitmap.cs Platforms/Android/Widgets/SnusWidgets.cs Platforms/Android/Resources/layout/widget_ring.xml Platforms/Android/Resources/xml/widget_ring_info.xml
git commit -m "feat(widgets): 4x2 Progress-ring widget (5b) with bitmap-drawn ring"
```

---

### Task 5: Puff + SOS 4×2 widget (design 5c) — visual + Home taps

**Files:**
- Create: `Platforms/Android/Resources/layout/widget_sos.xml`
- Create: `Platforms/Android/Resources/xml/widget_sos_info.xml`
- Create: `Platforms/Android/Resources/drawable/widget_chip_mint.xml`
- Create: `Platforms/Android/Resources/drawable/widget_chip_coral.xml`
- Create: `Platforms/Android/Resources/drawable/widget_sos_circle.xml`
- Modify: `Platforms/Android/Widgets/SnusWidgets.cs` (add `SnusWidgetSos` class)

**Interfaces:**
- Consumes: `WidgetView` (Task 2), `WidgetIntents.Home` (Task 3), `@drawable/widget_puff` + color tokens (Task 1).
- Produces: provider `SnusWidgetSos` (its SOS button opens Home for now; Task 6 repoints it to the SOS deep link).

- [ ] **Step 1: Create the chip drawables.** Create `Platforms/Android/Resources/drawable/widget_chip_mint.xml`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<shape xmlns:android="http://schemas.android.com/apk/res/android" android:shape="rectangle">
    <solid android:color="@color/widget_pill" />
    <corners android:radius="9dp" />
</shape>
```

Create `Platforms/Android/Resources/drawable/widget_chip_coral.xml`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<shape xmlns:android="http://schemas.android.com/apk/res/android" android:shape="rectangle">
    <solid android:color="@color/widget_chip_coral" />
    <corners android:radius="9dp" />
</shape>
```

- [ ] **Step 2: Create the SOS circle drawable.** Create `Platforms/Android/Resources/drawable/widget_sos_circle.xml` — a coral disc over a darker disc offset 4dp down, recreating the `0 4px 0` bottom-shadow:

```xml
<?xml version="1.0" encoding="utf-8"?>
<layer-list xmlns:android="http://schemas.android.com/apk/res/android">
    <!-- Shadow disc, sitting 4dp lower -->
    <item android:top="4dp">
        <shape android:shape="oval">
            <solid android:color="@color/widget_sos_shadow" />
        </shape>
    </item>
    <!-- Coral disc on top, leaving a 4dp shadow lip at the bottom -->
    <item android:bottom="4dp">
        <shape android:shape="oval">
            <solid android:color="@color/widget_sos" />
        </shape>
    </item>
</layer-list>
```

- [ ] **Step 3: Create the 5c layout.** Create `Platforms/Android/Resources/layout/widget_sos.xml`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<!-- 4x2 Puff + SOS widget (design 5c): Puff, message + chips + milestone bar, coral SOS button. -->
<LinearLayout xmlns:android="http://schemas.android.com/apk/res/android"
    android:id="@+id/w_root"
    android:layout_width="match_parent"
    android:layout_height="match_parent"
    android:orientation="horizontal"
    android:gravity="center_vertical"
    android:background="@drawable/widget_bg"
    android:paddingLeft="18dp"
    android:paddingRight="18dp"
    android:paddingTop="16dp"
    android:paddingBottom="16dp">

    <ImageView
        android:layout_width="78dp"
        android:layout_height="78dp"
        android:layout_marginEnd="14dp"
        android:contentDescription="@null"
        android:src="@drawable/widget_puff" />

    <LinearLayout
        android:layout_width="0dp"
        android:layout_height="wrap_content"
        android:layout_weight="1"
        android:orientation="vertical">

        <TextView
            android:id="@+id/w_msg"
            android:layout_width="match_parent"
            android:layout_height="wrap_content"
            android:text="Day 0 — Puff's proud of you."
            android:textSize="13.5sp"
            android:textStyle="bold"
            android:maxLines="1"
            android:fontFamily="sans-serif-black"
            android:textColor="@color/widget_ink" />

        <LinearLayout
            android:layout_width="wrap_content"
            android:layout_height="wrap_content"
            android:orientation="horizontal"
            android:layout_marginTop="6dp">
            <TextView
                android:id="@+id/w_money2"
                android:layout_width="wrap_content"
                android:layout_height="wrap_content"
                android:background="@drawable/widget_chip_mint"
                android:paddingLeft="8dp"
                android:paddingRight="8dp"
                android:paddingTop="4dp"
                android:paddingBottom="4dp"
                android:text="💰 0"
                android:textSize="11sp"
                android:textStyle="bold"
                android:textColor="@color/widget_primary" />
            <TextView
                android:id="@+id/w_streak2"
                android:layout_width="wrap_content"
                android:layout_height="wrap_content"
                android:layout_marginStart="6dp"
                android:background="@drawable/widget_chip_coral"
                android:paddingLeft="8dp"
                android:paddingRight="8dp"
                android:paddingTop="4dp"
                android:paddingBottom="4dp"
                android:text="🔥 0d"
                android:textSize="11sp"
                android:textStyle="bold"
                android:textColor="@color/widget_coral_text" />
        </LinearLayout>

        <ProgressBar
            android:id="@+id/w_bar"
            style="@android:style/Widget.ProgressBar.Horizontal"
            android:layout_width="match_parent"
            android:layout_height="9dp"
            android:layout_marginTop="6dp"
            android:max="100"
            android:progress="0"
            android:progressTint="@color/widget_fill"
            android:progressBackgroundTint="@color/widget_track" />

        <TextView
            android:id="@+id/w_mscap"
            android:layout_width="match_parent"
            android:layout_height="wrap_content"
            android:layout_marginTop="6dp"
            android:text="🏆 0 days until 2 weeks free"
            android:textSize="9.5sp"
            android:textStyle="bold"
            android:maxLines="1"
            android:textColor="@color/widget_muted" />
    </LinearLayout>

    <FrameLayout
        android:id="@+id/w_sos"
        android:layout_width="64dp"
        android:layout_height="64dp"
        android:layout_marginStart="12dp"
        android:background="@drawable/widget_sos_circle">
        <TextView
            android:layout_width="wrap_content"
            android:layout_height="wrap_content"
            android:layout_gravity="center"
            android:text="SOS"
            android:textSize="14sp"
            android:textStyle="bold"
            android:letterSpacing="0.04"
            android:fontFamily="sans-serif-black"
            android:textColor="#FFFFFF" />
    </FrameLayout>
</LinearLayout>
```

- [ ] **Step 4: Create the 5c provider metadata.** Create `Platforms/Android/Resources/xml/widget_sos_info.xml`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<appwidget-provider xmlns:android="http://schemas.android.com/apk/res/android"
    android:minWidth="250dp"
    android:minHeight="110dp"
    android:targetCellWidth="4"
    android:targetCellHeight="2"
    android:updatePeriodMillis="3600000"
    android:initialLayout="@layout/widget_sos"
    android:previewLayout="@layout/widget_sos"
    android:resizeMode="horizontal"
    android:widgetCategory="home_screen" />
```

- [ ] **Step 5: Add the `SnusWidgetSos` provider.** In `Platforms/Android/Widgets/SnusWidgets.cs`, append after `SnusWidgetRing`:

```csharp
[BroadcastReceiver(Label = "SnusStop · Puff + SOS", Exported = false)]
[IntentFilter(new[] { AppWidgetManager.ActionAppwidgetUpdate })]
[MetaData("android.appwidget.provider", Resource = "@xml/widget_sos_info")]
public class SnusWidgetSos : AppWidgetProvider
{
    public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
    {
        var d = WidgetData.Read(context.GetSharedPreferences(WidgetPrefs.Name, FileCreationMode.Private)!);
        foreach (var id in appWidgetIds)
        {
            var v = new RemoteViews(context.PackageName, Resource.Layout.widget_sos);
            v.SetTextViewText(Resource.Id.w_msg, $"Day {d.Days} — Puff's proud of you.");
            v.SetTextViewText(Resource.Id.w_money2, $"💰 {d.MoneyInt}");
            v.SetTextViewText(Resource.Id.w_streak2, $"🔥 {d.Streak}d");
            v.SetProgressBar(Resource.Id.w_bar, 100, d.MilestonePct, false);
            v.SetTextViewText(Resource.Id.w_mscap, $"🏆 {d.DaysLeft} until {d.Milestone} free");
            v.SetOnClickPendingIntent(Resource.Id.w_root, WidgetIntents.Home(context, WidgetIntents.SosHome));
            // SOS button opens Home for now; Task 6 repoints it to the SOS deep link.
            v.SetOnClickPendingIntent(Resource.Id.w_sos, WidgetIntents.Home(context, WidgetIntents.SosButton));
            appWidgetManager.UpdateAppWidget(id, v);
        }
    }
}
```

- [ ] **Step 6: Verify the project builds.**

Run: `dotnet build "Nicotine_Stop.csproj" -c Debug`
Expected: `Build succeeded`.

- [ ] **Step 7: Commit.**

```bash
git add Platforms/Android/Widgets/SnusWidgets.cs Platforms/Android/Resources/layout/widget_sos.xml Platforms/Android/Resources/xml/widget_sos_info.xml Platforms/Android/Resources/drawable/widget_chip_mint.xml Platforms/Android/Resources/drawable/widget_chip_coral.xml Platforms/Android/Resources/drawable/widget_sos_circle.xml
git commit -m "feat(widgets): 4x2 Puff + SOS widget (5c) visuals with chips and SOS button"
```

---

### Task 6: SOS deep-link plumbing

**Files:**
- Create: `Services/WidgetNavigation.cs`
- Modify: `Platforms/Android/MainActivity.cs`
- Modify: `Views/MainTabsPage.cs`
- Modify: `Platforms/Android/Widgets/WidgetIntents.cs` (add `Sos(...)`)
- Modify: `Platforms/Android/Widgets/SnusWidgets.cs` (repoint 5c SOS button)

**Interfaces:**
- Consumes: existing `Sos.SosTakeoverPage`, `IServiceProvider`, `MainActivity`.
- Produces: `WidgetNavigation.RequestSos()`, `WidgetNavigation.ConsumePending()`, `event Action? SosRequested`; `WidgetIntents.Sos(Context, int)`.

- [ ] **Step 1: Create the navigation bridge.** Create `Services/WidgetNavigation.cs` — a plain, platform-neutral static so shared code and the Android activity can both reach it:

```csharp
namespace Nicotine_Stop.Services;

/// <summary>
/// Bridges a widget's "open SOS" tap to the in-app SOS takeover. MainActivity (Android) calls
/// <see cref="RequestSos"/> when it receives the deep-link intent; MainTabsPage listens and, once
/// it is loaded, opens SOS. A pending flag covers the cold-start case where the request arrives
/// before the page exists — it is consumed after load.
/// </summary>
public static class WidgetNavigation
{
    private static bool _pending;

    /// <summary>Raised when a widget requests the SOS takeover (fires only if a listener exists).</summary>
    public static event Action? SosRequested;

    public static void RequestSos()
    {
        _pending = true;
        SosRequested?.Invoke();
    }

    /// <summary>Returns true once if an SOS request is outstanding, then clears it.</summary>
    public static bool ConsumePending()
    {
        if (!_pending) return false;
        _pending = false;
        return true;
    }
}
```

- [ ] **Step 2: Handle the intent in `MainActivity`.** Replace the body of `Platforms/Android/MainActivity.cs`:

```csharp
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Nicotine_Stop.Services;

namespace Nicotine_Stop
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            HandleWidgetIntent(Intent);
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);
            Intent = intent;                 // keep the activity's current intent in sync
            HandleWidgetIntent(intent);
        }

        // A widget's SOS button launches us with navigate=sos. Route it to the shared bridge; the
        // MAUI page layer decides how/when to present the SOS takeover.
        private static void HandleWidgetIntent(Intent? intent)
        {
            if (intent?.GetStringExtra("navigate") == "sos")
                WidgetNavigation.RequestSos();
        }
    }
}
```

- [ ] **Step 3: Open SOS from `MainTabsPage`.** In `Views/MainTabsPage.cs`, make three edits.

(a) At the end of the constructor (after `UpdateTabs();`), subscribe to the bridge:

```csharp
        WidgetNavigation.SosRequested += OnSosRequested;
```

(b) Replace the existing `OnSos` handler:

```csharp
    private async void OnSos(object? sender, EventArgs e)
    {
        var sos = _services.GetRequiredService<Sos.SosTakeoverPage>();
        await Navigation.PushModalAsync(new NavigationPage(sos));
    }
```

with a shared open method, the button handler, and the bridge handler:

```csharp
    private async void OnSos(object? sender, EventArgs e) => await OpenSosAsync();

    private void OnSosRequested()
    {
        // Fired from a widget deep link (background thread). Marshal to UI; only act once loaded —
        // the cold-start case is picked up by ConsumePending() in OnAppearing.
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (_loaded && WidgetNavigation.ConsumePending())
                await OpenSosAsync();
        });
    }

    private async Task OpenSosAsync()
    {
        if (Navigation.ModalStack.Count > 0) return;   // SOS (or another modal) already showing
        var sos = _services.GetRequiredService<Sos.SosTakeoverPage>();
        await Navigation.PushModalAsync(new NavigationPage(sos));
    }
```

(c) At the very end of `OnAppearing` (after the `_widgets.Update(...)` line), consume a cold-start request:

```csharp
        if (WidgetNavigation.ConsumePending())
            await OpenSosAsync();
```

- [ ] **Step 4: Add the SOS PendingIntent builder.** In `Platforms/Android/Widgets/WidgetIntents.cs`, add inside the `WidgetIntents` class (after `Home`):

```csharp
    /// <summary>Opens the app straight into the Craving SOS takeover (via MainActivity's deep-link).</summary>
    public static PendingIntent Sos(Context context, int requestCode)
    {
        var intent = new Intent(context, typeof(MainActivity));
        intent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.NewTask);
        intent.PutExtra("navigate", "sos");
        return PendingIntent.GetActivity(
            context, requestCode, intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)!;
    }
```

- [ ] **Step 5: Repoint the 5c SOS button.** In `Platforms/Android/Widgets/SnusWidgets.cs`, in `SnusWidgetSos.OnUpdate`, replace:

```csharp
            // SOS button opens Home for now; Task 6 repoints it to the SOS deep link.
            v.SetOnClickPendingIntent(Resource.Id.w_sos, WidgetIntents.Home(context, WidgetIntents.SosButton));
```

with:

```csharp
            v.SetOnClickPendingIntent(Resource.Id.w_sos, WidgetIntents.Sos(context, WidgetIntents.SosButton));
```

- [ ] **Step 6: Verify the project builds.**

Run: `dotnet build "Nicotine_Stop.csproj" -c Debug`
Expected: `Build succeeded`.

- [ ] **Step 7: Commit.**

```bash
git add Services/WidgetNavigation.cs Platforms/Android/MainActivity.cs Views/MainTabsPage.cs Platforms/Android/Widgets/WidgetIntents.cs Platforms/Android/Widgets/SnusWidgets.cs
git commit -m "feat(widgets): deep-link the 5c SOS button into the Craving SOS takeover"
```

---

### Task 7: Remove superseded resources, prune dead fields, full verification

**Files:**
- Delete: `Platforms/Android/Resources/layout/widget_2x2.xml`
- Delete: `Platforms/Android/Resources/layout/widget_4x2.xml`
- Delete: `Platforms/Android/Resources/xml/widget_2x2_info.xml`
- Delete: `Platforms/Android/Resources/xml/widget_4x2_info.xml`
- Modify: `Platforms/Android/Widgets/SnusWidgets.cs` (drop now-unused `AvoidedCap`, `Wins` from `WidgetView` + `Read`)

**Interfaces:**
- Consumes: nothing new.
- Produces: final three-widget set; no dead resources or fields.

- [ ] **Step 1: Confirm the old layouts/info are unreferenced.**

Run: `grep -rn "widget_2x2\|widget_4x2" Platforms/Android/Widgets Platforms/Android/Resources`
Expected: no matches (all providers now use `widget_compact` / `widget_ring` / `widget_sos`). If anything matches, fix it before deleting.

- [ ] **Step 2: Delete the superseded resource files.**

```bash
git rm Platforms/Android/Resources/layout/widget_2x2.xml Platforms/Android/Resources/layout/widget_4x2.xml Platforms/Android/Resources/xml/widget_2x2_info.xml Platforms/Android/Resources/xml/widget_4x2_info.xml
```

- [ ] **Step 3: Prune the two now-unused `WidgetView` fields.** In `Platforms/Android/Widgets/SnusWidgets.cs`, remove `AvoidedCap` and `Wins` from the record (no remaining widget renders "cravings won" or the short caption). Replace the record with:

```csharp
/// <summary>The values a widget renders, recomputed for the current moment.</summary>
internal readonly record struct WidgetView(
    int Days,              // raw day count — ring centre (5b) and "Day N" message (5c)
    string Day,            // "12d 4h" — compact hero (5a)
    string Money,          // "156 kr" — amount with the user's currency symbol
    string MoneyInt,       // "156" — grouped amount, NO symbol, for the tight 5c chip
    int Streak,
    string Milestone,      // next-milestone title, e.g. "2 weeks"
    int MilestonePct,      // 0..100, progress toward the next milestone (bars)
    double RingProgress,   // 0..1, same progress for the drawn ring arc (5b)
    string DaysLeft,       // "2 days"
    int Avoided,
    string SkippedCaption);// addiction-aware: "pouches skipped" / "cigarettes not smoked"
```

Then update both `return new WidgetView(...)` statements in `WidgetData.Read` to drop the removed arguments. No-plan branch:

```csharp
        if (quitTicks <= 0)
            return new WidgetView(
                Days: 0, Day: "0d 0h", Money: "0", MoneyInt: "0", Streak: 0,
                Milestone: "next milestone", MilestonePct: 0, RingProgress: 0d, DaysLeft: "",
                Avoided: 0, SkippedCaption: "skipped");
```

Normal branch:

```csharp
        var copy = AddictionCopy.For(profile.Addiction);
        return new WidgetView(
            Days: s.Days,
            Day: $"{s.Days}d {s.Hours}h",
            Money: StatsCalculator.FormatMoney(s.Money, profile.Currency, false),
            MoneyInt: StatsCalculator.FormatNumber(s.Money, profile.Currency),
            Streak: s.CurrentStreak,
            Milestone: s.Next?.Title ?? "1 year",
            MilestonePct: (int)(s.RingProgress * 100),
            RingProgress: s.RingProgress,
            DaysLeft: StatsCalculator.DaysUntilNextLabel(s),
            Avoided: s.PouchesAvoided,
            SkippedCaption: copy.SkippedCaption);
```

The `int wins = p.GetInt("wins", 0);` line at the top of `Read` is now unused — delete it. (`WidgetUpdateService` may keep writing `wins` to prefs; that is harmless and out of scope.)

- [ ] **Step 4: Verify the project builds.**

Run: `dotnet build "Nicotine_Stop.csproj" -c Debug`
Expected: `Build succeeded`, 0 warnings about unused locals from the pruned code.

- [ ] **Step 5: Run the core unit tests.**

Run: `dotnet test "SnusStop.Core.Tests/SnusStop.Core.Tests.csproj"`
Expected: all pass.

- [ ] **Step 6: On-device / emulator verification (the real acceptance).** Deploy the app (use the `run` skill or `dotnet build -t:Run -f net10.0-android`). Complete onboarding so a plan exists, then from the launcher's widget picker add all three widgets and check:

  - **Compact (2×2):** Puff mark + "SNUSSTOP", 🔥 streak, "Nd Nh", money in the user's currency, milestone bar + label. Tap → app opens Home.
  - **Progress ring (4×2):** ring track + round-capped fill sweep matching milestone progress; centred day count + "days free"; right column money / pouches-skipped (or "cigarettes not smoked" for a cigarette profile) / "N days · until 2 weeks free". Tap → Home.
  - **Puff + SOS (4×2):** Puff, "Day N — Puff's proud of you.", 💰/🔥 chips, milestone bar, caption; coral SOS disc with bottom-shadow lip. Tap body → Home; **tap SOS → Craving SOS takeover** (verify from both a cold start and with the app already open).
  - **Both themes:** toggle system dark mode; confirm every widget re-colours correctly (card, ink, ring/track/fill, chips, SOS shadow).
  - **No-plan state:** (optional) add a widget before completing onboarding — it shows zeros without crashing.

  Record the outcome (a screenshot or a short written pass/fail per widget) in the completion note.

- [ ] **Step 7: Commit.**

```bash
git add -A
git commit -m "chore(widgets): remove superseded 2x2/4x2 resources and prune dead WidgetView fields"
```

---

## Self-Review

**Spec coverage:**
- 3-widget set (Compact/Ring/SOS) → Tasks 3/4/5. ✓
- Live values via `WidgetData.Read` → reused; extended in Task 2. ✓
- Theming via `values`/`values-night` + bitmap ring color resolution → Task 1 tokens, Task 4 `ContextCompat.GetColor`. ✓
- RemoteViews inflation constraint → all layouts use only allowed classes (ImageView for the mascot/dots, FrameLayout/LinearLayout/TextView/ProgressBar). ✓
- Bitmap ring technique → Task 4 `RingBitmap`. ✓
- Puff swappable asset → Task 1 `widget_puff.xml`. ✓
- SOS deep-link (widget → MainActivity → bridge → existing OnSos, cold + warm) → Tasks 5/6. ✓
- Addiction-aware skipped caption → `SkippedCaption` (Task 2), used in Task 4. ✓
- Money formatting via `FormatMoney`/`FormatNumber` → Task 2. ✓
- No-plan placeholder → preserved in `Read` (Tasks 2/7). ✓
- Remove old 4×2 + rename providers → Tasks 3/4 rename, Task 7 deletes resources. ✓
- Core tests stay green → Tasks 2/7 run them. ✓

**Placeholder scan:** no TBD/TODO; every code step contains the full content. ✓

**Type consistency:** `WidgetView` members are referenced consistently — `Days`, `Day`, `Money`, `MoneyInt`, `Streak`, `Milestone`, `MilestonePct`, `RingProgress`, `DaysLeft`, `Avoided`, `SkippedCaption` across Tasks 3–5; `AvoidedCap`/`Wins` exist only until pruned in Task 7 (and are referenced by no surviving provider). `WidgetIntents.Home`/`Sos` request-code constants (`CompactHome`/`RingHome`/`SosHome`/`SosButton`) match their call sites. `RingBitmap.Draw(Context, int, double)` matches its Task 4 call. Layout ids match provider `SetXxx` calls (`w_root`, `w_day`, `w_money`, `w_streak`, `w_ms`, `w_progress`, `w_ring`, `w_avoided`, `w_skipcap`, `w_daysleft`, `w_mscap`, `w_msg`, `w_money2`, `w_streak2`, `w_bar`, `w_sos`). ✓
