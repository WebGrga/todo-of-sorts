namespace ToDoOfSorts.App.Controls;

public sealed class InkStamp : Grid
{
    private Color _ink = Color.FromArgb("#B83220"), _paper = Color.FromArgb("#FFF9E9");
    private readonly Label _text;
    private readonly GraphicsView _frame, _wear;
    public Color Ink { get => _ink; set { _ink = value; _text.TextColor = value; _frame.Invalidate(); } }
    public Color Paper { get => _paper; set { _paper = value; _wear.Invalidate(); } }
    public string Word { get => _text.Text; set { _text.Text = value; _text.FontSize = value.Contains('\n') ? 36 : value.Length > 7 ? 27 : 39; _text.LineHeight = .85; } }
    public InkStamp()
    {
        InputTransparent = true; HeightRequest = 55; WidthRequest = 132;
        _frame = new GraphicsView { Drawable = new StampDrawing(this, false), InputTransparent = true };
        _text = new Label { Text = "DONE", FontFamily = "Stamp", FontSize = 39, CharacterSpacing = 1, TextColor = _ink, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center, Margin = new Thickness(9, 0), InputTransparent = true, FontAutoScalingEnabled = false };
        _wear = new GraphicsView { Drawable = new StampDrawing(this, true), InputTransparent = true };
        _text.TranslationY = -3;
        Add(_frame); Add(_text); Add(_wear);
    }
    private sealed class StampDrawing(InkStamp owner, bool wear) : IDrawable
    {
        public void Draw(ICanvas canvas, RectF rect)
        {
            if (!wear)
            {
                canvas.StrokeColor = owner.Ink; canvas.StrokeSize = 3; canvas.DrawRectangle(3, 3, rect.Width - 6, rect.Height - 6);
                canvas.StrokeSize = 1; canvas.DrawRectangle(7, 7, rect.Width - 14, rect.Height - 14); return;
            }
            canvas.StrokeColor = owner.Paper.WithAlpha(.68f); canvas.StrokeSize = .65f;
            for (var y = 8; y < rect.Height; y += 12)
                canvas.DrawLine(2, y + 5, rect.Width - 2, y - 5);
            canvas.FillColor = owner.Paper.WithAlpha(.55f);
            for (var i = 0; i < 35; i++) canvas.FillRectangle(5 + i * 37 % Math.Max(6, (int)rect.Width - 10), 5 + i * 13 % Math.Max(6, (int)rect.Height - 10), 1.2f, .7f);
        }
    }
}

public sealed class InkBurst : GraphicsView, IDrawable
{
    public double Phase { get; set; }
    public Color Ink { get; set; } = Colors.Red;
    public InkBurst() { Drawable = this; InputTransparent = true; }
    public void Draw(ICanvas canvas, RectF rect)
    {
        if (Phase <= 0 || Phase >= 1) return;
        canvas.FillColor = Ink.WithAlpha((float)(1 - Phase));
        for (int i = 0; i < 16; i++)
        {
            var angle = i * Math.PI / 8;
            var x = rect.Width * .66 + Math.Cos(angle) * (25 + i % 4 * 14) * Phase;
            var y = rect.Height * .7 + Math.Sin(angle) * 60 * Phase;
            canvas.FillEllipse((float)x, (float)y, 3 + i % 3, 2 + i % 2);
        }
    }
}
