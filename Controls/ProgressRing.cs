using Microsoft.Maui.Graphics;

namespace Nicotine_Stop.Controls;

/// <summary>Round-capped progress arc starting at 12 o'clock, sweeping clockwise. Used at 232px
/// (Home hero) and 74px (Health donut).</summary>
public class ProgressRing : GraphicsView
{
    public static readonly BindableProperty ProgressProperty = BindableProperty.Create(
        nameof(Progress), typeof(double), typeof(ProgressRing), 0d, propertyChanged: OnVisualChanged);

    public static readonly BindableProperty StrokeWidthProperty = BindableProperty.Create(
        nameof(StrokeWidth), typeof(double), typeof(ProgressRing), 17d, propertyChanged: OnVisualChanged);

    public static readonly BindableProperty TrackColorProperty = BindableProperty.Create(
        nameof(TrackColor), typeof(Color), typeof(ProgressRing), Color.FromArgb("#E3EEE7"), propertyChanged: OnVisualChanged);

    public static readonly BindableProperty FillColorProperty = BindableProperty.Create(
        nameof(FillColor), typeof(Color), typeof(ProgressRing), Color.FromArgb("#14B36B"), propertyChanged: OnVisualChanged);

    private readonly RingDrawable _drawable = new();

    public ProgressRing()
    {
        Drawable = _drawable;
        BackgroundColor = Colors.Transparent;
        Sync();
    }

    public double Progress { get => (double)GetValue(ProgressProperty); set => SetValue(ProgressProperty, value); }
    public double StrokeWidth { get => (double)GetValue(StrokeWidthProperty); set => SetValue(StrokeWidthProperty, value); }
    public Color TrackColor { get => (Color)GetValue(TrackColorProperty); set => SetValue(TrackColorProperty, value); }
    public Color FillColor { get => (Color)GetValue(FillColorProperty); set => SetValue(FillColorProperty, value); }

    private static void OnVisualChanged(BindableObject b, object o, object n) => ((ProgressRing)b).Sync();

    private void Sync()
    {
        _drawable.Progress = Math.Clamp(Progress, 0, 1);
        _drawable.StrokeWidth = (float)StrokeWidth;
        _drawable.TrackColor = TrackColor;
        _drawable.FillColor = FillColor;
        Invalidate();
    }

    private sealed class RingDrawable : IDrawable
    {
        public double Progress { get; set; }
        public float StrokeWidth { get; set; } = 17;
        public Color TrackColor { get; set; } = Colors.Gray;
        public Color FillColor { get; set; } = Colors.Green;

        public void Draw(ICanvas canvas, RectF rect)
        {
            float d = Math.Min(rect.Width, rect.Height);
            if (d <= 0) return;
            float sw = StrokeWidth;
            float r = (d - sw) / 2f;
            float cx = rect.X + rect.Width / 2f;
            float cy = rect.Y + rect.Height / 2f;

            canvas.StrokeSize = sw;
            canvas.StrokeLineCap = LineCap.Round;

            canvas.StrokeColor = TrackColor;
            canvas.DrawCircle(cx, cy, r);

            if (Progress > 0)
            {
                canvas.StrokeColor = FillColor;
                float sweep = (float)(360 * Progress);
                // 0° = 3 o'clock, angles increase counter-clockwise; start at top (90°) and go clockwise.
                canvas.DrawArc(cx - r, cy - r, r * 2, r * 2, 90, 90 - sweep, true, false);
            }
        }
    }
}
