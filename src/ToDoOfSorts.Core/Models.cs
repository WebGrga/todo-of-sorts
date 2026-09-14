using System.Text.Json;
using System.Text.Json.Serialization;

namespace ToDoOfSorts.Core;

public enum Outcome { Planned, InProgress, Completed, Skipped, Rescheduled, Unresolved }
public enum Experience { Energetic, Tactile, Calm }

public sealed class AppSettings
{
    public Experience Theme { get; set; } = Experience.Energetic;
    public bool Sound { get; set; }
    public bool Haptics { get; set; } = true;
    public bool ReduceMotion { get; set; }
    public string TimeZoneId { get; set; } = TimeZoneInfo.Local.Id;
    public bool QuietHours { get; set; } = true;
    public TimeOnly QuietStart { get; set; } = new(22, 0);
    public TimeOnly QuietEnd { get; set; } = new(8, 0);
    public bool EveningReview { get; set; }
    public TimeOnly ReviewTime { get; set; } = new(20, 30);
    public List<string> Categories { get; set; } = ["Training", "Career", "Learning", "Home", "Personal", "Uncategorized"];
}

public sealed class TaskDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string Category { get; set; } = "Uncategorized";
    public int Xp { get; set; } = 20;
    public bool Commitment { get; set; } = true;
    public TimeOnly? Time { get; set; }
    public bool Reminder { get; set; }
    public DateOnly StartDate { get; set; }
    public List<DayOfWeek> RepeatDays { get; set; } = [];
    public bool Archived { get; set; }
}

public sealed class TaskOccurrence
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DefinitionId { get; set; }
    public Guid WorkId { get; set; } = Guid.NewGuid();
    public Guid? MovedFromId { get; set; }
    public Guid? MovedToId { get; set; }
    public DateOnly Date { get; set; }
    public string Title { get; set; } = "";
    public string Category { get; set; } = "Uncategorized";
    public int Xp { get; set; }
    public bool Commitment { get; set; }
    public bool CountedAttempt { get; set; }
    public bool IsRecurringSlot { get; set; }
    public TimeOnly? OriginalTime { get; set; }
    public TimeOnly? Time { get; set; }
    public bool Reminder { get; set; }
    public Outcome Status { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public double StampAngle { get; set; } = -8;
    public string? SkipReason { get; set; }
    [JsonIgnore] public bool IsActive => Status is Outcome.Planned or Outcome.InProgress;
}

public sealed class DayPlan
{
    public DateOnly Date { get; set; }
    public string TimeZoneId { get; set; } = "UTC";
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public bool Rest { get; set; }
    public bool Celebrated { get; set; }
    public List<Guid> Commitments { get; set; } = [];
}

public sealed record TaskEvent(Guid Id, Guid? OccurrenceId, DateTimeOffset At, string Kind, string Details);
public sealed record RewardEntry(Guid Id, Guid OccurrenceId, DateTimeOffset At, int Points);

public sealed class AppState
{
    public int SchemaVersion { get; set; } = 1;
    public long Revision { get; set; }
    public long ScheduledRevision { get; set; } = -1;
    public DateOnly? LastGenerated { get; set; }
    public AppSettings Settings { get; set; } = new();
    public List<TaskDefinition> Definitions { get; set; } = [];
    public List<TaskOccurrence> Tasks { get; set; } = [];
    public List<DayPlan> Days { get; set; } = [];
    public List<TaskEvent> Events { get; set; } = [];
    public List<RewardEntry> Rewards { get; set; } = [];
}

public sealed record TaskDraft(string Title, string Category, int Xp, DateOnly Date,
    TimeOnly? Time, bool Reminder, bool Commitment, IReadOnlyList<DayOfWeek> RepeatDays);

public interface IClock { DateTimeOffset UtcNow { get; } }
public sealed class SystemClock : IClock { public DateTimeOffset UtcNow => DateTimeOffset.UtcNow; }

public static class StateJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };
    public static string Serialize(AppState state) => JsonSerializer.Serialize(state, Options);
    private static readonly JsonSerializerOptions CompactOptions = new(Options) { WriteIndented = false };
    public static string SerializeCompact(AppState state) => JsonSerializer.Serialize(state, CompactOptions);
    public static AppState Deserialize(string json) => JsonSerializer.Deserialize<AppState>(json, Options)
        ?? throw new InvalidDataException("This backup is empty.");
    public static AppState Clone(AppState state) => Deserialize(Serialize(state));
    public static AppSettings CloneSettings(AppSettings settings) => JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(settings, Options), Options)!;
}

public static class PlanningTime
{
    public static TimeZoneInfo Zone(string id) => TimeZoneInfo.FindSystemTimeZoneById(id);
    public static DateOnly Today(AppState s, DateTimeOffset now) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, Zone(s.Settings.TimeZoneId)).DateTime);
    public static DateTimeOffset Resolve(DateOnly date, TimeOnly time, string zoneId)
    {
        var zone = Zone(zoneId);
        var local = DateTime.SpecifyKind(date.ToDateTime(time), DateTimeKind.Unspecified);
        while (zone.IsInvalidTime(local)) local = local.AddMinutes(1);
        var offset = zone.IsAmbiguousTime(local) ? zone.GetAmbiguousTimeOffsets(local).Max() : zone.GetUtcOffset(local);
        return new DateTimeOffset(local, offset).ToUniversalTime();
    }
    public static DayPlan Day(AppState state, DateOnly date)
    {
        var day = state.Days.FirstOrDefault(d => d.Date == date);
        if (day is not null) return day;
        day = new DayPlan { Date = date, TimeZoneId = state.Settings.TimeZoneId,
            StartsAt = Resolve(date, TimeOnly.MinValue, state.Settings.TimeZoneId),
            EndsAt = Resolve(date.AddDays(1), TimeOnly.MinValue, state.Settings.TimeZoneId) };
        state.Days.Add(day);
        return day;
    }
}
