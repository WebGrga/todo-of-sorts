namespace ToDoOfSorts.App.Controls;

public sealed class PaperSurface : GraphicsView, IDrawable
{
    public Color PaperColor { get; set; } = Color.FromArgb("#FFF9E9");
    public PaperSurface() { Drawable = this; InputTransparent = true; }
    public void Draw(ICanvas canvas, RectF rect)
    {
        var path = new PathF(); path.MoveTo(0, 0); path.LineTo(rect.Width, 0); path.LineTo(rect.Width, rect.Height - 3);
        var i = 0;
        for (var x = rect.Width; x > 0; x -= 5 + i % 4 * 2)
        {
            path.LineTo(Math.Max(0, x - 5 - i % 4 * 2), rect.Height - (i % 2 == 0 ? .2f + i % 3 * .4f : 2.1f + i % 3 * .5f));
            i++;
        }
        path.LineTo(0, 0); path.Close();
        canvas.FillColor = PaperColor; canvas.FillPath(path);
        canvas.FillColor = Color.FromArgb("#746B46").WithAlpha(.032f);
        for (var y = 9; y < rect.Height - 6; y += 11)
            for (var x = 5 + (int)y % 13; x < rect.Width; x += 19)
                canvas.FillCircle(x, y, .42f);
    }
}

public sealed class DashedRule : GraphicsView, IDrawable
{
    public Color Ink { get; set; } = Color.FromArgb("#BDB49A");
    public DashedRule() { Drawable = this; InputTransparent = true; }
    public void Draw(ICanvas canvas, RectF rect)
    {
        canvas.StrokeColor = Ink; canvas.StrokeSize = 1;
        canvas.StrokeDashPattern = [3, 3]; canvas.DrawLine(0, .5f, rect.Width, .5f);
    }
}

public sealed class CommitmentMeter : GraphicsView, IDrawable
{
    private double _progress;
    public double Progress { get => _progress; set { _progress = value; Invalidate(); } }
    public int Count { get; set; }
    public Color Ink { get; set; } = Colors.DarkOliveGreen;
    public Color Track { get; set; } = Colors.Gray;
    public CommitmentMeter() { Drawable = this; HeightRequest = 7; InputTransparent = true; }
    public void Draw(ICanvas canvas, RectF rect)
    {
        var count = Math.Clamp(Count, 1, 24); var gap = 5f;
        var width = (rect.Width - (count - 1) * gap) / count;
        for (var i = 0; i < count; i++)
        {
            canvas.FillColor = Track; canvas.FillRectangle(i * (width + gap), 0, width, rect.Height);
            var filled = Math.Clamp(Progress * count - i, 0, 1);
            if (filled > 0) { canvas.FillColor = Ink; canvas.FillRectangle(i * (width + gap), 0, width * (float)filled, rect.Height); }
        }
    }
}
