using ToDoOfSorts.Core;
using ToDoOfSorts.App.Controls;
using ToDoOfSorts.App.Design;
using ToDoOfSorts.App.Services;
using ToDoOfSorts.App.ViewModels;
using ToDoOfSorts.App.Views;

namespace ToDoOfSorts.App;

public partial class MainPage : ContentPage
{
    private readonly AppSession _session;
    private readonly HomeViewModel _vm;
    private bool _progress, _working, _loading, _routeBusy;
    private DateOnly? _lastToday;
    private long _renderedRevision = -1;
    private AppTheme _renderedTheme;
    private Guid? _lastStamp;
    private Label? _progressText, _xpText, _runText, _message;
    private Label? _reminderMessage;
    private CommitmentMeter? _meter;
    private VerticalStackLayout? _celebration;
    public MainPage(AppSession session)
    {
        InitializeComponent(); _session = session; _vm = new HomeViewModel(session); BindingContext = _vm;
        SafeAreaEdges = SafeAreaEdges.All;
        NotificationRoute.Arrived += async () => await HandleRouteAsync();
        _session.ReminderStatusChanged += UpdateReminderMessage;
        if (Application.Current is { } app) app.RequestedThemeChanged += (_, _) =>
        {
            if (Navigation.NavigationStack.LastOrDefault() != this || _working) return;
            var next = Palette.For(_session.State.Settings.Theme);
            foreach (var sheet in Surface.Children.OfType<PaperSheet>()) sheet.RefreshTheme(next);
            Render();
        };
    }
    protected override async void OnAppearing() { base.OnAppearing(); await ReloadAsync(); }
    protected override bool OnBackButtonPressed() => PaperSheet.CloseTop(this) || base.OnBackButtonPressed();
    public async Task RefreshFromResumeAsync() { if (!_working && !_loading) await ReloadAsync(); }
    private async Task ReloadAsync()
    {
        if (_loading) return; _loading = true;
        try
        {
            await _session.LoadAsync(false);
            if (_lastToday is null || _vm.SelectedDate == _lastToday) _vm.SelectedDate = _session.Today;
            var dayChanged = _lastToday != _session.Today;
            _lastToday = _session.Today;
            if (_renderedRevision != _session.State.Revision || dayChanged || _renderedTheme != Application.Current?.RequestedTheme) Render();
            await _session.SyncRemindersAsync();
        }
        catch (Exception ex)
        {
            Body.Clear(); Body.Add(Ui.Heading("Couldn't open\nyour day.", Colors.DarkRed));
            Body.Add(Ui.Text("Your saved data has been left intact. " + ex.Message, 15, Colors.DarkRed));
            Body.Add(Ui.Button("Try again", Colors.DarkOliveGreen, Colors.White, async (_, _) => await ReloadAsync()));
        }
        finally { _loading = false; }
        await HandleRouteAsync();
    }
    private async Task Guard(Func<Task> action)
    {
        if (_working) return; _working = true;
        try { await action(); }
        catch (Exception ex) { await PaperSheet.AlertAsync(this, Palette.For(_session.State.Settings.Theme), "Couldn't finish that", ex.Message, "OK"); }
        finally { _working = false; }
    }
    private void Render()
    {
        _vm.Refresh(); var p = Palette.For(_session.State.Settings.Theme);
        _renderedRevision = _session.State.Revision; _renderedTheme = Application.Current?.RequestedTheme ?? AppTheme.Unspecified;
        BackgroundColor = p.Desk; Surface.BackgroundColor = p.Desk; NavigationBar.BackgroundColor = p.Desk;
        Ui.ApplySystemBars(this, p);
        TodayTab.BackgroundColor = ProgressTab.BackgroundColor = AddTask.BackgroundColor = Colors.Transparent;
        TodayTab.TextColor = _progress ? p.Muted : p.Text; ProgressTab.TextColor = _progress ? p.Text : p.Muted;
        AddTask.TextColor = p.ActionText; MoreButton.TextColor = p.Text; NavRule.Color = p.Rule;
        TodayIndicator.Color = RecordIndicator.Color = p.Text;
        TodayIndicator.IsVisible = !_progress; RecordIndicator.IsVisible = _progress;
        Body.Clear();
        Body.Spacing = _progress ? 14 : 0;
        var date = Ui.Button(_progress ? "YOUR PROGRESS" : _vm.SelectedDate.ToString("ddd / dd MMM").ToUpperInvariant() + "  ▾", Colors.Transparent, p.Text, async (_, _) => await ChooseDayAsync());
        date.Padding = 0; date.HorizontalOptions = LayoutOptions.Start; date.FontSize = 11; date.HeightRequest = 44;
        var sound = Ui.Button(_session.State.Settings.Sound ? "SOUND ON" : "SOUND OFF", Colors.Transparent, p.Text, async (_, _) => await Guard(async () =>
        { await _session.ChangeAsync(s => s.Settings.Sound = !s.Settings.Sound); await _session.Feedback.PrepareAsync(); Render(); }));
        sound.FontSize = 10; sound.CornerRadius = 22; sound.BorderColor = p.Rule; sound.BorderWidth = 1;
        sound.MinimumHeightRequest = 44; sound.HeightRequest = 44; sound.Padding = new Thickness(12, 0);
        Body.Add(Ui.Row(date, sound));
        if (_progress) RenderProgress(p); else RenderToday(p);
        _reminderMessage = Ui.Text("", 12, p.Text); Body.Add(_reminderMessage); UpdateReminderMessage();
    }
    private void UpdateReminderMessage()
    {
        if (_reminderMessage is null) return;
        _reminderMessage.Text = _session.ReminderError ?? "";
        _reminderMessage.IsVisible = _session.ReminderError is not null;
    }
    private void RenderToday(Palette p)
    {
        var heading = Ui.Heading(_vm.Heading, p.Text, 53); heading.Margin = new Thickness(0, 8, 0, 12); Body.Add(heading);
        var day = _vm.Summary;
        var tasks = _session.State.Tasks.Where(t => t.Date == _vm.SelectedDate).ToArray();
        _progressText = Ui.Mono(_vm.ProgressLabel, 10, p.Text);
        _xpText = Ui.Mono(_vm.XpLabel, 10, p.Text); _runText = Ui.Mono(_vm.RunLabel, 10, p.Text);
        var metrics = new Grid { ColumnDefinitions = [new(GridLength.Star), new(GridLength.Auto), new(GridLength.Auto)], ColumnSpacing = 12, Margin = new Thickness(0, 0, 0, 12) };
        metrics.Add(_progressText); metrics.Add(_xpText, 1); metrics.Add(_runText, 2); Body.Add(metrics);
        _meter = new CommitmentMeter { Progress = day.Progress, Count = day.Commitments, Ink = p.Accent, Track = p.Rule, Margin = new Thickness(0, 0, 0, 23) };
        SemanticProperties.SetDescription(_meter, $"{day.StampedCommitments} of {day.Commitments} commitments complete");
        Body.Add(_meter);
        if (tasks.Length == 0)
        {
            var empty = new Grid { Padding = new Thickness(20, 23), MinimumHeightRequest = 180, Rotation = -.6 };
            empty.Add(new PaperSurface { PaperColor = p.Paper });
            var content = new VerticalStackLayout { Spacing = 14, Padding = 18 };
            content.Add(Ui.Mono(day.Rest ? "ROOM TO REST." : "YOUR FIRST MARK STARTS HERE.", 10, p.PaperMuted));
            content.Add(Ui.Text(day.Rest ? "You've made room to rest. Your run pauses here." : $"One thing you want to finish {(_vm.SelectedDate == _session.Today ? "today" : "that day")}.\nGive it a name.", 18, p.Ink, true));
            if (_vm.SelectedDate >= _session.Today)
                content.Add(Ui.Button("+ ADD A TASK", Colors.Transparent, p.Stamp, async (_, _) => await OpenEditorAsync()));
            empty.Add(content); Body.Add(empty);
        }
        var slips = new VerticalStackLayout { Spacing = 14 };
        var number = 0;
        foreach (var group in new[] { true, false })
        {
            var items = tasks.Where(t => t.Commitment == group).OrderBy(t => t.Time ?? TimeOnly.MaxValue).ToArray();
            if (items.Length == 0) continue;
            foreach (var task in items) slips.Add(new PaperTaskView(task, _session, CompleteAsync, OpenDetailsAsync, ++number));
        }
        Body.Add(slips);
        _message = Ui.Mono(_lastStamp.HasValue ? "Your mark stays." : "Make your mark.", 11, p.Muted);
        var end = Ui.Row(_message, Ui.Button("Undo last stamp", Colors.Transparent, p.Text, async (_, _) => await UndoAsync()));
        ((View)end.Children[1]).IsVisible = _lastStamp.HasValue && _session.State.Tasks.Any(t => t.Id == _lastStamp && t.Status == Outcome.Completed);
        end.Margin = new Thickness(0, 10, 0, 0); Body.Add(end);
        _celebration = new VerticalStackLayout { Spacing = 16, IsVisible = day.Won, Padding = new Thickness(12, 22), HorizontalOptions = LayoutOptions.Fill };
        var seal = new InkStamp { Word = "DAY\nCLEARED.", WidthRequest = 205, HeightRequest = 114, Ink = p.Accent, Paper = p.Desk, Rotation = -5, HorizontalOptions = LayoutOptions.Center };
        var run = Statistics.Runs(_session.State, _vm.SelectedDate <= _session.Today ? _vm.SelectedDate : _session.Today).Current;
        var milestone = run is 3 or 7 or 14 or 30 ? $"\n{run}-DAY RUN. Look what you're building." : "";
        var winCopy = Ui.Mono("You did what you said you would.\nNothing left to prove today." + milestone, 11, p.Text); winCopy.HorizontalTextAlignment = TextAlignment.Center;
        _celebration.Add(new DashedRule { Ink = p.Rule, HeightRequest = 1, Margin = new Thickness(0, 0, 0, 12) });
        _celebration.Add(seal); _celebration.Add(winCopy); Body.Add(_celebration);
        if (_vm.SelectedDate == _session.Today)
        {
            var unresolved = _session.State.Tasks.Count(t => t.Date < _session.Today && t.Status == Outcome.Unresolved && t.CountedAttempt);
            if (unresolved > 0) Body.Add(Ui.Button($"Review {unresolved} unresolved from earlier days", Colors.Transparent, p.Text,
                (_, _) => { if (_working) return; _vm.SelectedDate = _session.State.Tasks.Where(t => t.Date < _session.Today && t.Status == Outcome.Unresolved && t.CountedAttempt).Max(t => t.Date); Render(); }));
        }
    }
    private async Task CompleteAsync(Guid id, PaperTaskView view)
    {
        await Guard(async () =>
        {
            var task = TaskEngine.Find(_session.State, id);
            if (task.Date != _session.Today) { await OpenDetailsAsync(id); return; }
            var plan = _session.State.Days.First(d => d.Date == task.Date);
            if (plan.ConfirmedAt is null && !plan.Rest)
            {
                var n = _session.State.Tasks.Count(t => t.Date == task.Date && t.Commitment && t.IsActive);
                if (n > 0 && !await PaperSheet.AlertAsync(this, Palette.For(_session.State.Settings.Theme), "Set today's commitments?", $"Confirm {n} commitments, then stamp this task. Later additions will be bonuses.", "Set & stamp", "Not yet")) return;
            }
            bool awarded = false;
            await _session.ChangeAsync(s => awarded = TaskEngine.Complete(s, id, _session.Clock.UtcNow));
            if (!awarded) return;
            _lastStamp = id;
            var sync = _session.SyncRemindersAsync();
            await view.PlayStampAsync(() =>
            {
                if (_progressText is not null) _progressText.Text = _vm.ProgressLabel;
                if (_xpText is not null) _xpText.Text = _vm.XpLabel;
                if (_runText is not null) _runText.Text = _vm.RunLabel;
                if (_meter is not null) _meter.Progress = _vm.Summary.Progress;
            });
            _vm.Refresh();
            if (_progressText is not null) _progressText.Text = _vm.ProgressLabel;
            if (_xpText is not null) _xpText.Text = _vm.XpLabel;
            if (_runText is not null) _runText.Text = _vm.RunLabel;
            if (_meter is not null) _meter.Progress = _vm.Summary.Progress;
            if (_message is not null) _message.Text = $"{task.Title}. DONE.";
            var won = _vm.Summary.Won;
            var showWin = won && !_session.State.Days.First(d => d.Date == task.Date).Celebrated;
            if (showWin)
            {
                await _session.ChangeAsync(s => s.Days.First(d => d.Date == task.Date).Celebrated = true);
                var p = Palette.For(_session.State.Settings.Theme);
                var bigSeal = new InkStamp
                {
                    Word = "DAY CLEARED",
                    HeightRequest = 110,
                    HorizontalOptions = LayoutOptions.Fill,
                    VerticalOptions = LayoutOptions.Center,
                    BackgroundColor = p.Paper,
                    Ink = p.Stamp,
                    Paper = p.Paper,
                    Rotation = -6
                };
                WinOverlay.Clear(); WinOverlay.Add(bigSeal); WinOverlay.IsVisible = true;
                if (!_session.State.Settings.ReduceMotion && !_session.Feedback.SystemReducedMotion)
                {
                    bigSeal.Scale = 1.5; bigSeal.Opacity = 0;
                    await Task.WhenAll(bigSeal.FadeToAsync(1, 120), bigSeal.ScaleToAsync(1, 250, Easing.SpringOut));
                }
                _session.Feedback.Impact(_session.State.Settings, true);
                await Task.Delay(550); WinOverlay.IsVisible = false;
                if (_celebration is not null)
                {
                    _celebration.IsVisible = true;
                    if (!_session.State.Settings.ReduceMotion && !_session.Feedback.SystemReducedMotion)
                    { _celebration.Scale = 1.25; _celebration.Opacity = 0; await Task.WhenAll(_celebration.FadeToAsync(1, 120), _celebration.ScaleToAsync(1, 260, Easing.SpringOut)); }
                }
                SemanticScreenReader.Default.Announce("Day cleared. You did what you said you would.");
            }
            await sync;
            // No sort or reflow while a stamp lands; preserve the scroll position on refresh.
            var y = PageScroll.ScrollY; Render(); await PageScroll.ScrollToAsync(0, y, false);
        });
    }
    private async Task UndoAsync() => await Guard(async () =>
    {
        if (_lastStamp is not { } id) return;
        await _session.ChangeAsync(s => TaskEngine.Reopen(s, id, _session.Clock.UtcNow)); _lastStamp = null;
        Render(); await _session.SyncRemindersAsync();
    });
    private async Task OpenEditorAsync()
    {
        if (_working || PaperSheet.IsOpen(this)) return;
        var revision = _session.State.Revision;
        await PaperSheet.ShowAsync(this, new TaskEditorPage(_session, _vm.SelectedDate < _session.Today ? _session.Today : _vm.SelectedDate));
        if (_session.State.Revision != revision) Render();
    }
    private async Task OpenDetailsAsync(Guid id)
    {
        if (Navigation.NavigationStack.LastOrDefault() != this) return;
        await Navigation.PushAsync(new TaskDetailsPage(_session, id), false);
    }
    private async void OnAdd(object? sender, EventArgs e) => await OpenEditorAsync();
    private async void OnToday(object? sender, EventArgs e) { if (_working) return; if (_progress || _vm.SelectedDate != _session.Today) { _progress = false; _vm.SelectedDate = _session.Today; Render(); } await PageScroll.ScrollToAsync(0, 0, false); }
    private async void OnProgress(object? sender, EventArgs e) { if (_working) return; if (!_progress) { _progress = true; Render(); } await PageScroll.ScrollToAsync(0, 0, false); }

