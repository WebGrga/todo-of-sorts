namespace ToDoOfSorts.Core;

public sealed record ReminderPlan(string Id, Guid? TaskId, DateTimeOffset At, string Title, string Body);

public static class ReminderPlanner
{
    public const int MaximumPending = 60;
    public static IReadOnlyList<ReminderPlan> Build(AppState state, DateTimeOffset now)
    {
        var plans = new List<ReminderPlan>();
        foreach (var task in state.Tasks.Where(t => (t.IsActive || t.Status == Outcome.Unresolved) && t.Reminder && t.Time.HasValue))
        {
            var zone = state.Days.FirstOrDefault(d => d.Date == task.Date)?.TimeZoneId ?? state.Settings.TimeZoneId;
            var at = PlanningTime.Resolve(task.Date, task.Time!.Value, zone);
            at = ApplyQuietHours(at, state.Settings, zone);
            if (at <= now) continue;
            plans.Add(new($"task-{task.Id:N}", task.Id, at, task.Title, "Ready to make your mark? Start, move, or skip intentionally."));
        }
        if (state.Settings.EveningReview)
        {
            var today = PlanningTime.Today(state, now);
            for (var i = 0; i < 7; i++)
            {
                var date = today.AddDays(i);
                if (state.Days.Any(d => d.Date == date && d.Rest)) continue;
                var at = ApplyQuietHours(PlanningTime.Resolve(date, state.Settings.ReviewTime, state.Settings.TimeZoneId), state.Settings, state.Settings.TimeZoneId);
                if (at > now) plans.Add(new($"review-{date:yyyyMMdd}", null, at, "How did today go?", "Your finished work deserves a look. Review your day."));
            }
        }
        return plans.OrderBy(p => p.At).Take(MaximumPending).ToArray();
    }

    public static DateTimeOffset ApplyQuietHours(DateTimeOffset at, AppSettings settings, string zone)
    {
        if (!settings.QuietHours || settings.QuietStart == settings.QuietEnd) return at;
        var local = TimeZoneInfo.ConvertTime(at, PlanningTime.Zone(zone));
        var time = TimeOnly.FromDateTime(local.DateTime);
        var overnight = settings.QuietStart > settings.QuietEnd;
        var inside = overnight ? time >= settings.QuietStart || time < settings.QuietEnd
            : time >= settings.QuietStart && time < settings.QuietEnd;
        if (!inside) return at;
        var date = DateOnly.FromDateTime(local.DateTime);
        if (overnight && time >= settings.QuietStart) date = date.AddDays(1);
        return PlanningTime.Resolve(date, settings.QuietEnd, zone);
    }
}
