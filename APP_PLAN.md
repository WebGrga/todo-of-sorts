# ToDoOfSorts — V1 product and build plan

Status: approved product direction. The first native beta is implemented; see README.md and TESTING.md for delivered scope, build steps, and verified behavior.
Date: 11 September 2026.

## 1. Product promise

**Make following through feel immediately rewarding, and let the user end the day knowing they did what mattered.**

The core loop is: choose a small commitment → act → stamp it DONE → feel the impact → see the marked paper in your day's record → choose the next action.

The signature interaction is a physical-feeling stamp. The owner explicitly rejected the checkbox-led concept: completion must feel emphatic, like slamming a large ink stamp onto paper or receiving a major game-completion payoff. This replaces the earlier checkbox preview as the product direction.

Task entry supports that loop. The product's distinguishing features are the quality of completion, a satisfying finish to the day, useful reminders, and honest history. We are designing for satisfaction and a sense of accomplishment; whether it improves execution is a hypothesis to test with actual use.

Owner decisions: support both iPhone and Android; iPhone is the primary personal device, with an older Android available for additional testing. Mac access is confirmed. Prioritize broad Android compatibility using supported platform defaults; identifying the owner's Android OS version is not a planning prerequisite. Start with energetic, game-like feedback. Design for three experience themes: Energetic, Tactile, and Calm. No accounts, subscription system, cloud dependency, or AI in V1.

## 2. What counts as winning a day

Use two clearly named groups: **My commitments** and **Bonus**.

- Suggest 1–3 commitments each day. These are the tasks the user wants to be able to say they followed through on. Allow more without a blocking limit.
- Other tasks can remain visible as bonuses. They earn XP but do not move the daily finish line.
- Before the first action, a compact `Set today` action confirms the commitments. Recurring tasks prefill suggestions; planning should usually take under a minute.
- A **day won** means completing every confirmed commitment within that day. Completing one commitment is still visibly celebrated.
- A **full clear** means all commitments and all bonuses scheduled for the day were completed. It earns a richer visual finish, with no additional currency or second streak.
- Skipping or moving a commitment after confirmation leaves that original day's commitment unmet. The user can do this freely; the record explains the outcome without scolding.
- Adding a task after confirmation defaults to Bonus. The daily finish line stays stable.
- Allow explicit corrections to an accidentally confirmed plan, with an audit record and recalculation. This is a personal tool, so do not build an anti-cheating system.
- Removing every commitment makes a day unplanned, never a win. A rest day must be selected before confirming commitments; it does not retroactively erase an unsuccessful day.

This is an intentional refinement of a single undifferentiated list. Test whether the distinction feels useful or adds friction. If it does not help, simplify to one committed list before release.

**Progress uses task count. XP measures effort separately.** Two of three commitments is 67%, whatever their point values. Bonus completion cannot disguise an unfinished commitment. The UI must never show an unexplained mismatch between a task fraction and its progress bar.

## 3. The reward experience

The primary reward is the act of **stamping the task finished**, followed by visible evidence that stays. Each task is a paper slip; completed slips retain a large, slightly angled ink impression. The day's record becomes a collection of things decisively finished. XP supports this experience and must not dominate it.

### Signature sequence: press → slam → imprint → settle

1. **Press:** the large `Stamp it` area responds immediately; the paper sinks slightly and the stamp readies above it. A normal press/release is sufficient—no compulsory long hold, repeated tapping, or physical pressure sensing.
2. **Slam:** on activation and successful persistence, an oversized DONE stamp drops rapidly toward the paper. The final contact is synchronized with a heavy haptic and an optional layered thud/ink snap.
3. **Impact:** the paper compresses, the surrounding surface gives one short jolt, and a few ink flecks kick outward. Use a brief scale overshoot and moment of visual stillness to give the impact weight. Do not use strobing or repeated screen flashes.
4. **Imprint:** the stamp settles into a large permanent mark on the slip. The task title remains readable. XP kicks outward from the impact, then the day progress advances.
5. **Evidence:** the slip remains stamped in the day's record. On reopening, render its completed state immediately without replaying the impact.