    private async Task ChooseDayAsync()
    {
        if (_working) return;
        var choice = await PaperSheet.ChooseAsync(this, Palette.For(_session.State.Settings.Theme), "OPEN A DAY.", "Today", "Tomorrow", "Yesterday", "Choose a date…");
        if (choice is null or "Cancel") return;
        if (choice != "Choose a date…")
        {
            _vm.SelectedDate = _session.Today.AddDays(choice == "Tomorrow" ? 1 : choice == "Yesterday" ? -1 : 0);
            _progress = false; Render(); await PageScroll.ScrollToAsync(0, 0, false); return;
        }
        var p = Palette.For(_session.State.Settings.Theme);
        var selected = await PaperSheet.DateAsync(this, p, _vm.SelectedDate);
        if (selected is not { } date) return;
        _vm.SelectedDate = date; _progress = false; Render(); await PageScroll.ScrollToAsync(0, 0, false);
    }

    private async void OnMore(object? sender, EventArgs e)
    {
        if (_working) return;
        var day = _vm.Summary;
        var actions = new List<string> { "Settings", "Open another day" };
        if (!day.Confirmed && _vm.SelectedDate >= _session.Today)
        {
            if (!day.Rest && day.Commitments > 0) actions.Add($"Set {day.Commitments} commitments");
            actions.Add(day.Rest ? "Make this an active day" : "Make this a rest day");
        }
        var choice = await PaperSheet.ChooseAsync(this, Palette.For(_session.State.Settings.Theme), "YOUR DAY.", actions.ToArray());
        if (choice == "Settings") await Navigation.PushAsync(new SettingsPage(_session), false);
        else if (choice == "Open another day") await ChooseDayAsync();
        else if (choice is not null && choice.StartsWith("Set "))
            await Guard(async () => { await _session.ChangeAsync(s => TaskEngine.ConfirmDay(s, _vm.SelectedDate, _session.Clock.UtcNow)); Render(); await _session.SyncRemindersAsync(); });
        else if (choice is "Make this an active day" or "Make this a rest day")
            await Guard(async () => { await _session.ChangeAsync(s => TaskEngine.SetRest(s, _vm.SelectedDate, !day.Rest, _session.Clock.UtcNow)); Render(); await _session.SyncRemindersAsync(); });
    }

