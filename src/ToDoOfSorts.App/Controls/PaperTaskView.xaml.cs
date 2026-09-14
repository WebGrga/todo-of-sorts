using ToDoOfSorts.Core;
using ToDoOfSorts.App.Design;
using ToDoOfSorts.App.Services;

namespace ToDoOfSorts.App.Controls;

public partial class PaperTaskView : ContentView
{
    private readonly TaskOccurrence _task;
    private readonly AppSession _session;
    private readonly Func<Guid, PaperTaskView, Task> _complete;
    private readonly Func<Guid, Task> _details;
    private bool _busy;
    public PaperTaskView(TaskOccurrence task, AppSession session, Func<Guid, PaperTaskView, Task> complete, Func<Guid, Task> details, int number = 1)
    {
        InitializeComponent(); _task = task; _session = session; _complete = complete; _details = details;
        var p = Palette.For(session.State.Settings.Theme);
        Stock.PaperColor = p.Paper; StampButton.TextColor = p.Stamp;
        Rotation = number % 3 == 1 ? -.8 : number % 3 == 2 ? .6 : -.5;
        TaskNumber.Text = number.ToString("00");
        SemanticProperties.SetDescription(TaskTitle, $"{task.Title}. Tap for details and actions.");
        Imprint.Ink = p.Stamp; Imprint.Paper = p.Paper; Imprint.Rotation = task.StampAngle;
        Flecks.Ink = p.Stamp; FlyingXp.TextColor = p.Stamp;
        TaskTitle.Text = task.Title;
        Meta.Text = $"{task.Category.ToUpperInvariant()} / {(task.Time.HasValue ? task.Time.Value.ToString("HH:mm") : "ANYTIME")}" + (task.Status == Outcome.InProgress ? " / STARTED" : !task.Commitment ? " / BONUS" : "");
        Points.Text = $"+{task.Xp} XP"; FlyingXp.Text = Points.Text;
        SemanticProperties.SetDescription(StampButton, task.Date == session.Today ? $"Stamp {task.Title} done" : $"Open {task.Title}");
        Shadow = new Shadow { Brush = Color.FromArgb("#302D17"), Offset = new Point(0, 4), Radius = 4, Opacity = .16f };
        RenderOutcome(task.Status);
    }
    private bool Reduced => _session.State.Settings.ReduceMotion || _session.Feedback.SystemReducedMotion;
    private void RenderOutcome(Outcome outcome)
    {
        var done = outcome == Outcome.Completed;
        Imprint.IsVisible = done; Points.IsVisible = outcome is Outcome.Planned or Outcome.InProgress;
        StampButton.IsEnabled = outcome is Outcome.Planned or Outcome.InProgress;
        StampButton.FontFamily = StampButton.IsEnabled ? "Stamp" : "Mono";
        StampButton.FontSize = StampButton.IsEnabled ? 23 : 10;
        StampButton.TextColor = StampButton.IsEnabled ? Palette.For(_session.State.Settings.Theme).Stamp : Color.FromArgb("#686552");
        Perforation.IsVisible = !done;
        StampButton.Text = outcome switch { Outcome.Completed => "STAMPED.", Outcome.Skipped => "SKIPPED INTENTIONALLY", Outcome.Rescheduled => "MOVED TO ANOTHER DAY", Outcome.Unresolved => "NO OUTCOME RECORDED", _ => "STAMP IT ↓" };
        if (StampButton.IsEnabled && _task.Date != _session.Today)
        { StampButton.Text = "VIEW TASK →"; StampButton.FontFamily = "Mono"; StampButton.FontSize = 12; }
    }
    private async void OnPressed(object? sender, EventArgs e) { if (!_busy && !Reduced) await Paper.ScaleToAsync(.976, 65); }
    private async void OnReleased(object? sender, EventArgs e) { if (!_busy) await Paper.ScaleToAsync(1, 65); }
    private async void OnStamp(object? sender, EventArgs e)
    {
        if (_busy) return; _busy = true; StampButton.IsEnabled = false;
        try { await _complete(_task.Id, this); }
        finally { _busy = false; Paper.Scale = 1; if (!Imprint.IsVisible) StampButton.IsEnabled = true; }
    }
    private async void OnDetails(object? sender, TappedEventArgs e) { if (!_busy) await _details(_task.Id); }
    public async Task PlayStampAsync(Action? onImpact = null)
    {
        RenderOutcome(Outcome.Completed);
        var settings = _session.State.Settings;
        if (Reduced) { _session.Feedback.Impact(settings); onImpact?.Invoke(); return; }
        var energetic = settings.Theme == Experience.Energetic;
        Imprint.Scale = energetic ? 2.8 : settings.Theme == Experience.Tactile ? 1.25 : 1;
        Imprint.Opacity = 0; Imprint.TranslationX = energetic ? -22 : 0; Imprint.TranslationY = energetic ? -37 : 0;
        await Task.WhenAll(Imprint.FadeToAsync(1, 90), Imprint.ScaleToAsync(.91, 140, Easing.CubicIn), Imprint.TranslateToAsync(0, 0, 140, Easing.CubicIn));
        _session.Feedback.Impact(settings);
        onImpact?.Invoke();
        if (energetic)
        {
            new Animation(v => { Flecks.Phase = v; Flecks.Invalidate(); }, 0, 1).Commit(Flecks, "ink", length: 420);
            await Task.WhenAll(Paper.ScaleToAsync(.975, 45), Paper.TranslateToAsync(0, 4, 45));
            await Task.WhenAll(Paper.ScaleToAsync(1, 100), Paper.TranslateToAsync(0, 0, 100, Easing.SpringOut));
        }
        FlyingXp.Opacity = 1;
        await Task.WhenAll(Imprint.ScaleToAsync(1, 170, Easing.SpringOut), FlyingXp.TranslateToAsync(0, -32, 330, Easing.CubicOut), FlyingXp.FadeToAsync(0, 420));
        SemanticScreenReader.Default.Announce($"{_task.Title}. Done. {_task.Xp} XP earned.");
    }
}
