# Task 04 design: timestamped cardinal gestures

Implements task 04 and section 3.2 of the supplied development plan. Preserve Unity 6000.6.0f1, the Task 02 package graph, the corrected root, the alternate project, and Task 03 application behavior. No commits or pushes.

## Gesture rules

- Four cardinal directions. Commit on the first sample whose travel strictly exceeds 0.06 of the screen's shorter dimension, measured in physical screen proportions from normalized coordinates captured at Begin.
- Recognize at or before 350000 microseconds from Begin. Later movement cancels. Horizontal wins exact diagonal ties; use a 1e-12 shorter-dimension tolerance solely for floating-point equality.
- Freeze pointer ownership and interaction-phase identity/kind at Begin. UI ownership retains its control identity until release. Gameplay movement across UI does not change ownership.
- Only the first gameplay contact begun in an active phase owns recognition. Other held contacts remain excluded, including after the first ends; they must release and begin again. Inactive contacts never arm and do not reserve the active slot.
- Emit one typed GestureCommand per contact, carrying sequence, original input timestamp, pointer, captured phase, intent, direction, and normalized endpoints. EnemySequence produces Parry, PlayerOpening produces Attack. Inactive phases produce neither.
- A changed/expired phase, OS cancellation, out-of-order timestamp, suspension, disable, or screen-dimension change cancels incomplete gestures. Movement or repeated Begin cannot revive a committed/cancelled contact before its terminal release.
- End may recognize if it is the first delivered sample past threshold. Cancel never recognizes. The combat clock, command queue, offensive buffer, defensive windows, and gameplay outcomes remain later tasks.

## Boundaries and capture

Pure C# input values and the recognizer live in Domain/Input. Application owns the phase-source and UI-ownership interfaces plus a monotonic phase context. Presentation copies EnhancedTouch callback primitives into immutable samples: combined device/touch ID, normalized position, phase, and source-event time rounded to the nearest microsecond (half away from zero).

EnhancedTouch callbacks execute per recorded source state change. Its Touch ring-buffer records are never retained. A balanced, shared session lease acquires EnhancedTouch support and disables redundant event merging while captures are active, including after settings replacement; the last lease restores prior flags on surviving settings objects. This transient setting also affects other devices and costs additional event processing, which preserves the required threshold-crossing samples. Update-mode changes suppress synthetic history-rebuild callbacks and cancel held gestures. Device removal emits terminal cancellation for removed contacts using current Input State time, without recognizing a command.

UI ownership uses a synchronous EventSystem raycast at Begin's actual position and accepts GraphicRaycaster hits only. UI module state from a later frame cannot determine ownership. Screen metrics are captured for recognition; dimension changes cancel contacts. Enable/re-enable seeds currently held device contacts as excluded until release.

Bootstrap explicitly composes an initially inactive phase context, capture adapter, UI event system, editable GestureTuning asset, and a short direction trace. Refuge stays empty until an active phase is explicitly supplied. Pause/focus/teardown cancel capture immediately. No gesture surface/HUD layout or combat demo is added.

## Proof

Tests first fail for missing APIs, then pass against actual compiled assemblies. Edit Mode tests exercise threshold/time/direction boundaries, rectangular normalization, ownership crossings, phase changes, multi-contact exclusion, cancellation, and duplicate suppression. Play Mode tests queue real TouchState events through Input System, including consecutive moves in one update, and assert source timestamps, phase/ownership capture, cancellation, suspension, enable leases, re-enable behavior, and visual feedback. Use public device APIs without adding package testables or changing the package baseline.

Physical-touch latency and one-thumb ergonomics remain Task 10. Synthetic queued events establish the capture/recognition contracts only.
