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

    public RootPage(IServiceProvider services, IProfileRepository profiles)
    {
        _services = services;
        _profiles = profiles;
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
        Page next = profile?.OnboardingComplete == true
            ? _services.GetRequiredService<MainTabsPage>()
            : _services.GetRequiredService<OnboardingPage>();

        if (Window is not null)
            Window.Page = next;
    }
}
