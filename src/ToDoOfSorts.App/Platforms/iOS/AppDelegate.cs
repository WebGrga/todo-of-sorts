using Foundation;

namespace ToDoOfSorts.App;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    public override bool FinishedLaunching(UIKit.UIApplication application, NSDictionary? launchOptions)
    {
        Platforms.iOS.IosReminders.Register();
        return base.FinishedLaunching(application, launchOptions);
    }
}
