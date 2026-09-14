using SQLite;
using ToDoOfSorts.Core;

namespace ToDoOfSorts.Infrastructure;

public sealed class StateRow
{
    [PrimaryKey] public int Id { get; set; } = 1;
    public string Json { get; set; } = "";
    public int Version { get; set; } = 1;
}

/// <summary>A transactional SQLite aggregate keeps task, event, reward and
/// scheduler-outbox intent atomic. Projections are always rebuildable.</summary>
public sealed class LocalStore(string path, IClock clock)
{
    static LocalStore() => SQLitePCL.Batteries_V2.Init();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private SQLiteConnection Open()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var connection = new SQLiteConnection(path, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex);
        connection.BusyTimeout = TimeSpan.FromSeconds(5);
        connection.ExecuteScalar<string>("PRAGMA journal_mode=WAL");
        connection.CreateTable<StateRow>();
        return connection;
    }
    private static AppState Read(StateRow? row)
    {
        if (row is null) return new AppState();
        if (row.Version != 1) throw new InvalidDataException("This database requires a newer app version.");
        var state = StateJson.Deserialize(row.Json);
        Backup.Validate(state);
        return state;
    }
    public async Task<AppState> ReadAsync() => await MutateAsync(_ => { });
    public async Task<AppState> MutateAsync(Action<AppState> mutation, bool reconcile = true)
    {
        await _gate.WaitAsync();
        try
        {
            return await Task.Run(() =>
            {
                using var db = Open();
                AppState? result = null;
                db.RunInTransaction(() =>
                {
                    var row = db.Find<StateRow>(1);
                    var state = Read(row);
                    if (reconcile) TaskEngine.Reconcile(state, clock.UtcNow);
                    mutation(state);
                    // Compare with the persisted representation instead of serializing an
                    // entire second "before" snapshot on every read and command.
                    if (StateJson.SerializeCompact(state) != row?.Json)
                    {
                        state.Revision++;
                        db.InsertOrReplace(new StateRow { Json = StateJson.SerializeCompact(state) });
                    }
                    result = state;
                });
                return result!;
            });
        }
        finally { _gate.Release(); }
    }
    public async Task AcknowledgeScheduleAsync(long revision)
    {
        await _gate.WaitAsync();
        try
        {
            await Task.Run(() =>
            {
                using var db = Open();
                db.RunInTransaction(() =>
                {
                    var state = Read(db.Find<StateRow>(1));
                    if (state.Revision != revision || state.ScheduledRevision == revision) return;
                    state.ScheduledRevision = revision;
                    db.InsertOrReplace(new StateRow { Json = StateJson.SerializeCompact(state) });
                });
            });
        }
        finally { _gate.Release(); }
    }
    public async Task ImportAsync(string json)
    {
        var replacement = Backup.Parse(json);
        await _gate.WaitAsync();
        try
        {
            await Task.Run(() =>
            {
                using var db = Open();
                db.RunInTransaction(() =>
                {
                    var old = db.Find<StateRow>(1);
                    if (old is not null) db.InsertOrReplace(new StateRow { Id = 2, Json = old.Json });
                    replacement.Revision++; replacement.ScheduledRevision = -1;
                    TaskEngine.Reconcile(replacement, clock.UtcNow);
                    db.InsertOrReplace(new StateRow { Json = StateJson.SerializeCompact(replacement) });
                });
            });
        }
        finally { _gate.Release(); }
    }
}
