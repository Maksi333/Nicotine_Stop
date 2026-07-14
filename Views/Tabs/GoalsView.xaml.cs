using Microsoft.Extensions.DependencyInjection;
using Nicotine_Stop.ViewModels;
using SnusStop.Core.Models;

namespace Nicotine_Stop.Views.Tabs;

public partial class GoalsView : ContentView
{
    private readonly IServiceProvider _services;
    private readonly GoalsViewModel _vm;

    public GoalsView(GoalsViewModel vm, IServiceProvider services)
    {
        InitializeComponent();
        _vm = vm;
        _services = services;
        BindingContext = vm;
        vm.EditRequested += OnEditRequested;
    }

    private async void OnEditRequested(GoalItem? goal)
    {
        var nav = Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation;
        if (nav is null) return;

        var page = _services.GetRequiredService<GoalEditPage>();
        page.Init(goal);
        page.Closed += async () => await _vm.LoadAsync();
        await nav.PushModalAsync(page);
    }
}
