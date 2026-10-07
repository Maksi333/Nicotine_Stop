using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;
using SnusStop.Core.Models;
using SnusStop.Core.Services;

namespace Nicotine_Stop.Platforms.Android.Widgets;

public static class WidgetPrefs
{
    public const string Name = "snusstop_widget";
}

/// <summary>
/// Source-generated JSON for the goals list in widget prefs. Reflection-based serialization is not
/// guaranteed to survive trimming in Release builds; this is.
/// </summary>
[JsonSerializable(typeof(List<GoalItem>))]
internal partial class WidgetJson : JsonSerializerContext { }

/// <summary>The values a widget renders, recomputed for the current moment.</summary>
internal readonly record struct WidgetView(
    int Days,              // raw day count — ring centre (5b) and hero line (5c)
    string Day,            // "12d 4h" — compact hero (5a)
    string HoursMin,       // "4h 23m" — hours/minutes past the day count (5b centre, 5c hero)
    string Money,          // "156 kr" — amount with the user's currency symbol
    string MoneyInt,       // "156" — grouped amount, NO symbol, for the tight 5c chip
    int Streak,
    string Milestone,      // next-milestone title, e.g. "2 weeks"
    int MilestonePct,      // 0..100, progress toward the next milestone (bars)
    double RingProgress,   // 0..1, same progress for the drawn ring arc (5b)
    string DaysLeft,       // "2 days"
    int Avoided,
    string SkippedCaption);// addiction-aware: "pouches skipped" / "cigarettes not smoked"

/// <summary>What the goal widget renders, recomputed for the current moment.</summary>
internal readonly record struct GoalWidgetView(
    string Title,          // "🎯 Weekend trip", or the no-goal / all-funded message
    string Percent,        // "62%" ("" when there is no goal)
    int Pct,               // 0..100 for the bar
    string TimeLine,       // "9 days in · 23 days to go"
    string Money,          // "💰 1.240 kr" — total saved, net of slip spending
    string Clean);         // "⏱ 12d 4h" — time off nicotine since the last slip

/// <summary>
/// Turns the raw plan stored in prefs into current stats. Because the widget process runs on its
/// own (the app may be closed), it recomputes the time-derived values here against
/// <see cref="DateTime.UtcNow"/> rather than reading a snapshot frozen at the last app launch —
/// that is what lets the periodic refresh show live numbers.
/// </summary>
internal static class WidgetData
{
    public static WidgetView Read(ISharedPreferences p)
    {
        // No plan stored yet (widget added before the app was opened). Show neutral zeros.
        if (StoredProfile(p) is not Profile profile)
            return new WidgetView(
                Days: 0, Day: "0d 0h", HoursMin: "0h 0m", Money: "0", MoneyInt: "0", Streak: 0,
                Milestone: "next milestone", MilestonePct: 0, RingProgress: 0d, DaysLeft: "",
                Avoided: 0, SkippedCaption: "skipped");

        var s = StatsNow(p, profile);

        var copy = AddictionCopy.For(profile.Addiction);
        return new WidgetView(
            Days: s.Days,
            Day: $"{s.Days}d {s.Hours}h",
            HoursMin: $"{s.Hours}h {s.Min}m",
            Money: StatsCalculator.FormatMoney(s.Money, profile.Currency, false),
            MoneyInt: StatsCalculator.FormatNumber(s.Money, profile.Currency),
            Streak: s.CurrentStreak,
            Milestone: s.Next?.Title ?? "1 year",
            MilestonePct: (int)(s.RingProgress * 100),
            RingProgress: s.RingProgress,
            DaysLeft: StatsCalculator.DaysUntilNextLabel(s),
            Avoided: s.PouchesAvoided,
            SkippedCaption: copy.SkippedCaption);
    }

    /// <summary>
    /// The goal widget: the active goal's progress against the live savings total, allocated exactly
    /// as the Goals tab does it (<see cref="GoalAllocator"/>).
    /// </summary>
    public static GoalWidgetView ReadGoal(ISharedPreferences p)
    {
        if (StoredProfile(p) is not Profile profile)
            return new GoalWidgetView("🎯 Set a goal in Puffy", "", 0, "Open Puffy to get started", "💰 0", "⏱ 0d 0h");

        var s = StatsNow(p, profile);
        string money = $"💰 {StatsCalculator.FormatMoney(s.Money, profile.Currency, false)}";
        string clean = $"⏱ {s.Days}d {s.Hours}h";

        var goals = StoredGoals(p);
        if (goals.Count == 0)
            return new GoalWidgetView("🎯 Set a goal in Puffy", "", 0, "Your savings will pay for it", money, clean);

        var active = GoalAllocator.Active(goals, s.Money, profile.SplitSavings, StatsCalculator.PerDayCost(profile));
        if (active is null)
            return new GoalWidgetView("🏆 All goals funded", "100%", 100, "Add a new goal in Puffy", money, clean);

        // Floor, so a goal only reads 100% once it is actually funded.
        int pct = (int)(active.Fraction * 100);
        return new GoalWidgetView($"🎯 {active.Name}", $"{pct}%", pct, active.TimeLine, money, clean);
    }

