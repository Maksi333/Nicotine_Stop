using Microsoft.Extensions.DependencyInjection;
using Nicotine_Stop.ViewModels;

namespace Nicotine_Stop.Views.Tabs;

public partial class HomeView : ContentView
{
    private readonly IServiceProvider _services;

    public HomeView(HomeViewModel vm, IServiceProvider services)
    {
        InitializeComponent();
        BindingContext = vm;
        _services = services;
    }

    private async void OnOpenSettings(object? sender, EventArgs e)
    {
        var nav = Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation;
        if (nav is null) return;
        await nav.PushModalAsync(_services.GetRequiredService<SettingsPage>());
    }
}
