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
}