    private void RenderProgress(Palette p)
    {
        Body.Add(Ui.Heading("YOUR\nPROGRESS.", p.Text, 48));
        Body.Add(Ui.Text("See what you finish, and where your effort goes.", 14, p.Muted));
        var today = _session.Today;
        var days = Enumerable.Range(0, 7).Select(i => Statistics.Day(_session.State, today.AddDays(-6 + i))).ToArray();
        var runs = Statistics.Runs(_session.State, today);
        var planned = days.Sum(d => d.Planned); var completed = days.Sum(d => d.Completed);
        var totals = new Grid { ColumnDefinitions = [new(GridLength.Star), new(GridLength.Star), new(GridLength.Star)], ColumnSpacing = 12, Margin = new Thickness(0, 4, 0, 8) };
        View Metric(string value, string label) => new VerticalStackLayout { Spacing = 4, Children = { Ui.Heading(value, p.Text, 29), Ui.Mono(label, 9, p.Muted) } };
        totals.Add(Metric($"{completed}/{planned}", "TASKS DONE"));
        totals.Add(Metric(planned == 0 ? "—" : $"{(double)completed / planned:P0}", "COMPLETION"), 1);
        totals.Add(Metric(runs.Current.ToString(), "DAY RUN"), 2);
        Body.Add(totals);
        Body.Add(Ui.Mono("LAST 7 DAYS · INCLUDING TODAY", 10, p.Muted));
        var ledger = new Grid(); ledger.Add(new PaperSurface { PaperColor = p.Paper });
        var rows = new VerticalStackLayout { Padding = new Thickness(15, 9, 15, 12), Spacing = 0 };
        foreach (var day in days)
        {
            var row = new Grid { ColumnDefinitions = [new(new GridLength(64)), new(GridLength.Star), new(new GridLength(48))], ColumnSpacing = 10, MinimumHeightRequest = 38 };
            var date = Ui.Mono(day.Date == today ? "TODAY" : day.Date.ToString("ddd dd").ToUpperInvariant(), 10, p.Ink); date.VerticalOptions = LayoutOptions.Center;
            row.Add(date);
            row.Add(new CommitmentMeter { Count = 1, Progress = day.Progress, Ink = p.Ink, Track = p.Ink.WithAlpha(.15f), HeightRequest = 5, VerticalOptions = LayoutOptions.Center }, 1);
            var rate = Ui.Mono(day.Rest ? "REST" : !day.Confirmed ? "—" : $"{day.Progress:P0}", 10, p.Ink); rate.HorizontalTextAlignment = TextAlignment.End; rate.VerticalOptions = LayoutOptions.Center; row.Add(rate, 2);
            SemanticProperties.SetDescription(row, $"{day.Date:dddd, d MMMM}. {day.Label}. {day.StampedCommitments} of {day.Commitments} commitments. Open this day.");
            var tap = new TapGestureRecognizer();
            tap.Tapped += async (_, _) => { if (_working) return; _vm.SelectedDate = day.Date; _progress = false; Render(); await PageScroll.ScrollToAsync(0, 0, false); };
            row.GestureRecognizers.Add(tap); rows.Add(row);
        }
        ledger.Add(rows); Body.Add(ledger);
        Body.Add(Ui.Mono($"{days.Sum(d => d.Skipped)} SKIPPED   {days.Sum(d => d.Moved)} MOVED   {days.Sum(d => d.Unresolved)} UNRESOLVED", 9, p.Muted));
        Body.Add(Ui.Rule(p.Rule)); Body.Add(Ui.Mono("BY CATEGORY", 10, p.Muted));
        var categories = Statistics.Categories(_session.State, today.AddDays(-6), today);
        if (categories.Count == 0) Body.Add(Ui.Text("Your patterns will show up here as you build a record.", 14, p.Muted));
        foreach (var c in categories)
        {
            Body.Add(Ui.Row(Ui.Text(c.Name, 14, p.Text), Ui.Mono($"{c.Completed}/{c.Planned} · {c.Rate:P0}", 10, p.Text)));
            Body.Add(new CommitmentMeter { Count = 1, Progress = c.Rate, Ink = p.Accent, Track = p.Rule, HeightRequest = 5 });
        }
        Body.Add(Ui.Rule(p.Rule));
        Body.Add(Ui.Mono($"BEST RUN / {runs.Best} DAYS     TOTAL / {_session.State.Rewards.Sum(r => r.Points)} XP", 10, p.Text));
        Body.Add(Ui.Text("Tap a day to see its tasks. Draft plans enter these totals when you set your day. Moved work stays in the original day's history.", 11, p.Muted));
    }
    private async Task HandleRouteAsync()
    {
        if (_loading || _working || _routeBusy || NotificationRoute.Action is null) return;
        _routeBusy = true;
        try
        {
            var route = NotificationRoute.Consume();
            await _session.LoadAsync();
            if (route.Id is not { } id || !_session.State.Tasks.Any(t => t.Id == id))
            {
                _progress = route.Id is null; _vm.SelectedDate = _session.Today;
                await Navigation.PopToRootAsync(false); Render(); return;
            }
            await _session.ChangeAsync(s => TaskEngine.Event(s, id, _session.Clock.UtcNow, "ReminderAction", route.Action ?? "open"));
            var needsMove = route.Action == "start" && TaskEngine.Find(_session.State, id).Date != _session.Today;
            if (route.Action == "start" && !needsMove) await _session.ChangeAsync(s => TaskEngine.Start(s, id, _session.Clock.UtcNow));
            else if (route.Action == "skip") await _session.ChangeAsync(s => TaskEngine.Skip(s, id, null, _session.Clock.UtcNow));
            _vm.SelectedDate = TaskEngine.Find(_session.State, id).Date; _progress = false; Render();
            if (route.Action == "move" || needsMove) await Navigation.PushAsync(new MoveTaskPage(_session, id), false);
            else await Navigation.PushAsync(new TaskDetailsPage(_session, id), false);
            await _session.SyncRemindersAsync();
        }
        catch (Exception ex) { await PaperSheet.AlertAsync(this, Palette.For(_session.State.Settings.Theme), "Reminder", ex.Message, "OK"); }
        finally { _routeBusy = false; }
    }
}
