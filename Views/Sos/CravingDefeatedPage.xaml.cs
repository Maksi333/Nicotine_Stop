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
        Helpers.SafeArea.ApplyInsets(this, top: true, bottom: true);
    }

    public void Init(int xp)
    {
        int wins = _state.CravingsWon;
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
