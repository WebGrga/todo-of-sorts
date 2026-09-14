using ToDoOfSorts.Core;
using ToDoOfSorts.App.Design;
using ToDoOfSorts.App.Services;

namespace ToDoOfSorts.App.Views;

public sealed class TaskDetailsPage(AppSession session, Guid id) : FormPage(session, "Task details")
{
    protected override void OnAppearing() { base.OnAppearing(); Render(); }
    private void Render()
    {
        Form.Clear(); var task = TaskEngine.Find(Session.State, id);
        Form.Add(Ui.Heading(task.Title, P.Text, 30));
        Form.Add(Ui.Text($"{task.Date:ddd, d MMM} / {task.Time?.ToString("HH:mm") ?? "Anytime"}\n{task.Category} · {task.Xp} XP · {(task.Commitment ? "Commitment" : "Bonus")}", 14, P.Muted));
        var status = task.Status switch { Outcome.InProgress => "IN PROGRESS", Outcome.Rescheduled => "MOVED", Outcome.Unresolved => "NO OUTCOME RECORDED", _ => task.Status.ToString().ToUpperInvariant() };
        Form.Add(Ui.Text(status, 13, task.Status == Outcome.Completed ? P.ActionText : P.Text, true));
        if (task.SkipReason is { Length: > 0 } reason) Form.Add(Ui.Text(reason, 14, P.Muted));
        if (task.IsActive)
        {
            if (task.Date == Session.Today && task.Status != Outcome.InProgress)
                Action("START THIS TASK", async () => { await Session.ChangeAsync(s => TaskEngine.Start(s, id, Session.Clock.UtcNow)); Render(); await Session.SyncRemindersAsync(); }, true);
            Action("Edit", async () => { await PaperSheet.ShowAsync(this, new TaskEditorPage(Session, task.Date, id)); Render(); });
        }
        if (task.Status is not (Outcome.Completed or Outcome.Rescheduled))
        {
            Action("Move to another day or time", () => Navigation.PushAsync(new MoveTaskPage(Session, id), false));
            if (task.Status != Outcome.Skipped) Action("Skip intentionally", async () =>
            {
                var reason = await PaperSheet.ChooseAsync(this, P, "SKIP THIS TASK.", "No reason", "No longer needed", "Not enough time", "Too much today", "Other");
                if (reason is null or "Cancel") return;
                if (reason == "Other") reason = await PaperSheet.PromptAsync(this, P, "Optional reason", "What changed?", "Skip", "Cancel", maxLength: 200);
                if (reason is null) return;
                await Session.ChangeAsync(s => TaskEngine.Skip(s, id, reason == "No reason" ? null : reason, Session.Clock.UtcNow)); Render(); await Session.SyncRemindersAsync();
            });
        }
        if (task.Status is Outcome.Completed or Outcome.Skipped)
            Action("Undo outcome / reopen", async () => { await Session.ChangeAsync(s => TaskEngine.Reopen(s, id, Session.Clock.UtcNow)); Render(); await Session.SyncRemindersAsync(); });
        if (task.MovedToId is { } destination) Action("Open moved task", () => Navigation.PushAsync(new TaskDetailsPage(Session, destination), false));
        Form.Add(Ui.Rule(P.Rule));
        var extra = new VerticalStackLayout { Spacing = 12, IsVisible = false };
        Action("Corrections & history", () => { extra.IsVisible = !extra.IsVisible; return Task.CompletedTask; });
        Button ExtraAction(string title, Func<Task> action) => Ui.Button(title, Colors.Transparent, P.Text, async (_, _) => await Attempt(action));
        if (task.Date <= Session.Today && task.Status != Outcome.Rescheduled)
            extra.Add(ExtraAction("Record an earlier completion", () => Navigation.PushAsync(new CorrectCompletionPage(Session, id), false)));
        extra.Add(ExtraAction(task.Commitment ? "Make this a bonus" : "Make this a commitment", async () =>
        {
            if (!await PaperSheet.AlertAsync(this, P, "Correct the plan?", "This changes this day's finish line and recalculates its win and your run. The correction remains in history.", "Correct plan", "Cancel")) return;
            await Session.ChangeAsync(s => TaskEngine.AmendCommitment(s, id, !task.Commitment, Session.Clock.UtcNow)); Render();
        }));
        var definition = Session.State.Definitions.First(d => d.Id == task.DefinitionId);
        if (!definition.Archived && definition.RepeatDays.Count > 0)
            extra.Add(ExtraAction("Stop this routine", async () =>
            {
                if (!await PaperSheet.AlertAsync(this, P, "Stop repeating?", "Future unconfirmed copies will be removed. Your history and confirmed commitments stay.", "Stop routine", "Cancel")) return;
                await Session.ChangeAsync(s => TaskEngine.ArchiveRoutine(s, definition.Id, Session.Clock.UtcNow)); Render(); await Session.SyncRemindersAsync();
            }));
        extra.Add(Ui.Rule(P.Rule)); extra.Add(Ui.Text("TASK HISTORY", 11, P.Muted, true));
        foreach (var entry in Session.State.Events.Where(e => e.OccurrenceId == id).OrderByDescending(e => e.At).Take(30))
        {
            var at = TimeZoneInfo.ConvertTime(entry.At, PlanningTime.Zone(Session.State.Settings.TimeZoneId));
            extra.Add(Ui.Text($"{at:dd MMM HH:mm} · {entry.Kind}\n{entry.Details}", 12, P.Text));
        }
        Form.Add(extra);
    }
}

