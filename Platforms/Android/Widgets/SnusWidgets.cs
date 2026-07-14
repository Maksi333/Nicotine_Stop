using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;

namespace Nicotine_Stop.Platforms.Android.Widgets;

public static class WidgetPrefs
{
    public const string Name = "snusstop_widget";
}

[BroadcastReceiver(Label = "SnusStop · day & money", Exported = false)]
[IntentFilter(new[] { AppWidgetManager.ActionAppwidgetUpdate })]
[MetaData("android.appwidget.provider", Resource = "@xml/widget_2x2_info")]
public class SnusWidget2x2 : AppWidgetProvider
{
    public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
    {
        var p = context.GetSharedPreferences(WidgetPrefs.Name, FileCreationMode.Private)!;
        foreach (var id in appWidgetIds)
        {
            var v = new RemoteViews(context.PackageName, Resource.Layout.widget_2x2);
            v.SetTextViewText(Resource.Id.w_day, $"{p.GetInt("day", 0)}d {p.GetInt("hour", 0)}h");
            v.SetTextViewText(Resource.Id.w_money, $"{p.GetString("money", "0")} kr");
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
        var p = context.GetSharedPreferences(WidgetPrefs.Name, FileCreationMode.Private)!;
        foreach (var id in appWidgetIds)
        {
            var v = new RemoteViews(context.PackageName, Resource.Layout.widget_4x2);
            v.SetTextViewText(Resource.Id.w_day, $"{p.GetInt("day", 0)}d {p.GetInt("hour", 0)}h");
            v.SetTextViewText(Resource.Id.w_money, $"{p.GetString("money", "0")} kr");
            v.SetTextViewText(Resource.Id.w_streak, $"🔥 {p.GetInt("streak", 0)}");
            v.SetTextViewText(Resource.Id.w_milestone, $"🏆 {p.GetString("milestone", "next milestone")} free");
            v.SetTextViewText(Resource.Id.w_daysleft, $"{p.GetString("daysleft", "")} to go");
            v.SetProgressBar(Resource.Id.w_progress, 100, p.GetInt("mpct", 0), false);
            appWidgetManager.UpdateAppWidget(id, v);
        }
    }
}
