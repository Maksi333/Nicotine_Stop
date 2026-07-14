using Android.Appwidget;
using Android.Content;
using Nicotine_Stop.Platforms.Android.Widgets;
using SnusStop.Core.Models;
using SnusStop.Core.Services;

namespace Nicotine_Stop.Services;

/// <summary>Persists a compact stats snapshot for the home-screen widgets and pokes them to refresh.</summary>
public class WidgetUpdateService
{
    public void Update(Profile profile, Stats s)
    {
        var ctx = global::Android.App.Application.Context;
        if (ctx is null) return;

        var prefs = ctx.GetSharedPreferences(WidgetPrefs.Name, FileCreationMode.Private);
        var editor = prefs?.Edit();
        if (editor is null) return;

        editor.PutInt("day", s.Days);
        editor.PutInt("hour", s.Hours);
        editor.PutString("money", StatsCalculator.FormatMoney(s.Money, false));
        editor.PutInt("streak", s.CurrentStreak);
        editor.PutString("milestone", s.Next?.Title ?? "1 year");
        editor.PutInt("mpct", (int)(s.RingProgress * 100));
        editor.PutString("daysleft", StatsCalculator.DaysUntilNextLabel(s));
        editor.Apply();

        Trigger(ctx, typeof(SnusWidget2x2));
        Trigger(ctx, typeof(SnusWidget4x2));
    }

    private static void Trigger(Context ctx, Type providerType)
    {
        var mgr = AppWidgetManager.GetInstance(ctx);
        var cn = new ComponentName(ctx, Java.Lang.Class.FromType(providerType));
        var ids = mgr?.GetAppWidgetIds(cn);
        if (ids is null || ids.Length == 0) return;

        var intent = new Intent(ctx, providerType);
        intent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
        intent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, ids);
        ctx.SendBroadcast(intent);
    }
}
