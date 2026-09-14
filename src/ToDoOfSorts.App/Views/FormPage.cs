using ToDoOfSorts.App.Design;
using ToDoOfSorts.App.Services;

namespace ToDoOfSorts.App.Views;

public class FormPage : ContentPage
{
    protected readonly AppSession Session;
    protected Palette P;
    protected readonly VerticalStackLayout Form = new() { Spacing = 15, Padding = new Thickness(20, 14, 20, 28), MaximumWidthRequest = 450 };
    protected bool Busy;
    private readonly Grid _footer = new() { IsVisible = false, Padding = new Thickness(20, 10, 20, 12), MaximumWidthRequest = 450 };
    protected override bool OnBackButtonPressed()
    {
        if (PaperSheet.CloseTop(this) || Busy) return true;
        if (Navigation.NavigationStack.Count <= 1) return base.OnBackButtonPressed();
        _ = GoBackAsync(); return true;
    }
    private Task GoBackAsync() => Attempt(async () =>
    { if (Navigation.NavigationStack.LastOrDefault() == this) await Navigation.PopAsync(false); });
    public FormPage(AppSession session, string title)
    {
        Session = session; P = Palette.For(session.State.Settings.Theme); Title = title;
        BackgroundColor = P.Desk;
        NavigationPage.SetHasNavigationBar(this, false);
        var surface = new Grid { BackgroundColor = P.Desk, RowDefinitions = [new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto)] };
        var back = Ui.Button("‹", Colors.Transparent, P.Text, async (_, _) =>
        {
            if (!Busy) await GoBackAsync();
        });
        back.FontSize = 32; back.Padding = 0; back.WidthRequest = 48;
        SemanticProperties.SetDescription(back, "Back");
        var header = new Grid { ColumnDefinitions = [new(new GridLength(48)), new(GridLength.Star)], ColumnSpacing = 8, Padding = new Thickness(8, 0, 20, 0) };
        header.Add(back);
        var caption = Ui.Mono(title.ToUpperInvariant(), 12, P.Text); caption.VerticalOptions = LayoutOptions.Center; header.Add(caption, 1);
        surface.Add(header); surface.Add(new ScrollView { Content = Form }, 0, 1);
        surface.Add(_footer, 0, 2);
        Content = surface;
        SafeAreaEdges = SafeAreaEdges.All;
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (Application.Current is { } app) app.RequestedThemeChanged += OnThemeChanged;
        RefreshTheme();
    }
    protected override void OnDisappearing()
    {
        if (Application.Current is { } app) app.RequestedThemeChanged -= OnThemeChanged;
        base.OnDisappearing();
    }
    private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e) => RefreshTheme();
    private void RefreshTheme()
    {
        var next = Palette.For(Session.State.Settings.Theme);
        if (Content is Grid host)
            foreach (var sheet in host.Children.OfType<PaperSheet>()) sheet.RefreshTheme(next);
        Ui.Recolor(this, P, next); P = next;
        Ui.ApplySystemBars(this, P);
    }
    protected async Task Attempt(Func<Task> work)
    {
        if (Busy) return; Busy = true;
        try { await work(); }
        catch (Exception ex) { await PaperSheet.AlertAsync(this, P, "Couldn't finish that", ex.Message, "OK"); }
        finally { Busy = false; }
    }
    protected View Toggle(string label, Switch control, string? explanation = null)
    {
        control.OnColor = P.Accent; control.ThumbColor = P.Paper;
        var text = new VerticalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center };
        text.Add(Ui.Text(label, 15, P.Text, true));
        if (explanation is not null) text.Add(Ui.Text(explanation, 12, P.Muted));
        return Ui.Row(text, control);
    }
    protected Button Action(string title, Func<Task> action, bool primary = false)
    {
        var button = Ui.Button(title, primary ? P.Stamp : Colors.Transparent, primary ? P.Paper : P.Text, async (_, _) => await Attempt(action));
        if (primary) { button.FontFamily = "Stamp"; button.FontSize = 21; button.Text = title.ToUpperInvariant(); button.CharacterSpacing = .6; }
        Form.Add(button); return button;
    }
    protected void FooterAction(string title, Func<Task> action)
    {
        var button = Ui.Button(title, P.Stamp, P.Paper, (_, _) => { });
        button.FontFamily = "Stamp"; button.FontSize = 22; button.CornerRadius = 6;
        button.Clicked += async (_, _) => await Attempt(async () =>
        {
            button.IsEnabled = false;
            try { await action(); }
            finally { button.IsEnabled = true; }
        });
        _footer.Clear(); _footer.Add(button); _footer.IsVisible = true;
    }
}
