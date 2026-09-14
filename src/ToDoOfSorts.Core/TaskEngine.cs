namespace ToDoOfSorts.Core;

public static class TaskEngine
{
    public static void Reconcile(AppState s, DateTimeOffset now)
    {
        var today = PlanningTime.Today(s, now);
        PlanningTime.Day(s, today);
        var from = s.LastGenerated is { } previous && previous < today ? previous : today;
        foreach (var definition in s.Definitions.Where(d => !d.Archived && d.RepeatDays.Count > 0))
        {
            var start = from > definition.StartDate ? from : definition.StartDate;
            for (var date = start; date <= today.AddDays(14); date = date.AddDays(1))
            {
                if (!definition.RepeatDays.Contains(date.DayOfWeek) ||
                    s.Tasks.Any(t => t.DefinitionId == definition.Id && t.Date == date && t.IsRecurringSlot)) continue;
                s.Tasks.Add(CreateOccurrence(s, definition, date, true));
            }
        }
        s.LastGenerated = today;
        foreach (var task in s.Tasks.Where(t => t.IsActive && PlanningTime.Day(s, t.Date).EndsAt <= now).ToArray())
        {
            task.Status = Outcome.Unresolved;
            Event(s, task.Id, now, "Unresolved", "Day ended without a recorded outcome.");
        }
    }

    public static TaskOccurrence Add(AppState s, TaskDraft draft, DateTimeOffset now)
    {
        ValidateDraft(s, draft, now);
        var d = new TaskDefinition { Title = draft.Title.Trim(), Category = draft.Category.Trim(), Xp = draft.Xp,
            StartDate = draft.Date, Time = draft.Time, Reminder = draft.Reminder && draft.Time.HasValue,
            Commitment = draft.Commitment, RepeatDays = draft.RepeatDays.Distinct().ToList() };
        s.Definitions.Add(d);
        var task = CreateOccurrence(s, d, draft.Date, d.RepeatDays.Count > 0);
        s.Tasks.Add(task);
        Event(s, task.Id, now, "Added", task.Title);
        Reconcile(s, now);
        return task;
    }

    private static TaskOccurrence CreateOccurrence(AppState s, TaskDefinition d, DateOnly date, bool recurring)
    {
        var day = PlanningTime.Day(s, date);
        return new TaskOccurrence { DefinitionId = d.Id, Date = date, Title = d.Title, Category = d.Category,
            Xp = d.Xp, Time = d.Time, OriginalTime = d.Time, Reminder = d.Reminder,
            Commitment = d.Commitment && day.ConfirmedAt is null && !day.Rest,
            CountedAttempt = day.ConfirmedAt.HasValue, IsRecurringSlot = recurring,
            StampAngle = -6 - (s.Tasks.Count % 5) };
    }

    public static void Edit(AppState s, Guid id, TaskDraft draft, DateTimeOffset now, bool updateRoutine, bool updateRepeat = false)
    {
        ValidateDraft(s, draft, now);
        var t = Find(s, id);
        if (!t.IsActive) throw new InvalidOperationException("Reopen this task before editing it.");
        if (t.Date != draft.Date) throw new InvalidOperationException("Use Move to change the task's day.");
        var day = PlanningTime.Day(s, t.Date);
        var before = $"{t.Title}; {t.Category}; {t.Xp} XP; {t.Time}";
        t.Title = draft.Title.Trim(); t.Category = draft.Category.Trim(); t.Xp = draft.Xp;
        if (day.ConfirmedAt is null) { t.Commitment = draft.Commitment && !day.Rest; t.OriginalTime = draft.Time; }
        t.Time = draft.Time; t.Reminder = draft.Reminder && draft.Time.HasValue;
        Event(s, id, now, "Edited", $"{before} → {t.Title}; {t.Category}; {t.Xp} XP; {t.Time}");
        if (!updateRoutine && !updateRepeat) return;
        var definition = s.Definitions.Single(d => d.Id == t.DefinitionId);
        if (updateRoutine || definition.RepeatDays.Count == 0 || definition.Archived)
        {
            definition.Title = t.Title; definition.Category = t.Category; definition.Xp = t.Xp;
            definition.Time = t.Time; definition.Reminder = t.Reminder; definition.Commitment = draft.Commitment;
        }
        definition.RepeatDays = draft.RepeatDays.Distinct().ToList(); definition.Archived = false;
        t.IsRecurringSlot = definition.RepeatDays.Count > 0;
        foreach (var future in s.Tasks.Where(x => x.DefinitionId == definition.Id && x.Date > t.Date &&
            x.IsRecurringSlot && !x.CountedAttempt && x.Status == Outcome.Planned).ToArray()) s.Tasks.Remove(future);
        Reconcile(s, now);
        Event(s, id, now, "RoutineEdited", "Future repeats updated; historical tasks retained.");
    }

