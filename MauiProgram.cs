using CommunityToolkit.Maui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nicotine_Stop.Data;
using Nicotine_Stop.Services;
using Plugin.LocalNotification;

namespace Nicotine_Stop;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseLocalNotification()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Nunito-Regular.ttf", "Nunito");
                fonts.AddFont("Nunito-SemiBold.ttf", "NunitoSemiBold");
                fonts.AddFont("Nunito-Bold.ttf", "NunitoBold");
                fonts.AddFont("Nunito-ExtraBold.ttf", "NunitoExtraBold");
                fonts.AddFont("Nunito-Black.ttf", "NunitoBlack");
            });

        RegisterServices(builder.Services);

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static void RegisterServices(IServiceCollection services)
    {
        // App services
        services.AddSingleton<ThemeService>();
        services.AddSingleton<ClockService>();
        services.AddSingleton<NotificationService>();
        services.AddSingleton<GameScoreStore>();
        services.AddSingleton<SnusStop.Core.Services.IDailyXpStore, PreferencesDailyXpStore>();
        services.AddSingleton<SnusStop.Core.Services.IDailyXpService>(sp =>
            new SnusStop.Core.Services.DailyXpService(sp.GetRequiredService<SnusStop.Core.Services.IDailyXpStore>()));

        // Data
        services.AddSingleton<AppDatabase>();
        services.AddSingleton<IProfileRepository, ProfileRepository>();
        services.AddSingleton<IGoalRepository, GoalRepository>();
        services.AddSingleton<IEventRepository, EventRepository>();
        services.AddSingleton<AppState>();
        services.AddSingleton<CsvExportService>();
        services.AddSingleton<WidgetUpdateService>();

        // View models — main tabs share single instances (subscribe to clock/state once)
        services.AddTransient<ViewModels.OnboardingViewModel>();
        services.AddSingleton<ViewModels.MainShellViewModel>();
        services.AddSingleton<ViewModels.HomeViewModel>();
        services.AddSingleton<ViewModels.GoalsViewModel>();
        services.AddSingleton<ViewModels.HealthViewModel>();
        services.AddSingleton<ViewModels.JourneyViewModel>();

        // Pages & views
        services.AddTransient<Views.RootPage>();
        services.AddTransient<Views.Onboarding.OnboardingPage>();
        services.AddTransient<Views.Tabs.HomeView>();
        services.AddTransient<Views.Tabs.GoalsView>();
        services.AddTransient<Views.Tabs.HealthView>();
        services.AddTransient<Views.Tabs.JourneyView>();
        services.AddTransient<Views.MainTabsPage>();
        services.AddTransient<Views.GoalEditPage>();
        services.AddTransient<ViewModels.SettingsViewModel>();
        services.AddTransient<Views.SettingsPage>();

        // SOS flow
        services.AddTransient<Views.Sos.SosTakeoverPage>();
        services.AddTransient<Views.Sos.BreathePage>();
        services.AddTransient<Views.Sos.RemindWhyPage>();
        services.AddTransient<Views.Sos.CravingDefeatedPage>();
        services.AddTransient<Views.Sos.SlipPage>();
        services.AddTransient<Views.Sos.PostSlipPage>();

        // Games
        services.AddTransient<Views.Games.GamePickerPage>();
        services.AddTransient<Views.Games.MinesweeperPage>();
        services.AddTransient<Views.Games.PouchPopPage>();
        services.AddTransient<Views.Games.MemoryMatchPage>();
        services.AddTransient<Views.Games.ReflexTapPage>();
    }
}
