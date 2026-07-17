namespace Nicotine_Stop.Services;

/// <summary>
/// Bridges a widget's "open SOS" tap to the in-app SOS takeover. MainActivity (Android) calls
/// <see cref="RequestSos"/> when it receives the deep-link intent; MainTabsPage listens and, once
/// it is loaded, opens SOS. A pending flag covers the cold-start case where the request arrives
/// before the page exists — it is consumed after load.
/// </summary>
public static class WidgetNavigation
{
    private static bool _pending;

    /// <summary>Raised when a widget requests the SOS takeover (fires only if a listener exists).</summary>
    public static event Action? SosRequested;

    public static void RequestSos()
    {
        _pending = true;
        SosRequested?.Invoke();
    }

    /// <summary>Returns true once if an SOS request is outstanding, then clears it.</summary>
    public static bool ConsumePending()
    {
        if (!_pending) return false;
        _pending = false;
        return true;
    }
}
