# Beta verification and phone test guide

## Verified during this build

- September 13 panel-flash fix, build 53607017: paper panels receive a bounded height before first layout, and overlays attach to permanent page grids without detaching/recreating the page. Android screen recordings reproduced three full-screen paper flashes before the sizing fix; the final three-cycle recording contained none across 95 captured frames. Header-region peak average luma fell from 204.773 to 65.6021 on the same dark-mode scene. Large local recordings and extracted frames are intentionally excluded from the source repository.
- This fix built with zero warnings/errors. Emulator checks verified repeated Android-back dismissal, restored underlying controls, the three-dot menu, editing from task details, and Add Task staying above the keyboard. Same signing certificate and preserved task history verified. APK: 24,334,223 bytes. No task/reminder rules changed; voice entry and planning prompts remain proposals in `design/VOICE_AND_PLANNING.md`.

- September 13: compact Release APK installed over the previous emulator app with its SQLite history intact. New tasks saved and stamped successfully; all 28 regression tests passed again.
- Android API 36 screenshots verified compact task entry, paper input contrast in light/dark mode, a fixed save button above the keyboard using MAUI's resize setting, scrollable optional fields, custom paper menus/confirmation, and added clearance below DONE impressions. These checks use the emulator; physical-phone and iPhone rendering remain to be tested.
- Remote distribution now builds Release with partial trimming/compression and AOT disabled. The installer dropped from 86,779,612 bytes to about 25 MB while retaining ARM64/x64 support and the existing signing key.
- Build 53605352 final APK: 24,668,969 bytes. Verified category edits save and appear in task history; Progress shows today's 50% alongside earlier days. Final option chips show complete labels. Signature verified against the previously distributed APK's certificate before upload.

- September 11 visual correction: native Android screenshots checked for pending/completed slips, unclipped condensed headings, monospaced metadata, torn paper edges, the editor, and the persistent day-cleared seal. A subsequent screenshot verified the compact weekly ledger opens at its top.
- The development sample was reopened and stamped again: its final status returned to Completed, total XP returned to 40, and the run returned to one day. Task details remain accessible by tapping the paper heading. The emulator's existing tasks were preserved across reinstallations.
- The revised Android package and iPhone managed compilation both completed with zero warnings/errors; all 28 existing regression tests passed. The redesigned iPhone UI still needs rendering and testing on Apple hardware.

- 28 automated tests pass: duplicate completion; undo/re-complete; bonuses; count-based progress; empty plans; same-day and cross-day moves; explicit skips; day rollover; recurrence generation/catch-up; routine archiving; rest and broken runs; plan corrections; quiet hours including midnight; reminder cancellation and queue bounds; DST gap/overlap behavior; historical completion correction; backup validation/round-trip; CSV escaping; historical snapshots; SQLite restart persistence; rollback; concurrent writes; scheduler revision acknowledgement; import replacement.
- Android native debug APK compiled successfully. Last full packaging check: zero warnings and zero errors.
- iPhone target managed compilation on Windows succeeded with zero warnings and zero errors. This does not validate native Apple linking, signing, deployment, or iPhone runtime behavior.
- Android 15 / API 35 emulator: clean first launch, task creation, automatic commitment confirmation on first stamp, completed paper impression, 20 XP award, day-win state, and state preservation across a full app force-stop/relaunch.
- Emulator notification permission prompt appeared at the first requested reminder.
- A task reminder scheduled for 12:50 was delivered by Android while the app was in the background. The notification contained Start, Move, and Skip actions.
- The notification's Skip action opened task details, changed the outcome to Skipped, and recorded both ReminderAction and Skipped events.
- Visual inspection led to a shorter editor heading, a fixed Save action, corrected stamp typography, matching system-bar colors, and a celebration overlay visible independently of scroll position. Final editor and completed-task screens were inspected in the emulator.
- The shortened editor also saved a new bonus task directly from the keyboard's Done action.
- No AndroidRuntime crash was reported during those exercised flows.

Test tasks live only in the isolated development emulator. The installable APK starts with an empty database on a new phone.

## Still needs physical-phone testing

September 13 audit follow-up: see `APP_AUDIT.md` for the before/after ratings and measured results. Build 53608556 uses profiled AOT (superseding the earlier JIT-only packaging note): 32,126,620-byte installer, roughly 2.3-second measured emulator cold launch. The 37 domain/storage tests pass; Android Release and managed iPhone compilation have zero warnings/errors.

Additional emulator checks passed: intentional skip/reopen, stamp/day-clear/undo with recalculated Progress, background notification delivery and Start from another task's details, Move to tomorrow, repeat edit persistence, stopping repeats while retaining a moved task, keyboard save visibility, light/dark draft preservation, native picker contrast, 150% settings and 200% home/navigation text. Three panel cycles on the final build produced no full-screen bright flash in 89 decoded frames. Final APK signature and upgrade persistence were verified. These checks supplement, and do not replace, the physical-phone cases below.

1. **First impression:** create three commitments, enable sound, and stamp them. Judge whether impact feels immediate and whether the final seal feels like an earned finish. Compare all three profiles.
2. **Persistence:** stamp a task, close/reopen the app, then undo through task details. XP and the win must reverse together. Stamp again and confirm there is only one net award.
3. **Reminders:** test foreground, background, locked phone, OS notification settings, battery-saving mode, device restart, and precision access on Android. Verify Start and Move actions as well as Skip.
4. **Daily use:** set a plan, add a bonus, move a commitment to tomorrow, skip another, and review that day. The original attempts must remain visible. Try an actual midnight and a recurring routine over several days.
5. **Accessibility:** larger system text, screen reader, reduced motion, silent mode, and narrow displays. Task details and stamp actions should remain reachable.
6. **Backups:** export JSON, save it outside the app, preview a restore, then restore. Export CSV and inspect it in a spreadsheet. Native share/file-picker flows still need platform testing.
7. **iPhone:** build and sign on the Mac, install on the physical iPhone, and run every check above. Haptic quality and Apple notification behavior cannot be established by the Windows compilation check or Android emulator.

## Explicit beta limits

- Each phone has independent local storage. There is no live sync or account.
- Reminders have a rolling 14-day / 60-pending-request horizon, refreshed when the app opens. Indefinite native repeating alerts without reopening the app remain future work.
- Only daily/selected-weekday routines are implemented; no monthly recurrence, projects, timers, widgets, AI, or calendar integration.
- Current/best runs and milestone copy are included. A separate collectible badge gallery is not included.
- Quiet-hour deferrals and platform restrictions mean this is not an alarm-clock-grade delivery guarantee.
- JSON restore keeps the previous SQLite state as a recovery row. A user-facing recovery-browser screen is not yet included; keep exported backups.
- Android packaging is debug-signed for personal testing. iPhone packaging/signing and any store distribution must be completed with the owner's Apple tooling and credentials.

## Reporting a problem

Record the phone/OS, the action you took, what you expected, and what happened. For timing or streak issues, include the planning time zone and task date/time. Export a backup before reinstalling so your history can be preserved.
