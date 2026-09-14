using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using ToDoOfSorts.Core;
using ToDoOfSorts.App.Services;
using SystemClock = ToDoOfSorts.Core.SystemClock;

namespace ToDoOfSorts.App.Platforms.Android;

public sealed class NotificationPermission : Permissions.BasePlatformPermission
{
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
        OperatingSystem.IsAndroidVersionAtLeast(33) ? [(global::Android.Manifest.Permission.PostNotifications, true)] : [];
}

public sealed class AndroidReminders : IReminderService
{
    private const string Channel = "planned-tasks";
    private static Context Context => global::Android.App.Application.Context;
    internal static int Number(string value) => BitConverter.ToInt32(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)), 0) & int.MaxValue;
    private static PendingIntent AlarmIntent(string id, Guid? taskId, long at)
    {
        var intent = new Intent(Context, typeof(ReminderReceiver));
        intent.SetAction("app.todoofsorts.REMINDER");
        intent.PutExtra("reminder_id", id); intent.PutExtra("task_id", taskId?.ToString() ?? ""); intent.PutExtra("at", at);
        return PendingIntent.GetBroadcast(Context, Number(id), intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)!;
    }
    public Task<ReminderAccess> GetAccessAsync()
    {
        var allowed = NotificationManagerCompat.From(Context)!.AreNotificationsEnabled();
        var alarm = (AlarmManager)Context.GetSystemService(Context.AlarmService)!;
        var precise = !OperatingSystem.IsAndroidVersionAtLeast(31) || alarm.CanScheduleExactAlarms();
        return Task.FromResult(new ReminderAccess(allowed, precise, !allowed ? "Reminders are off in Android settings."
            : precise ? "Reminders are enabled." : "Reminders are enabled. Android may delay them while saving battery."));
    }
    public async Task<ReminderAccess> RequestAccessAsync()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(33)) await Permissions.RequestAsync<NotificationPermission>();
        return await GetAccessAsync();
    }
    public void OpenSettings()
    {
        var intent = new Intent(OperatingSystem.IsAndroidVersionAtLeast(26) ? global::Android.Provider.Settings.ActionAppNotificationSettings : global::Android.Provider.Settings.ActionApplicationDetailsSettings);
        if (OperatingSystem.IsAndroidVersionAtLeast(26)) intent.PutExtra(global::Android.Provider.Settings.ExtraAppPackage, Context.PackageName);
        else intent.SetData(global::Android.Net.Uri.Parse("package:" + Context.PackageName));
        intent.AddFlags(ActivityFlags.NewTask); Context.StartActivity(intent);
    }
    public async Task ReplaceAsync(IReadOnlyList<ReminderPlan> plans)
    {
        EnsureChannel();
        var alarm = (AlarmManager)Context.GetSystemService(Context.AlarmService)!;
        var oldIds = Preferences.Default.Get("scheduled-ids", "").Split('|', StringSplitOptions.RemoveEmptyEntries);
        var access = await GetAccessAsync();
        var desired = access.Allowed ? plans : [];
        var newIds = desired.Select(p => p.Id).ToHashSet();
        foreach (var id in oldIds)
        {
            alarm.Cancel(AlarmIntent(id, null, 0));
            if (!newIds.Contains(id)) NotificationManagerCompat.From(Context)!.Cancel(Number(id));
        }
        // Save cancellation inventory before scheduling; a partial failure is recoverable next launch.
        Preferences.Default.Set("scheduled-ids", string.Join('|', oldIds.Concat(newIds).Distinct()));
        foreach (var plan in desired)
        {
            var pending = AlarmIntent(plan.Id, plan.TaskId, plan.At.ToUnixTimeMilliseconds());
            if (access.Precise) alarm.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, plan.At.ToUnixTimeMilliseconds(), pending);
            else alarm.SetAndAllowWhileIdle(AlarmType.RtcWakeup, plan.At.ToUnixTimeMilliseconds(), pending);
        }
        Preferences.Default.Set("scheduled-ids", string.Join('|', newIds));
    }
    internal static void EnsureChannel()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26)) return;
        var manager = (NotificationManager)Context.GetSystemService(Context.NotificationService)!;
        manager.CreateNotificationChannel(new NotificationChannel(Channel, "Planned tasks", NotificationImportance.Default)
            { Description = "The reminders you choose for your tasks." });
    }
    internal static void Show(string id, Guid? taskId, string title, string body)
    {
        EnsureChannel();
        PendingIntent Route(string action)
        {
            var intent = new Intent(Context, typeof(MainActivity));
            intent.SetAction("app.todoofsorts." + action); intent.PutExtra("task_id", taskId?.ToString() ?? "");
            intent.PutExtra("task_action", action); intent.AddFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
            return PendingIntent.GetActivity(Context, Number(id + action), intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)!;
        }
        var builder = new NotificationCompat.Builder(Context, Channel);
        builder.SetSmallIcon(Resource.Drawable.notification_stamp); builder.SetContentTitle(title); builder.SetContentText(body);
        builder.SetStyle(new NotificationCompat.BigTextStyle().BigText(body)); builder.SetAutoCancel(true);
        builder.SetContentIntent(Route("open")); builder.SetGroup("todoofsorts-tasks");
        if (taskId.HasValue) { builder.AddAction(0, "Start", Route("start")); builder.AddAction(0, "Move", Route("move")); builder.AddAction(0, "Skip", Route("skip")); }
        NotificationManagerCompat.From(Context)!.Notify(Number(id), builder.Build()!);
    }
}

