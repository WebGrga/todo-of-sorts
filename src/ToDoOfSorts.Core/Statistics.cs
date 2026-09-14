namespace ToDoOfSorts.Core;

public sealed record DaySummary(DateOnly Date, int Commitments, int StampedCommitments, int Planned,
    int Completed, int Skipped, int Moved, int Unresolved, int Xp, bool Won, bool Rest, bool Confirmed)
{
    public double Progress => Commitments == 0 ? 0 : (double)StampedCommitments / Commitments;
    public string Label => Rest ? "Rest" : Won ? "Won" : !Confirmed ? "Unplanned" : "Partial";
}
public sealed record CategorySummary(string Name, int Completed, int Planned)
{
    public double Rate => Planned == 0 ? 0 : (double)Completed / Planned;
}
public sealed record RunSummary(int Current, int Best, int WonIn30Days);

public static class Statistics
{
    public static DaySummary Day(AppState s, DateOnly date)
    {
        var plan = s.Days.FirstOrDefault(d => d.Date == date);
        var tasks = s.Tasks.Where(t => t.Date == date).ToArray();
        var confirmedIds = plan?.Commitments.ToHashSet();
        var commitments = plan?.ConfirmedAt.HasValue == true
            ? tasks.Where(t => confirmedIds!.Contains(t.Id)).ToArray()
            : tasks.Where(t => t.Commitment).ToArray();
        var attempts = tasks.Where(t => t.CountedAttempt).ToArray();
        bool CompletedOnDay(TaskOccurrence t) => t.Status == Outcome.Completed && t.CompletedAt is { } at &&
            plan is not null && at >= plan.StartsAt && at < plan.EndsAt;
        var stamped = commitments.Count(CompletedOnDay);
        var taskIds = tasks.Select(t => t.Id).ToHashSet();
        return new(date, commitments.Length, stamped, attempts.Length, attempts.Count(CompletedOnDay),
            attempts.Count(t => t.Status == Outcome.Skipped), attempts.Count(t => t.Status == Outcome.Rescheduled),
            attempts.Count(t => t.Status == Outcome.Unresolved), s.Rewards.Where(r => taskIds.Contains(r.OccurrenceId)).Sum(r => r.Points),
            plan?.ConfirmedAt.HasValue == true && commitments.Length > 0 && stamped == commitments.Length,
            plan?.Rest == true, plan?.ConfirmedAt.HasValue == true);
    }

    public static RunSummary Runs(AppState s, DateOnly today)
    {
        var tasks = s.Tasks.ToDictionary(t => t.Id);
        var current = 0; var best = 0; var won30 = 0;
        DateOnly? previous = null;
        foreach (var day in s.Days.Where(d => d.Date <= today).OrderBy(d => d.Date))
        {
            if (previous is { } last && day.Date.DayNumber - last.DayNumber > 1) current = 0;
            var won = day.ConfirmedAt.HasValue && day.Commitments.Count > 0 && day.Commitments.All(id =>
                tasks.TryGetValue(id, out var task) && task.Date == day.Date && task.Status == Outcome.Completed &&
                task.CompletedAt >= day.StartsAt && task.CompletedAt < day.EndsAt);
            if (won) { current++; best = Math.Max(best, current); if (day.Date.DayNumber >= today.DayNumber - 29) won30++; }
            else if (!day.Rest && day.Date < today) current = 0;
            previous = day.Date;
        }
        if (previous is { } end && today.DayNumber - end.DayNumber > 1) current = 0;
        return new(current, best, won30);
    }

    public static IReadOnlyList<CategorySummary> Categories(AppState s, DateOnly from, DateOnly through) =>
        s.Tasks.Where(t => t.CountedAttempt && t.Date >= from && t.Date <= through)
            .GroupBy(t => t.Category).Select(g => new CategorySummary(g.Key,
                g.Count(t => t.Status == Outcome.Completed), g.Count())).OrderByDescending(c => c.Rate).ToArray();
}
