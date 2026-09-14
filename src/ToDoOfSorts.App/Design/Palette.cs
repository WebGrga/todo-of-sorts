using ToDoOfSorts.Core;
using Microsoft.Maui.Platform;

namespace ToDoOfSorts.App.Design;

public sealed record Palette(Color Desk, Color Paper, Color Ink, Color Text, Color Muted, Color Rule, Color Stamp, Color Accent)
{
    public Color PaperMuted => Color.FromArgb("#686552");
    public Color ActionText => Desk.Red < .2f
        ? Color.FromArgb(Stamp.Equals(Color.FromArgb("#48674F")) ? "#A7C4A3" : Stamp.Equals(Color.FromArgb("#334A67")) ? "#A8C6EB" : "#FF9B85")
        : Color.FromArgb(Stamp.Equals(Color.FromArgb("#48674F")) ? "#3E5B44" : Stamp.Equals(Color.FromArgb("#334A67")) ? "#2B405B" : "#A92F1E");
    public static Palette For(Experience theme)
    {
        var dark = Application.Current?.RequestedTheme == AppTheme.Dark;
        return new(Color.FromArgb(dark ? "#191A17" : "#DEDBD0"), Color.FromArgb(dark ? "#EEE5CE" : "#FFF9E9"),
            Color.FromArgb("#25271F"), Color.FromArgb(dark ? "#F0EDDF" : "#292D23"),
            Color.FromArgb(dark ? "#B4B8A5" : "#646555"), Color.FromArgb(dark ? "#45493C" : "#B8BBA9"),
            Color.FromArgb(theme == Experience.Calm ? "#48674F" : theme == Experience.Tactile ? "#334A67" : "#B83220"),
            Color.FromArgb(dark ? "#D7F27F" : "#354D2E"));
    }
}