The interaction needs anticipation, contact, and a lasting mark. Merely swapping a checkbox icon for a tiny stamp fails this direction. Prototype the timing and sound/haptic synchronization on actual phones before expanding features.

| Moment | Proposed response | Lasting evidence |
| --- | --- | --- |
| Any completion | Oversized DONE stamp slams onto the paper slip; contact haptic, optional thud/snap, paper compression and ink flecks | Large ink mark stays on the slip; XP total updates |
| Commitment completed | Progress punches forward immediately after stamp contact | Commitment count advances; stamped work remains visible |
| Last commitment completed | Final task stamp leads into a larger DAY CLEARED seal across the day's summary; fuller impact and celebratory sound finish | Day marked Won, streak advances, stamped task names remain available |
| Full clear | EVERYTHING. DONE. treatment, combined with DAY CLEARED when simultaneous | Finished daily sheet retained in history |
| Streak milestone | Distinct but short treatment at 3, 7, 14, and 30 wins in a run | Milestone entry in history |

Timing targets for the first native prototype, not claims of measured performance:

- Press response in the next rendered frame, with perceived latency below 100 ms.
- Press preparation responds immediately; stamp approach takes roughly 100–160 ms after activation; contact compression lasts roughly 60–90 ms; ink settles over 180–280 ms. Tune the combined task sequence to roughly 450–650 ms.
- Day-win sequence lasts around 1.0–1.4 seconds, escalates from the final task impact, and never blocks the next action. One coordinated sequence replaces stacked reward dialogs.
- Preparation can respond before saving; the successful impact/imprint and award happen only after the local completion transaction succeeds. A failed save must release the pressed state and offer retry without stamping success.
- Keep completed slips in place until interaction settles; any later grouping must not unexpectedly move the next tap target. Do not grey finished work into visual insignificance.
- A visible Undo action reverses completion, XP, progress, and streak consequences together. Reopening the task remains possible after the Undo affordance disappears.
- Show the major day celebration at most once per day; undo and re-complete must not repeatedly trigger it. Recalculate the actual day status normally.
- Completing from a notification persists the same result. On next open, show an accurate compact recap rather than replaying a burst of old celebrations.
- If a late correction removes a win, recompute the run and mark any affected milestone record as corrected. Do not show a punitive failure animation.

Feedback settings: haptics on by default; sound opt-in. Prototype Energetic first: exaggerated stamp approach, heavy impact, short surface jolt, ink burst, large XP kick, and DAY CLEARED seal. Tactile uses a realistic rubber-stamp impression with a dry paper/wood sound and a tighter impact. Calm uses a gentle ink press with no surface jolt or flecks. All three retain the signature stamping action and visible completed slips. Themes control palette, motion timings, effect density, haptic pattern, sound assets, and copy tone; they never change XP, progress, or streak rules. Use a feedback profile so all three share one implementation. Respect reduced motion and platform sound settings. Reduced motion applies the clear stamp immediately with a static completion announcement; the result must remain satisfying and legible without sound, vibration, or animation.

Avoid variable rewards, random chests, point penalties, reward multipliers, purchasable streak repairs, and endless prompts to do more after a win. The desired ending is permission to feel finished.

## 4. Screens and everyday flows

Two main destinations: **Today** and **Progress**. A small settings button and a prominent add action are sufficient.

### Today

1. Date and current run.
2. Dominant “Today's commitments — 2 of 3” indicator.
3. Pending commitments as paper slips with a generous Stamp it action and time/category/effort shown only when present.
4. A quieter Bonus section.
5. Completed slips with permanent ink stamps, plus earned XP. Day history preserves the same visual vocabulary.

Each slip has a clearly labeled, generous Stamp it action, distinct from its title/details action. No checkbox is the primary completion control. Secondary actions are Start, Move, Skip, and Edit. Keep a visible Undo affordance after impact; tapping the completed stamp does not silently toggle the task back to unfinished. Screen-reader and keyboard activation use the same completion command without requiring a gesture. Swipe shortcuts may come later, but essential actions must always have a visible accessible alternative.

Start marks the occurrence In progress. It does not earn XP. A task may also go directly from Planned to Completed. V1 does not require a timer or proof of work.

