using System.Globalization;
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

/// <summary>
/// Turns the raw plan stored in prefs into current stats. Because the widget process runs on its
/// own (the app may be closed), it recomputes the time-derived values here against
/// <see cref="DateTime.UtcNow"/> rather than reading a snapshot frozen at the last app launch —
/// that is what lets the hourly refresh show live numbers.
/// </summary>
internal static class WidgetData
{
    public static WidgetView Read(ISharedPreferences p)
    {
        long quitTicks = p.GetLong("quitTicks", 0L);
        int wins = p.GetInt("wins", 0);

        // No plan stored yet (widget added before the app was opened). Show neutral zeros.
        if (quitTicks <= 0)
            return new WidgetView(
                Days: 0, Day: "0d 0h", Money: "0", MoneyInt: "0", Streak: 0,
                Milestone: "next milestone", MilestonePct: 0, RingProgress: 0d, DaysLeft: "",
                Avoided: 0, SkippedCaption: "skipped", AvoidedCap: "skipped", Wins: wins);

        var profile = new Profile
        {
            QuitUtc = new DateTime(quitTicks, DateTimeKind.Utc),
            PouchesPerDay = p.GetInt("ppd", 0),
            PouchesPerCan = p.GetInt("ppc", 20),
            CanPrice = ParseDecimal(p.GetString("price", "0")),
            Currency = (Currency)p.GetInt("cur", 0),
            Addiction = (AddictionType)p.GetInt("addiction", 0),
        };

        long slipTicks = p.GetLong("slipTicks", 0L);
        DateTime? lastSlip = slipTicks > 0 ? new DateTime(slipTicks, DateTimeKind.Utc) : null;

        var s = StatsCalculator.Compute(profile, DateTime.UtcNow, lastSlip);

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
    }

    private static decimal ParseDecimal(string? v) =>
        decimal.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : 0m;
}

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
