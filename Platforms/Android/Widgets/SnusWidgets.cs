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
    string Day, string Money, int Streak, string Milestone,
    int MilestonePct, string DaysLeft, int Avoided, string AvoidedCap, int Wins);

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
            return new WidgetView("0d 0h", "0", 0, "next milestone", 0, "", 0, "skipped", wins);

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
    }

    private static decimal ParseDecimal(string? v) =>
        decimal.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : 0m;
}

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
