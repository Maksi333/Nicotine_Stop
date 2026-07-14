using Microsoft.Extensions.DependencyInjection;
using Nicotine_Stop.Services;

namespace Nicotine_Stop;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var services = IPlatformApplication.Current!.Services;

        // Apply theme colours before the first page renders.
        services.GetRequiredService<ThemeService>().Initialize();

        // RootPage decides onboarding vs. main based on the stored profile.
        var root = services.GetRequiredService<Views.RootPage>();
        return new Window(root);
    }
}
