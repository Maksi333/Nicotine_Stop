using Nicotine_Stop.Resources.Styles;

namespace Nicotine_Stop.Services;

/// <summary>
/// Swaps the active theme dictionary (Light/Dark) into the app resources and keeps it in sync
/// with the system setting. Views reference tokens via {DynamicResource Key} so they update live.
/// </summary>
public class ThemeService
{
    private bool _initialized;

    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        var app = Application.Current;
        if (app is null) return;

        Apply(app.RequestedTheme);
        app.RequestedThemeChanged += (_, e) => Apply(e.RequestedTheme);
    }

    public void Apply(AppTheme theme)
    {
        var app = Application.Current;
        if (app is null) return;

        var merged = app.Resources.MergedDictionaries;
        var existing = merged.FirstOrDefault(d => d is LightTheme or DarkTheme);
        if (existing is not null) merged.Remove(existing);

        ResourceDictionary dict = theme == AppTheme.Dark ? new DarkTheme() : new LightTheme();
        merged.Add(dict);
    }
}
