using Android.App;
using Android.Content.PM;
using Android.OS;

namespace ToDoOfSorts.App;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, WindowSoftInputMode = global::Android.Views.SoftInput.AdjustResize, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Route(Intent);
    }
    protected override void OnNewIntent(global::Android.Content.Intent? intent)
    {
        base.OnNewIntent(intent);
        Route(intent);
    }
    private static void Route(global::Android.Content.Intent? intent)
    {
        var action = intent?.GetStringExtra("task_action");
        if (action is null) return;
        var raw = intent?.GetStringExtra("task_id");
        Services.NotificationRoute.Set(Guid.TryParse(raw, out var id) ? id : null, action);
        intent?.RemoveExtra("task_action");
    }
}
