using ToDoOfSorts.Core;
using ToDoOfSorts.Infrastructure;
using ToDoOfSorts.App.Design;
using ToDoOfSorts.App.Services;

namespace ToDoOfSorts.App.Views;

public sealed class SettingsPage : FormPage
{
    private Experience _theme;
    private readonly Switch _sound, _haptics, _motion, _quiet, _review;
    private readonly TimePicker _quietStart, _quietEnd, _reviewTime;
    private readonly Label _reminderStatus;
    private readonly Label _categoryNames, _zoneName;
    public SettingsPage(AppSession session) : base(session, "Your preferences")
    {
        var settings = session.State.Settings;
        Form.Add(Ui.Heading("Your kind\nof satisfying.", P.Text, 43));
        _theme = settings.Theme;
        Form.Add(Ui.Mono("STAMP THEME", 11, P.Muted));
        Form.Add(Ui.Choices(Enum.GetNames<Experience>(), (int)_theme, P, selected => _theme = (Experience)selected));
        Form.Add(Ui.Text("Energetic: heavy impact and ink burst.\nTactile: a crisp rubber stamp.\nCalm: a gentle ink press.", 13, P.Muted));
        _sound = new Switch { IsToggled = settings.Sound }; _haptics = new Switch { IsToggled = settings.Haptics }; _motion = new Switch { IsToggled = settings.ReduceMotion };
        Form.Add(Toggle("Stamp sound", _sound)); Form.Add(Toggle("Feel the impact", _haptics)); Form.Add(Toggle("Reduced motion", _motion, "System accessibility settings are respected too."));
        Action("Try the sound & impact", async () => { await Session.Feedback.PrepareAsync(); Session.Feedback.Impact(EditedSettings()); });
        Form.Add(Ui.Rule(P.Rule)); Form.Add(Ui.Text("REMINDERS", 12, P.Text, true));
        _reminderStatus = Ui.Text("Checking reminder access…", 13, P.Muted); Form.Add(_reminderStatus);
        Action("Enable reminders", async () => { var access = await Session.Reminders.RequestAccessAsync(); _reminderStatus.Text = access.Description; await Session.SyncRemindersAsync(); });
        Action("Open phone notification settings", () => { Session.Reminders.OpenSettings(); return Task.CompletedTask; });
#if ANDROID
        Action("Allow precise reminder timing", () =>
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(31))
            {
                var context = global::Android.App.Application.Context;
                var intent = new global::Android.Content.Intent(global::Android.Provider.Settings.ActionRequestScheduleExactAlarm,
                    global::Android.Net.Uri.Parse("package:" + context.PackageName));
                intent.AddFlags(global::Android.Content.ActivityFlags.NewTask); context.StartActivity(intent);
            }
            return Task.CompletedTask;
        });
