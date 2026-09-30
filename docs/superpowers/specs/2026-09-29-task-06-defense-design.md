# Task 06 Defense and Graybox Design

Implement task 06 from the original plan: guard, dodge charges, parry windows,
damage, stagger, recovery and death in a graybox encounter. Every authored
attack/defense combination must follow its mask. Preserve all Task 05 work.

## Intended result

Forever We Reign is the working title. The player is a soldier fighting bosses
to regain his throne, finding it cursed at the end as a sequel hook. Record this
in a separate narrative brief; no campaign or ending implementation belongs here.

A separate saved GrayboxEncounter scene supplies two anchored primitives,
readable authored tells, resource/state feedback and temporary test controls.
Bootstrap still reaches the inactive Refuge. Final control appearance is Task 09.

## Combat authority and chronology

Domain owns immutable strikes, defense commands, tuning and DefenseCombatant.
Application CombatEncounter consumes the existing timeline. Gesture parries and
guard/dodge commands share one mixed source queue, ordering by timestamp and
global arrival number. Existing GestureCommand APIs remain compatible.

Raw touch records select a test control on Begin through UI ownership and retain
that ownership through release. Guard starts on Begin and stops on End/Cancel;
dodge commits once on Begin. These source-timestamped commands enter before the
input batch is resolved. UI button callbacks cannot decide impact or damage.
Control contacts held through suspension cannot produce a new action on resume.

The encounter derives the next strike's timing from pending timeline milestones,
including warning retiming after resume. Player recovery and charge regeneration
run against combat time, separately from the authored attack milestone queue.
All outcomes, including death, are set synchronously within impact resolution.
No physics, animation or UI callback can inflict damage or revive the player.

## Exact prototype rules

- Health 100; guard 100; three dodge charges. Health and guard clamp at zero.
- Legal blocks absorb the entire strike and spend its authored guard cost.
  Depleting guard staggers for 600000us; that blocked strike causes no health
  damage. Subsequent strikes during stagger remain dangerous. Guard never
  regenerates during this encounter.
- A dodge consumes one charge on acceptance. Avoidance is inclusive from 50000
  through 230000us after commitment, on an explicitly allowed safe side. The
  action finishes at 360000us, with its final portion representing recovery.
- One charge returns per 3000000us without an accepted dodge, serially to three.
  Failed or exhausted dodge requests do not reset that timer.
- Parry requires an authored allowed mask, the exact screen-space direction,
  and an inclusive timestamp in the last 140000us through impact. No early
  defensive buffering. Invalid attempts do not lock or consume resources.
- Successful parry recovery is 250000us. An unblocked hit imposes 200000us
  recovery unless lethal. Both are documented editable prototype choices.
- A fresh guard, dodge or parry may start from Ready or Guarding. Committing
  dodge/parry releases guard. Recovery and stagger reject new defensive actions;
  guard release is always safe. Exact action-end timestamps permit a new action.
- Death at zero health is terminal. Later tied strikes or recovery events cannot
  restore health or action eligibility. Defeat ends the graybox sequence.
- Suspension clears held guard and armed parry without restoring spent resources
  or resetting committed action/recovery timers. The clock freezes those timers.

Authored masks permit Guard, Parry and/or Dodge. Dodge requires explicit Left,
Right or Both safe sides. Guard requires a positive cost. Invalid masks, timing,
directions or resource values fail construction. Category names do not override
the data. The graybox has a fixed standard/heavy/guard-break/unparryable/feint
fixture sequence; it is a test rig, not the Task 08 pattern-selection system.

## Integration and boundaries

CombatTimeline gains a typed defense payload, atomic milestone scheduling and
read-only pending-impact lookup. CombatSession preserves its gesture-only entry
point while adding a mixed-input entry point and frame/suspension notifications.
The driver submits both command types into one bounded list. Task 05 FIFO, ties,
stall-before-catchup and warning-retiming tests remain enabled.

The application encounter owns subscriptions and pending strikes. Ending,
restarting or unloading it releases them. The view owns its primitives and
materials. The runtime test controls retain pointer ownership and never convert
a gesture beginning elsewhere into a held shield.

Openings, offensive swipes/buffering, enemy balance and Focus are Task 07. AI
selection is Task 08. Approved HUD/device ergonomics are Tasks 09-10. Rigged art
and production animation/materials/audio are Tasks 11-12. Runtime definitions now
do not implement Task 13's ScriptableObject authoring pipeline.

## Acceptance

- Exhaustive defense masks and cardinal directions with literal expected results.
- Exact parry/dodge, stagger/recovery and recharge boundaries, terminal death,
  resource retention, stale phase and post-resume shifted-strike timing.
- Mixed-input tie ordering and bounded/atomic scheduling behavior.
- Real queued touch tests for guard hold/release, dodge, ownership and suspension.
- Saved scene startup, actual primitive render and presentation-independent
  outcomes. All existing Unity/Python suites and pinned package baseline pass.
- Independent review followed by fixes and fresh receipts. Physical acceptance,
  production asset acceptance and final control design remain unrun.

## Review clarifications

Guard action IDs bind terminal releases to their original contact, including
rejected source timestamps. Guard-only cancellation runs after the timeline so
queue pressure cannot strand an earlier accepted press. It preserves a fresh
contact and committed parry. At driver capacity the terminal release replaces
the last pending record and increments the drop counter.

Nonlethal hits always enforce their 200ms recovery: hits during stagger retain
the existing stagger deadline and continue into Recovery when the hit deadline
is later. Disposal cancels only still-owned pending milestone IDs; a resolved ID
can be reused by another owner.
