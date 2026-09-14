using Microsoft.Maui.Controls.Shapes;
using ToDoOfSorts.App.Design;
#if IOS
using UIKit;
#endif

namespace ToDoOfSorts.App.Views;

// An overlay in the current page, keeping the day's paper visible behind it.
public class PaperSheet : Grid
{
    private static readonly Dictionary<ContentPage, PaperSheet> Active = [];
    protected Palette P;
    protected readonly VerticalStackLayout Fields = new() { Spacing = 12 };
    protected readonly Grid Footer = new();
    private readonly Border _panel;
    private Action? _dismiss;
    private double _preferredHeight;
    public bool Busy { get; protected set; }
    public PaperSheet(Palette palette, string title, double height = 390)
    {
        P = palette; _preferredHeight = height;
        var shade = new BoxView { Color = Colors.Black.WithAlpha(.48f) };
        var tap = new TapGestureRecognizer(); tap.Tapped += (_, _) => Dismiss(); shade.GestureRecognizers.Add(tap); Add(shade);
        var layout = new Grid { RowDefinitions = [new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto)], RowSpacing = 12 };
        var close = Ui.Button("×", Colors.Transparent, P.Ink, (_, _) => Dismiss());
        close.FontSize = 24; close.WidthRequest = 48; close.Padding = 0;
        SemanticProperties.SetDescription(close, "Close " + title);
        layout.Add(Ui.Row(Ui.Heading(title, P.Ink, 28), close));
        layout.Add(new ScrollView { Content = Fields }, 0, 1); layout.Add(Footer, 0, 2);
        _panel = new Border { BackgroundColor = P.Paper, StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(18, 18, 0, 0) },
            Padding = new Thickness(20, 8, 20, 16), Content = layout, HeightRequest = height, VerticalOptions = LayoutOptions.End, HorizontalOptions = LayoutOptions.Fill, MaximumWidthRequest = 450 };
        Add(_panel); SizeChanged += (_, _) => Resize(Height);
    }
    protected void SetHeight(double height) { _preferredHeight = height; Resize(Height); }
    private void Resize(double availableHeight) => _panel.HeightRequest = availableHeight > 0
        ? Math.Min(_preferredHeight, Math.Max(100, availableHeight - 16)) : _preferredHeight;
    public void Dismiss()
    {
        if (Busy) return;
#if ANDROID
        if (Platform.CurrentActivity?.CurrentFocus is { } focus)
        {
            var keyboard = Platform.CurrentActivity.GetSystemService(global::Android.Content.Context.InputMethodService) as global::Android.Views.InputMethods.InputMethodManager;
            keyboard?.HideSoftInputFromWindow(focus.WindowToken, global::Android.Views.InputMethods.HideSoftInputFlags.None);
            focus.ClearFocus();
        }
#elif IOS
        Platform.GetCurrentUIViewController()?.View?.EndEditing(true);
#endif
        _dismiss?.Invoke();
    }
    protected void Finish() { Busy = false; Dismiss(); }
    public void RefreshTheme(Palette next) { Ui.Recolor(this, P, next); P = next; }
    public static bool IsOpen(ContentPage page) => Active.ContainsKey(page);
    public static bool CloseTop(ContentPage page)
    {
        if (!Active.TryGetValue(page, out var sheet)) return false;
        sheet.Dismiss(); return true;
    }
    public static async Task ShowAsync(ContentPage page, PaperSheet sheet)
    {
        if (Active.ContainsKey(page)) return;
        // Keep the native page mounted: replacing Content here flashes the Android window
        // background and recreates the underlying controls on every open and close.
        if (page.Content is not Grid host) throw new InvalidOperationException("Paper sheets need a permanent Grid page root.");
        var background = host.Children.OfType<VisualElement>()
            .Select(view => (View: view, WasTransparent: view.InputTransparent)).ToArray();
        foreach (var item in background) item.View.InputTransparent = true;
        Grid.SetRowSpan(sheet, Math.Max(1, host.RowDefinitions.Count));
        Grid.SetColumnSpan(sheet, Math.Max(1, host.ColumnDefinitions.Count));
        sheet.ZIndex = 100;
        // Constrain the first measure too; waiting for SizeChanged briefly fills the screen.
        sheet.Resize(host.Height);
        host.Add(sheet); Active[page] = sheet;
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        sheet._dismiss = () =>
        {
            if (!Active.Remove(page)) return;
            sheet._dismiss = null; host.Remove(sheet);
            foreach (var item in background) item.View.InputTransparent = item.WasTransparent;
            done.TrySetResult();
        };
        await done.Task;
    }
    public static async Task<string?> ChooseAsync(ContentPage page, Palette p, string title, params string[] choices)
    {
        string? result = null;
        var sheet = new PaperSheet(p, title, 92 + choices.Length * 60);
        foreach (var choice in choices)
        {
            var button = Ui.Button(choice, Colors.Transparent, p.Ink, (_, _) => { result = choice; sheet.Dismiss(); });
            button.HorizontalOptions = LayoutOptions.Fill; button.FontFamily = "BodyBold"; button.FontSize = 16;
            button.BorderWidth = 1; button.BorderColor = p.Ink.WithAlpha(.15f); button.CornerRadius = 6; sheet.Fields.Add(button);
        }
        await ShowAsync(page, sheet); return result;
    }
    public static async Task<DateOnly?> DateAsync(ContentPage page, Palette p, DateOnly selected)
    {
        DateOnly? result = null;
        var sheet = new PaperSheet(p, "OPEN A DAY.", 300);
        var picker = new DatePicker { Date = selected.ToDateTime(TimeOnly.MinValue), TextColor = p.Ink,
            BackgroundColor = Colors.Transparent, Format = "dddd, d MMMM yyyy", FontSize = 17 };
        sheet.Fields.Add(Ui.Text("Choose the day you want to see.", 15, p.PaperMuted)); sheet.Fields.Add(picker);
        sheet.Footer.Add(Ui.Button("OPEN THIS DAY", p.Stamp, p.Paper, (_, _) =>
        { result = DateOnly.FromDateTime(picker.Date ?? selected.ToDateTime(TimeOnly.MinValue)); sheet.Dismiss(); }));
        await ShowAsync(page, sheet); return result;
    }
    public static async Task<bool> AlertAsync(ContentPage page, Palette p, string title, string message, string accept, string cancel)
    {
        var accepted = false;
        var sheet = new PaperSheet(p, title, 340);
        sheet.Fields.Add(Ui.Text(message, 16, p.Ink));
        sheet.Footer.Add(Ui.Row(Ui.Button(cancel, Colors.Transparent, p.Ink, (_, _) => sheet.Dismiss()),
            Ui.Button(accept, p.Stamp, p.Paper, (_, _) => { accepted = true; sheet.Dismiss(); })));
        await ShowAsync(page, sheet); return accepted;
    }
    public static async Task AlertAsync(ContentPage page, Palette p, string title, string message, string cancel)
    {
        var sheet = new PaperSheet(p, title, 330);
        sheet.Fields.Add(Ui.Text(message, 16, p.Ink));
        sheet.Footer.Add(Ui.Button(cancel, p.Stamp, p.Paper, (_, _) => sheet.Dismiss()));
        await ShowAsync(page, sheet);
    }
    public static async Task<string?> PromptAsync(ContentPage page, Palette p, string title, string message, string accept = "Save", string cancel = "Cancel", int maxLength = 180, string? initialValue = null)
    {
        string? result = null;
        var sheet = new PaperSheet(p, title, 340);
        var entry = new Entry { Text = initialValue, TextColor = p.Ink, PlaceholderColor = p.PaperMuted, MaxLength = maxLength, FontFamily = "Body", FontSize = 18, BackgroundColor = Colors.Transparent };
        sheet.Fields.Add(Ui.Text(message, 14, p.PaperMuted)); sheet.Fields.Add(entry);
        sheet.Footer.Add(Ui.Row(Ui.Button(cancel, Colors.Transparent, p.Ink, (_, _) => sheet.Dismiss()),
            Ui.Button(accept, p.Stamp, p.Paper, (_, _) => { result = entry.Text ?? ""; sheet.Dismiss(); })));
        await ShowAsync(page, sheet); return result;
    }
}
