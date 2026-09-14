using ToDoOfSorts.Core;
using ToDoOfSorts.Infrastructure;

namespace ToDoOfSorts.App.Services;

public sealed record ReminderAccess(bool Allowed, bool Precise, string Description);
public interface IReminderService
{
    Task<ReminderAccess> GetAccessAsync();
    Task<ReminderAccess> RequestAccessAsync();
    Task ReplaceAsync(IReadOnlyList<ReminderPlan> plans);
    void OpenSettings();
}
public interface IFeedbackService
{
    bool SystemReducedMotion { get; }
    Task PrepareAsync();
    void Impact(AppSettings settings, bool dayWin = false);
}

public sealed class AppSession(LocalStore store, IClock clock, IReminderService reminders, IFeedbackService feedback)
{
    public LocalStore Store { get; } = store;
    public IClock Clock { get; } = clock;
    public IReminderService Reminders { get; } = reminders;
    public IFeedbackService Feedback { get; } = feedback;
    public AppState State { get; private set; } = new();
    public string? ReminderError { get; private set; }
    public event Action? ReminderStatusChanged;
    public DateOnly Today => PlanningTime.Today(State, Clock.UtcNow);
    private readonly SemaphoreSlim _sync = new(1, 1);
    private readonly SemaphoreSlim _stateGate = new(1, 1);

    public async Task LoadAsync(bool syncReminders = true)
    {
        await _stateGate.WaitAsync();
        try { State = await Store.ReadAsync(); }
        finally { _stateGate.Release(); }
        await Feedback.PrepareAsync();
        if (syncReminders) await SyncRemindersAsync();
    }
    public async Task ChangeAsync(Action<AppState> mutation)
    {
        await _stateGate.WaitAsync();
        try { State = await Store.MutateAsync(mutation); }
        finally { _stateGate.Release(); }
    }
    public async Task SyncRemindersAsync()
    {
        await _sync.WaitAsync();
        var previousError = ReminderError;
        try
        {
            // Re-read under the scheduler gate so a slower previous call cannot restore stale alerts.
            var latest = await Store.ReadAsync();
            await Reminders.ReplaceAsync(ReminderPlanner.Build(latest, Clock.UtcNow));
            await Store.AcknowledgeScheduleAsync(latest.Revision);
            ReminderError = null;
        }
        catch (Exception ex) { ReminderError = "Reminders need attention: " + ex.Message; }
        finally { _sync.Release(); if (previousError != ReminderError) MainThread.BeginInvokeOnMainThread(() => ReminderStatusChanged?.Invoke()); }
    }
}

public static class NotificationRoute
{
    public static Guid? TaskId { get; private set; }
    public static string? Action { get; private set; }
    public static event Action? Arrived;
    public static void Set(Guid? id, string? action)
    {
        TaskId = id; Action = action;
        MainThread.BeginInvokeOnMainThread(() => Arrived?.Invoke());
    }
    public static (Guid? Id, string? Action) Consume()
    {
        var result = (TaskId, Action); TaskId = null; Action = null; return result;
    }
}
