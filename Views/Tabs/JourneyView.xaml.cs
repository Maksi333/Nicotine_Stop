using Nicotine_Stop.ViewModels;

namespace Nicotine_Stop.Views.Tabs;

public partial class JourneyView : ContentView
{
    public JourneyView(JourneyViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
