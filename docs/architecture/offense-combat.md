# Offense, openings, balance, and Focus

Task 07 extends the primitive **Forever We Reign** encounter with exclusive
EnemySequence and PlayerOpening phases, directional damage, one pending swipe,
combo damage, enemy balance, and Focus accumulation. The fixed fixture cycle can
now finish in victory or defeat. Restart creates a fresh duel.

The [Task 07 design](../superpowers/specs/2026-09-29-task-07-offense-design.md)
bounds this work. The [combat timing contract](combat-timing.md) still governs
source-time mapping, integer microseconds, sealed boundaries, tie priority, and
suspension. The [defense contract](defense-combat.md) preserves Task 06's authored
masks, resources, and historical evidence; this document describes the current
opening cycle that wraps that defense encounter.

## Ownership

| Layer | Responsibility |
| --- | --- |
| Domain: CombatOffenseTuning | Validated opening, attack, buffer, combo, balance, and Focus defaults |
| Domain: OffenseCombatant | Enemy health, opening identity, attack admission/impact, one pending command, and resolved-hit combo state |
| Domain: DefenseCombatant | Shared player action lock, offensive windup/recovery, defensive resources, and terminal player health |
| Domain: CombatMomentum | Balance gain/break/decay and Focus gain/cap in combat time |
| Domain: OrderedTouchRecord | Raw source sample, ownership retained from actual Begin, and capture dimensions |
| Domain: CombatTimeline | One ordered chronology for raw samples, commands, and owned milestones |
| Application: CombatOpeningController | Phase boundaries, ordered recognition, player attack milestones, resource advancement, and terminal outcome |
| Presentation: GrayboxEncounterRoot/View | Fixed fixture cycle, temporary controls, resource/timer feedback, suspension/resume, and restart |

Domain and Application remain engine-free. Enemy damage and terminal outcomes
resolve before view callbacks. Animation, colliders, camera, UI, and material
changes cannot grant an attack, reward a defense, or revive either combatant.
Existing Task 06 callers retain the defense-only encounter path.

## Exclusive phases and the fixed fixture cycle

The current graybox treats each authored strike as one synthetic enemy sequence:

1. Commit the fixture's telegraph, enemy impact, enemy recovery, and opening-start boundary.
2. Resolve the authored defense mask at impact without changing the committed strike.
3. After enemy recovery, enter a normal 2s opening or the pending balance-break 3s opening.
4. At opening close, enter a new EnemySequence and commit the next fixture.

The five fixture definitions and order are preserved: Standard, Heavy,
Guard-break, Unparryable, and Feint. Their labels are test descriptions. They do
not implement pattern decks, reactive selection, or feint AI. Task 08 owns decks,
cooldowns, selection history, encounter seeds, and safe selection boundaries.

Each transition uses a strictly increasing phase identity. An opening is
**[start, end)**: a source event at start belongs to PlayerOpening; a source event
at end belongs to the next phase. Attacks require the current opening identity,
and their impact must be strictly before its end. Parry recognition belongs to
EnemySequence and cannot become an attack by crossing a phase boundary.

A zero-recovery authored strike has an opening boundary tied to its impact.
That tie preserves inclusive enemy defense: input and enemy impact resolve first,
then the opening starts at the same combat timestamp. Positive-recovery strikes
use the ordinary half-open rule above.

Balance reaching 100 resets pressure to zero immediately and marks a pending
break. The longer opening begins at the committed enemy recovery boundary.
It does not interrupt an authored noninterruptible strike or introduce AI selection.

## Source-ordered raw recognition

The graybox enables ordered recognition on TimestampedTouchCapture. Capture
retains the actual Input System timestamp, normalized position, dimensions, and
pointer ownership from Begin. Gameplay-owned raw samples join typed guard/dodge
commands in the driver's bounded arrival batch. CombatSession maps and queues the
complete batch before advancing the timeline.

CombatOpeningController runs the cardinal recognizer inside the chronological
raw record, using the semantic phase at that event time. Its accepted command is
applied inline at input priority. Re-enqueueing a same-time command after the
horizon is sealed would reject it, so recognition does not create a second queue
pass. Immediate Task 04 recognition and the inactive Refuge remain compatible.

Before each event resolves, the controller advances resources and synchronizes
opening boundaries. It reads the actual pending opening-start milestone, including
retiming after resume. This makes the half-open endpoints hold even though input
priority precedes phase-transition milestone priority. A contact begun in an old
phase is cancelled, then remains excluded until release; it is never reinterpreted
as a fresh gesture in the next phase.

If the session rejects a raw record because its source timestamp is stale or the
timeline is full, it resets contacts after the accepted batch resolves. The driver
does the same after inbox overflow; device removal also cancels contacts. This
prevents a dropped terminal sample from retaining gameplay ownership indefinitely.
When the next enemy commitment fails for lack of capacity at opening close, the
application retries after the batch drains, using a fresh future telegraph time.
An observer that does not attempt commitment receives one readiness notification.

At a tied timestamp, death precedes inputs, then telegraph, impact, phase
transition, recovery completion, and buffered offense. Raw gameplay samples and
typed controls share input priority and arrival order. PlayerImpact is a distinct
milestone kind at impact priority. Incoming-strike lookup and resume warning
selection consider enemy impacts, so a player impact is never treated as an
incoming enemy strike.

## Attack admission and shared recovery

