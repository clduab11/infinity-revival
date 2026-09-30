# Task 08 acceptance: seeded enemy patterns

**Complete for the Task 08 contract, 2026-09-29.** Forever We Reign now selects
immutable authored enemy patterns at safe sequence boundaries. Unfinished input
cannot reach selection or change a committed attack. Work remains local and
uncommitted on the canonical `infinity-redux` checkout.

## Delivered behavior

- Four small prototype decks teach sword directions, shield patience/recovery,
  polearm dodge sides, and hammer guard conservation. Each contains three
  patterns with one to three steps, stable IDs, and captured content/balance
  revisions. These are selection fixtures; finished opponents remain Tasks 26-29.
- Authored weights, exact combat-time cooldowns, a two-entry history, approved
  resource snapshots, completed defense outcomes, and difficulty eligibility
  feed a local seeded selector. Xorshift32 uses rejection sampling over its
  nonzero state range; no global randomness or new dependency is involved.
- A prepared decision survives queue rejection and suspension. RNG, history,
  and cooldowns advance only after atomic admission of all enemy milestones
  and the final opening marker. Later observations cannot reroll the pattern.
- The director owns Waiting, Prepared, Executing, PlayerOpening, and Terminal
  states under the duel. One opening follows final recovery, including resume
  retiming. Balance breaks upgrade that opening without replacing committed
  steps. Wake ownership uses unique admission identity, preserving foreign
  replacements even when ID, kind, and timestamp match.

Every built-in deck preserves the first teaching strike: tell at 10000us, Up parry,
safe left dodge, impact at 660000us, recovery at 1160000us. Prototype cooldowns are
0/4000000/6000000us. The [architecture contract](../architecture/enemy-pattern-selection.md)
records tuning, eligibility, ownership and captured revisions.

## Native verification

| Suite | Preserved Task 07 cases | New Task 08 cases | Final passed |
| --- | ---: | ---: | ---: |
| Edit Mode | 488 | 144 | 632/632 |
| Play Mode | 56 | 4 | 60/60 |
| Python validator | 12 | 0 | 12/12 |

Zero failures or skips in final suites. The pinned Editor 6000.6.0f1 executed
Edit Mode and graphics-backed Play Mode serially. Every prior test identity and
test source remains present; no existing test was removed or weakened.

Coverage includes immutable collection/definition validation, literal seed draws,
exact cooldown deadlines, eligibility/history, pure previews, stale/foreign
decisions, atomic rejection, nonoverlap, one final opening, zero-recovery ties,
pause/resume, startup while paused, cached boundary retries, terminal cleanup,
and exact-value foreign wake replacement. Play Mode queues actual EnhancedTouch
events through capture and the timestamped application pipeline, proving that
unfinished/canceled contacts leave strike data, future deadlines, and RNG intact.
Completed parries reward the player without changing the committed definition.

The [actual replay receipt](evidence/task-08-replay.json) extracts four native
TestContext traces at 30/60/120 FPS and jitter. All traces agree: patterns a at 0,
c at 3160000us, and a at 6320000us; impacts at 660000/3820000/6980000us. This literal
fixture uses zero-damage strikes to isolate scheduling and selection across
multiple sequences. Production-deck definition checks and raw input integration
are separate tests, not claims of finished combat animation or device acceptance.

## Tests-first and review evidence

The [tests-first record](evidence/task-08-test-first.md) and
[compile RED](evidence/task-08-missing-api-red.log) precede production APIs.
The [functional RED](evidence/task-08-wake-ownership-red.xml) records 631/632
passing cases, with the sole failure reproducing foreign wake deletion. The
admission-token fix passes the final suite. Paused startup and cached capacity
retries are also covered. The [independent source review](evidence/task-08-review.md)
found no additional actionable P1/P2 issues in final source.

Portable [Edit Mode](evidence/task-08-editmode.xml),
[Play Mode](evidence/task-08-playmode.xml), [Python](evidence/task-08-python.log),
and [baseline](evidence/task-08-baseline.json) receipts accompany the
[machine-readable bundle](task-08-evidence-2026-09-29.json).

## Preservation and qualification

The [source inventory](evidence/task-08-source-preservation.json) compares 429
pre-task files: 422 byte-identical, seven intentional extensions, no missing paths.
All 62 alternate-project sources, existing GUIDs, scenes, package pins, Editor
settings, pipeline control, prior tests and historical receipts are unchanged.
The baseline verifies 48 direct/65 resolved packages and 139 unique asset GUIDs.
No dependency bump, commit, push, art generation, purchase or service change.

The [native portrait render](../media/forever-we-reign-task-08-graybox.png) shows
the selected pattern and state. It remains an instrumented primitive encounter.
Physical-phone input/ergonomics and a player build are unrun in this work order.
Task 09 is next: portrait HUD and reach calibration. Task 10 qualifies the prototype
on hardware; Task 13 supplies content assets/authoring; Task 22 owns persistent
difficulty scaling and active abilities. The soldier/throne/curse narrative and
the [asset readiness plan](../production/asset-readiness.md) remain preserved.
