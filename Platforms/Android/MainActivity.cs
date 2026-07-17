using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Nicotine_Stop.Services;

namespace Nicotine_Stop
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            HandleWidgetIntent(Intent);
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);
            Intent = intent;                 // keep the activity's current intent in sync
            HandleWidgetIntent(intent);
        }

        // A widget's SOS button launches us with navigate=sos. Route it to the shared bridge; the
        // MAUI page layer decides how/when to present the SOS takeover.
        private static void HandleWidgetIntent(Intent? intent)
        {
            if (intent?.GetStringExtra("navigate") == "sos")
                WidgetNavigation.RequestSos();
        }
    }
}
