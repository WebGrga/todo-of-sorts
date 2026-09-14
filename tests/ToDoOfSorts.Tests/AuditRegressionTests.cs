using ToDoOfSorts.Core;
using ToDoOfSorts.Infrastructure;
using Xunit;

namespace ToDoOfSorts.Tests;

public sealed class AuditRegressionTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 11, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 9, 11);

    [Fact] public void BonusOnlyDayCanStartAndCompleteWithoutInventingAWin()
    {
        var state = DomainTests.Empty(); var task = DomainTests.Add(state, commitment: false);
        TaskEngine.Start(state, task.Id, Now);
        TaskEngine.Start(state, task.Id, Now);
        Assert.Single(state.Events, e => e.Kind == "Started");
        Assert.Equal(1, Statistics.Day(state, Today).Planned);
        Assert.True(TaskEngine.Complete(state, task.Id, Now));
        Assert.Equal(20, Statistics.Day(state, Today).Xp);
        Assert.False(Statistics.Day(state, Today).Won);
        Assert.False(Statistics.Day(state, Today).Confirmed); Backup.Validate(state);
    }

    [Fact] public void ChangingOneOffToDailyDoesNotDuplicateTheEditedTask()
    {
        var state = DomainTests.Empty(); var task = DomainTests.Add(state);
        TaskEngine.Edit(state, task.Id, new("Daily gym", "Training", 20, Today, null, false, true, Enum.GetValues<DayOfWeek>()), Now, false, true);
        Assert.Single(state.Tasks, t => t.Date == Today);
        Assert.Contains(state.Tasks, t => t.Date == Today.AddDays(1) && t.Title == "Daily gym");
        TaskEngine.Reconcile(state, Now);
        Assert.Equal(state.Tasks.Count, state.Tasks.Select(t => (t.DefinitionId, t.Date)).Distinct().Count()); Backup.Validate(state);
    }

    [Fact] public void RepeatOnlyEditPreservesRoutineDetailsAndConfirmedFutureTasks()
    {
        var state = DomainTests.Empty(); var task = DomainTests.Add(state, repeat: Enum.GetValues<DayOfWeek>());
        TaskEngine.ConfirmDay(state, Today.AddDays(1), Now);
        var confirmed = state.Tasks.Single(t => t.Date == Today.AddDays(1));
        TaskEngine.Edit(state, task.Id, new("Only today's title", "Home", 10, Today, null, false, true, [DayOfWeek.Friday]), Now, false, true);
        Assert.Equal("Gym", state.Definitions.Single().Title);
        Assert.Contains(state.Tasks, t => t.Id == confirmed.Id);
        Assert.DoesNotContain(state.Tasks, t => t.Date == Today.AddDays(2));
        Assert.Contains(state.Tasks, t => t.Date == Today.AddDays(7) && t.Title == "Gym"); Backup.Validate(state);
    }

    [Fact] public void CorrectingSkippedCompletionClearsOldSkipReason()
    {
        var state = DomainTests.Empty(); var task = DomainTests.Add(state);
        TaskEngine.Skip(state, task.Id, "No time", Now);
        TaskEngine.CorrectCompletion(state, task.Id, Now, Now);
        Assert.Null(task.SkipReason); Assert.Equal(20, Statistics.Day(state, Today).Xp); Backup.Validate(state);
    }

    [Fact] public void MovingSkippedTaskWithinDayClearsOldSkipReason()
    {
        var state = DomainTests.Empty(); var task = DomainTests.Add(state);
        TaskEngine.Skip(state, task.Id, "No time", Now);
        TaskEngine.Move(state, task.Id, Today, new(18, 0), Now);
        Assert.Null(task.SkipReason); Assert.Equal(Outcome.Planned, task.Status); Backup.Validate(state);
    }

    [Fact] public void RunProjectionHandlesMissingDaysAndRestWithoutScanningCalendarGaps()
    {
        var state = DomainTests.Empty(); var task = DomainTests.Add(state); TaskEngine.Complete(state, task.Id, Now);
        TaskEngine.SetRest(state, Today.AddDays(1), true, Now);
        Assert.Equal(1, Statistics.Runs(state, Today.AddDays(2)).Current);
        Assert.Equal(0, Statistics.Runs(state, Today.AddDays(3)).Current);
        Assert.Equal(1, Statistics.Runs(state, Today.AddYears(5)).Best);
        Assert.Equal(0, Statistics.Runs(state, Today.AddYears(5)).WonIn30Days);
    }

    [Fact] public void BackupRejectsMissingDayAndBrokenReverseMoveLink()
    {
        var state = DomainTests.Empty(); var task = DomainTests.Add(state);
        var missing = StateJson.Clone(state); missing.Days.Clear();
        Assert.Throws<InvalidDataException>(() => Backup.Validate(missing));
        var moved = TaskEngine.Move(state, task.Id, Today.AddDays(1), null, Now);
        task.MovedToId = null;
        Assert.Throws<InvalidDataException>(() => Backup.Validate(state));
    }

    [Fact] public void BackupRejectsEmptyRecordWithReadableError()
    {
        var state = DomainTests.Empty(); state.Tasks.Add(null!);
        Assert.Throws<InvalidDataException>(() => Backup.Validate(state));
        Assert.Throws<InvalidDataException>(() => Backup.Parse("not json"));
    }

    [Fact] public void StoppingRoutinePreservesManuallyMovedTasksAndTheirLinks()
    {
        var state = DomainTests.Empty(); var task = DomainTests.Add(state, repeat: Enum.GetValues<DayOfWeek>());
        var destination = TaskEngine.Move(state, task.Id, Today.AddDays(1), null, Now);
        TaskEngine.ArchiveRoutine(state, task.DefinitionId, Now);
        Assert.Contains(state.Tasks, t => t.Id == destination.Id);
        Assert.Equal(destination.Id, task.MovedToId);
        Assert.Equal(2, state.Tasks.Count); Backup.Validate(state);
    }
}
