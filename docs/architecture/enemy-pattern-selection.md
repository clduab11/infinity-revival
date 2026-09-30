# Seeded enemy pattern selection

Task 08 extends the **Forever We Reign** graybox with immutable regular-enemy
decks and deterministic weighted selection. A whole pattern commits at a safe
EnemySequence boundary, then runs through its authored recoveries before one
player opening. Selection cannot inspect unfinished input or replace committed
attacks.

The [Task 08 design](../superpowers/specs/2026-09-29-task-08-enemy-patterns-design.md)
bounds this work. The [combat timing contract](combat-timing.md),
[defense contract](defense-combat.md), and [offense contract](offense-combat.md)
retain their existing chronology, masks, resources, and input ownership rules.

## Prototype content and ownership

| Layer | Responsibility |
| --- | --- |
| Domain: pattern/deck definitions | Stable IDs, copied immutable steps and collections, authored weights/cooldowns/eligibility, captured content and balance revisions |
| Domain: selector | Eligibility, two-entry commitment history, weighted local RNG, prepared decisions, transactional commitment |
| Application: enemy director | Safe-boundary preparation/admission, checked schedules, combat-time wakes, state and trace |
| Application: encounter/opening controller | Atomic milestone reservation, one final opening, resolved observations, source-ordered recognition, terminal cleanup |
| Presentation: graybox root/view | Encounter seed and prototype deck composition, selected pattern/state display, existing temporary controls |

Domain and Application remain engine-free. No behavior-tree middleware, runtime
language model, machine learning, navigation, or new dependency enters selection.

Four single-phase prototype decks contain three patterns each:

| Deck | Teaching purpose |
| --- | --- |
| Sword guard | Basic direction recognition |
| Shield guard | Patient defense and recovery punishment |
| Polearm guard | Safe dodge-side recognition |
| Hammer guard | Guard conservation and heavy attacks |

A pattern contains one to three steps; the definition contract permits at most
16 patterns in a deck. Stable IDs and captured content/balance revision strings
make the selected content auditable. Definitions copy caller collections and
expose immutable data, so later caller edits cannot change committed content.
Weights and cooldowns are explicit prototype tuning, not final balance. The
prototype decks capture these exact revision strings:

```text
Content revision: prototype-enemy-decks-v1
Balance revision: task08-balance-2026-09-29-v1
```

Deck definitions may supply an optional first-teaching pattern ID. When configured
and eligible, that pattern is mandatory for the first successful admission; later
choices use the weighted policy. An ineligible custom introduction permits an
eligible first fallback instead. Every prototype deck supplies the same one-step teaching
pattern: Task 06 strike 1,
telegraph at 10000us, Up parry, safe left dodge, impact at 660000us, and recovery
at 1160000us. All prototype telegraphs last at least 650000us. Steps cannot
overlap: each subsequent tell begins at or after the preceding recovery, with
its authored gap. Existing inclusive parry and dodge windows remain unchanged.

These decks exercise the selection contract. Finished regular archetypes remain
Tasks 26–29; ScriptableObject content authoring and validators remain Task 13.
Task 08 requires no generated art or production asset batch.

## Approved observations and eligibility

The director supplies an immutable observation of player health, guard, dodge
charges, Focus, last resolved defense outcome, and last resolved player attack
direction. These values come from completed logical outcomes. The selector has
no reference to raw touches, active contacts, pending gesture recognition,
buffered attack direction, view state, or an active dodge side.

Authored eligibility may require minimum guard, dodge charges, or Focus and a
difficulty tier in 0–5. An authored preferred completed-defense outcome doubles
the candidate's weight. Tier input filters selection only; persistent difficulty
progression, rewards, and stat scaling remain Task 22.

Selection uses the observations captured when a decision is prepared. Later
resources or completed outcomes cannot mutate that cached decision or an admitted
pattern. Built-in decks retain an unconditional fallback. A custom deck whose
candidates all fail resource/tier eligibility can wait for a later approved
observation rather than inventing an attack.

## Cooldowns, history, weights, and seed

Prototype cooldowns are 0, 4000000, and 6000000us. A cooldown starts on successful
sequence admission and expires at its exact integer combat-time deadline.
Suspension freezes combat time, so device clocks and presentation frames do not
shorten it. When all otherwise eligible candidates are cooling down, the director
schedules one wake at the earliest deadline without drawing or changing history.

