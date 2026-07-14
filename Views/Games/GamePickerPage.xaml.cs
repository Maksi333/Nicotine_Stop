using Microsoft.Extensions.DependencyInjection;

namespace Nicotine_Stop.Views.Games;

public partial class GamePickerPage : ContentPage
{
    private readonly IServiceProvider _services;

    public GamePickerPage(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
        Helpers.SafeArea.ApplyInsets(this, top: true, bottom: true);
    }

    private async void OnBack(object? sender, EventArgs e) => await Navigation.PopAsync();
    private async void OnBackToDay(object? sender, EventArgs e) => await Navigation.PopModalAsync();

    private async void OnMinesweeper(object? sender, EventArgs e) =>
        await Navigation.PushAsync(_services.GetRequiredService<MinesweeperPage>());

    private async void OnPouchPop(object? sender, EventArgs e) =>
        await Navigation.PushAsync(_services.GetRequiredService<PouchPopPage>());

    private async void OnMemory(object? sender, EventArgs e) =>
        await Navigation.PushAsync(_services.GetRequiredService<MemoryMatchPage>());

    private async void OnReflex(object? sender, EventArgs e) =>
        await Navigation.PushAsync(_services.GetRequiredService<ReflexTapPage>());
}
