# Interactive Play startup clock correction

The combat clock startup defect is corrected. Native Unity verification passes
669 Edit Mode and 79 Play Mode tests; the repository validator and its 12 tests
also pass. The operator's interactive Play recheck and physical phone acceptance
remain separate, unrun checks.

This is the historical startup-correction record. The operator subsequently
reported a preliminary control check with one teardown exception. See the
[teardown and dodge-feedback follow-up](2026-09-30-teardown-and-dodge-feedback.md)
for the subsequent correction and current automated results.

## Failure and cause

The supplied log contains 424 `deviceNowUs` argument exceptions, 423 related
Input System callback error entries, and 331 queued-event-limit warnings.
The actual Editor log shows the first clock failure before the queue warnings.

`GrayboxEncounterRoot.Awake` starts the encounter before the first regular input
update. The former default clock used `InputState.currentTime`, which subtracts
the Input System's cached offset from native input time. At Play entry, Unity
resets its realtime epoch; Input System 1.20.0 refreshes its cached offset in
`InputManager.OnUpdate`, after before-update listeners. With reload options
disabled, encounter startup can read the previous Editor epoch. Subsequent
input callbacks then appear to move backward, and strict domain validation
correctly rejects them.

The default source now uses `Time.realtimeSinceStartupAsDouble`, the epoch used
by public `InputEvent.time` and EnhancedTouch touch timestamps. Injected clock
providers and domain validation remain unchanged. No timestamp clamping, error
suppression, or input-queue-limit increase was introduced.

## Changes

- Default combat frame and lifecycle timestamps use Unity's realtime epoch.
- Four regression cases cover stale cached offsets at startup, pause before
  the first update, stale offsets through before-update callbacks, and native
  touch timestamps during dynamic updates.
- Deprecated root and collection lookups use Unity 6.6 APIs. Root lookups follow
  single-scene loads; collection assertions do not require ordering. The
  remaining Editor authoring lookup was also updated.
- The operator explicitly approved keeping installed Unity AI Assistant
  `2.20.0-pre.1` and Inference `2.6.1`. The dependency policy, graph fingerprint,
  and README now record 50 direct and 69 resolved packages. The manifest and
  Unity-generated lockfile were preserved byte for byte during this correction.

Assistant is prerelease. Both retained AI packages include runtime assemblies,
and release inclusion and stripping still require built-player evidence. No previous package
version changed, no AI credits were spent, and no commit or push was performed.

## Verification

| Check | Result | Evidence |
| --- | --- | --- |
| Regression before correction | 2/2 fail as expected, startup clock roughly 10 seconds ahead | [Native RED XML](evidence/2026-09-30-input-clock-startup-red.xml) |
| Final native Edit Mode | 669/669 pass, no skips | [Edit Mode XML](evidence/2026-09-30-input-clock-editmode.xml) |
| Final native Play Mode | 79/79 pass, no skips; all prior 75 cases preserved | [Play Mode XML](evidence/2026-09-30-input-clock-playmode.xml) |
| Baseline verifier unit tests | 12/12 pass | Command result recorded during correction |
| Structural validator | PASS, 50 direct, 69 resolved, 155 GUIDs, native Git checks | [Correction receipt](2026-09-30-input-clock-fix.json) |
| Focused code review | No actionable P0/P1/P2 findings | Read-only independent review |

Native checks used an isolated project copy while the operator's Editor remained
open. All 99 game C# sources matched that copy. Final native logs contain zero
`deviceNowUs` exceptions, queue-overflow warnings, deprecated-lookup warnings,
or C# compiler errors. Original packages, scene metadata, project settings,
and the disabled Pipeline control were preserved.

Queue overflow was not proven to be an independent recursive-input defect.
Its absence in the automated runs does not replace the operator's mouse/touch
recheck in the original interactive Editor.

## Operator recheck

1. Stop Play Mode and allow script compilation to finish.
2. Open Console and clear the old entries.
3. Enable Input Debugger's mouse/pen touch simulation and focus the portrait
   Game view.
4. Enter Play, exercise guard, both dodges, and cardinal swipes, then use
   pause/resume and restart.
5. Confirm fresh red exceptions and queued-event warnings do not recur.

Task 09's physical same-hand exit and Task 10's device qualification remain
UNRUN. This correction does not establish player-build or physical performance
acceptance. Historical Task 09 counts and its earlier dependency receipt remain
historical evidence.
