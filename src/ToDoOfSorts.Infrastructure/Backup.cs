using System.Text;
using ToDoOfSorts.Core;

namespace ToDoOfSorts.Infrastructure;

public static class Backup
{
    public const int MaxBytes = 20 * 1024 * 1024;
    public static AppState Parse(string json)
    {
        if (Encoding.UTF8.GetByteCount(json) > MaxBytes) throw new InvalidDataException("Maximum backup size is 20 MB.");
        AppState state;
        try { state = StateJson.Deserialize(json); }
        catch (System.Text.Json.JsonException ex) { throw new InvalidDataException("This file isn't a valid ToDoOfSorts backup.", ex); }
        Validate(state);
        return state;
    }
    public static void Validate(AppState s)
    {
        if (s.SchemaVersion != 1) throw new InvalidDataException("Unsupported backup version.");
        if (s.Settings is null || s.Definitions is null || s.Tasks is null || s.Days is null || s.Rewards is null || s.Events is null)
            throw new InvalidDataException("The backup is missing required data.");
        if (s.Definitions.Any(d => d is null) || s.Tasks.Any(t => t is null) || s.Days.Any(d => d is null) || s.Rewards.Any(r => r is null) || s.Events.Any(e => e is null))
            throw new InvalidDataException("The backup contains an empty record.");
        _ = PlanningTime.Zone(s.Settings.TimeZoneId);
        if (!Enum.IsDefined(s.Settings.Theme) || s.Settings.Categories is null || s.Settings.Categories.Any(c => string.IsNullOrWhiteSpace(c) || c.Length > 40))
            throw new InvalidDataException("Invalid settings.");
        if (s.Tasks.Count > 100_000 || s.Days.Count > 20_000 || s.Events.Count > 500_000)
            throw new InvalidDataException("This backup exceeds the supported history size.");
        if (s.Tasks.Select(t => t.Id).Distinct().Count() != s.Tasks.Count ||
            s.Definitions.Select(d => d.Id).Distinct().Count() != s.Definitions.Count ||
            s.Days.Select(d => d.Date).Distinct().Count() != s.Days.Count ||
            s.Events.Select(e => e.Id).Distinct().Count() != s.Events.Count ||
            s.Rewards.Select(r => r.Id).Distinct().Count() != s.Rewards.Count)
            throw new InvalidDataException("Duplicate records in backup.");
        var definitions = s.Definitions.Select(d => d.Id).ToHashSet();
        var tasks = s.Tasks.ToDictionary(t => t.Id);
        var days = s.Days.ToDictionary(d => d.Date);
        foreach (var t in s.Tasks)
        {
            if (!definitions.Contains(t.DefinitionId) || !days.ContainsKey(t.Date) || string.IsNullOrWhiteSpace(t.Title) || t.Title.Length > 180 ||
                string.IsNullOrWhiteSpace(t.Category) || t.Category.Length > 40 || t.Xp is not (10 or 20 or 40) || !Enum.IsDefined(t.Status) ||
                !double.IsFinite(t.StampAngle) || Math.Abs(t.StampAngle) > 20 ||
                (t.Status == Outcome.Completed) != t.CompletedAt.HasValue)
                throw new InvalidDataException("Invalid task in backup.");
            if (t.MovedToId is { } to && (!tasks.TryGetValue(to, out var target) || target.MovedFromId != t.Id || target.WorkId != t.WorkId))
                throw new InvalidDataException("Invalid reschedule link.");
            if (t.MovedFromId is { } from && (!tasks.TryGetValue(from, out var source) || source.MovedToId != t.Id || source.WorkId != t.WorkId))
                throw new InvalidDataException("Invalid reschedule link.");
            if (t.CompletedAt is { } completed && (completed < days[t.Date].StartsAt || completed >= days[t.Date].EndsAt))
                throw new InvalidDataException("A completion is outside its task's day.");
        }
        foreach (var d in s.Definitions)
            if (d.RepeatDays is null || d.RepeatDays.Any(x => !Enum.IsDefined(x)) || string.IsNullOrWhiteSpace(d.Title) || d.Title.Length > 180 ||
                string.IsNullOrWhiteSpace(d.Category) || d.Category.Length > 40 || d.Xp is not (10 or 20 or 40))
                throw new InvalidDataException("Invalid routine.");
        foreach (var d in s.Days)
        {
            _ = PlanningTime.Zone(d.TimeZoneId);
            if (d.EndsAt <= d.StartsAt || d.Commitments is null || d.Commitments.Distinct().Count() != d.Commitments.Count ||
                d.Commitments.Any(id => !tasks.TryGetValue(id, out var t) || t.Date != d.Date))
                throw new InvalidDataException("Invalid day plan.");
        }
        if (s.Rewards.Any(r => !tasks.ContainsKey(r.OccurrenceId))) throw new InvalidDataException("Reward refers to an unknown task.");
        var points = s.Rewards.GroupBy(r => r.OccurrenceId).ToDictionary(g => g.Key, g => g.Sum(r => r.Points));
        foreach (var t in s.Tasks)
            if (points.GetValueOrDefault(t.Id) != (t.Status == Outcome.Completed ? t.Xp : 0))
                throw new InvalidDataException("Task rewards do not match recorded completions.");
    }
    public static string Csv(AppState s)
    {
        static string Cell(string value)
        {
            if (value.Length > 0 && "=+-@\t\r".Contains(value[0])) value = "'" + value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        var result = new StringBuilder("Date,Task,Category,Role,Outcome,XP,Original time,Current time,Completed at,Skip reason\r\n");
        var points = s.Rewards.GroupBy(r => r.OccurrenceId).ToDictionary(g => g.Key, g => g.Sum(r => r.Points));
        foreach (var t in s.Tasks.OrderBy(t => t.Date).ThenBy(t => t.Time))
            result.AppendLine(string.Join(",", new[] { t.Date.ToString("yyyy-MM-dd"), t.Title, t.Category,
                t.Commitment ? "Commitment" : "Bonus", t.Status.ToString(),
                points.GetValueOrDefault(t.Id).ToString(),
                t.OriginalTime?.ToString("HH:mm") ?? "", t.Time?.ToString("HH:mm") ?? "", t.CompletedAt?.ToString("O") ?? "", t.SkipReason ?? "" }.Select(Cell)));
        return result.ToString();
    }
}
