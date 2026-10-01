# Progression and combat feel candidates

These notes preserve the operator's 2026-09-30 ideas. They are candidates for
future design, not implemented statistics or an approved replacement for the
existing combat and progression contracts.

## Operator observations

The interactive Editor check preliminarily passed: both dodges worked, mouse
swipes emulated sword actions, and the ability control responded with no ability
equipped. Rapid dodge taps did not produce equally rapid dodges. One remaining
clock exception occurred in component teardown.

The requested feel is fluid movement and attack feedback under repeated input.
The operator suggested Endurance to track dodging, and levelable Attack,
Vitality (health), and Speed (dodging and attack speed).

## Existing mechanics

Dodging already has a resource: three charges, one spent per accepted dodge.
An accepted dodge occupies 360 ms; additional dodge presses during that action
are rejected. One charge returns every three seconds without an accepted dodge.
Rejected presses do not spend charges or delay recharge. Defense has no input
buffer. Offensive input supports one buffered swipe lasting 120 ms; the first
buffered direction wins.

Task 19's current progression plan names health, power, guard, and Focus
generation. Changing that attribute set requires a later design decision.

## Candidates to revisit at Task 19

| Candidate | Intended role | Design question |
| --- | --- | --- |
| Attack | Damage progression | Use the existing power attribute or rename it? |
| Vitality | Maximum health | Use the existing health attribute or rename it? |
| Endurance | Dodge capacity or recharge | Expose the existing charge resource first; choose one bounded progression effect later. |
| Speed | Dodge or attack tempo | Decide which action timings may change and how readability, resource use, and input buffering stay consistent. |

Recommended constraint: input acknowledgement and measured latency stay
independent of character statistics. A faster character is a balance choice;
responsive controls are a baseline requirement. Enemy telegraphs and parry
windows should not silently change when character Speed changes.

## Current scope and sequencing

- Correct teardown without reading an expired clock source.
- Make the dodge controls reflect actual action lockout as well as charges.
  Keep unavailable controls owned by UI so presses cannot turn into sword swipes.
- Task 10 measures physical-device latency, recognition, readability, and frame
  times. The Editor observation does not accept that device gate.
- Task 12 connects authored animation, camera, audio, and effects to combat
  events for presentation quality.
- Task 19 implements character levels and allocated attributes after the
  candidate stat design is settled.

No Endurance stat, Speed modifier, XP system, or changed action timing was added
with this corrective work.
