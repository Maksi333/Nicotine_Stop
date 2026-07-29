using Microsoft.Maui.Controls.Shapes;

namespace Nicotine_Stop.Controls;

/// <summary>
/// The badge-unlock banner: emoji, what unlocked, and the XP it paid. Shown as an overlay by
/// <see cref="Views.MainTabsPage"/>, one at a time. <see cref="ShowAsync"/> completes only once the
/// banner has faded back out, so a caller can await them straight out of a queue.
/// </summary>
public class BadgeToast : ContentView
{
    private static readonly TimeSpan Dwell = TimeSpan.FromMilliseconds(3500);

    private readonly Label _emoji;
    private readonly Label _title;
    private readonly Label _xp;
    private TaskCompletionSource? _dismissed;

    public BadgeToast()
    {
        _emoji = new Label { FontSize = 26, VerticalOptions = LayoutOptions.Center };
        _title = new Label { FontFamily = "NunitoBlack", FontSize = 15 };
        _xp = new Label { FontFamily = "NunitoExtraBold", FontSize = 12, TextColor = Color.FromArgb("#14B36B") };
        _title.SetDynamicResource(Label.TextColorProperty, "Ink");

        var card = new Border
        {
            StrokeThickness = 1.5,
            Padding = new Thickness(14, 12),
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Content = new HorizontalStackLayout
            {
                Spacing = 12,
                Children =
                {
                    _emoji,
                    new VerticalStackLayout { Spacing = 1, Children = { _title, _xp } },
                },
            },
        };
        card.SetDynamicResource(BackgroundColorProperty, "Card");
        card.SetDynamicResource(Border.StrokeProperty, "CardBorder");

        Content = card;
        Margin = new Thickness(16, 8, 16, 0);
        VerticalOptions = LayoutOptions.Start;
        IsVisible = false;
        Opacity = 0;

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => _dismissed?.TrySetResult();
        GestureRecognizers.Add(tap);
    }

    public async Task ShowAsync(string emoji, string title, int xp)
    {
        _emoji.Text = emoji;
        _title.Text = title;
        _xp.Text = $"+{xp} XP";

        _dismissed = new TaskCompletionSource();
        IsVisible = true;
        TranslationY = -24;

        await Task.WhenAll(this.FadeToAsync(1, 180), this.TranslateToAsync(0, 0, 180, Easing.CubicOut));
        await Task.WhenAny(_dismissed.Task, Task.Delay(Dwell));
        await this.FadeToAsync(0, 160);

        IsVisible = false;
    }
}
