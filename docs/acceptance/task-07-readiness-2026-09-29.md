# Task 07 readiness: openings, offense, balance and Focus

**Complete for Task 07.** Forever We Reign now has phase-owned attack openings,
directional damage, one-command buffering, combo damage, enemy balance and Focus.
The fixed graybox duel can finish in victory or defeat and restart cleanly.

Project: `C:\Users\cld-main\Desktop\github-projects\infinity-redux`
Editor: Unity 6000.6.0f1 (f7f8ed4d1e24)
Scene: `Assets/Game/Scenes/GrayboxEncounter.unity`

## Delivered contract

- Normal openings last 2s; balance breaks select a 3s opening after the committed
  enemy sequence recovers. Boundaries are half-open, with strictly increasing IDs.
- Four-direction attacks use one shared player action lock. Default enemy health
  is 100, damage is 20, windup is 100ms and recovery is 400ms. Third alternating
  opposite-direction hits deal 22 damage, then reset the chain.
- One pending swipe, first wins, expires at exactly 120ms and retains its opening
  identity. Recovery inherited from defense also supplies a consumption trigger.
- Attacks must impact strictly before opening close. Closure, death, victory,
  suspension and disposal clear transient input. Committed recovery keeps its timer.
- Successful parry/dodge/block grants 25/10/5 balance and prototype Focus. Balance
  resets at 100 and marks a break; after a 3s idle delay it decays one point per
  complete 100ms. Focus caps at 100 and does not decay.
- Gameplay touch recognition runs in source-time timeline order, alongside typed
  defense commands. Old contacts cancel across phases. Late/rejected terminals,
  inbox overflow, settings/lifecycle changes and removed devices cannot strand ownership.
- Capacity-rejected next-enemy admission retries after its input batch drains.
  Zero-recovery strikes resolve inclusive defense and impact before opening at
  that same timestamp. Milestone IDs stay reserved through pre-resolution handoff.
- The primitive view displays enemy health, balance, Focus, opening time, buffered
  offense and terminal outcomes. View callbacks cannot change combat outcomes.

Authored values and additional prototype choices are distinguished in the
[offense architecture](../architecture/offense-combat.md) and
[design](../superpowers/specs/2026-09-29-task-07-offense-design.md).

## Verification

| Check | Result and portable evidence |
| --- | --- |
| Full Edit Mode suite | 488/488 passed, zero failed/skipped, [XML](evidence/task-07-editmode.xml) |
| Full Play Mode suite | 56/56 passed, zero failed/skipped, [XML](evidence/task-07-playmode.xml) |
| New cases | 107 Edit Mode and 8 Play Mode; all 381/48 Task 06 case identities retained |
| Literal replay | Actual 30/60/120 FPS and jitter outputs identical: enemy HP38, player HP100, balance25, Focus25; [receipt](evidence/task-07-replay.json) |
| Package/GUID baseline | 48 direct/65 resolved pins unchanged, 126 unique GUIDs; [baseline](evidence/task-07-baseline.json) |
| Python validator suite | 12/12 passed, [receipt](evidence/task-07-python.log) |
| Independent review | No remaining P1/P2 findings; [review and RED records](evidence/task-07-review.md) |
| Native presentation | Graphics-backed, pixel-checked and visually inspected 720 x 1280 [graybox](../media/forever-we-reign-task-07-graybox.png) |
| Preservation | 385 before-snapshot files: 372 unchanged, 13 intentional extensions, zero missing; 62 alternate-project source files unchanged |

The [machine-readable receipt](task-07-evidence-2026-09-29.json) records hashes,
test counts, replay and exact [source preservation](evidence/task-07-source-preservation.json).
Existing metadata GUIDs, Bootstrap/SampleScene/Graybox scene bytes, Editor pin,
package graph, project settings and the prior Pipeline configuration are unchanged.
Historical Task 05/06 acceptance records and images retain their bytes and scope.

## Run and next work

Open GrayboxEncounter in the pinned Editor and enter Play Mode. For desktop
validation, enable Unity Input Debugger's touch simulation from mouse or pen.
Defend during EnemySequence, then swipe through the arena during PlayerOpening.
Hold/release between actions; restart creates a fresh duel. These controls remain
temporary instruments. The rejected concept control design is still deferred to Task 09.

Task 08 is next: pattern decks, cooldowns, selection history and seeded enemy
selection at safe boundaries. Focus accumulation is implemented; ability effects,
equipment selection and persistence remain Task 22. Campaign progression, saves,
rewards and the cursed-throne ending are not implemented by this work order.

Physical-phone input, ergonomics, sustained frame-time qualification and player
build acceptance remain UNRUN. The full prototype gate remains unaccepted until
its later work orders. No art generation, purchase, dependency upgrade, commit or
push occurred. Tasks 05-07 remain local on `task-05-combat-timing` at the preserved
base commit. The [asset plan](../production/asset-readiness.md) remains applicable.