    public static void ConfirmDay(AppState s, DateOnly date, DateTimeOffset now)
    {
        if (date < PlanningTime.Today(s, now)) throw new InvalidOperationException("Past plans cannot be confirmed retroactively.");
        var day = PlanningTime.Day(s, date);
        if (day.ConfirmedAt is not null) return;
        if (day.Rest) throw new InvalidOperationException("Turn off rest day before setting commitments.");
        var tasks = s.Tasks.Where(t => t.Date == date && t.IsActive).ToArray();
        if (!tasks.Any(t => t.Commitment)) throw new InvalidOperationException("Choose at least one commitment for your day.");
        day.ConfirmedAt = now;
        day.Commitments = tasks.Where(t => t.Commitment).Select(t => t.Id).ToList();
        foreach (var task in tasks) { task.CountedAttempt = true; task.OriginalTime = task.Time; }
        Event(s, null, now, "DayConfirmed", $"{date:yyyy-MM-dd}: {day.Commitments.Count} commitments");
    }

    public static void AmendCommitment(AppState s, Guid id, bool included, DateTimeOffset now)
    {
        var task = Find(s, id);
        var day = PlanningTime.Day(s, task.Date);
        if (day.Rest && included) throw new InvalidOperationException("A rest day has no commitments.");
        task.Commitment = included;
        day.Commitments.Remove(id);
        if (included && day.ConfirmedAt.HasValue) day.Commitments.Add(id);
        Event(s, id, now, "PlanCorrected", included ? "Added to commitments by explicit correction." : "Removed from commitments by explicit correction.");
    }

    public static void SetRest(AppState s, DateOnly date, bool rest, DateTimeOffset now)
    {
        var day = PlanningTime.Day(s, date);
        if (date < PlanningTime.Today(s, now) || day.ConfirmedAt.HasValue)
            throw new InvalidOperationException("Rest is chosen before setting the day's commitments.");
        day.Rest = rest;
        if (rest) foreach (var t in s.Tasks.Where(t => t.Date == date)) t.Commitment = false;
        Event(s, null, now, "RestDay", $"{date:yyyy-MM-dd}: {rest}");
    }

    public static bool Complete(AppState s, Guid id, DateTimeOffset now)
    {
        var task = Find(s, id);
        if (task.Status == Outcome.Completed) return false;
        if (!task.IsActive) throw new InvalidOperationException("Reopen or move this task before stamping it.");
        if (task.Date != PlanningTime.Today(s, now)) throw new InvalidOperationException("Move this task to today to complete it, or record a past completion in history.");
        var day = PlanningTime.Day(s, task.Date);
        if (!day.Rest && day.ConfirmedAt is null && s.Tasks.Any(t => t.Date == task.Date && t.Commitment && t.IsActive)) ConfirmDay(s, task.Date, now);
        task.CountedAttempt = true;
        task.Status = Outcome.Completed; task.CompletedAt = now;
        s.Rewards.Add(new(Guid.NewGuid(), id, now, task.Xp));
        Event(s, id, now, "Completed", $"Stamped done. +{task.Xp} XP");
        return true;
    }

    public static void Start(AppState s, Guid id, DateTimeOffset now)
    {
        var task = Find(s, id);
        if (!task.IsActive) return;
        if (task.Status == Outcome.InProgress) return;
        if (task.Date != PlanningTime.Today(s, now)) throw new InvalidOperationException("Move this task to today to start it.");
        var day = PlanningTime.Day(s, task.Date);
        if (!day.Rest && day.ConfirmedAt is null && s.Tasks.Any(t => t.Date == task.Date && t.Commitment && t.IsActive)) ConfirmDay(s, task.Date, now);
        task.CountedAttempt = true;
        task.Status = Outcome.InProgress;
        Event(s, id, now, "Started", "Started work; no XP awarded.");
    }

    public static void Reopen(AppState s, Guid id, DateTimeOffset now)
    {
        var task = Find(s, id);
        if (task.Status == Outcome.Rescheduled) throw new InvalidOperationException("This task has a linked move. Open the destination task instead.");
        if (task.IsActive) return;
        var points = s.Rewards.Where(r => r.OccurrenceId == id).Sum(r => r.Points);
        if (points != 0) s.Rewards.Add(new(Guid.NewGuid(), id, now, -points));
        task.Status = task.Date < PlanningTime.Today(s, now) ? Outcome.Unresolved : Outcome.Planned;
        task.CompletedAt = null; task.SkipReason = null;
        Event(s, id, now, "Reopened", "Outcome reversed; any awarded XP reversed.");
    }