The selector retains the last two committed pattern IDs. It suppresses an
immediate repeat when another legal candidate exists, then halves the other
recent candidate's weight with a minimum of one. One remaining legal candidate
is allowed. Authored base weights are bounded to 1–1000000. History adjustment and
the preferred completed-defense multiplier determine the final integer draw
weights. At most 16 candidates and a maximum doubled weight of 2000000 keep the
draw bound at or below 32000000.

The encounter owns a local xorshift32 generator. Seed zero normalizes to
0x6D2B79F5. It uses neither Unity/global randomness nor System.Random. Because
xorshift32 emits a nonzero domain of 4294967295 states, a bounded draw subtracts
one from each result, rejects values outside the largest complete multiple of the bound,
then takes the remainder. That rejection rule avoids modulo bias. Previewing or
preparing works on copied RNG state; only a successful committed admission
advances the selector's live state.

The seed, stable pattern/strike IDs, captured content/balance revisions, and
approved observations supply a reproducible selection record. Restart constructs
a fresh duel and resets seed state, history, and cooldowns.

## Safe boundaries and atomic commitment

The director has Waiting, Prepared, Executing, PlayerOpening, and Terminal states
under the duel. Initial selection, opening-close selection, cooldown wake, and
capacity retry require a running clock, EnemySequence phase, and no committed
sequence. Selection is prepared once per admission cycle and cached if admission
is rejected.

The director constructs checked schedules from immutable steps. One pattern
owns all 3 × N enemy milestones plus the controller's single final-opening
marker. The encounter validates IDs, duplicates, times, arithmetic, ordering,
and capacity before reserving the batch. Rejection leaves the timeline, RNG,
cooldowns, and history unchanged. Retry retains the prepared pattern and uses a
future tell time; it does not reroll under queue pressure.

Only after atomic timeline admission does the selector commit its RNG state,
history, and cooldown. CommitStrike remains the legacy one-step wrapper. The
final live recovery milestone supplies the opening boundary, including resume
retiming. Nonoverlap keeps one current tell and one earliest incoming impact.
For a tied zero-recovery impact/opening, inclusive defense resolves before the
opening begins.

Once admitted, every step remains committed. Input and later observations cannot
reroll, shorten, cancel, or replace an attack. A balance break upgrades the one
opening after final recovery; it does not interrupt the remaining committed
steps. The existing source-time recognizer still cancels old contacts at phase
transitions rather than reinterpreting their intent.

## Suspension and terminal ownership

Suspension freezes cooldowns and retains the committed sequence. Existing resume
warning/retiming rules apply to enemy milestones; player action timers retain
their established independent deadlines. A prepared decision survives a rejected
admission or suspension and resumes through the same safe-boundary checks.

Victory, defeat, and disposal cancel owned wakes, release subscriptions, and
prevent later admission or resource mutation. Cleanup preserves foreign timeline
ownership. The ArrivalOrder admission token distinguishes an owned cooldown wake
from a foreign record with the same ID, kind, and timestamp after that wake is
cancelled. Restart disposes the old director/session and composes a fresh duel.

## Evidence and production boundary

Task 08 native verification passes **632/632 Edit Mode, 60/60 Play Mode, and
12/12 Python tests**, with zero failures or skips. It preserves all 488 Edit Mode
and 56 Play Mode Task 07 test identities and adds 144 Edit Mode and four Play Mode
tests. Actual seeded pattern/strike/resource traces are identical at 30/60/120 FPS
and jittered delivery. The
[acceptance report](../acceptance/task-08-enemy-patterns.md) and
[machine-readable receipt](../acceptance/task-08-evidence-2026-09-29.json)
record suite counts, seeded pattern/strike/resource traces at 30/60/120 FPS
and jittered delivery, and preservation evidence. Required contracts cover exact
cooldown boundaries, immutable definitions, unbiased seeded draws, eligibility,
history, cached retries, atomic multi-step admission, final-opening ownership,
pause/resume, terminal cleanup, and unfinished raw gestures.

Historical Task 01–07 receipts remain intact. Automated and Editor evidence do
not establish physical touch latency, one-thumb ergonomics, production animation
contact, or sustained device performance. Task 09 is the next work order for the
portrait HUD and reach calibration; Task 10 requires physical acceptance before
the Task 11/12 rigged 3D pipeline. The
[asset readiness plan](../production/asset-readiness.md) and
[narrative brief](../production/narrative-brief.md) preserve those gates and the
approved soldier/throne/curse premise.