public sealed class MoveTaskPage : FormPage
{
    public MoveTaskPage(AppSession session, Guid id) : base(session, "Move task")
    {
        var task = TaskEngine.Find(session.State, id);
        Form.Add(Ui.Heading("Change\nthe moment.", P.Text));
        Form.Add(Ui.Text(task.Title, 18, P.Text, true));
        Form.Add(Ui.Text("Moving across days preserves the original attempt in your history.", 13, P.Muted));
        var date = new DatePicker { Date = (task.Date < session.Today ? session.Today : task.Date).ToDateTime(TimeOnly.MinValue), MinimumDate = session.Today.ToDateTime(TimeOnly.MinValue), TextColor = P.Text };
        var timed = new Switch { IsToggled = task.Time.HasValue };
        var time = new TimePicker { Time = task.Time?.ToTimeSpan() ?? new TimeSpan(17, 0, 0), TextColor = P.Text, Format = "HH:mm" };
        Action("Tomorrow", () => { date.Date = session.Today.AddDays(1).ToDateTime(TimeOnly.MinValue); return Task.CompletedTask; });
        Form.Add(Ui.Field("DAY", date, P)); Form.Add(Toggle("At a specific time", timed));
        var timeField = Ui.Field("TIME", time, P); timeField.IsVisible = timed.IsToggled;
        timed.Toggled += (_, _) => timeField.IsVisible = timed.IsToggled; Form.Add(timeField);
        FooterAction("MOVE TASK", async () =>
        {
            await Session.ChangeAsync(s => TaskEngine.Move(s, id, DateOnly.FromDateTime(date.Date ?? DateTime.Today), timed.IsToggled ? TimeOnly.FromTimeSpan(time.Time ?? TimeSpan.Zero) : null, Session.Clock.UtcNow));
            await Navigation.PopAsync(false); await Session.SyncRemindersAsync();
        });
    }
}

public sealed class CorrectCompletionPage : FormPage
{
    public CorrectCompletionPage(AppSession session, Guid id) : base(session, "Correct completion")
    {
        var task = TaskEngine.Find(session.State, id);
        Form.Add(Ui.Heading("Keep the\nrecord honest.", P.Text));
        Form.Add(Ui.Text($"When did you finish “{task.Title}” on {task.Date:d MMM}? This correction stays in history.", 16, P.Text));
        var time = new TimePicker { Time = new TimeSpan(12, 0, 0), Format = "HH:mm", TextColor = P.Text };
        Form.Add(Ui.Field("COMPLETED AT", time, P));
        FooterAction("RECORD COMPLETION", async () =>
        {
            var zone = Session.State.Days.First(d => d.Date == task.Date).TimeZoneId;
            var at = PlanningTime.Resolve(task.Date, TimeOnly.FromTimeSpan(time.Time ?? TimeSpan.Zero), zone);
            await Session.ChangeAsync(s => TaskEngine.CorrectCompletion(s, id, at, Session.Clock.UtcNow));
            await Navigation.PopAsync(false); await Session.SyncRemindersAsync();
        });
    }
}
