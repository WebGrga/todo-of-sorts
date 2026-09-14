using Foundation;
using UserNotifications;
using UIKit;
using ToDoOfSorts.Core;
using ToDoOfSorts.App.Services;

namespace ToDoOfSorts.App.Platforms.iOS;

public sealed class IosReminders : IReminderService
{
    private static readonly NotificationDelegate Delegate = new();
    private static UNUserNotificationCenter Center => UNUserNotificationCenter.Current;
    public static void Register()
    {
        var actions = new[] {
            UNNotificationAction.FromIdentifier("start", "Start", UNNotificationActionOptions.Foreground),
            UNNotificationAction.FromIdentifier("move", "Move", UNNotificationActionOptions.Foreground),
            UNNotificationAction.FromIdentifier("skip", "Skip", UNNotificationActionOptions.Foreground)
        };
        Center.SetNotificationCategories(new NSSet<UNNotificationCategory>(UNNotificationCategory.FromIdentifier("task", actions, [], UNNotificationCategoryOptions.None)));
        Center.Delegate = Delegate;
    }
    public async Task<ReminderAccess> GetAccessAsync()
    {
        var settings = await Center.GetNotificationSettingsAsync();
        var allowed = settings.AuthorizationStatus is UNAuthorizationStatus.Authorized or UNAuthorizationStatus.Provisional;
        return new(allowed, true, allowed ? "Reminders are enabled. Focus and notification settings control how they appear." : "Reminders are off in iPhone settings.");
    }
    public async Task<ReminderAccess> RequestAccessAsync()
    {
        await Center.RequestAuthorizationAsync(UNAuthorizationOptions.Alert | UNAuthorizationOptions.Sound | UNAuthorizationOptions.Badge);
        return await GetAccessAsync();
    }
    public void OpenSettings() => UIApplication.SharedApplication.OpenUrl(new NSUrl(UIApplication.OpenSettingsUrlString), new NSDictionary(), null);
    public async Task ReplaceAsync(IReadOnlyList<ReminderPlan> plans)
    {
        var pending = await Center.GetPendingNotificationRequestsAsync();
        var access = await GetAccessAsync();
        var desired = access.Allowed ? plans : [];
        var ids = desired.Select(p => p.Id).ToHashSet();
        var obsolete = pending.Where(p => !ids.Contains(p.Identifier)).Select(p => p.Identifier).ToArray();
        Center.RemovePendingNotificationRequests(obsolete);
        var delivered = await Center.GetDeliveredNotificationsAsync();
        Center.RemoveDeliveredNotifications(delivered.Where(n => !ids.Contains(n.Request.Identifier)).Select(n => n.Request.Identifier).ToArray());
        foreach (var plan in desired)
        {
            var at = plan.At.UtcDateTime;
            var content = new UNMutableNotificationContent { Title = plan.Title, Body = plan.Body,
                Sound = UNNotificationSound.Default, CategoryIdentifier = plan.TaskId.HasValue ? "task" : "review",
                UserInfo = NSDictionary.FromObjectAndKey(new NSString(plan.TaskId?.ToString() ?? ""), new NSString("task_id")) };
            var trigger = UNCalendarNotificationTrigger.CreateTrigger(new NSDateComponents {
                Year = at.Year, Month = at.Month, Day = at.Day, Hour = at.Hour, Minute = at.Minute, Second = at.Second,
                TimeZone = NSTimeZone.FromName("UTC") }, false);
            await Center.AddNotificationRequestAsync(UNNotificationRequest.FromIdentifier(plan.Id, content, trigger));
        }
    }
}

public sealed class NotificationDelegate : UNUserNotificationCenterDelegate
{
    public override void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification,
        Action<UNNotificationPresentationOptions> completionHandler) => completionHandler(UNNotificationPresentationOptions.Banner | UNNotificationPresentationOptions.Sound);
    public override void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response, Action completionHandler)
    {
        try
        {
            var raw = response.Notification.Request.Content.UserInfo["task_id"]?.ToString();
            NotificationRoute.Set(Guid.TryParse(raw, out var id) ? id : null, response.ActionIdentifier);
        }
        finally { completionHandler(); }
    }
}
