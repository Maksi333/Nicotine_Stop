using Microsoft.Extensions.DependencyInjection;
using Nicotine_Stop.Controls;
using Nicotine_Stop.Data;
using Nicotine_Stop.Views.Onboarding;

namespace Nicotine_Stop.Views;

/// <summary>
/// First page shown. Decides where to go: a completed profile → main tabs, otherwise onboarding.
/// Doubles as a brief branded splash while the local DB is read.
/// </summary>
public class RootPage : ContentPage
{
    private readonly IServiceProvider _services;
    private readonly IProfileRepository _profiles;
    private readonly Services.AppState _state;

    public RootPage(IServiceProvider services, IProfileRepository profiles, Services.AppState state)
    {
        _services = services;
        _profiles = profiles;
        _state = state;
        BackgroundColor = Color.FromArgb("#14B36B");
        Content = new Grid
        {
            Children =
            {
                new PuffMascot
                {
                    Diameter = 104,
                    Cheeks = true,
                    Float = true,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                },
            },
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var profile = await _profiles.GetAsync();

        Page next;
        if (profile?.OnboardingComplete == true)
        {
            // Load before resolving the page: constructing MainTabsPage builds the tab view models,
            // which compute their stats up front. Resolving first would render the placeholder
            // profile for a frame — a nicotine-free counter dated from year 1.
            await _state.EnsureLoadedAsync();
            next = _services.GetRequiredService<MainTabsPage>();
        }
        else
        {
            next = _services.GetRequiredService<OnboardingPage>();
        }

        if (Window is not null)
            Window.Page = next;
    }
}
