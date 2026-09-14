using ToDoOfSorts.Core;
using ToDoOfSorts.App.Design;
using ToDoOfSorts.App.Services;

namespace ToDoOfSorts.App.Views;

public sealed class TaskEditorPage : PaperSheet
{
    private readonly AppSession _session;
    private readonly Guid? _id;
    private readonly Entry _title;
    private readonly DatePicker _date;
    private readonly TimePicker _time;
    private readonly Switch _reminder, _commitment, _routine;
    private readonly VerticalStackLayout _timeFields, _extras;
    private readonly Label _error, _planNote;
    private readonly Button _save, _timeButton;
    private string _category;
    private int _xp, _repeat;
    private bool _timed, _expanded, _acknowledgedReminder;
    private readonly HashSet<DayOfWeek> _weekdays = [];
    private readonly HashSet<DayOfWeek> _originalRepeat = [];

    public TaskEditorPage(AppSession session, DateOnly date, Guid? id = null) : base(Palette.For(session.State.Settings.Theme), id.HasValue ? "EDIT TASK." : "ADD A TASK.", 370)
    {
        _session = session; _id = id;
        var task = id.HasValue ? TaskEngine.Find(session.State, id.Value) : null;
        var definition = task is null ? null : session.State.Definitions.First(d => d.Id == task.DefinitionId);
        _category = task?.Category ?? "Uncategorized";
        _xp = task?.Xp ?? 20; _timed = task?.Time.HasValue == true;
        _title = new Entry { Placeholder = "What do you want to finish?", Text = task?.Title, MaxLength = 180, FontSize = 20,
            TextColor = P.Ink, PlaceholderColor = P.PaperMuted, FontFamily = "BodyBold", BackgroundColor = Colors.Transparent, ReturnType = ReturnType.Done };
        Fields.Add(_title); Fields.Add(new BoxView { Color = P.Ink.WithAlpha(.25f), HeightRequest = 1 });
        _date = new DatePicker { Date = date.ToDateTime(TimeOnly.MinValue), MinimumDate = session.Today.ToDateTime(TimeOnly.MinValue),
            TextColor = P.Ink, BackgroundColor = Colors.Transparent, FontFamily = "Mono", FontSize = 13, Format = "ddd, d MMM", IsEnabled = !id.HasValue };
        _timeFields = new VerticalStackLayout { Spacing = 4, IsVisible = _timed };
        _timeButton = Ui.Button(_timed ? "REMOVE TIME" : "+ TIME", Colors.Transparent, P.Ink, (_, _) => ToggleTime());
        _timeButton.BorderColor = P.Ink.WithAlpha(.25f); _timeButton.BorderWidth = 1; _timeButton.CornerRadius = 6;
        Fields.Add(Ui.Row(_date, _timeButton));
        _time = new TimePicker { Time = task?.Time?.ToTimeSpan() ?? new TimeSpan(17, 0, 0), Format = "HH:mm", TextColor = P.Ink, BackgroundColor = Colors.Transparent };
        _reminder = new Switch { IsToggled = task?.Reminder ?? true };
        _timeFields.Add(Ui.Row(_time, Toggle("Remind me", _reminder)));
        _timeFields.Add(Ui.Text("Reminders follow your quiet hours.", 12, P.PaperMuted)); Fields.Add(_timeFields);
        _extras = new VerticalStackLayout { Spacing = 14, IsVisible = false };
        var more = Ui.Button("MORE OPTIONS  +", Colors.Transparent, P.PaperMuted, (_, _) => { });
        more.Padding = 0; more.HorizontalOptions = LayoutOptions.Start;
        more.Clicked += (_, _) => { _expanded = !_expanded; _extras.IsVisible = _expanded; more.Text = _expanded ? "FEWER OPTIONS  −" : "MORE OPTIONS  +"; UpdateHeight(); };
        Fields.Add(more); Fields.Add(_extras);
        _extras.Add(Ui.Mono("CATEGORY", 10, P.PaperMuted));
        var categories = session.State.Settings.Categories.Append(_category).Distinct().ToArray();
        _extras.Add(Choices(categories, Array.IndexOf(categories, _category), i => _category = categories[i]));
        _extras.Add(Ui.Mono("EFFORT", 10, P.PaperMuted));
        _extras.Add(Choices(["Quick · 10 XP", "Standard · 20 XP", "Deep · 40 XP"], _xp == 10 ? 0 : _xp == 40 ? 2 : 1, i => _xp = i == 0 ? 10 : i == 2 ? 40 : 20));
        _extras.Add(Ui.Mono("REPEAT", 10, P.PaperMuted));
        if (definition is { Archived: false, RepeatDays.Count: > 0 })
        {
            _originalRepeat.UnionWith(definition.RepeatDays); _weekdays.UnionWith(definition.RepeatDays);
            _repeat = _weekdays.Count == 7 ? 1 : _weekdays.SetEquals([DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday]) ? 2 : 3;
        }
        var weekdays = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap, IsVisible = _repeat == 3 };
        foreach (var day in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday })
        {
            var button = Ui.Button(day.ToString()[..3], Colors.Transparent, P.Ink, (_, _) => { });
            button.Margin = new Thickness(0, 0, 6, 6); button.CornerRadius = 6;
            void Paint() { button.BackgroundColor = _weekdays.Contains(day) ? P.Ink : P.Ink.WithAlpha(.07f); button.TextColor = _weekdays.Contains(day) ? P.Paper : P.Ink; }
            button.Clicked += (_, _) => { if (!_weekdays.Add(day)) _weekdays.Remove(day); Paint(); }; Paint(); weekdays.Add(button);
        }
        _extras.Add(Choices(["Once", "Daily", "Weekdays", "Custom"], _repeat, i => { _repeat = i; weekdays.IsVisible = i == 3; })); _extras.Add(weekdays);
        _commitment = new Switch { IsToggled = task?.Commitment ?? true };
        _planNote = Ui.Text("", 12, P.PaperMuted);
        _extras.Add(Toggle("Count toward clearing the day", _commitment)); _extras.Add(_planNote);
        _routine = new Switch();
        if (id.HasValue)
        {
            _extras.Add(Toggle("Apply task details to future repeats", _routine));
            _extras.Add(Ui.Text("Changing Repeat updates the routine's future schedule.", 12, P.PaperMuted));
        }
        void UpdatePlan()
        {
            var selected = DateOnly.FromDateTime(_date.Date ?? session.Today.ToDateTime(TimeOnly.MinValue));
            var plan = session.State.Days.FirstOrDefault(d => d.Date == selected);
            var wasLocked = !_commitment.IsEnabled;
            _commitment.IsEnabled = plan?.ConfirmedAt is null && plan?.Rest != true;
            if (!_commitment.IsEnabled) _commitment.IsToggled = task?.Commitment ?? false;
            else if (wasLocked && !id.HasValue) _commitment.IsToggled = true;
            _planNote.Text = _commitment.IsEnabled ? "Extra tasks can be bonuses instead." : plan?.Rest == true ? "Rest day. Tasks count as bonuses." : "This day's commitments are already set.";
        }
        _date.DateSelected += (_, _) => { _acknowledgedReminder = false; UpdatePlan(); }; UpdatePlan();
        _time.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(TimePicker.Time)) _acknowledgedReminder = false; };
        _reminder.Toggled += (_, _) => _acknowledgedReminder = false;
        _error = Ui.Text("", 13, P.Ink); _error.IsVisible = false;
        var footer = new VerticalStackLayout { Spacing = 8 }; footer.Add(_error);
        _save = Ui.Button(id.HasValue ? "SAVE CHANGES" : "ADD TASK", P.Stamp, P.Paper, async (_, _) => await SaveAsync());
        _save.FontFamily = "Stamp"; _save.FontSize = 22; _save.CornerRadius = 6; footer.Add(_save); Footer.Add(footer);
        _title.Completed += async (_, _) => await SaveAsync(); UpdateHeight();
    }
    private void ToggleTime() { _timed = !_timed; _timeFields.IsVisible = _timed; _timeButton.Text = _timed ? "REMOVE TIME" : "+ TIME"; _acknowledgedReminder = false; UpdateHeight(); }
    private void UpdateHeight() => SetHeight(_expanded ? 660 : _timed ? 460 : 370);
    private View Toggle(string label, Switch control)
    {
        control.OnColor = P.Stamp;
        void PaintThumb() => control.ThumbColor = control.IsToggled ? P.Paper : P.PaperMuted;
        control.Toggled += (_, _) => PaintThumb(); PaintThumb();
        return Ui.Row(Ui.Text(label, 14, P.Ink), control);
    }
    private View Choices(string[] names, int selected, Action<int> changed)
    {
        var row = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap };
        var buttons = new List<Button>();
        void Paint() { for (var i = 0; i < buttons.Count; i++) { buttons[i].BackgroundColor = i == selected ? P.Ink : P.Ink.WithAlpha(.07f); buttons[i].TextColor = i == selected ? P.Paper : P.Ink; } }
        for (var i = 0; i < names.Length; i++)
        {
            var index = i; var button = Ui.Button(names[i], Colors.Transparent, P.Ink, (_, _) => { selected = index; changed(index); Paint(); });
            button.CornerRadius = 6; button.Margin = new Thickness(0, 0, 6, 6);
            button.MinimumWidthRequest = Math.Min(280, names[i].Length * 8 + 36);
            button.MaximumWidthRequest = 300; button.LineBreakMode = LineBreakMode.WordWrap;
            FlexLayout.SetShrink(button, 0); buttons.Add(button); row.Add(button);
        }
        Paint(); return row;
    }
    private async Task SaveAsync()
    {
        if (Busy) return;
        _error.IsVisible = false;
        if (string.IsNullOrWhiteSpace(_title.Text)) { _error.Text = "Give your task a name first."; _error.IsVisible = true; _title.Focus(); return; }
        Busy = true; _save.IsEnabled = false;
        try
        {
            List<DayOfWeek> repeat = _repeat switch { 1 => Enum.GetValues<DayOfWeek>().ToList(), 2 => [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday], 3 => _weekdays.ToList(), _ => [] };
            if (_repeat == 3 && repeat.Count == 0) throw new InvalidOperationException("Choose at least one repeat day.");
            var date = DateOnly.FromDateTime(_date.Date ?? _session.Today.ToDateTime(TimeOnly.MinValue));
            var time = _timed ? TimeOnly.FromTimeSpan(_time.Time ?? TimeSpan.Zero) : (TimeOnly?)null;
            var draft = new TaskDraft(_title.Text.Trim(), _category, _xp, date, time, _timed && _reminder.IsToggled, _commitment.IsToggled, repeat);
            if (draft.Reminder && !_acknowledgedReminder)
            {
                var access = await _session.Reminders.RequestAccessAsync();
                var zone = _session.State.Days.FirstOrDefault(d => d.Date == date)?.TimeZoneId ?? _session.State.Settings.TimeZoneId;
                var at = PlanningTime.Resolve(date, time!.Value, zone);
                var shifted = ReminderPlanner.ApplyQuietHours(at, _session.State.Settings, zone);
                string? message = !access.Allowed ? "Notifications are off. Save the task without receiving an alert?" : shifted <= _session.Clock.UtcNow ? "That time has passed. Save the task without a past-time alert?" : shifted != at ? $"Quiet hours move this alert to {TimeZoneInfo.ConvertTime(shifted, PlanningTime.Zone(zone)):ddd HH:mm}. Save it?" : null;
                if (message is not null) { _acknowledgedReminder = true; _error.Text = message; _error.IsVisible = true; _save.Text = "SAVE TASK"; return; }
            }
            var changeRepeat = !_originalRepeat.SetEquals(repeat);
            await _session.ChangeAsync(s =>
            {
                if (_id is { } id) TaskEngine.Edit(s, id, draft, _session.Clock.UtcNow, _routine.IsToggled, changeRepeat);
                else TaskEngine.Add(s, draft, _session.Clock.UtcNow);
            });
            _title.Unfocus(); Finish();
            await _session.SyncRemindersAsync();
        }
        catch (Exception ex) { _error.Text = ex.Message; _error.IsVisible = true; }
        finally { Busy = false; _save.IsEnabled = true; }
    }
}
