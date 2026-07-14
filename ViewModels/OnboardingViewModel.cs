using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nicotine_Stop.Data;
using SnusStop.Core.Models;
using SnusStop.Core.Services;

namespace Nicotine_Stop.ViewModels;

public partial class OnboardingViewModel : ObservableObject
{
    private readonly IProfileRepository _profiles;
    private int _calYear;
    private int _calMonth;

    public event Action? Completed;

    public OnboardingViewModel(IProfileRepository profiles)
    {
        _profiles = profiles;

        foreach (var c in Currencies.All)
            CurrencyChips.Add(new CurrencyChipVM(c, c == Currency));

        MotivationOptions.Add(new("health", "My health", "❤️", Color.FromArgb("#EAF7F0")));
        MotivationOptions.Add(new("money", "The money", "💰", Color.FromArgb("#FFF6E3")));
        MotivationOptions.Add(new("focus", "Better focus", "🧠", Color.FromArgb("#F0EDFC")));
        MotivationOptions.Add(new("fitness", "Fitness", "🏃", Color.FromArgb("#EAF3FD")));
        MotivationOptions.Add(new("family", "My family", "👨‍👩‍👧", Color.FromArgb("#FDEEEC")));
        MotivationOptions.Add(new("own", "My own reason…", "✍️", Color.FromArgb("#E9F3EC"), isFreeText: true));

        var today = DateTime.Today;
        _calYear = today.Year;
        _calMonth = today.Month;
        BuildCalendar();
    }

    // ── Navigation ──────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressFraction))]
    [NotifyPropertyChangedFor(nameof(StepLabel))]
    [NotifyPropertyChangedFor(nameof(ShowProgressHeader))]
    private int step; // 0 Welcome · 1 Name · 2 Habits · 3 Cost · 4 Quit date · 5 Motivation · 6 Summary

    public double ProgressFraction => Step is >= 1 and <= 5 ? Step / 6.0 : 0;
    public string StepLabel => $"{Step}/6";
    public bool ShowProgressHeader => Step is >= 1 and <= 5;

    // ── Fields ──────────────────────────────────────────────────
    [ObservableProperty] private string name = "";
    [ObservableProperty] private int addictionIndex; // 0 Snus · 1 Cigarettes
    [ObservableProperty] private int pouchesPerDay = 15;
    [ObservableProperty] private int pouchesPerCan = 20;
    [ObservableProperty] private decimal canPrice = 45m;
    [ObservableProperty] private Currency currency = Currency.DKK;
    [ObservableProperty] private int quitModeIndex; // 0 already quit · 1 future date
    [ObservableProperty] private DateTime quitDate = DateTime.Today;
    [ObservableProperty] private TimeSpan quitTime = new(21, 30, 0);
    [ObservableProperty] private string ownReason = "";

    public ObservableCollection<CurrencyChipVM> CurrencyChips { get; } = new();
    public ObservableCollection<MotivationVM> MotivationOptions { get; } = new();
    public ObservableCollection<DayCellVM> QuitCalendar { get; } = new();

    // ── Derived display ─────────────────────────────────────────
    public bool AlreadyQuit => QuitModeIndex == 0;
    public string CurrencySymbol => Currency.Symbol();
    public DateTime QuitLocal => QuitDate.Date + QuitTime;

    public AddictionType Addiction => AddictionIndex == 1 ? AddictionType.Cigarettes : AddictionType.Snus;
    private AddictionCopy Copy => AddictionCopy.For(Addiction);

    // Every snus-specific word on the wizard comes from here.
    public string QuitHeadline => $"{Copy.QuitHeadline}\nKeep the money.";
    public string UnitsPerDayLabel => Copy.UnitsPerDayLabel;
    public string UnitsPerContainerLabel => Copy.UnitsPerContainerLabel;
    public string ContainerCostQuestion => Copy.ContainerCostQuestion;
    public string PerContainerLabel => Copy.PerContainerLabel;
    public string ContainerSavingsLine => $"Every skipped {Copy.Container} goes straight into your savings.";
    public string ContainersPerWeekLine => $"That’s about {CansPerWeek} {Copy.Containers} a week. Let’s turn that into money.";
    public string PerUnitLine => $"That’s {PerPouchStr} per {Copy.Unit} — a real daily saving.";
    public string YearUnitsLine => $"and {YearPouchesStr} {Copy.Units} never used";

