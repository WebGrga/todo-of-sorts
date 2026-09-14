using ToDoOfSorts.Core;
using ToDoOfSorts.Infrastructure;
using Xunit;

namespace ToDoOfSorts.Tests;

public class DomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 11, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 9, 11);
    internal static AppState Empty()
    {
        var s = new AppState(); s.Settings.TimeZoneId = "UTC"; TaskEngine.Reconcile(s, Now); return s;
    }
    internal static TaskOccurrence Add(AppState s, string title = "Gym", bool commitment = true, int xp = 20, DateOnly? date = null, TimeOnly? time = null, IReadOnlyList<DayOfWeek>? repeat = null) =>
        TaskEngine.Add(s, new(title, "Training", xp, date ?? Today, time, time.HasValue, commitment, repeat ?? []), Now);

    [Fact] public void DoubleStampAwardsOnce()
    {
        var s = Empty(); var t = Add(s);
        Assert.True(TaskEngine.Complete(s, t.Id, Now)); Assert.False(TaskEngine.Complete(s, t.Id, Now));
        Assert.Equal(20, s.Rewards.Sum(r => r.Points)); Assert.True(Statistics.Day(s, Today).Won);
    }
    [Fact] public void UndoAndRestampCannotFarmXp()
    {
        var s = Empty(); var t = Add(s); TaskEngine.Complete(s, t.Id, Now);
        TaskEngine.Reopen(s, t.Id, Now); TaskEngine.Reopen(s, t.Id, Now);
        Assert.Equal(0, s.Rewards.Sum(r => r.Points)); Assert.False(Statistics.Day(s, Today).Won);
        TaskEngine.Complete(s, t.Id, Now); Assert.Equal(20, s.Rewards.Sum(r => r.Points)); Backup.Validate(s);
    }
    [Fact] public void BonusDoesNotMoveTheFinishLine()
    {
        var s = Empty(); var a = Add(s); TaskEngine.ConfirmDay(s, Today, Now);
        var b = Add(s, "Extra", true, 40); Assert.False(b.Commitment);
        TaskEngine.Complete(s, a.Id, Now); Assert.True(Statistics.Day(s, Today).Won);
        Assert.Equal(1, Statistics.Day(s, Today).Commitments); TaskEngine.Complete(s, b.Id, Now);
        Assert.Equal(60, Statistics.Day(s, Today).Xp);
    }
    [Fact] public void ProgressIsCountBased()
    {
        var s = Empty(); var a = Add(s, xp: 10); Add(s, "Deep", xp: 40);
        TaskEngine.Complete(s, a.Id, Now); Assert.Equal(.5, Statistics.Day(s, Today).Progress);
    }
    [Fact] public void NoCommitmentsNeverWins()
    {
        var s = Empty(); Assert.False(Statistics.Day(s, Today).Won);
        Assert.Throws<InvalidOperationException>(() => TaskEngine.ConfirmDay(s, Today, Now));
    }
    [Fact] public void CrossDayMovePreservesSourceAndWorkIdentity()
    {
        var s = Empty(); var t = Add(s); TaskEngine.ConfirmDay(s, Today, Now);
        var moved = TaskEngine.Move(s, t.Id, Today.AddDays(1), new(9, 0), Now);
        Assert.Equal(Outcome.Rescheduled, t.Status); Assert.Equal(t.WorkId, moved.WorkId);
        Assert.Equal(t.Id, moved.MovedFromId); Assert.Equal(1, Statistics.Day(s, Today).Planned);
        Assert.Equal(1, Statistics.Day(s, Today).Moved); Assert.False(Statistics.Day(s, Today).Won);
        Backup.Validate(s);
    }
    [Fact] public void SameDayMovePreservesOriginalTimeAndAttemptCount()
    {
        var s = Empty(); var t = Add(s, time: new(14, 0)); TaskEngine.ConfirmDay(s, Today, Now);
        var same = TaskEngine.Move(s, t.Id, Today, new(18, 0), Now);
        Assert.Equal(t.Id, same.Id); Assert.Equal(new TimeOnly(14, 0), same.OriginalTime);
        Assert.Equal(1, Statistics.Day(s, Today).Planned);
    }
    [Fact] public void SkipIsNotCompletion()
    {
        var s = Empty(); var t = Add(s); TaskEngine.ConfirmDay(s, Today, Now);
        TaskEngine.Skip(s, t.Id, "Not enough time", Now);
        Assert.Equal(1, Statistics.Day(s, Today).Skipped); Assert.Empty(s.Rewards); Assert.False(Statistics.Day(s, Today).Won);
    }
    [Fact] public void RolloverReconcilesWithoutBackgroundExecution()
    {
        var s = Empty(); var t = Add(s); TaskEngine.ConfirmDay(s, Today, Now);
        TaskEngine.Reconcile(s, Now.AddDays(3)); Assert.Equal(Outcome.Unresolved, t.Status);
        Assert.Equal(1, Statistics.Day(s, Today).Unresolved);
    }
    [Fact] public void RecurrenceIsUniqueAndCatchesUp()
    {
        var s = Empty(); var t = Add(s, repeat: Enum.GetValues<DayOfWeek>());
        TaskEngine.Reconcile(s, Now); TaskEngine.Reconcile(s, Now.AddDays(20));
        Assert.Equal(s.Tasks.Count, s.Tasks.Select(t => (t.DefinitionId, t.Date)).Distinct().Count());
        Assert.Contains(s.Tasks, t => t.Date == Today.AddDays(19)); Backup.Validate(s);
    }
    [Fact] public void ArchivedRoutineKeepsHistory()
    {
        var s = Empty(); var t = Add(s, repeat: Enum.GetValues<DayOfWeek>());
        TaskEngine.Complete(s, t.Id, Now); TaskEngine.ArchiveRoutine(s, t.DefinitionId, Now);
        Assert.Single(s.Tasks); Assert.Equal(Outcome.Completed, s.Tasks[0].Status); Backup.Validate(s);
    }
    [Fact] public void RestPausesAndUnplannedDayBreaksRun()
    {
        var s = Empty(); var t = Add(s); TaskEngine.Complete(s, t.Id, Now);
        TaskEngine.SetRest(s, Today.AddDays(1), true, Now);
        TaskEngine.Reconcile(s, Now.AddDays(2)); Assert.Equal(1, Statistics.Runs(s, Today.AddDays(2)).Current);
        TaskEngine.Reconcile(s, Now.AddDays(3)); Assert.Equal(0, Statistics.Runs(s, Today.AddDays(3)).Current);
        Assert.Equal(1, Statistics.Runs(s, Today.AddDays(3)).Best);
    }
    [Fact] public void RestCannotEraseConfirmedFailure()
    {
        var s = Empty(); Add(s); TaskEngine.ConfirmDay(s, Today, Now);
        Assert.Throws<InvalidOperationException>(() => TaskEngine.SetRest(s, Today, true, Now));
    }
    [Fact] public void PlanCorrectionIsExplicitAndRecalculatesWin()
    {
        var s = Empty(); var a = Add(s); var b = Add(s, "Other"); TaskEngine.Complete(s, a.Id, Now);
        TaskEngine.AmendCommitment(s, b.Id, false, Now); Assert.True(Statistics.Day(s, Today).Won);
        Assert.Contains(s.Events, e => e.Kind == "PlanCorrected");
        TaskEngine.AmendCommitment(s, a.Id, false, Now); Assert.False(Statistics.Day(s, Today).Won);
    }
    [Fact] public void QuietHoursDeferToMorning()
    {
        var s = Empty(); Add(s, time: new(23, 0)); var plans = ReminderPlanner.Build(s, Now);
        Assert.Equal(new DateTimeOffset(2026, 9, 12, 8, 0, 0, TimeSpan.Zero), Assert.Single(plans).At);
    }
    [Fact] public void CompletedAndSkippedTasksCancelReminderIntent()
    {
        var s = Empty(); var a = Add(s, time: new(17, 0)); var b = Add(s, "Other", time: new(18, 0));
        Assert.Equal(2, ReminderPlanner.Build(s, Now).Count);
        TaskEngine.Complete(s, a.Id, Now); TaskEngine.Skip(s, b.Id, null, Now);
        Assert.Empty(ReminderPlanner.Build(s, Now));
    }
    [Fact] public void QuietHoursReminderSurvivesDayRollover()
    {
        var s = Empty(); Add(s, time: new(23, 0)); TaskEngine.ConfirmDay(s, Today, Now);
        var morning = new DateTimeOffset(2026, 9, 12, 7, 0, 0, TimeSpan.Zero);
        TaskEngine.Reconcile(s, morning);
        Assert.Equal(Outcome.Unresolved, s.Tasks.Single().Status);
        Assert.Single(ReminderPlanner.Build(s, morning));
    }
    [Fact] public void ReminderWindowIsBoundedAndChronological()
    {
        var s = Empty(); for (int i = 0; i < 70; i++) Add(s, "Task " + i, time: new(17, i % 60));
        var plans = ReminderPlanner.Build(s, Now); Assert.Equal(60, plans.Count);
        Assert.Equal(plans.OrderBy(p => p.At), plans);
    }
    [Fact] public void DstGapUsesNextValidTimeAndOverlapUsesFirstInstance()
    {
        var gap = PlanningTime.Resolve(new(2026, 3, 29), new(2, 30), "Europe/Zagreb");
        Assert.Equal(new DateTimeOffset(2026, 3, 29, 1, 0, 0, TimeSpan.Zero), gap);
        var overlap = PlanningTime.Resolve(new(2026, 10, 25), new(2, 30), "Europe/Zagreb");
        Assert.Equal(new DateTimeOffset(2026, 10, 25, 0, 30, 0, TimeSpan.Zero), overlap);
    }
    [Fact] public void CompletionCorrectionMustBeInsideOriginalDay()
    {
        var s = Empty(); var t = Add(s); TaskEngine.ConfirmDay(s, Today, Now); TaskEngine.Reconcile(s, Now.AddDays(1));
        Assert.Throws<InvalidOperationException>(() => TaskEngine.CorrectCompletion(s, t.Id, Now.AddDays(1), Now.AddDays(1)));
        TaskEngine.CorrectCompletion(s, t.Id, Now, Now.AddDays(1));
        Assert.True(Statistics.Day(s, Today).Won); Backup.Validate(s);
    }
    [Fact] public void BackupRoundTripPreservesHistoryAndRejectsDuplicateRewards()
    {
        var s = Empty(); var t = Add(s); TaskEngine.Complete(s, t.Id, Now);
        var restored = Backup.Parse(StateJson.Serialize(s)); Assert.True(Statistics.Day(restored, Today).Won);
        restored.Rewards.Add(restored.Rewards[0]); Assert.Throws<InvalidDataException>(() => Backup.Validate(restored));
    }
    [Fact] public void CsvEscapesTitlesAndFormulaPrefixes()
    {
        var s = Empty(); Add(s, "=A1,\"test\"");
        Assert.Contains("\"'=A1,\"\"test\"\"\"", Backup.Csv(s));
    }
    [Fact] public void RenamingRoutineDoesNotRewritePastSnapshots()
    {
        var s = Empty(); var t = Add(s, repeat: Enum.GetValues<DayOfWeek>()); TaskEngine.Complete(s, t.Id, Now);
        var tomorrow = s.Tasks.First(t => t.Date == Today.AddDays(1));
        TaskEngine.Edit(s, tomorrow.Id, new("Changed", "Career", 40, tomorrow.Date, null, false, true, Enum.GetValues<DayOfWeek>()), Now, true);
        Assert.Equal("Gym", t.Title); Assert.Equal("Training", t.Category); Assert.Equal(20, t.Xp); Backup.Validate(s);
    }
}
