using Microsoft.Extensions.DependencyInjection;
using Nicotine_Stop.Views.Games;

namespace Nicotine_Stop.Views.Sos;

public partial class SosTakeoverPage : ContentPage
{
    private readonly IServiceProvider _services;

    public SosTakeoverPage(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
    }

    private async void OnClose(object? sender, EventArgs e) => await Navigation.PopModalAsync();

    private async void OnBreathe(object? sender, EventArgs e) =>
        await Navigation.PushAsync(_services.GetRequiredService<BreathePage>());

    private async void OnDistract(object? sender, EventArgs e) =>
        await Navigation.PushAsync(_services.GetRequiredService<GamePickerPage>());

    private async void OnRemind(object? sender, EventArgs e) =>
        await Navigation.PushAsync(_services.GetRequiredService<RemindWhyPage>());

    private async void OnSlipped(object? sender, EventArgs e) =>
        await Navigation.PushAsync(_services.GetRequiredService<SlipPage>());
}