    private static Profile? StoredProfile(ISharedPreferences p)
    {
        long quitTicks = p.GetLong("quitTicks", 0L);
        if (quitTicks <= 0) return null;

        return new Profile
        {
            QuitUtc = new DateTime(quitTicks, DateTimeKind.Utc),
            PouchesPerDay = p.GetInt("ppd", 0),
            PouchesPerCan = p.GetInt("ppc", 20),
            CanPrice = ParseDecimal(p.GetString("price", "0")),
            Currency = (Currency)p.GetInt("cur", 0),
            Addiction = (AddictionType)p.GetInt("addiction", 0),
            SplitSavings = p.GetBoolean("split", false),
        };
    }

    private static Stats StatsNow(ISharedPreferences p, Profile profile)
    {
        long slipTicks = p.GetLong("slipTicks", 0L);
        DateTime? lastSlip = slipTicks > 0 ? new DateTime(slipTicks, DateTimeKind.Utc) : null;

        var spending = ParseDecimal(p.GetString("slipSpending", "0"));
        return StatsCalculator.Compute(profile, DateTime.UtcNow, lastSlip, spending);
    }

    private static List<GoalItem> StoredGoals(ISharedPreferences p)
    {
        var json = p.GetString("goals", null);
        if (string.IsNullOrEmpty(json)) return new();
        try { return JsonSerializer.Deserialize(json, WidgetJson.Default.ListGoalItem) ?? new(); }
        catch (JsonException) { return new(); }
    }

    private static decimal ParseDecimal(string? v) =>
        decimal.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : 0m;
}

[BroadcastReceiver(Label = "Puffy · Compact", Exported = false)]
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

[BroadcastReceiver(Label = "Puffy · Progress ring", Exported = false)]
[IntentFilter(new[] { AppWidgetManager.ActionAppwidgetUpdate })]
[MetaData("android.appwidget.provider", Resource = "@xml/widget_ring_info")]
public class SnusWidgetRing : AppWidgetProvider
{
    public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
    {
        var d = WidgetData.Read(context.GetSharedPreferences(WidgetPrefs.Name, FileCreationMode.Private)!);
        var ring = RingBitmap.Draw(context, d.RingProgress);
        foreach (var id in appWidgetIds)
        {
            var v = new RemoteViews(context.PackageName, Resource.Layout.widget_ring);
            v.SetImageViewBitmap(Resource.Id.w_ring, ring);
            v.SetTextViewText(Resource.Id.w_ringdays, $"{d.Days}d");
            v.SetTextViewText(Resource.Id.w_ringcap, d.HoursMin);
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

[BroadcastReceiver(Label = "Puffy · Puff + SOS", Exported = false)]
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
            v.SetTextViewText(Resource.Id.w_dayhero, $"{d.Days}d {d.HoursMin}");
            v.SetTextViewText(Resource.Id.w_money2, $"💰 {d.MoneyInt}");
            v.SetTextViewText(Resource.Id.w_streak2, $"🔥 {d.Streak}d");
            v.SetProgressBar(Resource.Id.w_bar, 100, d.MilestonePct, false);
            v.SetTextViewText(Resource.Id.w_mscap, $"🏆 {d.DaysLeft} until {d.Milestone} free");
            v.SetOnClickPendingIntent(Resource.Id.w_root, WidgetIntents.Home(context, WidgetIntents.SosHome));
            v.SetOnClickPendingIntent(Resource.Id.w_sos, WidgetIntents.Sos(context, WidgetIntents.SosButton));
            appWidgetManager.UpdateAppWidget(id, v);
        }
    }
}

[BroadcastReceiver(Label = "Puffy · Goal + SOS", Exported = false)]
[IntentFilter(new[] { AppWidgetManager.ActionAppwidgetUpdate })]
[MetaData("android.appwidget.provider", Resource = "@xml/widget_goal_info")]
public class SnusWidgetGoal : AppWidgetProvider
{
    public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
    {
        var d = WidgetData.ReadGoal(context.GetSharedPreferences(WidgetPrefs.Name, FileCreationMode.Private)!);
        foreach (var id in appWidgetIds)
        {
            var v = new RemoteViews(context.PackageName, Resource.Layout.widget_goal);
            v.SetTextViewText(Resource.Id.w_goalname, d.Title);
            v.SetTextViewText(Resource.Id.w_goalpct, d.Percent);
            v.SetProgressBar(Resource.Id.w_goalbar, 100, d.Pct, false);
            v.SetTextViewText(Resource.Id.w_goaltime, d.TimeLine);
            v.SetTextViewText(Resource.Id.w_money3, d.Money);
            v.SetTextViewText(Resource.Id.w_clean3, d.Clean);
            v.SetOnClickPendingIntent(Resource.Id.w_root, WidgetIntents.Home(context, WidgetIntents.GoalHome));
            v.SetOnClickPendingIntent(Resource.Id.w_sos, WidgetIntents.Sos(context, WidgetIntents.GoalSos));
            appWidgetManager.UpdateAppWidget(id, v);
        }
    }
}
