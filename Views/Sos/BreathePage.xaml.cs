using Microsoft.Extensions.DependencyInjection;
using Nicotine_Stop.Services;
using SnusStop.Core.Models;
using SnusStop.Core.Services;

namespace Nicotine_Stop.Views.Sos;

public partial class BreathePage : ContentPage
{
    private readonly AppState _state;
    private readonly IServiceProvider _services;
    private CancellationTokenSource? _cts;
    private bool _completed;

    public BreathePage(AppState state, IServiceProvider services)
    {
        InitializeComponent();
        _state = state;
        _services = services;
        Helpers.SafeArea.ApplyInsets(this, top: true, bottom: true);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _completed = false;
        _cts = new CancellationTokenSource();
        _ = RunAsync(_cts.Token);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _cts?.Cancel();
    }

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            for (int cycle = 1; cycle <= 4 && !ct.IsCancellationRequested; cycle++)
            {
                UpdateDots(cycle);
                CycleLabel.Text = $"Cycle {cycle} of 4 · in 4s → hold 7s → out 8s";
                await PhaseAsync("Breathe in", 4, 1.0, ct);
                await PhaseAsync("Hold", 7, null, ct);
                await PhaseAsync("Breathe out", 8, 0.55, ct);
            }
            if (!ct.IsCancellationRequested)
            {
                PhaseLabel.Text = "Well done";
                CountLabel.Text = "✓";
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task PhaseAsync(string label, int seconds, double? scaleTo, CancellationToken ct)
    {
        PhaseLabel.Text = label;
        if (scaleTo is double s)
            _ = BreathVisual.ScaleTo(s, (uint)(seconds * 1000), Easing.SinInOut);
        for (int i = seconds; i > 0 && !ct.IsCancellationRequested; i--)
        {
            CountLabel.Text = i.ToString();
            await Task.Delay(1000, ct);
        }
    }

    private void UpdateDots(int cycle)
    {
        var dots = new[] { Dot1, Dot2, Dot3, Dot4 };
        for (int i = 0; i < dots.Length; i++)
        {
            bool active = i == cycle - 1;
            dots[i].WidthRequest = active ? 26 : 10;
            dots[i].Color = active ? Color.FromArgb("#8B7CF6") : Color.FromArgb("#40FFFFFF");
        }
    }

    private async void OnBack(object? sender, EventArgs e)
    {
        _cts?.Cancel();
        await Navigation.PopAsync();
    }

    private async void OnOkay(object? sender, EventArgs e)
    {
        if (_completed) return;
        _completed = true;
        _cts?.Cancel();

        await _state.AddEventAsync(EventLog.CravingWon(DateTime.UtcNow, "breathe", XpService.CravingXp));
        var celebrate = _services.GetRequiredService<CravingDefeatedPage>();
        celebrate.Init(XpService.CravingXp);
        await Navigation.PushAsync(celebrate);
    }
}
