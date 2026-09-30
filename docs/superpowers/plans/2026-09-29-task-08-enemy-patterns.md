# Task 08 implementation plan

> For agentic workers: use superpowers:subagent-driven-development for independent
> domain/content work, with root owning existing integration and native Unity.

**Goal:** Deterministic authored enemy selection that cannot inspect unfinished
input or alter committed attacks.

**Architecture:** Immutable domain pattern definitions and transactional seeded
selector; application director coordinates safe selection and combat-time wakes.
CombatEncounter/OpeningController atomically admit multi-step sequences. The view
shows the selected prototype pattern and state.

**Tech stack:** Existing pinned Unity/C# assemblies, NUnit and Python validator.
**Spec:** [Task 08 design](../specs/2026-09-29-task-08-enemy-patterns-design.md).

## Global constraints

- Preserve all488 Edit Mode/56 Play Mode/12 Python baseline cases.
- Editor6000.6.0f1 and48 direct/65 resolved package pins remain unchanged.
- No unfinished input reaches selection; no committed attack is replaced.
- Cooldowns use integer combat time; admission is atomic; retries retain choice.
- No commit/push/art/dependency/service mutation. Keep the dirty canonical branch.
- Root alone runs Unity, serially; all writers freeze Assets during native checks.

## Tasks and ownership

- [x] Inspect authoritative requirements, verify Task07 hashes and snapshot sources.
- [x] Record model, tuning, safe-boundary and bounded-scope rulings.
- [x] Domain definitions/content tests first, observe native RED, implement.
- [x] Selector tests first, observe native RED, implement weighted seeded policy.
- [x] Director and atomic-sequence tests first, observe native RED, implement.
- [x] Connect seeded graybox and raw input acceptance; preserve first teaching strike.
- [x] Full Unity/Python checks and actual30/60/120+jitter trace comparison.
- [x] Independent task/final review, fix reproduced findings and fresh verification.
- [x] README, architecture, portable receipts and source preservation inventory.

Root: existing CombatEncounter/OpeningController extensions, ScheduledEnemyStrike,
graybox wiring/view, Play Mode tests, serial Unity, preservation and receipts.
Model/content worker: new pattern values/deck definitions, prototype deck factory
and definition/content tests. Selector worker: seeded generator, selector/decision
and selection tests. Director worker: new application enemy director and tests.
Reviewers are read-only and do not rerun Unity.

## Interface decisions

ScheduledEnemyStrike(EnemyStrike,long telegraphTimeUs) supplies checked impact and
recovery times. CombatEncounter.CommitSequence(schedule, optional extra milestone)
reserves a complete batch before ownership mutation; controller owns its marker.
CombatOpeningController.CommitSequence(schedule) owns one final opening and keeps
CommitStrike as the one-step wrapper.

EnemyPatternDeck owns stable identity/archetype/lesson/revisions/copied patterns
and an optional first teaching pattern ID. EnemyPatternSelector.TryPrepare returns
an opaque immutable selection; CanCommit/TryCommit separate validation and state
changes. EnemyPatternDirector builds checked immutable schedules, reserves them,
then commits selector history/cooldowns/RNG. Public state/trace is read-only.

## Review focus

- Malformed steps, IDs, arithmetic or capacity leave timeline/selector unchanged.
- A prepared choice survives queue pressure, suspension and frame-rate changes.
- Same-time follow-up/recovery and resumed sequences keep one correct final opening.
- Unfinished raw contacts cannot modify selection, strike data or authored times.
- Terminal/disposal/canceled cooldown wakes preserve foreign milestone ownership.

## Rulings

- Direct Proceed Task08 authorizes reversible implementation and the established
  execution method; additional skill approval/commit gates do not override it.
- Preserve the canonical dirty branch to retain Tasks05-07, rather than creating
  a checkout that omits them. No destructive cleanup.
- Commit whole patterns atomically; balance breaks upgrade the final opening.
  Progressive uncommitted-tail interruption would introduce another ownership
  contract without an authored requirement.
- Prototype decks use immutable C# content until Task13 supplies asset authoring.
- Missing weights/cooldowns/history/RNG/observation values are explicit tuning.
- Xorshift32 has 4294967295 nonzero states. Use draw minus one and upper-tail
  rejection over that exact range. Weights cap at 1000000, total at 32000000.
- Pending wake ownership includes unique timeline ArrivalOrder. A canceled ID
  reused with identical milestone values belongs to its new admission.
- Difficulty0-5 is a selection input; persistent tiers/scaling remain Task22.

## Final verification

Native pinned Unity: 632/632 Edit Mode and 60/60 Play Mode; Python 12/12.
All 488/56 Task 07 test identities and source files retained. Four actual seeded
replays match at 30/60/120 FPS and jitter. Independent final source review has
no additional P1/P2 findings. Expected compile RED and functional wake-ownership
RED receipts precede final GREEN. See the Task 08 acceptance bundle.
