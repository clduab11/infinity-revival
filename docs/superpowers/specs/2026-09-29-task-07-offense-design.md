# Task 07: openings, offense, balance and Focus

Work order: player openings, directional attacks, one-command buffering, enemy
balance and Focus accumulation. Exit: no attacks leak across phases; opening and
resource changes are reproducible. Preserve all Task 06 behavior and evidence.

## Rules and prototype choices

Authored: normal opening 2000000us, balance-break opening 3000000us, offensive
buffer 120000us; defense pressure Parried25/Dodged10/Blocked5; break at100/reset;
decay starts after3000000us without successful defense. Focus accumulates from
successful defense. GuardBroken/Hit/IgnoredAfterDeath do not grant rewards.

New editable choices: enemy HP100, base damage20, windup100000us plus400000us
recovery (500000us total); third alternating opposite-direction swipe (L-R-L,
R-L-R,U-D-U,D-U-D) grants10% damage, then chain resets. Balance decays one unit
per100000us after its delay, with integer combat-time accumulation. Focus cap100,
gains25/10/5, no decay. Ability effects/equipment/persistence remain Task22.

Openings are [start,end); an attack must impact strictly before the opening end.
Each admitted attack owns a future impact, recovery and buffered-offense record.
One pending command: first wins, no replacement; expires at exact120000us.
Buffered requests preserve their original opening identity. Close, suspension,
death, victory and disposal clear buffer/unfinished recognition/combo. Existing
committed recovery retains its timer. Death is terminal; enemy zero means victory
only when player remains alive, otherwise defeat. No rewards/save commits here.

The fixed rig treats each authored strike as one synthetic enemy sequence.
Open after its recovery. A balance break resets pressure immediately and selects
the 3s opening at that recovery boundary, preserving committed noninterruptible
attacks. This introduces no AI/pattern-deck selection.

## Architecture and timing

Domain owns offense tuning, enemy health, attack/combo/buffer state and momentum.
DefenseCombatant gains an authored offense action lock (Attacking then Recovery),
sharing existing monotonic combat time and terminal player health.
Application's optional CombatOpeningController wraps the existing defense
encounter and owns opening transitions and player attack milestones. Existing
Task06 callers keep the defense-only path.

Raw capture keeps source timestamps, ownership at actual Begin and screen metrics.
An optional ordered-record mode queues samples alongside typed defense commands
before the session validates/advances. Recognition happens inside chronological
input records, with the semantic phase at that event time. Recognition outputs
are applied inline at input priority, avoiding a same-time reenqueue behind the
sealed horizon. Refuge and Task04 immediate recognition remain unchanged.

A pre-resolution hook synchronizes half-open phase endpoints before inputs.
The pending opening-start milestone supplies the actual retimed recovery boundary.
Every transition has a strictly increasing phase identity. Captured old contacts
cancel instead of being reinterpreted. Player impacts have a separate milestone
kind, so resume warning lookup never treats them as incoming enemy strikes.

Opening, impact, recovery and buffered milestones are scheduled in advance.
Buffered resolution occurs after tied phase closure/recovery and validates phase,
expiry, action lock and terminal state. Domain resources advance at every event
time, then frame end, not through view callbacks. The view supplies only feedback.

Review rulings: rejected raw records reset contacts after accepted records resolve;
device removal and driver inbox overflow also cancel unfinished recognition.
Capacity-rejected next-enemy admission retries at frame end with a future telegraph
time. A zero enemy recovery ties opening start to impact: inclusive defense and
enemy impact resolve first, then opening starts at the same timestamp. Ordinary
positive-recovery openings retain their half-open input boundary.

## Verification and boundaries

Meaningful tests: source batches straddling both endpoints, stale parry/attack,
exact buffer expiry/capacity/FIFO, combo/reset, attack admission/capacity/death,
balance threshold/decay and Focus saturation, pause/resume, actual touch pipeline,
30/60/120+jitter literal replay, and presentation outcome independence.

Retain all381 Edit Mode/48 Play Mode/12 Python baseline cases. Root serializes
native Unity; preserve Editor/package pins, existing GUIDs/scenes/alternate project.
Final controls are Task09; physical acceptance is Task10; no assets generated,
no purchase, commit, push, infrastructure mutation or unrelated ability work.
