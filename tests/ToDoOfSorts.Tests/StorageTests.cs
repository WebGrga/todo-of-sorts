using ToDoOfSorts.Core;
using ToDoOfSorts.Infrastructure;
using Xunit;

namespace ToDoOfSorts.Tests;

public sealed class StorageTests
{
    private sealed class Clock : IClock { public DateTimeOffset UtcNow => new(2026, 9, 11, 12, 0, 0, TimeSpan.Zero); }
    private static string NewPath() => Path.Combine(Path.GetTempPath(), "todoofsorts-tests-" + Guid.NewGuid().ToString("N"), "state.db3");
    [Fact] public async Task CompletionSurvivesReopeningDatabase()
    {
        var path = NewPath(); var clock = new Clock(); var store = new LocalStore(path, clock); Guid id = default;
        await store.MutateAsync(s => { s.Settings.TimeZoneId = "UTC"; id = DomainTests.Add(s).Id; });
        await store.MutateAsync(s => TaskEngine.Complete(s, id, clock.UtcNow));
        var loaded = await new LocalStore(path, clock).ReadAsync();
        Assert.Equal(Outcome.Completed, loaded.Tasks.Single().Status); Assert.Equal(20, loaded.Rewards.Sum(r => r.Points));
    }
    [Fact] public async Task FailedCommandRollsBackEveryChange()
    {
        var store = new LocalStore(NewPath(), new Clock());
        await store.ReadAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.MutateAsync(s => { DomainTests.Add(s); throw new InvalidOperationException("failure"); }));
        Assert.Empty((await store.ReadAsync()).Tasks);
    }
    [Fact] public async Task RapidConcurrentCompletionRemainsIdempotent()
    {
        var clock = new Clock(); var store = new LocalStore(NewPath(), clock); Guid id = default;
        await store.MutateAsync(s => { s.Settings.TimeZoneId = "UTC"; id = DomainTests.Add(s).Id; });
        await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => store.MutateAsync(s => TaskEngine.Complete(s, id, clock.UtcNow))));
        Assert.Equal(20, (await store.ReadAsync()).Rewards.Sum(r => r.Points));
    }
    [Fact] public async Task StaleSchedulerAcknowledgementCannotClearNewIntent()
    {
        var store = new LocalStore(NewPath(), new Clock()); var first = await store.ReadAsync();
        var next = await store.MutateAsync(s => DomainTests.Add(s));
        await store.AcknowledgeScheduleAsync(first.Revision);
        Assert.NotEqual(next.Revision, (await store.ReadAsync()).ScheduledRevision);
        await store.AcknowledgeScheduleAsync(next.Revision);
        Assert.Equal(next.Revision, (await store.ReadAsync()).ScheduledRevision);
    }
    [Fact] public async Task ImportValidatesBeforeReplacingAndRestoresExactly()
    {
        var store = new LocalStore(NewPath(), new Clock()); var source = DomainTests.Empty(); DomainTests.Add(source);
        await store.ImportAsync(StateJson.Serialize(source));
        await Assert.ThrowsAsync<InvalidDataException>(() => store.ImportAsync("{\"schemaVersion\":99}"));
        Assert.Single((await store.ReadAsync()).Tasks);
    }
}
