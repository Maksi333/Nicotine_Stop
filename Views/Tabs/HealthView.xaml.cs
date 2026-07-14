using Nicotine_Stop.ViewModels;

namespace Nicotine_Stop.Views.Tabs;

public partial class HealthView : ContentView
{
    public HealthView(HealthViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
