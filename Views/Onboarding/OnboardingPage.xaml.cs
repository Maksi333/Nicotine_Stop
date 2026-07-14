using Microsoft.Extensions.DependencyInjection;
using Nicotine_Stop.ViewModels;

namespace Nicotine_Stop.Views.Onboarding;

public partial class OnboardingPage : ContentPage
{
    private readonly IServiceProvider _services;

    public OnboardingPage(OnboardingViewModel vm, IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
        BindingContext = vm;
        vm.Completed += OnCompleted;
        Helpers.SafeArea.ApplyInsets(this, top: true, bottom: true);
    }

    private void OnCompleted()
    {
        if (Window is not null)
            Window.Page = _services.GetRequiredService<Nicotine_Stop.Views.MainTabsPage>();
    }
}
