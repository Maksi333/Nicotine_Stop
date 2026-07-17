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
