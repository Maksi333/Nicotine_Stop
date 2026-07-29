using Microsoft.Extensions.DependencyInjection;
using Nicotine_Stop.ViewModels;

namespace Nicotine_Stop.Views.Onboarding;

public partial class OnboardingPage : ContentPage
{
    private readonly IServiceProvider _services;

    public OnboardingPage(OnboardingViewModel vm, IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
        BindingContext = vm;
        vm.Completed += OnCompleted;
    }

    private async void OnCompleted()
    {
        if (Window is null) return;

        // Read the just-saved profile before building the tabs, so the home stats render with real
        // numbers on their first frame instead of the placeholder profile.
        await _services.GetRequiredService<Services.AppState>().EnsureLoadedAsync();
        Window.Page = _services.GetRequiredService<Nicotine_Stop.Views.MainTabsPage>();
    }
}
