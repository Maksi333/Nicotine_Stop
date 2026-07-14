namespace Nicotine_Stop.Services;

/// <summary>Per-game personal bests, kept in app preferences so they survive a restart.</summary>
public sealed class GameScoreStore
{
    private const string Prefix = "highscore.";

    /// <summary>Stable key for Reflex Tap, whose score is taps landed in one 60-second round.</summary>
    public const string ReflexTap = "reflex";

    public int GetHighScore(string gameId) => Preferences.Default.Get(Prefix + gameId, 0);

    /// <summary>
    /// Records a result. Returns true only when it is a strictly new best — matching the old
    /// record is not beating it, so it must not celebrate.
    /// </summary>
    public bool TrySetHighScore(string gameId, int score)
    {
        if (score <= GetHighScore(gameId)) return false;
        Preferences.Default.Set(Prefix + gameId, score);
        return true;
    }
}
