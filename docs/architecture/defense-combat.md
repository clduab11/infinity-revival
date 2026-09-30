# Defense combat and the graybox encounter

Task 06 adds deterministic guard, dodge, parry, damage, stagger, recovery, and
death to the Task 05 chronology. The graybox is a separate 3D encounter for
**Forever We Reign**. It uses primitives and temporary controls to exercise the
rules before the final HUD and production asset pipeline.

The authoritative task boundary is the [Task 06 design](../superpowers/specs/2026-09-29-task-06-defense-design.md).
The [combat timing contract](combat-timing.md) continues to govern source-time
mapping, suspension, sealed boundaries, and same-time event priority.

## Ownership

| Layer | Responsibility |
|---|---|
| Domain: EnemyStrike | Immutable authored defenses, safe dodge sides, parry direction, costs, damage, and attack durations |
| Domain: CombatDefenseTuning | Validated prototype resources and player action durations |
| Domain: DefenseCombatant | Player state, resources, legal action acceptance, synchronous impact resolution, and player timers |
| Domain: CombatTimeline | Ordered mixed events, atomic milestone admission, and current pending-impact lookup |
| Application: CombatSession | Map source timestamps, queue the full mixed batch, resolve chronology, and notify suspension/frame advancement |
| Application: CombatEncounter | Own committed strike definitions and subscriptions; route defense commands and phase-valid parries |
| Presentation: CombatTimingDriver | Collect both command types into one bounded batch and integrate Input System/lifecycle updates |
| Presentation: TimestampedDefenseControls | Select a test control from raw touch Begin and preserve that pointer's ownership |
| Presentation: GrayboxEncounterRoot/View | Compose the encounter, show tells/results/resources, and own temporary scene presentation |

Domain and Application remain engine-free. Impact resolution sets health, guard,
state, and death before publishing its result. Physics, animation, camera, and
UI callbacks do not determine damage or revive a dead combatant.

## One mixed chronology

Gesture parries and typed guard/dodge commands retain their source timestamps.
CombatTimingDriver collects them in one arrival list before the input batch is
resolved. CombatSession maps timestamps and enqueues the whole batch before
advancing CombatTimeline. The gesture-only entry point remains compatible.

The timeline sorts first by mapped combat time. At a tied time, death milestones
precede commands, then telegraph, impact, phase transition, recovery completion,
and buffered-offense milestones. Gesture and defense commands share the same
command priority and global arrival counter. Their independent sequence numbers
are identities, not a reason to reorder a guard release and parry.

Both the input batch and timeline default to 1024 records. Full queues reject ordinary
new input. A terminal guard release at driver capacity replaces the last pending
input and counts that displaced record as dropped, so bounded admission cannot
strand guard. Mapping or timeline rejection of a terminal release triggers a
guard-only cancellation after chronological resolution. Raw presses and releases
share a unique guard action ID: cancellation for an old contact cannot clear a
fresh contact or disarm a committed parry. Session and driver counters
expose mapping/queue rejections and dropped input respectively. Sealed times
cannot accept retroactive commands; future or inactive source segments reject.

## Authored strikes and resume

CombatEncounter commits three milestones as one atomic batch: telegraph, impact,
and enemy recovery completion. Impact time is telegraph time plus the strike's
telegraph duration; enemy recovery completion adds its recovery duration.
Capacity, duplicate-ID, sealed-time, and arithmetic checks run before admission,
so a rejected strike cannot leave an orphan tell or impact in the queue.

The encounter looks up the next pending impact when resolving a parry.
It does not cache the originally scheduled impact time. After resume, the
timeline shifts remaining milestones to restore at least 650000us of warning;
parry acceptance uses that currently pending impact and its associated strike.
Captured parries from a different interaction phase cannot defend this encounter.

Player recovery, stagger, dodge action completion, and charge regeneration are
computed in combat time by DefenseCombatant. They do not depend on the enemy's
recovery milestone. Frame advancement updates them even between attack events.
Suspension freezes combat time, discards commands, and clears held guard and
armed parry. It retains spent resources and committed action/recovery timers.
Resume uses the existing 3000000us countdown and requires fresh control input.

## Legal defense rules

An authored mask allows Guard, Parry, Dodge, or a nonempty combination of them.
Unknown mask bits fail construction. Dodge-enabled strikes must specify Left,
Right, or Both safe sides; other strikes must specify no safe dodge side.
Guard-enabled strikes require a positive guard cost. Damage cannot be negative.
Parry directions must be valid cardinal screen-space directions. Telegraph
duration must be positive and enemy recovery duration cannot be negative.

| Defense | Acceptance and impact behavior |
|---|---|
| Guard | Start from Ready/Guarding with guard remaining; a legal block absorbs all health damage and spends the authored cost, capped at remaining guard |
| Guard depletion | If cost reaches or exceeds remaining guard, resolve GuardBroken with zero health damage for that strike and begin stagger |
| Dodge | Start from Ready/Guarding with a charge; consume one immediately; avoid only during the inclusive window and on an authored safe side |
| Parry | Start from Ready/Guarding; require the Parry mask, exact authored direction, and an inclusive timestamp within the final window through impact |
| Unprotected impact | When no legal held guard, armed parry, or active safe dodge succeeds, take the strike's full authored damage |

Committing dodge or parry releases guard. Invalid parry attempts do not consume
resources, lock the player, or buffer an early defense. Recovery and stagger
reject new defensive starts. Guard release is resource-neutral. A successful
parry is armed for its exact strike ID, direction, and pending impact timestamp.

## Exact editable prototype defaults

All durations below are integer microseconds in combat time.

