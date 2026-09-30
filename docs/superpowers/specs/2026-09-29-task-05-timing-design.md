# Task 05 Combat Timing Design

Task 05 establishes chronology, not guard, damage, attacks, recovery mechanics,
or encounter content. Those consumers begin in Task 06. The original section 3.7
and task 05 are authoritative.

## Timing and ordering

Domain owns an integer microsecond `CombatClock` and bounded `CombatTimeline`.
Application owns `CombatSession`, which maps input source timestamps, queues a
complete frame's commands, and advances the timeline only afterward. Presentation
owns `CombatTimingDriver`, composed alongside existing touch capture but inactive
in the Refuge. It uses the same `InputState.currentTime` basis as EnhancedTouch.

Inputs are ordered by mapped timestamp, then arrival order, independent of the
recognizer's sequence number. At a common timestamp death precedes commands;
commands precede telegraphs, impacts, phase transitions, recovery completions,
and buffered offense milestones. This preserves tied input arrival order and
lets the future resolver validate defensive commands before their impact.
Attack commands are requests; scheduling buffered offense remains Task 07.

The queue defaults to 1024 pending records. Full queues reject new records.
The resolved horizon is sealed: commands or milestones at or before a completed
boundary are rejected, never silently retimed or applied retroactively. Arbitrary
late delivery beyond a completed frame is not supported by this local timeline.
The replay acceptance supplies records before advancing their containing frame.

## Suspension

Input System's before-update hook checks for a gap greater than 150000us before
EnhancedTouch can recognize a delayed batch. Exactly 150000us remains valid.
Focus loss and application pause suspend synchronously. Suspension freezes at
the last accepted combat boundary, discards pending commands and unfinished
gestures, and retains resolved events and pending authored milestones.

Resume defaults to a three-second countdown in source time. Input stays disabled.
A stalled countdown also suspends. The first frame completing the countdown
rebases the active mapping segment; countdown overshoot is not combat time.
Timestamps from earlier segments are rejected. The next pending impact is shifted
to at least 650000us after the frozen combat boundary. All remaining milestones
shift by the same amount to preserve their intervals. A typed warning event
re-arms presentation at that boundary; Task 09 will render the final combat HUD.
Focus recovery requests resume automatically only when both lifecycle flags
permit it. A frame stall requires an explicit resume request through the driver.

## Scope and integration limits

Refuge remains inactive; tests explicitly start a timing session. No attacks or
combat buttons are added to the scene. Gesture phases remain the Task 04 port;
the encounter resolver must provide timestamp-aware phase ownership when phase
boundaries occur within one input batch. Typed phase milestones do not mutate
that port in Task 05. Animation, physics and UI do not resolve timeline records.

Process restart/checkpoint persistence is Task 15. Physical device timing,
final visible warning/HUD, hit stop and slow motion remain unaccepted. This
work makes no cross-platform deterministic networking claim.

The user rejected the controls and their appearance in the README concept on
2026-09-29. Task 09 must revisit their layout and visual treatment; the rest of
that image remains a useful aspirational art direction, not a gameplay capture.

## Acceptance

- Meaningful clock, queue and session tests, including literal tie-order oracles.
- One recorded stream produces identical event times, order and outcome state
  at 30, 60 and 120 presentation frames per second, plus jittered delivery.
- Real queued Input System events preserve source timestamps and resolve defense
  before a same-time impact, independent of Unity presentation callbacks.
- Lifecycle and pre-input stall tests cancel held gestures, freeze chronology,
  complete countdown and preserve the re-armed warning interval.
- Existing suites, dependency validator, original GUIDs and source assets pass.
