using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Nicotine_Stop.ViewModels;

public partial class MainShellViewModel : ObservableObject
{
    [ObservableProperty] private int currentTab; // 0 Home · 1 Goals · 2 Health · 3 Journey

    [RelayCommand]
    private void SelectTab(int index) => CurrentTab = index;
}