public static class Ui
{
    public static void Recolor(IVisualTreeElement root, Palette previous, Palette next)
    {
        var oldColors = new[] { previous.Desk, previous.Paper, previous.Ink, previous.Text, previous.Muted, previous.Rule, previous.Stamp, previous.Accent, previous.ActionText };
        var newColors = new[] { next.Desk, next.Paper, next.Ink, next.Text, next.Muted, next.Rule, next.Stamp, next.Accent, next.ActionText };
        Color Map(Color value) { var index = Array.IndexOf(oldColors, value); return index < 0 ? value : newColors[index]; }
        void Visit(IVisualTreeElement item)
        {
            if (item is VisualElement visual && visual.BackgroundColor is { } background) visual.BackgroundColor = Map(background);
            switch (item)
            {
                case Label label: label.TextColor = Map(label.TextColor); break;
                case Button button: button.TextColor = Map(button.TextColor); if (button.BorderColor is { } border) button.BorderColor = Map(border); break;
                case InputView input: input.TextColor = Map(input.TextColor); if (input.PlaceholderColor is { } placeholder) input.PlaceholderColor = Map(placeholder); break;
                case DatePicker date: date.TextColor = Map(date.TextColor); break;
                case TimePicker time: time.TextColor = Map(time.TextColor); break;
                case Picker picker: picker.TextColor = Map(picker.TextColor); break;
                case Switch toggle: if (toggle.OnColor is { } on) toggle.OnColor = Map(on); if (toggle.ThumbColor is { } thumb) toggle.ThumbColor = Map(thumb); break;
                case BoxView box: box.Color = Map(box.Color); break;
            }
            foreach (var child in item.GetVisualChildren()) Visit(child);
        }
        Visit(root);
    }
    public static void ApplySystemBars(ContentPage page, Palette p)
    {
        if (page.Parent is NavigationPage nav) { nav.BackgroundColor = p.Desk; nav.BarBackgroundColor = p.Desk; nav.BarTextColor = p.Text; }
#if ANDROID
        if (Platform.CurrentActivity?.Window is { } window)
        {
            window.DecorView.SetBackgroundColor(p.Desk.ToPlatform());
            var controller = new AndroidX.Core.View.WindowInsetsControllerCompat(window, window.DecorView);
            var light = p.Desk.Red * .2126 + p.Desk.Green * .7152 + p.Desk.Blue * .0722 > .5;
            controller.AppearanceLightStatusBars = light; controller.AppearanceLightNavigationBars = light;
        }
#endif
    }
    public static Label Text(string text, double size = 15, Color? color = null, bool bold = false) => new()
    {
        Text = text, FontSize = size, TextColor = color ?? Colors.Black,
        FontFamily = bold ? "BodyBold" : "Body", LineBreakMode = LineBreakMode.WordWrap
    };
    public static View Heading(string text, Color color, double size = 42)
    {
        var lines = text.ToUpperInvariant().Split('\n');
        var stack = new VerticalStackLayout { Spacing = 0 };
        foreach (var line in lines)
        {
            var label = new Label { Text = line, TextColor = color, FontFamily = "Stamp", FontSize = size,
                LineBreakMode = LineBreakMode.WordWrap, CharacterSpacing = -.6,
                FontAutoScalingEnabled = lines.Length == 1, VerticalTextAlignment = TextAlignment.Center };
            if (lines.Length == 1) stack.Add(label);
            else
            {
                // Anton reserves space outside its capital letters. Keep its native text frame
                // intact, then position that frame inside a line sized to the visible capitals.
                var lineBox = new AbsoluteLayout { HeightRequest = size * .98, IsClippedToBounds = false };
                AbsoluteLayout.SetLayoutBounds(label, new Rect(0, -size * .30, 1, size * 1.6));
                AbsoluteLayout.SetLayoutFlags(label, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.WidthProportional);
                lineBox.Add(label); stack.Add(lineBox);
            }
        }
        SemanticProperties.SetHeadingLevel(stack, SemanticHeadingLevel.Level1);
        return stack;
    }
    public static Label Mono(string text, double size, Color color) => new()
    { Text = text, FontFamily = "Mono", FontSize = size, TextColor = color, CharacterSpacing = .2 };
    public static Button Button(string text, Color background, Color foreground, EventHandler action)
    {
        var button = new Button { Text = text, BackgroundColor = background, TextColor = foreground, FontFamily = "Mono",
            CornerRadius = 6, Padding = new Thickness(14, 10), MinimumHeightRequest = 48, FontSize = 12, CharacterSpacing = .2, LineBreakMode = LineBreakMode.WordWrap };
        button.Clicked += action;
        return button;
    }
    public static View Rule(Color color) => new BoxView { Color = color, HeightRequest = 1, Margin = new Thickness(0, 10) };
    public static View Field(string label, View input, Palette p)
    {
        input.BackgroundColor = Colors.Transparent;
        if (input is InputView entry) { entry.TextColor = p.Ink; entry.PlaceholderColor = p.PaperMuted; }
        if (input is Picker picker) { picker.TextColor = p.Ink; picker.TitleColor = p.PaperMuted; }
        if (input is DatePicker date) date.TextColor = p.Ink;
        if (input is TimePicker time) time.TextColor = p.Ink;
        var field = new Border { BackgroundColor = p.Paper, StrokeThickness = 0, Padding = new Thickness(12, 2), Content = input };
        return new VerticalStackLayout { Spacing = 7, Children = { Mono(label, 10, p.Muted), field } };
    }
    public static Grid Row(View left, View right)
    {
        left.VerticalOptions = LayoutOptions.Center; right.VerticalOptions = LayoutOptions.Center;
        var row = new Grid { ColumnDefinitions = [new(GridLength.Star), new(GridLength.Auto)], ColumnSpacing = 12 };
        row.Add(left); row.Add(right, 1); return row;
    }
    public static Grid EqualRow(View left, View right)
    {
        var row = new Grid { ColumnDefinitions = [new(GridLength.Star), new(GridLength.Star)], ColumnSpacing = 12 };
        row.Add(left); row.Add(right, 1); return row;
    }
    public static View Choices(string[] names, int selected, Palette p, Action<int> changed)
    {
        var row = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap };
        var buttons = new List<Button>();
        void Paint()
        {
            for (var i = 0; i < buttons.Count; i++)
            {
                buttons[i].BackgroundColor = i == selected ? p.Stamp : p.Paper;
                buttons[i].TextColor = i == selected ? p.Paper : p.Ink;
                SemanticProperties.SetDescription(buttons[i], names[i] + (i == selected ? ", selected" : ""));
            }
        }
        for (var i = 0; i < names.Length; i++)
        {
            var index = i;
            var button = Button(names[i], p.Paper, p.Ink, (_, _) => { selected = index; Paint(); changed(index); });
            button.MinimumWidthRequest = Math.Min(280, names[i].Length * 8 + 36); button.MaximumWidthRequest = 300;
            button.Margin = new Thickness(0, 0, 6, 6); FlexLayout.SetShrink(button, 0);
            buttons.Add(button); row.Add(button);
        }
        Paint(); return row;
    }
}
