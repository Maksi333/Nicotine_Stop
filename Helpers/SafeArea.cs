namespace Nicotine_Stop.Helpers;

/// <summary>
/// Pads a page by the real system-bar (status bar / gesture bar / cutout) insets once its platform
/// view is attached. On devices that already reserve the bars the inset is 0 (no-op); it only kicks
/// in on edge-to-edge devices (Android 15+, e.g. the Galaxy S24) where content draws under the bars.
/// </summary>
public static class SafeArea
{
    public static void ApplyInsets(ContentPage page, bool top = true, bool bottom = false)
    {
        page.Loaded += (_, _) => Apply(page, top, bottom);
    }

    private static void Apply(ContentPage page, bool top, bool bottom)
    {
        try
        {
            if (page.Handler?.PlatformView is not Android.Views.View view) return;
            var insets = view.RootWindowInsets;
            if (insets is null) return;

            int topPx, bottomPx;
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.R)
            {
                var bars = insets.GetInsets(Android.Views.WindowInsets.Type.SystemBars());
                topPx = bars.Top;
                bottomPx = bars.Bottom;
            }
            else
            {
#pragma warning disable CA1422
                topPx = insets.SystemWindowInsetTop;
                bottomPx = insets.SystemWindowInsetBottom;
#pragma warning restore CA1422
            }

            double density = view.Context?.Resources?.DisplayMetrics?.Density ?? 1;
            var p = page.Padding;
            double newTop = top && topPx > 0 ? topPx / density : p.Top;
            double newBottom = bottom && bottomPx > 0 ? bottomPx / density : p.Bottom;

            if (Math.Abs(newTop - p.Top) < 0.5 && Math.Abs(newBottom - p.Bottom) < 0.5) return;
            page.Padding = new Thickness(p.Left, newTop, p.Right, newBottom);
        }
        catch { /* insets unavailable — leave padding as-is */ }
    }
}
