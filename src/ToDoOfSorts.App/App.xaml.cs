using ToDoOfSorts.App.Services;

namespace ToDoOfSorts.App;

public partial class App : Application
{
    private readonly AppSession _session;
    public App(AppSession session)
    {
        InitializeComponent(); _session = session;
#if ANDROID
        Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.Application.SetWindowSoftInputModeAdjust(this,
            Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.WindowSoftInputModeAdjust.Resize);
#endif
    }
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var home = new MainPage(_session);
        var navigation = new NavigationPage(home);
        NavigationPage.SetHasNavigationBar(home, false);
        var window = new Window(navigation);
        window.Resumed += async (_, _) => await home.RefreshFromResumeAsync();
        return window;
    }
}
