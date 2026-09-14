# App audit — September 13, 2026

The initial product-quality rating was **6.5/10**: a distinctive stamp interaction, but uneven supporting flows and avoidable performance work. After the repair and verification passes, my rating is **9/10 for the exercised Android beta experience**. Ratings are a design/engineering judgment, not a guarantee of defect-free behavior. A cross-platform 10/10 would require physical-phone and iPhone evidence that this Windows audit cannot supply.

## Findings and changes

| Area | Initial rating | Finding | Change |
|---|---:|---|---|
| Execution and progress | 7.5 | Bonus-only days could not start/complete; future cards promised a stamp that could not happen | Bonuses can execute without inventing a day win; future actions say View task; empty/bonus counts are explicit |
| Adding and editing | 7 | Repeat changes could silently do nothing; weekday routines reopened as Custom; saves waited for notification maintenance | Repeat changes update the schedule explicitly; weekday selection restores correctly; durable save dismisses promptly, scheduling continues |
| Navigation and visual consistency | 6 | Native headers had pale status strips in dark mode; page crossfades overlapped text; native picker confirmation was unreadable at night | Custom headers, matching night resources, immediate navigation, unchanged pages retained, live theme recoloring, readable native date/time actions |
| Settings | 6 | Save was buried; theme used a native picker; category/time-zone labels became stale | Fixed Save button; app-styled theme choices; optional time controls; labels refresh; actual build number shown |
| Task actions | 6 | Rare corrections dominated the screen; future Start failed; old skip reasons survived corrections | Main actions simplified, corrections/history expandable, invalid future Start hidden, stale reasons cleared |
| Reliability | 6 | Stopping a routine could delete a manually moved destination and break history | Linked moved tasks preserved; regression test; stricter backup validation; repeated Start is idempotent |
| Speed with history | 5 | Repeated full-history scans, duplicate projections, repeated JSON snapshots | Indexed streak/reward calculations, cached home projections, compact storage, fewer serialized copies |
| Startup | 5 | JIT-only package cold-started slowly on the emulator | Profiled AOT for common startup paths; modest package-size increase for a large measured speed gain |
| Reminders | 7 | Saving waited for scheduler work; errors could be invisible; overnight Start led to an error; Start from another details page left the wrong task visible | Scheduling follows durable saves; errors update on Today; past-day Start offers Move; routes explicitly open the correct destination; evening review opens Progress |
| Accessibility | 6 | Fixed action-row height could clip enlarged text; small native headers differed from the app | Flexible task-action rows, wrapping buttons, larger-text inspection, consistent accessible Back and choice labels |
| iPhone/build tools | — | Missing UIKit import broke compilation; a framework override also overrode shared projects | Import fixed; app-specific MobileTarget property isolates the iPhone restore/build selection |

## Performance evidence

Same Windows PC, Release, three timed samples after warm-up. The synthetic fixture contains 2,920 tasks across 365 days, with 2,434 rewards. It is isolated from app data. Reproduce with `dotnet run --project tests/ToDoOfSorts.Performance -c Release`.

| Operation | Before (median) | After (median) |
|---|---:|---:|
| Run/streak calculation | 114.34 ms | 0.64 ms |
| Backup/state validation | 77.23 ms | 8.67 ms |
| CSV export | 110.47 ms | 5.94 ms |
| Reconciled SQLite read | 148.63 ms | 65.34 ms |

Android API 36 emulator, same existing task data: the JIT package took 7.003 s after install and 8.685 s on a cold launch; the profiled package took 2.238 s after install and 2.312 s cold. These are emulator observations, not promised physical-phone timings. The profiled package was approximately 32 MB, compared with approximately 25 MB JIT and the original 87 MB Debug installer.

## Verification

- 37 domain/storage tests pass, including new coverage for bonus-only execution, idempotent Start, repeat edits, linked history preservation, gap/rest streak calculations, stale skip reasons, and malformed backups.
- Managed iPhone compilation succeeds with zero warnings/errors. Apple linking, signing, rendering, and physical-device behavior are not established by that check.
- Android API 36: upgrade preserves task history; compact entry saves a scheduled task; keyboard leaves Save reachable; typed text survives a light/dark switch; Calm preference saves and updates the stamp; native time-picker confirmation is legible in both modes.
- Start, intentional Skip with a reason, Reopen, Stamp, Day cleared, and Undo were exercised. Progress correctly recalculated after Undo. Settings were inspected at normal and 150% system text.
- A scheduled task notification arrived with the app backgrounded, with Start/Move/Skip actions. Its initial Start routing exposed and led to a fix for opening from another task's details page.
- The corrected notification Start route was retested with another task's details open before backgrounding: the intended task opened in IN PROGRESS state. Android delivered the second inexact reminder about a minute after its requested time, within its OS scheduling window.
- Move to tomorrow, opening the future card, changing Once to Weekdays, reopening the saved Weekdays choice, and stopping the routine were exercised. The manually moved task remained after stopping repeats.
- The final 200% text check shows complete bottom-tab labels with Add Task wrapping; task cards remain readable. Paper switches have visible off-state thumbs. Action text uses contrasting colors for the desk surface separately from ink on paper.
- Three task-sheet open/close cycles were recorded on the final build. All 89 decoded frames stayed below the bright-header threshold (maximum mean luma 65.60 versus threshold 150); visual contact-sheet inspection confirms the panels appear without a full-screen white flash. This is transition verification, not a physical-phone frame-rate measurement.
- Android Release and managed iPhone compilation completed with zero warnings/errors. No Android crash-buffer entries were present after the exercised flows.
- Final audited APK: **0.1.0 (53608556)**, **32,126,620 bytes**, ARM64/x64. Its signature was verified and an update installation preserved existing local data. Build products and private distribution details are intentionally excluded from this source repository.

## Current assessment

| Area | Rating after changes | Remaining evidence or improvement |
|---|---:|---|
| Daily execution / stamp | 9 | Physical haptic and sound feel is subjective and device-dependent |
| Task entry / editing | 9 | Voice entry remains a separate planned feature |
| Navigation / consistency | 9 | Further narrow-screen and landscape device coverage |
| Settings / accessibility | 8.5 | Real TalkBack/VoiceOver and more device sizes |
| History / recurrence / data | 9 | Longer real-day usage and on-device backup round trips |
| Performance | 9 | Actual phone launch, battery and sustained-use measurements |
| Reminders | 8.5 | Lock-screen, reboot and manufacturer battery restrictions on real phones |
| iPhone runtime | Unrated | Mac build/signing and physical-device verification |

## Scope and follow-up

Voice entry and planning prompts remain in `design/VOICE_AND_PLANNING.md`; this audit improves the existing beta rather than adding that separate feature set.

Physical haptic quality, battery/lock-screen reminder behavior across manufacturers, VoiceOver/TalkBack use by a person, and the iPhone runtime still require real-device evidence. These are limits on any claim of universal perfection, even when the locally exercised flows pass.