    public static void Skip(AppState s, Guid id, string? reason, DateTimeOffset now)
    {
        var task = Find(s, id);
        if (task.Status == Outcome.Skipped) return;
        if (task.Status is Outcome.Completed or Outcome.Rescheduled) throw new InvalidOperationException("Reopen this task before skipping it.");
        task.Status = Outcome.Skipped; task.SkipReason = reason;
        Event(s, id, now, "Skipped", string.IsNullOrWhiteSpace(reason) ? "Intentionally skipped." : reason);
    }

    public static TaskOccurrence Move(AppState s, Guid id, DateOnly date, TimeOnly? time, DateTimeOffset now)
    {
        var task = Find(s, id);
        if (date < PlanningTime.Today(s, now)) throw new InvalidOperationException("Choose today or a future date.");
        if (task.Status is Outcome.Completed or Outcome.Rescheduled) throw new InvalidOperationException("Reopen a completed task before moving it.");
        var before = $"{task.Date:yyyy-MM-dd} {task.Time}";
        if (date == task.Date)
        {
            task.Time = time; task.Reminder &= time.HasValue; task.Status = Outcome.Planned; task.SkipReason = null;
            if (!task.CountedAttempt) task.OriginalTime = time;
            Event(s, id, now, "MovedWithinDay", $"{before} → {date:yyyy-MM-dd} {time}");
            return task;
        }
        var targetDay = PlanningTime.Day(s, date);
        var destination = new TaskOccurrence { DefinitionId = task.DefinitionId, WorkId = task.WorkId,
            MovedFromId = task.Id, Date = date, Title = task.Title, Category = task.Category, Xp = task.Xp,
            Commitment = task.Commitment && targetDay.ConfirmedAt is null && !targetDay.Rest,
            CountedAttempt = targetDay.ConfirmedAt.HasValue, Time = time, OriginalTime = time,
            Reminder = task.Reminder && time.HasValue, StampAngle = task.StampAngle };
        task.Status = Outcome.Rescheduled; task.MovedToId = destination.Id; task.SkipReason = null;
        s.Tasks.Add(destination);
        Event(s, id, now, "Rescheduled", $"{before} → {date:yyyy-MM-dd} {time}; destination {destination.Id}");
        return destination;
    }

    public static void CorrectCompletion(AppState s, Guid id, DateTimeOffset completedAt, DateTimeOffset now)
    {
        var task = Find(s, id);
        var day = PlanningTime.Day(s, task.Date);
        if (completedAt < day.StartsAt || completedAt >= day.EndsAt || completedAt > now)
            throw new InvalidOperationException("Choose the actual completion time within this task's day, in the past.");
        if (task.Status == Outcome.Rescheduled) throw new InvalidOperationException("Correct the moved task at its destination instead.");
        if (task.Status != Outcome.Completed) s.Rewards.Add(new(Guid.NewGuid(), id, now, task.Xp));
        task.Status = Outcome.Completed; task.CompletedAt = completedAt; task.CountedAttempt = true; task.SkipReason = null;
        Event(s, id, now, "CompletionCorrected", $"Explicit correction: completed at {completedAt:O}");
    }

    public static void ArchiveRoutine(AppState s, Guid definitionId, DateTimeOffset now)
    {
        var d = s.Definitions.Single(x => x.Id == definitionId); d.Archived = true;
        var today = PlanningTime.Today(s, now);
        s.Tasks.RemoveAll(t => t.DefinitionId == definitionId && t.Date > today && t.IsRecurringSlot &&
            t.MovedFromId is null && t.MovedToId is null && !t.CountedAttempt && t.Status == Outcome.Planned);
        Event(s, null, now, "RoutineArchived", d.Title);
    }

    public static TaskOccurrence Find(AppState s, Guid id) => s.Tasks.FirstOrDefault(t => t.Id == id)
        ?? throw new InvalidOperationException("This task no longer exists.");

    public static void Event(AppState s, Guid? id, DateTimeOffset now, string kind, string details) =>
        s.Events.Add(new(Guid.NewGuid(), id, now, kind, details));

    private static void ValidateDraft(AppState s, TaskDraft draft, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(draft.Title) || draft.Title.Trim().Length > 180)
            throw new InvalidOperationException("Give the task a name between 1 and 180 characters.");
        if (draft.Xp is not (10 or 20 or 40)) throw new InvalidOperationException("Choose Quick, Standard, or Deep effort.");
        if (string.IsNullOrWhiteSpace(draft.Category) || draft.Category.Length > 40) throw new InvalidOperationException("Choose a category.");
        if (draft.Date < PlanningTime.Today(s, now)) throw new InvalidOperationException("New plans start today or later.");
        if (draft.RepeatDays.Any(d => !Enum.IsDefined(d))) throw new InvalidOperationException("Invalid repeat day.");
    }
}
