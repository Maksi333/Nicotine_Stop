namespace Nicotine_Stop.Services;

/// <summary>
/// A single app-wide 1-second timer. View models subscribe to <see cref="Tick"/> to refresh their
/// live counters instead of each spinning up its own timer.
/// </summary>
public class ClockService
{
    private IDispatcherTimer? _timer;

    public DateTime NowUtc => DateTime.UtcNow;

    public event EventHandler? Tick;

    public void Start()
    {
        if (_timer is not null) return;
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null) return;

        _timer = dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (_, _) => Tick?.Invoke(this, EventArgs.Empty);
        _timer.Start();
    }

    public void Stop() => _timer?.Stop();
}
