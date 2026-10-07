using System.Globalization;
using Nicotine_Stop.Data;
using SnusStop.Core.Models;
using SnusStop.Core.Services;

namespace Nicotine_Stop.Views;

public partial class GoalEditPage : ContentPage
{
    private readonly IGoalRepository _goals;
    private GoalItem? _editing;
    private string? _photoPath;

    public event Action? Closed;

    public GoalEditPage(IGoalRepository goals)
    {
        InitializeComponent();
        _goals = goals;
        SaveBtn.Command = new Command(async () => await SaveAsync());
    }

    public void Init(GoalItem? goal)
    {
        _editing = goal;
        TitleLabel.Text = goal is null ? "New goal" : "Edit goal";
        NameEntry.Text = goal?.Name ?? "";
        PriceEntry.Text = goal is null ? "" : ((int)goal.Price).ToString();
        _photoPath = goal?.PhotoPath;
        ApplyPhoto();
        DeleteLabel.IsVisible = goal is not null;
    }

    private void ApplyPhoto()
    {
        bool has = !string.IsNullOrEmpty(_photoPath);
        PhotoImage.IsVisible = has;
        PhotoPlaceholder.IsVisible = !has;
        if (has) PhotoImage.Source = _photoPath;
    }

    private async void OnPickPhoto(object? sender, EventArgs e)
    {
        try
        {
            var photo = await MediaPicker.Default.PickPhotoAsync();
            if (photo is null) return;

            var dest = Path.Combine(FileSystem.AppDataDirectory, $"goal_{Guid.NewGuid():N}.jpg");
            using (var src = await photo.OpenReadAsync())
            using (var dst = File.Create(dest))
                await src.CopyToAsync(dst);

            _photoPath = dest;
            ApplyPhoto();
        }
        catch
        {
            // permission denied or cancelled — keep the placeholder
        }
    }

    private async Task SaveAsync()
    {
        var name = (NameEntry.Text ?? "").Trim();
        if (string.IsNullOrEmpty(name))
        {
            await DisplayAlert("Name needed", "Give your goal a name.", "OK");
            return;
        }
        if (!GoalPrice.TryParse(PriceEntry.Text, CultureInfo.CurrentCulture, out var price))
        {
            await DisplayAlert("Price needed", "Enter what your goal costs — a number above 0.", "OK");
            return;
        }

        var goal = _editing ?? new GoalItem { SortOrder = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds() };
        goal.Name = name;
        goal.Price = price;
        goal.PhotoPath = _photoPath;
        await _goals.UpsertAsync(goal);
        await CloseAsync();
    }

    private async void OnDelete(object? sender, EventArgs e)
    {
        if (_editing is null) return;
        bool ok = await DisplayAlert("Delete goal", $"Remove \"{_editing.Name}\"?", "Delete", "Keep");
        if (!ok) return;
        await _goals.DeleteAsync(_editing.Id);
        await CloseAsync();
    }

    private async void OnCancel(object? sender, EventArgs e) => await CloseAsync();

    private async Task CloseAsync()
    {
        if (Navigation.ModalStack.Count > 0)
            await Navigation.PopModalAsync();
        Closed?.Invoke();
    }
}