A ready or guarding player can begin an eligible opening attack. Commitment
releases held defense and enters Attacking until the 100000us impact, then
Recovery until 400000us after impact. The full default action lasts 500000us.
Defense and offense use the same monotonic player action lock; recovery or stagger
cannot be bypassed by issuing a different action.

Admission reserves player impact, recovery completion, and buffered-offense
milestones together before mutating attack state. Invalid phase, terminal state,
insufficient timeline capacity, or an impact at/after opening end rejects the
attack. Each owned impact resolves at most once and applies integer enemy damage.
Domain resources advance at every event and at frame end, never through rendering.

One pending command may wait while defense or offense action recovery remains:

- The first eligible request wins. A later request cannot replace it.
- It retains the original opening identity and expires at exactly 120000us after its mapped request time.
- Existing player action completion schedules a buffered-offense opportunity, including recovery inherited from defense at the opening boundary.
- An early consumption attempt retains the command while the action lock is still active. Consumption starts an attack only after readiness and before expiry.
- Closure wins a tied endpoint; recovery completes before buffered offense at a tied time. Consumption revalidates phase, terminal state, expiry, and the future impact deadline.

There is no early-defense buffer. The pending swipe belongs only to offense.
Closing the opening clears its pending swipe, unfinished recognition, and combo.
Committed shared recovery keeps its existing deadline even when opening-owned
records are cancelled. A new phase cannot erase the action lock.

Combo state counts resolved hits, not captured swipes. L-R-L, R-L-R, U-D-U, and
D-U-D give the third hit a 10% damage bonus, then reset the chain. With the default
20 base damage, that hit deals 22. A nonopposite direction starts a new chain;
phase close, suspension, terminal outcome, and disposal reset it.

## Exact editable prototype defaults

All durations are integer combat microseconds. Plan-authored values are preserved;
the additional prototype choices make the graybox executable without claiming
final combat balance or a completed content-authoring pipeline.

| Value | Default | Basis |
| --- | ---: | --- |
| Normal opening | 2000000us (2s) | Plan-authored |
| Balance-break opening | 3000000us (3s) | Plan-authored |
| One-command buffer duration | 120000us (120ms) | Plan-authored |
| Enemy maximum health | 100 | Prototype choice |
| Base attack damage | 20 | Prototype choice |
| Player attack windup | 100000us (100ms) | Prototype choice |
| Player attack recovery after impact | 400000us (400ms) | Prototype choice |
| Third alternating opposite-direction hit bonus | 10% | Prototype choice |
| Balance: parry / dodge / block | 25 / 10 / 5 | Plan-authored |
| Balance break threshold and reset | 100, then zero | Plan-authored |
| Balance decay delay after successful defense | 3000000us (3s) | Plan-authored |
| Balance decay step | One point per complete 100000us (100ms) after the delay | Prototype choice |
| Focus: parry / dodge / block | 25 / 10 / 5 | Prototype choices |
| Focus maximum | 100 | Prototype choice |
| Focus decay | None | Prototype choice |

Only Parried, Dodged, and Blocked award balance and Focus and refresh the balance
decay origin. GuardBroken, Hit, and IgnoredAfterDeath award neither. Decay uses
accumulated integer combat time: the first full step occurs 3100000us after the
last successful defense. Focus saturates at its cap. Suspension freezes combat
time, and a terminal stop prevents further resource gain or decay.

Focus accumulation is the full Task 07 resource scope. Active ability effects,
equip flow, and persistence remain Task 22. Task 07 does not spend Focus, equip an
ability, commit rewards, or write saves.

## Suspension, outcomes, and restart

Suspension clears pending swipes, combo, armed recognition, and held contacts.
Spent resources and committed player action/recovery timers survive. The clock
retains the established resume countdown and warning contract. Fresh input is
required after resume; an already-held contact cannot become a new attack.

Player death is terminal. Enemy zero health is Victory only when the player
remains alive; a dead player resolves Defeat. The controller cancels owned
milestones and incoming strikes, stops momentum changes, and enters Inactive.
Later records cannot revive combatants or change the outcome. Restart disposes
the old duel/session/subscriptions and creates fresh health, resources, IDs, and
fixture position. Unload and disposal cancel recognition and owned work.

The view reports opening phase/time, player health/guard/dodge charges, enemy
health, balance, Focus, attack result, and victory/defeat. The guard/dodge zones and
Resume/Restart buttons remain an instrumented layout. Task 09 owns final controls,
mirroring, reach calibration, and portrait HUD design.

## Verification and production boundary

The [Task 07 acceptance report](../acceptance/task-07-readiness-2026-09-29.md) and
[machine-readable receipt](../acceptance/task-07-evidence-2026-09-29.json) record
native-suite results, preservation evidence, and replay comparisons. Required
contracts cover raw batches across both opening endpoints, stale contacts,
first-wins buffering, exact expiry, shared recovery, combo reset, admission and
terminal state, balance decay/threshold, Focus cap, lifecycle integration, actual
capture, and literal 30/60/120 FPS plus jittered replay outcomes.

Historical Task 06 receipts remain unchanged. Automated and Editor contracts do
not establish touch latency, one-thumb ergonomics, phone safe areas/rotation,
production animation contact, or sustained device performance. Task 10 requires
recorded physical acceptance before the Task 11/12 rigged 3D asset and presentation
pipeline. No Task 07 generated art or production asset batch is required. The
[asset readiness plan](../production/asset-readiness.md) and [narrative brief](../production/narrative-brief.md)
preserve the later production gates and approved soldier/throne/curse premise.
