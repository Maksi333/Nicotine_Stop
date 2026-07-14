using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Shapes;
using Nicotine_Stop.Services;
using SnusStop.Core.Models;
using SnusStop.Core.Services;
using Trigger = SnusStop.Core.Models.Trigger;

namespace Nicotine_Stop.Views.Sos;

public partial class SlipPage : ContentPage
{
    private readonly AppState _state;
    private readonly IServiceProvider _services;
    private Trigger _selected = Trigger.None;
    private readonly List<(Border Border, Label Label, Trigger Trigger)> _chips = new();

    public SlipPage(AppState state, IServiceProvider services)
    {
        InitializeComponent();
        _state = state;
        _services = services;
        LogBtn.Command = new Command(async () => await LogAsync());
        BuildChips();
        Helpers.SafeArea.ApplyInsets(this, top: true, bottom: true);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var s = _state.StatsNow();
        string sym = _state.Profile.Currency.Symbol();
        KeepLabel.Text = $"{s.Days} total clean days\n{StatsCalculator.FormatMoney(s.Money, false)} {sym} saved\nAll badges & XP";
        ResetLabel.Text = $"Current streak\n({s.CurrentStreak} days → 0, and day 1 starts now)";
    }

    private void BuildChips()
    {
        foreach (var t in TriggerExtensions.Selectable)
        {
            var label = new Label
            {
                Text = $"{t.Emoji()} {t.Label()}",
                FontFamily = "NunitoExtraBold",
                FontSize = 13,
                VerticalOptions = LayoutOptions.Center,
            };
            label.SetDynamicResource(Label.TextColorProperty, "Muted");

            var border = new Border
            {
                StrokeThickness = 1.5,
                StrokeShape = new RoundRectangle { CornerRadius = 13 },
                Padding = new Thickness(15, 9),
                Margin = new Thickness(0, 0, 8, 8),
                Content = label,
            };
            border.SetDynamicResource(BackgroundColorProperty, "Card");
            border.SetDynamicResource(Border.StrokeProperty, "CardBorder");

            var captured = t;
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => Select(captured);
            border.GestureRecognizers.Add(tap);

            _chips.Add((border, label, t));
            Chips.Children.Add(border);
        }
    }

    private void Select(Trigger t)
    {
        _selected = _selected == t ? Trigger.None : t;
        foreach (var (border, label, trig) in _chips)
        {
            if (trig == _selected)
            {
                border.BackgroundColor = Color.FromArgb("#17322B");
                border.Stroke = Color.FromArgb("#17322B");
                label.TextColor = Colors.White;
            }
            else
            {
                border.SetDynamicResource(BackgroundColorProperty, "Card");
                border.SetDynamicResource(Border.StrokeProperty, "CardBorder");
                label.SetDynamicResource(Label.TextColorProperty, "Muted");
            }
        }
    }

    private async void OnBack(object? sender, EventArgs e) => await Navigation.PopAsync();

    private async Task LogAsync()
    {
        await _state.AddEventAsync(EventLog.Slip(DateTime.UtcNow, _selected, NoteEditor.Text));
        var post = _services.GetRequiredService<PostSlipPage>();
        await Navigation.PushAsync(post);
    }
}