    public string GreetingName => string.IsNullOrWhiteSpace(Name) ? "there" : Name.Trim();
    public string CansPerWeek => StatsCalculator.CansPerWeek(Snapshot()).ToString();
    public string PerPouchStr => StatsCalculator.FormatMoney(StatsCalculator.PerPouch(Snapshot()), Currency, true);
    public string PerDayStr => StatsCalculator.FormatMoney(StatsCalculator.PerDayCost(Snapshot()), Currency, true);
    public string WeekSaveStr => StatsCalculator.FormatMoney(StatsCalculator.WeekSave(Snapshot()), Currency, false);
    public string MonthSaveStr => StatsCalculator.FormatMoney(StatsCalculator.MonthSave(Snapshot()), Currency, false);
    public string YearSaveStr => StatsCalculator.FormatMoney(StatsCalculator.YearSave(Snapshot()), Currency, false);

    /// <summary>A count of units, not money — so it gets grouping but no currency symbol.</summary>
    public string YearPouchesStr => StatsCalculator.FormatNumber(StatsCalculator.YearPouches(Snapshot()), Currency);

    public string CalendarTitle => new DateTime(_calYear, _calMonth, 1).ToString("MMMM yyyy");
    public string QuitTimeStr => QuitLocal.ToString("HH:mm");

    public int DayNum => Math.Max(0, (int)(DateTime.Now - QuitLocal).TotalDays);
    public string QuitDayLabel
    {
        get
        {
            if (AlreadyQuit) return $"Day {DayNum} 🎉";
            int inDays = Math.Max(0, (int)(QuitLocal.Date - DateTime.Today).TotalDays);
            return inDays == 0 ? "Today 🎉" : $"In {inDays} days";
        }
    }

    // ── Commands ────────────────────────────────────────────────
    [RelayCommand]
    private void Next()
    {
        if (Step < 6) Step++;
    }

    [RelayCommand]
    private void Back()
    {
        if (Step > 0) Step--;
    }

    [RelayCommand]
    private void SelectCurrency(CurrencyChipVM chip)
    {
        Currency = chip.Currency;
        foreach (var c in CurrencyChips) c.Selected = c.Currency == Currency;
    }

    [RelayCommand]
    private static void ToggleMotivation(MotivationVM m) => m.Selected = !m.Selected;

    [RelayCommand]
    private void SelectDay(DayCellVM cell)
    {
        if (!cell.HasDay) return;
        QuitDate = new DateTime(_calYear, _calMonth, cell.N!.Value);
        foreach (var c in QuitCalendar) c.Selected = c.N == cell.N;
    }

    [RelayCommand]
    private void PrevMonth()
    {
        ShiftMonth(-1);
    }

    [RelayCommand]
    private void NextMonth()
    {
        ShiftMonth(1);
    }

    [RelayCommand]
    private async Task FinishAsync()
    {
        var motivations = MotivationOptions
            .Where(m => m.Selected && !m.IsFreeText)
            .Select(m => m.Label)
            .ToList();
        if (MotivationOptions.First(m => m.IsFreeText).Selected && !string.IsNullOrWhiteSpace(OwnReason))
            motivations.Add(OwnReason.Trim());

        var profile = Snapshot();
        profile.Motivations = motivations;
        profile.OnboardingComplete = true;

        await _profiles.SaveAsync(profile);
        Completed?.Invoke();
    }