#endif
        _quiet = new Switch { IsToggled = settings.QuietHours };
        _quietStart = new TimePicker { Time = settings.QuietStart.ToTimeSpan(), Format = "HH:mm", TextColor = P.Text };
        _quietEnd = new TimePicker { Time = settings.QuietEnd.ToTimeSpan(), Format = "HH:mm", TextColor = P.Text };
        Form.Add(Toggle("Quiet hours", _quiet));
        var quietTimes = Ui.EqualRow(Ui.Field("FROM", _quietStart, P), Ui.Field("UNTIL", _quietEnd, P));
        quietTimes.IsVisible = _quiet.IsToggled; _quiet.Toggled += (_, _) => quietTimes.IsVisible = _quiet.IsToggled; Form.Add(quietTimes);
        _review = new Switch { IsToggled = settings.EveningReview };
        _reviewTime = new TimePicker { Time = settings.ReviewTime.ToTimeSpan(), Format = "HH:mm", TextColor = P.Text };
        Form.Add(Toggle("Evening review", _review, "One gentle invitation to look at your day."));
        var reviewTime = Ui.Field("REVIEW AT", _reviewTime, P); reviewTime.IsVisible = _review.IsToggled;
        _review.Toggled += (_, _) => reviewTime.IsVisible = _review.IsToggled; Form.Add(reviewTime);
        Form.Add(Ui.Text("Opening the app refreshes upcoming reminders. Your phone's notification settings can delay or silence them.", 12, P.Muted));
        FooterAction("SAVE PREFERENCES", async () =>
        {
            var edited = EditedSettings();
            if (edited.EveningReview) await Session.Reminders.RequestAccessAsync();
            await Session.ChangeAsync(s => s.Settings = edited); await Navigation.PopAsync(false); await Session.SyncRemindersAsync();
        });
        Form.Add(Ui.Rule(P.Rule)); Form.Add(Ui.Text("CATEGORIES", 12, P.Text, true));
        _categoryNames = Ui.Text(string.Join(" · ", settings.Categories), 14, P.Muted); Form.Add(_categoryNames);
        Action("Add category", async () =>
        {
            var name = await PaperSheet.PromptAsync(this, P, "New category", "What would you like to call it?", maxLength: 40);
            if (string.IsNullOrWhiteSpace(name)) return;
            await Session.ChangeAsync(s => { if (!s.Settings.Categories.Contains(name.Trim(), StringComparer.OrdinalIgnoreCase)) s.Settings.Categories.Add(name.Trim()); });
            _categoryNames.Text = string.Join(" · ", Session.State.Settings.Categories);
            await PaperSheet.AlertAsync(this, P, "Category added", name.Trim(), "OK");
        });
        Action("Rename category", async () =>
        {
            var old = await PaperSheet.ChooseAsync(this, P, "RENAME CATEGORY.", Session.State.Settings.Categories.ToArray());
            if (old is null or "Cancel") return;
            var name = await PaperSheet.PromptAsync(this, P, "Rename category", "Past tasks retain their category. Future routines use the new name.", initialValue: old, maxLength: 40);
            if (string.IsNullOrWhiteSpace(name) || name.Trim() == old) return;
            await Session.ChangeAsync(s =>
            {
                if (s.Settings.Categories.Contains(name.Trim(), StringComparer.OrdinalIgnoreCase)) throw new InvalidOperationException("That category already exists.");
                s.Settings.Categories[s.Settings.Categories.IndexOf(old)] = name.Trim();
                foreach (var d in s.Definitions.Where(d => d.Category == old)) d.Category = name.Trim();
                foreach (var t in s.Tasks.Where(t => t.Date > Session.Today && !t.CountedAttempt && t.Category == old)) t.Category = name.Trim();
            });
            _categoryNames.Text = string.Join(" · ", Session.State.Settings.Categories);
            await PaperSheet.AlertAsync(this, P, "Category renamed", name.Trim(), "OK");
        });
        Form.Add(Ui.Rule(P.Rule)); Form.Add(Ui.Text("YOUR DATA", 12, P.Text, true));
        Form.Add(Ui.Text("Stored on this phone. No account, cloud sync, or remote analytics. Export a backup before changing phones.", 13, P.Muted));
        Action("Export backup", () => ExportAsync(false)); Action("Export history as CSV", () => ExportAsync(true));
        Action("Restore a backup", ImportAsync);
        _zoneName = Ui.Text($"Planning time zone: {settings.TimeZoneId}", 12, P.Muted); Form.Add(_zoneName);
        Action("Use this phone's current time zone", async () =>
        {
            var zone = TimeZoneInfo.Local.Id;
            if (!await PaperSheet.AlertAsync(this, P, "Change planning time zone?", $"Use {zone} for future days. Existing confirmed and historical days retain their boundaries.", "Change", "Cancel")) return;
            await Session.ChangeAsync(s =>
            {
                s.Settings.TimeZoneId = zone;
                foreach (var d in s.Days.Where(d => d.Date > Session.Today && d.ConfirmedAt is null))
                { d.TimeZoneId = zone; d.StartsAt = PlanningTime.Resolve(d.Date, TimeOnly.MinValue, zone); d.EndsAt = PlanningTime.Resolve(d.Date.AddDays(1), TimeOnly.MinValue, zone); }
            });
            _zoneName.Text = $"Planning time zone: {zone}";
            await Session.SyncRemindersAsync(); await PaperSheet.AlertAsync(this, P, "Time zone updated", zone, "OK");
        });
        Form.Add(Ui.Text($"ToDoOfSorts · {AppInfo.Current.VersionString} ({AppInfo.Current.BuildString})", 12, P.Muted));
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try { _reminderStatus.Text = (await Session.Reminders.GetAccessAsync()).Description; }
        catch { _reminderStatus.Text = "Could not check reminder access. Try opening phone settings."; }
    }
    private AppSettings EditedSettings()
    {
        var s = StateJson.CloneSettings(Session.State.Settings);
        s.Theme = _theme; s.Sound = _sound.IsToggled; s.Haptics = _haptics.IsToggled; s.ReduceMotion = _motion.IsToggled;
        // Feedback can be previewed while the rest of the page is being initialized.
        if (_quiet is not null)
        {
            s.QuietHours = _quiet.IsToggled; s.QuietStart = TimeOnly.FromTimeSpan(_quietStart.Time ?? TimeSpan.Zero); s.QuietEnd = TimeOnly.FromTimeSpan(_quietEnd.Time ?? TimeSpan.Zero);
            s.EveningReview = _review.IsToggled; s.ReviewTime = TimeOnly.FromTimeSpan(_reviewTime.Time ?? TimeSpan.Zero);
        }
        return s;
    }
    private async Task ExportAsync(bool csv)
    {
        var state = await Session.Store.ReadAsync();
        var name = $"ToDoOfSorts-{DateTime.Now:yyyyMMdd-HHmmss}.{(csv ? "csv" : "json")}";
        var path = Path.Combine(FileSystem.CacheDirectory, name);
        await File.WriteAllTextAsync(path, csv ? Backup.Csv(state) : StateJson.Serialize(state));
        await Share.Default.RequestAsync(new ShareFileRequest { Title = csv ? "Export history" : "Save your backup", File = new ShareFile(path) });
    }
    private async Task ImportAsync()
    {
        var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Choose a ToDoOfSorts JSON backup" });
        if (file is null) return;
        await using var input = await file.OpenReadAsync();
        using var memory = new MemoryStream(); var buffer = new byte[8192]; int count;
        while ((count = await input.ReadAsync(buffer)) > 0)
        { if (memory.Length + count > Backup.MaxBytes) throw new InvalidDataException("Maximum backup size is 20 MB."); await memory.WriteAsync(buffer.AsMemory(0, count)); }
        var json = System.Text.Encoding.UTF8.GetString(memory.ToArray()); var preview = Backup.Parse(json);
        if (!await PaperSheet.AlertAsync(this, P, "Restore this backup?", $"{preview.Tasks.Count} task attempts, {preview.Days.Count} days, {preview.Rewards.Sum(r => r.Points)} XP. This replaces the data on this phone. A recovery snapshot is kept locally.", "Restore", "Cancel")) return;
        await Session.Store.ImportAsync(json); await Session.LoadAsync(); await Navigation.PopAsync(false);
    }
}