Empty state: “What would make today feel worthwhile?” with Add a commitment. A completed day receives the DAY CLEARED seal and settles into a finished daily sheet with all the stamped task names. The user can enjoy the completed work rather than being pushed immediately to add more.

### Add/edit task sheet

Required: title. Everything else uses defaults or is optional:

- Date, default today; specific time optional.
- Commitment/Bonus, subject to the confirmed-plan rules.
- Category: Training, Career, Learning, Home, Personal, or Uncategorized. Basic rename/add support.
- Effort: Quick = 10 XP, Standard = 20 XP, Deep = 40 XP. Default Standard. These are self-assessed effort labels, not objective productivity measurements.
- Reminder when a time is set; explicit reminder toggle.
- Repeat: none, daily, or selected weekdays.

Store effort and category snapshots on each occurrence so editing tomorrow's template does not rewrite last week's history. Earned XP uses the saved occurrence value. Routine template edits affect future occurrences; a past occurrence requires an explicit correction.

### Move and Skip

Move offers Later today, Tomorrow, and Pick date/time. A move within the day preserves the same daily commitment and records the old/new time. A cross-day move creates a linked destination occurrence and records the source as Rescheduled.

The destination is included when its day is confirmed; if that day is already confirmed it defaults to Bonus. The source day's outcome remains visible. Moving a draft before confirmation is a plan edit, not a failed commitment.

Skip is an intentional outcome and earns no XP. An optional reason can be selected or omitted. Start with “No longer needed,” “Not enough time,” “Too much today,” and “Other.” Never require a justification.

### Day review

Show a brief review on request and on the next visit after rollover: completed, intentionally skipped, moved to another day, and unresolved. Offer explicit ways to move unresolved work; do not silently carry an accumulating backlog into Today.

### Progress

- Seven-day strip: won, partial, rest, or unplanned, with labels as well as color.
- Commitment completion and all-planned-task completion, with names that distinguish the measures.
- Completed / skipped / rescheduled / unresolved counts.
- Category completion bars with numerator and denominator.
- Current run, best run, and won days in the past 30 days.
- Readable day history, including plan amendments.

Use “Needs attention” instead of “Weakest category.” Sparse data should look sparse, not produce impressive but misleading percentages.

## 5. XP, streaks, dates, and statistics

### XP

XP = sum of non-reversed completion awards. No points for adding, starting, moving, or skipping tasks. An occurrence can have only one active completion award. A bonus earns the same points as an equally demanding commitment.

V1 displays today's XP and total earned XP. Levels, shops, cosmetic unlocks, and broad badge collections are deferred until XP itself proves useful. The user's historical work is the persistent reward.

### Streak

A run counts consecutive successful planned days. Rest days pause the run without adding to it; the UI labels this explicitly, for example “4-day run · rest yesterday.” An unplanned non-rest day or a planned day with unfinished commitments breaks the run at day close. Today does not break a run while it is still in progress.

Keep best run and total days won after a break. No guilt notifications or red failure screens. One successful day gives a clean return point.

### Dates and corrections

Use a local midnight boundary in V1, with an explicit planning time zone stored in settings, defaulting to the device zone at onboarding. Do not silently reinterpret past days after travel. Changing the planning zone affects future schedules; existing day snapshots preserve their zone and boundary instants.

Recurrences follow local wall-clock time in the planning zone. For a daylight-saving gap, use the next valid local time; for a repeated time, issue one occurrence at the first instance. Keep these policies centralized and tested.

If the app was closed at midnight, reconcile elapsed days on next launch from stored timestamps; do not depend on a background midnight job. A user can explicitly correct an earlier completion date/time in day history; record the correction, transfer attribution, and recalculate affected days. Do not silently backdate a late completion to rescue a streak.

### Metric definitions

