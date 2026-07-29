using Nicotine_Stop.Services;

namespace Nicotine_Stop.Views.Sos;

public partial class CravingDefeatedPage : ContentPage
{
    private readonly AppState _state;

    public CravingDefeatedPage(AppState state)
    {
        InitializeComponent();
        _state = state;
        BackBtn.Command = new Command(async () => await Navigation.PopModalAsync());
    }

    /// <param name="xp">XP actually granted. 0 when the activity's daily XP was already claimed —
    /// the win still counts, so only the XP chip is dropped, not the celebration.</param>
    public void Init(int xp)
    {
        int wins = _state.CravingsWon;
        XpChip.IsVisible = xp > 0;
        XpLabel.Text = $"+{xp} XP";
        WinsLabel.Text = $"🥊 {wins}";
        SubLabel.Text = Ordinal(wins) is { } ord
            ? $"That's the {ord} craving you've beaten. They're getting weaker — you're getting stronger."
            : "They're getting weaker — you're getting stronger.";
    }

    private static string? Ordinal(int n)
    {
        if (n <= 0) return null;
        int mod100 = n % 100;
        string suffix = (mod100 is >= 11 and <= 13) ? "th" : (n % 10) switch
        {
            1 => "st", 2 => "nd", 3 => "rd", _ => "th",
        };
        return $"{n}{suffix}";
    }
}
