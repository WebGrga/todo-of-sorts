using System.ComponentModel;
using ToDoOfSorts.Core;
using ToDoOfSorts.App.Services;

namespace ToDoOfSorts.App.ViewModels;

public sealed class HomeViewModel(AppSession session) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public DateOnly SelectedDate { get; set; }
    private AppState? _projectedState;
    private DateOnly _projectedDate, _projectedToday;
    private long _projectedRevision = -1;
    private DaySummary? _summary;
    private RunSummary? _runs;
    public string DateLabel => SelectedDate == session.Today ? $"TODAY / {SelectedDate:ddd, dd MMM}".ToUpperInvariant() : SelectedDate.ToString("ddd / dd MMM yyyy").ToUpperInvariant();
    public string Heading => Summary.Rest ? "Take a\nbreather." : "Consider\nit done.";
    public DaySummary Summary { get { EnsureProjection(); return _summary!; } }
    public string ProgressLabel => Summary.Commitments > 0 ? $"{Summary.StampedCommitments} / {Summary.Commitments} STAMPED"
        : Summary.Completed > 0 ? $"{Summary.Completed} BONUS STAMPED" : "NO COMMITMENTS";
    public string XpLabel => $"{Summary.Xp} XP";
    public string RunLabel { get { EnsureProjection(); return $"{_runs!.Current}-DAY RUN"; } }
    private void EnsureProjection()
    {
        var today = session.Today;
        if (_projectedState == session.State && _projectedRevision == session.State.Revision && _projectedDate == SelectedDate && _projectedToday == today) return;
        _summary = Statistics.Day(session.State, SelectedDate);
        _runs = Statistics.Runs(session.State, today);
        _projectedState = session.State; _projectedRevision = session.State.Revision; _projectedDate = SelectedDate; _projectedToday = today;
    }
    public void Refresh() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
}