[BroadcastReceiver(Enabled = true, Exported = false)]
public sealed class ReminderReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        var pending = GoAsync();
        var id = intent?.GetStringExtra("reminder_id") ?? "";
        var taskText = intent?.GetStringExtra("task_id");
        var expectedAt = intent?.GetLongExtra("at", 0) ?? 0;
        _ = Task.Run(async () =>
        {
            try
            {
                var store = new Infrastructure.LocalStore(Path.Combine(FileSystem.AppDataDirectory, "daybook.db3"), new SystemClock());
                var state = await store.ReadAsync();
                if (Guid.TryParse(taskText, out var taskId))
                {
                    var task = state.Tasks.FirstOrDefault(t => t.Id == taskId);
                    if (task is null || (!task.IsActive && task.Status != Outcome.Unresolved) || !task.Reminder || task.Time is null) return;
                    var zone = state.Days.First(d => d.Date == task.Date).TimeZoneId;
                    var at = ReminderPlanner.ApplyQuietHours(PlanningTime.Resolve(task.Date, task.Time.Value, zone), state.Settings, zone);
                    if (at.ToUnixTimeMilliseconds() != expectedAt) return;
                    AndroidReminders.Show(id, taskId, task.Title, "Ready to make your mark?");
                }
                else if (state.Settings.EveningReview) AndroidReminders.Show(id, null, "How did today go?", "Your finished work deserves a look. Review your day.");
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
            finally { pending?.Finish(); }
        });
    }
}

[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilter([Intent.ActionBootCompleted, Intent.ActionMyPackageReplaced, Intent.ActionTimeChanged, Intent.ActionTimezoneChanged])]
public sealed class RestoreRemindersReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        var pending = GoAsync();
        _ = Task.Run(async () =>
        {
            try
            {
                var store = new Infrastructure.LocalStore(Path.Combine(FileSystem.AppDataDirectory, "daybook.db3"), new SystemClock());
                var state = await store.ReadAsync();
                await new AndroidReminders().ReplaceAsync(ReminderPlanner.Build(state, DateTimeOffset.UtcNow));
                await store.AcknowledgeScheduleAsync(state.Revision);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
            finally { pending?.Finish(); }
        });
    }
}
