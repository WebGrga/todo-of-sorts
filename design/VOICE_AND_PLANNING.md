# Proposed voice entry and planning prompts

September 13 user feedback. This is a feature proposal, not functionality shipped in the panel-flicker fix.

## First increment

Add a microphone button beside task entry. Tap to start, show listening state and live text, tap to stop, then review and save using the existing task form. Recognition never creates a task until Save. Keep typing available after denied permission, cancellation, missing recognition support, or failure. Stop recording on dismissal or app backgrounding. Use the chosen recognition language rather than assuming every user speaks English.

Use platform speech recognition through a small service interface; keep recordings out of the task database. MAUI Community Toolkit supports speech-to-text on Android and iOS. Online recognition may send audio to the platform provider; explain this when enabling voice. Offline availability depends on device, language, and installed recognition support. Avoid bundling a large speech model in the APK.

This is moderate implementation work: the UI is small, but permission recovery, recognition lifecycle, device support, and noisy-environment testing matter. No backend is required for the initial platform-recognition approach.

## Planning prompts

Separate these from the existing evening review, which looks back at the completed day.

- Optional evening planning reminder, default 20:00 and user-adjustable: invite the user to add tomorrow's tasks. Tapping opens entry with tomorrow selected.
- Optional morning fallback, with its own chosen time: invite the user to add today's tasks only while that day's plan is empty.
- Define a nonempty plan as at least one active task, including generated recurring tasks. Skip rest days. Saving tasks cancels the relevant pending prompts; deleting/moving all tasks reconciles them again if the prompt time is still ahead.
- Use one prompt per kind/date with stable IDs, the existing scheduler reconciliation, quiet hours, time zone handling, and pending-request limit. Do not run an always-on background timer.
- Test late-night planning, morning entry, recurrence, day rollover/DST, time-zone changes, denied notification permission, restart, and update recovery. Displayed notifications may also need removal when a plan is added.

## Later increment

Speak several tasks naturally, detect separate tasks and proposed dates/times, then show editable draft slips with a single Add all action. This is materially more work than transcription because ambiguous phrases must not silently create incorrect reminders. Keep the original transcript available while reviewing drafts.

## References

- https://learn.microsoft.com/en-us/dotnet/communitytoolkit/maui/essentials/speech-to-text
- https://developer.apple.com/documentation/speech/sfspeechrecognizer/supportsondevicerecognition
