using CommunityToolkit.Mvvm.ComponentModel;
using SnusStop.Core.Models;

namespace Nicotine_Stop.ViewModels;

/// <summary>Currency selector chip (frame 1d). The view styles the selected state via triggers.</summary>
public partial class CurrencyChipVM : ObservableObject
{
    public Currency Currency { get; }
    public string Name { get; }

    [ObservableProperty] private bool selected;

    public CurrencyChipVM(Currency currency, bool selected)
    {
        Currency = currency;
        Name = currency.Code();
        this.selected = selected;
    }
}

/// <summary>Motivation card (frame 1f).</summary>
public partial class MotivationVM : ObservableObject
{
    public string Key { get; }
    public string Label { get; }
    public string Emoji { get; }
    public Color IconBg { get; }
    public bool IsFreeText { get; }

    [ObservableProperty] private bool selected;

    public MotivationVM(string key, string label, string emoji, Color iconBg, bool isFreeText = false)
    {
        Key = key;
        Label = label;
        Emoji = emoji;
        IconBg = iconBg;
        IsFreeText = isFreeText;
    }
}

/// <summary>One cell in a month calendar grid. Used by onboarding (pick a date) and Journey (status).</summary>
public partial class DayCellVM : ObservableObject
{
    public int? N { get; }               // null = blank pad cell
    public bool IsToday { get; }

    [ObservableProperty] private bool selected;

    // Journey status ("none" | "clean" | "today" | "slip"); the view maps it to themed colours.
    public string Status { get; init; } = "none";
    public bool HasCraving { get; init; }

    public DayCellVM(int? n, bool isToday = false, bool selected = false)
    {
        N = n;
        IsToday = isToday;
        this.selected = selected;
    }

    public string Text => N?.ToString() ?? "";
    public bool HasDay => N is not null;
}
