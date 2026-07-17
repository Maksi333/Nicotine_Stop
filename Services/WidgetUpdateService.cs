using System.Globalization;
using Android.Appwidget;
using Android.Content;
using Nicotine_Stop.Platforms.Android.Widgets;
using SnusStop.Core.Models;

namespace Nicotine_Stop.Services;

/// <summary>
/// Persists the raw quit plan the widgets need and pokes them to refresh. The widgets recompute
/// live stats from this on their own hourly schedule, so they stay current even while the app is
/// closed — we deliberately do NOT store pre-computed day/money values that would freeze.
/// </summary>
public class WidgetUpdateService
{
    public void Update(Profile profile, DateTime? lastSlipUtc, int cravingsWon)
    {
        var ctx = global::Android.App.Application.Context;
        if (ctx is null) return;

        var prefs = ctx.GetSharedPreferences(WidgetPrefs.Name, FileCreationMode.Private);
        var editor = prefs?.Edit();
        if (editor is null) return;

        // Raw inputs only. Everything time-derived (days, money, streak, milestone progress) is
        // computed by the widget at display time — see WidgetData.Read.
        editor.PutLong("quitTicks", profile.QuitUtc.Ticks);
        editor.PutInt("ppd", profile.PouchesPerDay);
        editor.PutInt("ppc", profile.PouchesPerCan);
        editor.PutString("price", profile.CanPrice.ToString(CultureInfo.InvariantCulture));
        editor.PutInt("cur", (int)profile.Currency);
        editor.PutInt("addiction", (int)profile.Addiction);
        editor.PutLong("slipTicks", lastSlipUtc?.Ticks ?? 0L);

        // Cravings won comes from the events log, which the widget can't read, so it stays a
        // stored count rather than something recomputed.
        editor.PutInt("wins", cravingsWon);

        editor.Apply();

        Trigger(ctx, typeof(SnusWidgetCompact));
        Trigger(ctx, typeof(SnusWidgetRing));
        Trigger(ctx, typeof(SnusWidgetSos));
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