    // ── Helpers ─────────────────────────────────────────────────
    private Profile Snapshot() => new()
    {
        Name = Name.Trim(),
        Addiction = Addiction,
        QuitUtc = QuitLocal.ToUniversalTime(),
        PouchesPerDay = PouchesPerDay,
        PouchesPerCan = PouchesPerCan,
        CanPrice = CanPrice,
        Currency = Currency,
    };

    private void ShiftMonth(int delta)
    {
        var m = new DateTime(_calYear, _calMonth, 1).AddMonths(delta);
        _calYear = m.Year;
        _calMonth = m.Month;
        BuildCalendar();
    }

    private void BuildCalendar()
    {
        QuitCalendar.Clear();
        var first = new DateTime(_calYear, _calMonth, 1);
        int lead = ((int)first.DayOfWeek + 6) % 7; // Monday-first grid
        for (int i = 0; i < lead; i++) QuitCalendar.Add(new DayCellVM(null));

        int days = DateTime.DaysInMonth(_calYear, _calMonth);
        var today = DateTime.Today;
        for (int d = 1; d <= days; d++)
        {
            bool sel = QuitDate.Year == _calYear && QuitDate.Month == _calMonth && QuitDate.Day == d;
            bool isToday = today.Year == _calYear && today.Month == _calMonth && today.Day == d;
            QuitCalendar.Add(new DayCellVM(d, isToday, sel));
        }
        OnPropertyChanged(nameof(CalendarTitle));
    }

    private void RaiseSavings()
    {
        OnPropertyChanged(nameof(CansPerWeek));
        OnPropertyChanged(nameof(PerPouchStr));
        OnPropertyChanged(nameof(PerDayStr));
        OnPropertyChanged(nameof(WeekSaveStr));
        OnPropertyChanged(nameof(MonthSaveStr));
        OnPropertyChanged(nameof(YearSaveStr));
        OnPropertyChanged(nameof(YearPouchesStr));
        OnPropertyChanged(nameof(CurrencySymbol));

        // These read the live numbers back, so they move with the steppers too.
        OnPropertyChanged(nameof(ContainersPerWeekLine));
        OnPropertyChanged(nameof(PerUnitLine));
        OnPropertyChanged(nameof(YearUnitsLine));
    }

    /// <summary>Re-labels the whole wizard the instant the user flips Snus ⇄ Cigarettes.</summary>
    private void RaiseCopy()
    {
        OnPropertyChanged(nameof(Addiction));
        OnPropertyChanged(nameof(QuitHeadline));
        OnPropertyChanged(nameof(UnitsPerDayLabel));
        OnPropertyChanged(nameof(UnitsPerContainerLabel));
        OnPropertyChanged(nameof(ContainerCostQuestion));
        OnPropertyChanged(nameof(PerContainerLabel));
        OnPropertyChanged(nameof(ContainerSavingsLine));
        RaiseSavings();
    }

    partial void OnAddictionIndexChanged(int value) => RaiseCopy();
    partial void OnPouchesPerDayChanged(int value) => RaiseSavings();
    partial void OnPouchesPerCanChanged(int value) => RaiseSavings();
    partial void OnCanPriceChanged(decimal value) => RaiseSavings();

    /// <summary>Switching currency re-renders every money string on the wizard.</summary>
    partial void OnCurrencyChanged(Currency value) => RaiseSavings();

    partial void OnQuitModeIndexChanged(int value)
    {
        OnPropertyChanged(nameof(AlreadyQuit));
        OnPropertyChanged(nameof(QuitDayLabel));
    }

    partial void OnQuitDateChanged(DateTime value)
    {
        OnPropertyChanged(nameof(DayNum));
        OnPropertyChanged(nameof(QuitDayLabel));
        OnPropertyChanged(nameof(QuitTimeStr));
    }

    partial void OnQuitTimeChanged(TimeSpan value)
    {
        OnPropertyChanged(nameof(DayNum));
        OnPropertyChanged(nameof(QuitDayLabel));
        OnPropertyChanged(nameof(QuitTimeStr));
    }

    partial void OnNameChanged(string value) => OnPropertyChanged(nameof(GreetingName));
}
