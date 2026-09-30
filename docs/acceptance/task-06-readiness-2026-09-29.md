# Task 06 readiness: authored graybox defense

**Complete for Task 06:** Guard, dodge charges, parry windows, damage, stagger,
recovery and death resolve against authored defense masks in a separate graybox
encounter. This does not accept the complete combat prototype or physical phones.

**Game title:** Forever We Reign. The approved soldier/throne/curse/sequel premise
is preserved in the [narrative brief](../production/narrative-brief.md).
It is a writing commitment, not an implemented campaign or ending.

```text
Project: C:\Users\cld-main\Desktop\github-projects\infinity-redux
Editor:  Unity 6000.6.0f1 (f7f8ed4d1e24)
Scene:   Assets/Game/Scenes/GrayboxEncounter.unity
```

## Delivered contract

- Engine-free immutable strikes and validated prototype defense tuning.
- All seven nonempty defense masks, cardinal parry directions, authored dodge
  sides, inclusive timing boundaries and literal damage/resource outcomes.
- Guard absorbs legal hits, spends authored cost and breaks into 600ms stagger.
  Dodge consumes one of three charges, avoids only during 50-230ms, completes at
  360ms, and recharges one charge per three undisturbed combat seconds.
- Parry accepts the last inclusive 140ms through impact and recovers for 250ms.
  Nonlethal hits recover for 200ms; late stagger hits keep their full recovery
  after the original stagger deadline. Death is synchronous and terminal.
- One timestamped mixed input queue preserves FIFO ties before impact. Guard
  End/Cancel remains tied to its original action; old releases cannot clear a
  new contact or accepted parry. Bounded admission cannot strand guard.
- Atomic authored strike scheduling and ownership-safe disposal. Retimed parries
  look up the actual pending impact after the three-second resume countdown.
  Player timers freeze with combat time and stay separate from shifted strikes.
- Raw test controls with begin-time ownership, device/lifecycle cancellation,
  temporary primitives, resource/tell/outcome display, defeat, restart and resume.
- Unity-authored separate scene and product title. Bootstrap remains an inactive
  Refuge; the original Bootstrap and SampleScene bytes are preserved.

See [defense architecture](../architecture/defense-combat.md),
[design](../superpowers/specs/2026-09-29-task-06-defense-design.md) and
[execution ledger](../superpowers/plans/2026-09-29-task-06-defense.md).

## Verification

| Check | Result and portable evidence |
|---|---|
| Full Edit Mode suite | 381/381 passed, zero failed/skipped, [XML](evidence/task-06-editmode.xml) |
| Full Play Mode suite | 48/48 passed, zero failed/skipped, [XML](evidence/task-06-playmode.xml) |
| New Task 06 cases | 168 Edit Mode and 15 Play Mode cases over the Task 05 baseline |
| Authored replay | Eight literal-outcome cases: two streams at 30/60/120 FPS and jitter, [receipt](evidence/task-06-replay.json) |
| Dependencies and metadata | 48 direct/65 resolved packages, 114 unique GUIDs, [baseline](evidence/task-06-baseline.json) |
| Python validator suite | 12/12 passed, [log](evidence/task-06-python.log) |
| Review | Six P2 findings fixed, final focused rereview clear, [record and red receipts](evidence/task-06-review.md) |
| Presentation | Graphics-backed 720 x 1280 [portrait](../media/forever-we-reign-graybox.png), both combatants pixel-checked and visually inspected |
| Preservation | 326 before-snapshot files: 317 unchanged, nine intentional extensions, none missing; 62 alternate-project source files unchanged |

Existing Task 05 tests remain enabled and pass. Its historical receipts keep
their original scope and hashes; this task intentionally extends its timeline,
session and driver contracts. All prior Unity metadata GUIDs are unchanged.
The manifest, lock graph, Editor pin and template render settings are unchanged.
The project settings diff is only the approved productName; build settings add
GrayboxEncounter while preserving Bootstrap and the disabled SampleScene.

The [machine-readable receipt](task-06-evidence-2026-09-29.json) and
[source-preservation inventory](evidence/task-06-source-preservation.json)
record exact hashes and the nine changed paths. Task 05 and Task 06 work remain
local on task-05-combat-timing. This task performed no commit or push.

## Run the graybox

Open the scene above in the pinned Editor and enter Play Mode. For a desktop
mouse, use Window > Analysis > Input Debugger, Options > Simulate Touch Input
From Mouse or Pen. This is the installed touch simulator, not a shipped desktop
combat adapter. Hold Guard, tap a dodge direction, or swipe through the arena in
the authored parry direction. Release between actions. Restart creates a fresh
combatant; ordinary resume preserves spent resources.

The five fixed attack fixtures repeat after enemy recovery. There is no offense,
victory loop, enemy pattern selection, progression or production character art.
The displayed controls are an instrumented test layout. The earlier concept's
controls remain rejected; Task 09 owns the final HUD and reach qualification.

## Asset needs and remaining acceptance

No generation is required for Task 06. Follow the [asset readiness plan](../production/asset-readiness.md):
complete Tasks 07-10, then use a small soldier/first-boss/arena concept batch to
approve the representative rigged 3D pair for Task 11. Production needs models,
skinning, animation, textures/materials, UI art, VFX and audio. GPT-Image can help
with concepts and supporting imagery; sprites cannot supply the planned 3D rig.
No images or production assets were generated or purchased in this task.

Player builds, physical touch latency, one-thumb ergonomics, safe areas/rotation,
sustained mobile performance, production animation contact and release exclusion
of development automation remain UNRUN or UNVERIFIED. The prototype gate remains
unaccepted. Task 07 is the next bounded work order: openings, offense buffering,
balance and Focus.
