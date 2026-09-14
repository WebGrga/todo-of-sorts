using System.Diagnostics;
using ToDoOfSorts.Core;
using ToDoOfSorts.Infrastructure;

// A repeatable, realistic history probe. Timings are reported, not asserted against
// a machine-dependent threshold. Run Release on the same machine for comparisons.
var now = new DateTimeOffset(2026, 9, 13, 12, 0, 0, TimeSpan.Zero);
var today = DateOnly.FromDateTime(now.Date);
var state = new AppState { LastGenerated = today };
state.Settings.TimeZoneId = "UTC";
for (var dayIndex = 0; dayIndex < 365; dayIndex++)
{
    var day = PlanningTime.Day(state, today.AddDays(-dayIndex)); day.ConfirmedAt = day.StartsAt;
    for (var i = 0; i < 8; i++)
    {
        var definition = new TaskDefinition { Title = "History task " + i, StartDate = day.Date };
        state.Definitions.Add(definition);
        var completed = i < 6 || dayIndex % 3 == 0;
        var task = new TaskOccurrence { DefinitionId = definition.Id, Date = day.Date, Title = definition.Title,
            Xp = 20, CountedAttempt = true, Commitment = true, Status = completed ? Outcome.Completed : Outcome.Skipped,
            CompletedAt = completed ? day.StartsAt.AddHours(10) : null };
        state.Tasks.Add(task); day.Commitments.Add(task.Id);
        if (completed) state.Rewards.Add(new(Guid.NewGuid(), task.Id, day.StartsAt.AddHours(10), 20));
        state.Events.Add(new(Guid.NewGuid(), task.Id, day.StartsAt.AddHours(10), completed ? "Completed" : "Skipped", task.Title));
    }
}
Console.WriteLine($"History: {state.Tasks.Count} tasks / {state.Days.Count} days / {state.Rewards.Count} rewards");
void Measure(string name, Action action)
{
    action(); var samples = new List<double>();
    for (var i = 0; i < 3; i++) { var timer = Stopwatch.StartNew(); action(); samples.Add(timer.Elapsed.TotalMilliseconds); }
    Console.WriteLine($"{name}: {samples.Order().ElementAt(1):F2} ms median");
}
Measure("Run statistics", () => Statistics.Runs(state, today));
Measure("Backup validation", () => Backup.Validate(state));
Measure("CSV export", () => Backup.Csv(state));
Measure("Day reconciliation", () => TaskEngine.Reconcile(state, now));
var directory = Path.Combine(Path.GetTempPath(), "todoofsorts-perf-" + Guid.NewGuid().ToString("N"));
var store = new LocalStore(Path.Combine(directory, "state.db3"), new ProbeClock(now));
await store.ImportAsync(StateJson.Serialize(state));
var samples = new List<double>();
for (var i = 0; i < 4; i++) { var timer = Stopwatch.StartNew(); await store.ReadAsync(); if (i > 0) samples.Add(timer.Elapsed.TotalMilliseconds); }
Console.WriteLine($"Reconciled database read: {samples.Order().ElementAt(1):F2} ms median");
sealed class ProbeClock(DateTimeOffset now) : IClock { public DateTimeOffset UtcNow => now; }