| Value | Default |
|---|---:|
| Maximum health | 100 |
| Maximum guard | 100 |
| Maximum dodge charges | 3 |
| Guard-break stagger | 600000us (0.60s) |
| Dodge action duration | 360000us (0.36s) |
| Dodge avoidance | 50000us through 230000us after commitment, inclusive |
| Parry window | Impact minus 140000us through impact, inclusive |
| Successful parry recovery | 250000us after impact |
| Nonlethal hit recovery | 200000us after impact |
| Dodge charge recharge | One charge per 3000000us, serially up to three |
| Strike telegraph | 650000us |
| Enemy recovery | 500000us |

Guard does not regenerate during this encounter. Every accepted dodge restarts
the recharge origin; failed or exhausted requests do not delay it. Other defenses
and hits do not reset recharge. The dodge tail is Recovery after the avoidance
window, retaining its original 360000us end. Exact action-end times permit a new
action. A hit during stagger preserves its existing stagger end and separately enforces
the full hit-recovery deadline. A late hit can therefore continue as Recovery
after Staggered ends.

Health and guard clamp at zero. Zero health synchronously enters terminal Dead,
clears action eligibility, and stops the graybox from committing further strikes.
Later tied impacts or recovery events cannot restore health. Explicit Restart
creates a fresh session and combatant; ordinary suspension/resume does not.

## Five deterministic fixtures

GrayboxStrikes cycles through these definitions in fixed order:

| Fixture label | Allowed defenses | Safe dodge side | Required parry | Guard cost | Damage |
|---|---|---|---|---:|---:|
| Standard | Guard, Parry, Dodge | Left | Up | 20 | 25 |
| Heavy | Guard, Dodge | Right | Not allowed | 40 | 35 |
| Guard-break | Parry, Dodge | Both | Left | 0 | 30 |
| Unparryable | Guard | None | Not allowed | 100 | 40 |
| Feint | Parry | None | Down | 0 | 20 |

The labels do not override these masks. In particular, the guard-break fixture
excludes Guard, while the guard-only fixture can deplete guard through its cost.
The feint label identifies a parry-only test fixture, not an implemented feint AI.
All five use the default strike durations and are not interruptible.
The root commits the next fixture after enemy recovery, only while running and
alive. This is a test sequence, not Task 08 pattern selection.

## Raw control ownership and scene lifecycle

Raw touch samples select Guard, Dodge Left, or Dodge Right only on Begin through
the UI ownership resolver. Guard press uses Begin's source timestamp and release
uses End/Cancel's timestamp. Dodge commits once on Begin. A pointer retains its
selected control when dragged; a second contact cannot steal or release it.
A gesture begun in gameplay cannot become guard by crossing into the UI zone.
Device removal cancels owned contacts. Suspension cancels held controls; an
already-held contact cannot trigger a fresh action when the countdown finishes.

Only Resume and Restart use UI button callbacks. The defense zones submit
timestamped commands and do not decide impact or damage. Their layout and
appearance are explicitly temporary. The user rejected the earlier generated
concept controls; Task 09 owns the final HUD and reach qualification.

The native Unity Editor authoring command creates a new empty scene and saves
[GrayboxEncounter](../../Assets/Game/Scenes/GrayboxEncounter.unity), with its own
root, camera, and light. It appends the enabled scene to build settings and applies
the approved product title. It does not author the encounter inside Refuge.
Bootstrap still opens Refuge with no active combat session.

The encounter tracks only its still-pending milestone IDs. Resolved IDs can be
reused by another owner without being routed to this strike or cancelled during
disposal. Ending the timing session disposes the root encounter and cancels its
owned controls. Capture shutdown, settings changes and screen-size changes
notify the control adapter to cancel held defenses.

Restart disposes encounter subscriptions/strike references and ends the previous
session before creating another. On unload, the root disposes the encounter,
capture and timing components unhook their inputs, and the view destroys its
owned materials. Primitives belong to the view hierarchy and have no combat
colliders. Presentation transforms and material choices are visual feedback.

## Verification boundary and next work

[Defense rules](../../Assets/Game/Tests/EditMode/DefenseRulesTests.cs) exercise
mask/direction/side legality and terminal outcomes. [Defense timing](../../Assets/Game/Tests/EditMode/DefenseTimingTests.cs)
covers exact windows, action ends, recharge, suspension resource retention, and
validation. [Encounter tests](../../Assets/Game/Tests/EditMode/DefenseEncounterTests.cs)
cover mixed FIFO ties, atomic admission, stale phases, and shifted resume timing.
[Queued touch integration](../../Assets/Game/Tests/PlayMode/TimestampedDefenseIntegrationTests.cs)
checks source timestamps and retained UI ownership through the actual capture
adapter. [Graybox runtime tests](../../Assets/Game/Tests/PlayMode/GrayboxEncounterTests.cs)
check saved-scene startup, graphics-backed primitive rendering, unload, and presentation-independent outcomes.
Camera-space raycast depth requires an actual graphics frame in headless tests.
The capture helper warms the render pipeline and yields before collecting the
portrait; pixel assertions require both combatants in the image.

These define synthetic and Editor runtime contracts. They do not establish
physical touch latency, one-thumb ergonomics, production animation contact, or
sustained phone performance. Task 10 requires recorded device acceptance.
Task 07 owns offense/openings/buffering/balance/Focus; Task 08 owns enemy AI.
Task 13 owns the later ScriptableObject authoring pipeline.

No generated images or finished art are required for this graybox. The
[asset-readiness brief](../production/asset-readiness.md) maps the later skeleton,
animation/material, and first-finished-boss gates. The [narrative brief](../production/narrative-brief.md)
preserves the soldier, throne, curse ending, and sequel hook without implementing them here.