- Daily commitment progress = commitments completed within their attributed day / confirmed commitments, after explicit audited corrections. Zero commitments displays no percentage.
- All-planned completion = scheduled occurrences completed in their attributed day / occurrences planned for that day. Include bonuses and retain skipped, cross-day-rescheduled, and unresolved source occurrences in the denominator. Exclude unconfirmed draft tasks.
- A linked reschedule creates two scheduling attempts across two dates but represents one piece of work. Label attempt-based reporting “planned attempts”; count completed work only once, at the completed destination. Do not count same-day moves as extra planned attempts.
- Weekly rates use summed numerators and denominators, not an unweighted average of daily percentages. Default comparisons use closed days; show Today separately as provisional.
- Category rates use the same denominator and the occurrence's saved category.
- Time-pattern reports group by originally confirmed scheduled time, preserve subsequent moves, and exclude untimed tasks from those comparisons. Completion time is a separate dimension.
- “Unresolved” means no terminal task outcome at day close. “No response recorded” is a notification interaction fact. Neither proves that a person saw and ignored an alert.

After at least 14 closed days, optionally surface a deterministic observation only if each compared group has at least 10 scheduled attempts. This threshold is a conservative product heuristic, not statistical significance. Always show sample sizes and avoid causal claims. The richer before-noon/after-evening insights are V1.1 if they delay the core loop.

## 6. Reminder system

Reminders are a release requirement. Build a native proof of delivery and actions before expanding the dashboard.

| Reminder | Default behavior | Actions |
| --- | --- | --- |
| Scheduled task | One alert at the chosen time; title and a short useful prompt | Start, Move, Skip; notification tap opens task |
| Evening review | Optional single prompt at a user-selected time | Review today |
| Follow-up | Only when explicitly requested through Snooze/Move | Open task |

Start opens the task and marks it In progress. Move opens the time picker; Skip may execute directly if supported and must also be undoable from history. Use platform-supported action counts/layouts; preserve access to every action inside the app.

Do not add automatic repeated nagging in V1. Batch simultaneous task alerts into one summary where practical, with access to the individual tasks. Allow quiet hours; show when a chosen reminder falls inside them and queue it for quiet-hours end unless explicitly overridden. Recheck completion before issuing a queued alert when execution is available.

Permission onboarding happens when the user first requests a reminder. If access is denied or revoked, show a clear reminders-off state and a settings route while keeping all other features usable.

### Platform constraints that affect the design

