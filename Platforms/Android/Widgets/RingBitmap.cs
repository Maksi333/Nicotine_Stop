using Android.Content;
using Android.Graphics;
using AndroidX.Core.Content;

// MAUI's global Microsoft.Maui.Graphics namespace also defines Paint/Color/RectF; pin these to the
// Android graphics types this renderer draws with.
using Paint = Android.Graphics.Paint;
using Color = Android.Graphics.Color;
using RectF = Android.Graphics.RectF;

namespace Nicotine_Stop.Platforms.Android.Widgets;

/// <summary>
/// Draws the 5b progress ring (track + round-capped fill arc). Only the arcs are painted here;
/// the day count and "days free" caption are themed TextViews overlaid on top in the layout, so
/// they re-colour instantly on a light/dark switch rather than staying frozen in a bitmap.
/// RemoteViews cannot host a custom view, so the arcs are rendered to a Bitmap and pushed via
/// SetImageViewBitmap. Colours come from theme resources, matching the current configuration.
/// </summary>
internal static class RingBitmap
{
    public static Bitmap Draw(Context context, double progress)
    {
        var res = context.Resources!;
        float density = res.DisplayMetrics!.Density;
        int size = (int)(118 * density);
        float stroke = 10 * density;

        var bmp = Bitmap.CreateBitmap(size, size, Bitmap.Config.Argb8888!)!;
        var canvas = new Canvas(bmp);

        var trackColor = new Color(ContextCompat.GetColor(context, Resource.Color.widget_ring_track));
        var fillColor = new Color(ContextCompat.GetColor(context, Resource.Color.widget_fill));

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

        return bmp;
    }
}
