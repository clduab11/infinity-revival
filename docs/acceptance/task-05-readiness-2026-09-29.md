# Task 05 readiness: combat timing

**Complete for the combat chronology and suspension contract.** Recorded streams
produce identical event times, ordering and synthetic outcomes at 30, 60 and
120 presentation FPS, plus jittered delivery. This does not accept a playable
combat prototype or physical devices.

Canonical project and installed Editor:

```text
C:\Users\cld-main\Desktop\github-projects\infinity-redux
Unity 6000.6.0f1 (f7f8ed4d1e24)
```

## Delivered contract

- Engine-free integer microsecond clock, bounded ordered timeline and application
  session. A complete input batch is mapped and queued before resolution.
- Timestamp ordering and FIFO tied inputs. At a common timestamp death precedes
  commands, then telegraph, impact, phase transition, recovery completion and
  buffered offense milestones. Defensive requests reach the future resolver
  before the corresponding impact. Full queues and sealed horizons reject records.
- Input timestamps retain the EnhancedTouch source clock. The before-update hook
  suspends on gaps greater than 150000us before delayed gesture recognition.
  Exactly 150000us remains valid. A stall inside input processing also suspends
  before impacts and notifies presentation observers.
- Focus, application pause and component disable freeze chronology and cancel
  unfinished gestures. Pending commands are discarded; resolved events and
  authored milestones are retained.
- Resume defaults to a three-second source-time countdown. Input stays disabled;
  countdown overshoot is discarded and old mapping segments cannot replay.
  The next pending impact receives at least 650000us after the resumed boundary;
  all pending milestones shift uniformly. A typed event re-arms the warning.
- Bootstrap composes an inactive driver in the Refuge. Explicit encounter start
  and end own the session and increasing phase identities. No attack or combat
  buttons are added to the scene.

See the [architecture contract](../architecture/combat-timing.md),
[design](../superpowers/specs/2026-09-29-task-05-timing-design.md) and
[execution ledger](../superpowers/plans/2026-09-29-task-05-combat-timing.md).

## Verification

| Check | Result and portable evidence |
| --- | --- |
| Full Edit Mode suite | 213/213 passed, zero failed/skipped, [XML](evidence/task-05-editmode.xml) |
| Full Play Mode suite | 33/33 passed, zero failed/skipped, [XML](evidence/task-05-playmode.xml) |
| Recorded replay | Identical nine resolved events, two defended impacts and one undefended impact at 30/60/120 FPS and jitter, [actual test receipts](evidence/task-05-replay.json) |
| New Task 05 tests | 32 clock, 27 timeline, six session and nine integration cases |
| Lifecycle sensitivity | Removing only the two reviewed fixes makes exactly their two regressions fail, 31/33 pass, [mutation XML](evidence/task-05-lifecycle-mutation.xml); accepted code restored before final green |
| Dependency and GUID baseline | 48 direct/65 resolved packages, 94 unique GUIDs, [result](evidence/task-05-baseline.json) |
| Python validator suite | 12/12 passed |
| Independent review | Two P2 findings reproduced and fixed; focused rereview found no remaining material defect, [record](evidence/task-05-review.md) |
| Preservation | Original asset GUIDs, all 62 alternate-project source files, package manifest/lock, project settings and scene bytes preserved |

The [machine-readable receipt](task-05-evidence-2026-09-29.json) records exact
UTC times, hashes, source preservation and local raw log paths. Existing tests
remain enabled. Of pre-existing source files, only Bootstrap composition and
README are intentionally edited. No dependencies, scene serialization or build
settings changed. New work remains local on branch `task-05-combat-timing`;
no commit or push was performed for Task 05.

Tests first produced the intended missing-API compilation failure. Integration
then exposed the batch Editor's focus routing: default public input updates
selected the Editor stream, which the driver ignores. The fixture temporarily
selects player input through public settings, creates its fake Touchscreen after
that configuration, and restores prior settings afterward. No private Input System
API, package testable, focus automation or disabled test is used.

## Remaining acceptance and next task

The synthetic resolver in the replay is a test oracle, not the Task 06 guard,
damage, dodge, parry-window or recovery mechanics. Task 07 owns offensive
buffering, Task 09 owns the combat HUD and rendered warning, and Task 15 owns
restart/checkpoint persistence. Timestamp-aware phase ownership across authored
boundaries inside one input batch remains an encounter integration obligation.
Arbitrary input arriving beyond a sealed boundary is rejected. No cross-platform
deterministic networking claim is made.

Player builds, physical touch latency, device focus/pause behavior, final warning
visibility, safe areas, rotation, one-thumb ergonomics and sustained performance
remain **UNRUN or UNVERIFIED**. Hit stop and slow motion remain later work.

The user rejected the README concept's control layout and appearance on
2026-09-29. Task 09 must revise them; the remaining image stays aspirational art
direction. Task 06 can now begin graybox guard, directional dodge/parry and recovery
on this timing foundation when requested.