- MAUI provides a shared application layer, but local notifications require platform implementations behind a common interface. [Microsoft local notification guidance](https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/local-notifications?view=net-maui-10.0)
- Android 13+ requires notification permission for ordinary notifications. Android exact alarm access is a separate capability; use the permission appropriate to the app's validated use case and store requirements, and fall back transparently when precision is unavailable. [Android notification permission](https://developer.android.com/develop/ui/views/notifications/notification-permission), [Android alarm scheduling](https://developer.android.com/develop/background-work/services/alarms)
- iOS can deliver already scheduled local alerts while the app is not running. However, background refresh executes when the OS chooses, so V1 cannot promise freshly computed behavioral messages at an exact moment. [Apple local scheduling](https://developer.apple.com/documentation/usernotifications/scheduling-a-notification-locally-from-your-app), [Apple background strategies](https://developer.apple.com/documentation/backgroundtasks/choosing-background-strategies-for-your-app)

Consequently, pre-schedule stable task messages and a generic optional evening message. Exact “two tasks left / reach 80%” messages are used in-app; use them in notifications only where a fresh, reliable calculation is available. Rebuild pending requests after local state changes. Do not ship static numeric claims that can become stale.

The scheduler uses stable IDs, cancels obsolete requests after completion/move/skip, and reconciles database intent against OS pending requests. A durable outbox makes failed platform calls retryable without losing the user's task update.

Schedule a rolling window within verified platform limits, prioritizing the nearest reminders, and refill whenever the app runs. Validate native repeating schedules for unchanged daily/weekday tasks so a long absence does not simply exhaust one-shot requests. Record the tested horizon and limitations before release; do not assume an unlimited pending queue or guaranteed refill in the background.

Test device restart, app termination, Android force-stop as a distinct condition, battery saving, revoked permission, time/zone changes, daylight saving, notification actions, and multiple rapid edits. Document OS-imposed gaps without claiming guaranteed delivery or visibility. On supported Android paths, restore schedules after reboot using the database.

## 7. Data and C# architecture

The four proposed product concepts are useful for the interface. Storage needs to distinguish a reusable task from one day's attempt and preserve events, otherwise recurrence and rescheduling will make the stats unreliable.

| Object | Responsibility |
| --- | --- |
| TaskDefinition | Reusable title, category, effort defaults, optional recurrence, archive state |
| TaskOccurrence | One dated scheduling attempt; definition link; title/category/XP snapshots; original/current time; commitment membership; status; linked reschedule IDs |
| DayPlan | Local date, planning zone, UTC boundaries, confirmation time, rest flag, confirmed commitment membership and amendments |
| TaskEvent | Append-only fact: added, confirmed, started, completed, skipped, moved, reopened, corrected; timestamp; source; before/after details; command ID |
| RewardEntry | Completion award/reversal, occurrence link, points, stable deduplication key |
| NotificationRequest | Stable platform ID, occurrence/action link, intended schedule, payload version, pending/cancel state, last scheduler result |
| Settings | Feedback, reminders, quiet hours, categories, planning zone |
| DaySummary / Streak / Statistics | Rebuildable projections from stored occurrences/events, never independently edited truth |

Occurrence terminal outcomes: Completed, Skipped, Rescheduled, Unresolved. Planned and InProgress are active states. Unresolved is assigned on day reconciliation; reopening/correction is explicit and logged. Reminder interaction facts belong to notification/event records, not the task outcome enum.

One SQLite transaction updates occurrence, event, reward, and notification outbox intent. UI feedback and OS scheduling happen after commit. Use unique recurrence keys and command IDs so retries and rapid taps cannot create duplicate occurrences or rewards.

Suggested solution structure:

```text
ToDoOfSorts.App           MAUI XAML views, view models, navigation, native adapters
ToDoOfSorts.Core          C# domain rules, commands, projections, interfaces
ToDoOfSorts.Infrastructure SQLite repositories, migrations, scheduling outbox
ToDoOfSorts.Tests         Domain, persistence, notification-planning tests
```

Use .NET 10 / .NET MAUI 10 at the current supported servicing level, C#, XAML, MVVM, dependency injection, and SQLite. Choose a broadly compatible Android minimum version supported by the selected MAUI release and dependencies during scaffolding, independently of the owner's particular handset. Target the current required Android SDK while guarding newer capabilities and providing fallbacks on older supported versions. Choose and pin SQLite/MVVM packages during scaffolding after checking current compatibility. Avoid a large framework stack for this small app. MAUI has its own support lifecycle, so budget routine updates. [Microsoft MAUI support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/maui)

Core interfaces: IClock, ITaskRepository, IUnitOfWork, INotificationScheduler, IFeedbackService. Services: CompleteTask, MoveTask, SkipTask, ConfirmDay, ReconcileDays, GenerateOccurrences, BuildStatistics. Keep score/streak rules independent of XAML and platform APIs.

Use native animation/haptic/audio capabilities behind IFeedbackService. Build a reusable CompletionStampView and PaperTaskView, with a coordinator for preparation, persistence, approach, contact, imprint, and reward propagation. Use an explicit motion timeline to synchronize sound and haptic to stamp contact. Save a deterministic stamp variant/angle with the completion presentation state so history does not randomly change on each render. Start with MAUI transforms and GraphicsView for the ink/paper treatment; evaluate additional graphics dependencies only if device measurements justify them. The stamp, haptic, and sound work is a core feature with its own prototype milestone.

Windows can host Android development. Mac access for iOS development is confirmed by the owner. Configure the required Apple tooling, signing, and device deployment during setup, then include iPhone testing in Stage 1 alongside Android. Both platforms remain release targets. [Microsoft supported platforms](https://learn.microsoft.com/en-us/dotnet/maui/supported-platforms?view=net-maui-10.0)

V1 is offline and single-device. Include versioned local JSON export/import with validation and a preview before replacing data; provide CSV export for human-readable history. Use the system share/file picker. No remote analytics by default. Local-only storage still needs a usable backup path and tested database migrations.

## 8. Scope boundaries

**V1:** Today, fast task entry, commitments/bonuses, simple repeat rules, complete/undo/start/move/skip, reliable scheduled reminders and permission states, optional review reminder, carefully tuned feedback, day wins, run history, basic weekly/category statistics, feedback/accessibility settings, local backup.

**V1.1:** Richer time-pattern observations, focus timer if users want it, widgets, easier routine setup, a small milestone collection, improved backup convenience.

**Later:** Accounts and sync, multi-device conflict resolution, AI suggestions grounded in sufficient history, calendar integrations, complex projects/subtasks, social features, and monetization.

No AI placeholder chat screen. First validate whether the core feedback makes someone follow through more often and feel better about what they did.

## 9. Build sequence and exit criteria

| Stage | Deliverable | Exit criterion |
| --- | --- | --- |
| 0. Build readiness | Configure MAUI and available Mac tooling/signing; select broadly compatible supported platform targets; review commitment rules | Both platform builds configured; Energetic prototype direction established |
| 1. Native risk prototype | Three paper task slips, persisted stamp/undo, synchronized Energetic ink impact and haptic/sound, DAY CLEARED seal, one actionable reminder | Stamping feels emphatic and satisfying on iPhone and Android; reminder/action works under tested app lifecycle states |
| 2. Daily execution | SQLite schema/migrations, add/edit, day confirmation, all task actions, day-win logic, rollover | Complete a day, restart the app, and see exactly the same correct state |
| 3. Reminder and recurrence hardening | Repeat generation, cancellation/reconciliation, outbox retries, quiet hours and permission recovery | Device matrix passes; no stale reminders after successful cancellation and reconciliation |
| 4. Evidence and polish | Weekly/category views, run history, review, Tactile and Calm profiles, accessibility, export/import, performance tuning | Stats reconcile to history, themes preserve identical rules, and backup restores accurately |
| 5. Personal beta | At least two weeks of normal use | Evidence supports keeping the reward loop, or identified changes are made before broadening scope |

Do not put calendar views or AI ahead of the native reward/reminder prototype. The largest uncertainties are how the interaction feels on a real phone and how reminders behave under OS constraints. Estimate calendar delivery only after Stage 1 and confirmation of team capacity and iOS access.

## 10. Verification and product validation

Automated tests must cover meaningful domain boundaries: double-completion deduplication; undo/re-complete; all/zero commitments; bonus independence; skipped and cross-day moves; recurrence uniqueness; inactive-day reconciliation; rest and broken runs; midnight/DST/zone changes; plan corrections; weighted weekly totals; history preservation; outbox retries; migration and backup restoration.

Physical-device checks cover feedback latency, rapid tapping, long titles, large text, screen readers, reduced motion, silent mode, task actions from notifications, cold starts, restart/force-stop distinctions, revoked permissions, delayed alarms, and stale-request cancellation. Test each supported platform before claiming support. Simulators cannot establish whether haptics feel right.

The beta measures real-world behavior, not app opens or XP inflation:

- Weekly completed commitments and completion rate, shown together so fewer planned tasks cannot masquerade as more execution.
- Won days, intentional skips, and moves per planned attempt.
- Time from opening Today to taking an action.
- Reminder usefulness and annoyance reported by the user; recorded interaction alone does not prove delivery or causation.
- A lightweight, occasional question: “Did finishing today feel satisfying?”

Use the first few days as a baseline and compare subsequent use cautiously; a small personal beta cannot establish causal effectiveness. If possible, compare Energetic, Tactile, and Calm feedback without changing task rules. Let the user keep the theme that encourages execution and a satisfying stopping point.

Proposed acceptance targets to validate, not research-backed guarantees: task entry usually under 15 seconds; daily commitment setup usually under one minute; reliable persistence across restart; no duplicated XP; no misleading reminder counts; completion feedback that the owner actively prefers using after two weeks.

## 11. Immediate next build

Build Stage 1 first: one native Today screen with three commitment slips, one bonus slip, persisted stamp/undo, task-count progress, XP, and the energetic DAY CLEARED seal. The oversized stamp, permanent ink, and synchronized physical feedback are the centerpiece of this milestone. Pair it with a scheduled local reminder and working task actions on both physical devices, prioritizing the iPhone experience.

That gives us something concrete to judge before investing in more screens: **does completing an ordinary task feel meaningfully better, and does the app reliably bring the user back to the action?**
