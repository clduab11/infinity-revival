# Task 12: combat presentation

Recorded 2026-09-30. **Technical Task 12 acceptance: PASS.** Task 12 connects
the original representative pair to the existing combat model. The consolidated
physical and visual checkpoint remains UNRUN and is due before Task 13.

## Delivered behavior

Open `Assets/Game/Scenes/CombatPrototype.unity` in the canonical Unity project.
It uses the existing portrait HUD and seeded enemy decks, with the Task 11
Soldier/Captain prefabs instead of primitive combatants. GrayboxEncounter,
Bootstrap, PrototypePairReview, their GUIDs, and their previous behavior remain
available.

Accepted guard, dodge, parry, and player attacks drive the imported Humanoid
clips. Rejected input does not start a motion; a buffered attack starts when
the combat application admits it. Enemy tells and attacks read the live impact
and recovery milestones, including the warning shift after resume.

The presenter samples clips from integer combat time through two manual
PlayableGraphs. Crossfades last at most 60 ms and finish before contact. Cuts
and enemy attacks reach their authored 100 ms contact pose at the logical
impact time. Same-clip restarts sample immediately. Motion remains in place;
combatant anchors supply the existing authored dodge displacement. The actors
face each other along the encounter axis.

Resolved outcomes trigger bounded camera translation, original short feedback
tones, pooled impact rings, and weapon trails. Camera rotation stays fixed.
There are four audio voices, eight impact slots, and two 12-point trails.
Animation, camera, and effect progression freeze when combat time freezes;
audio stops on suspension. Disable restores the camera and clears old effects,
destroys both graphs, and hides the pair. Re-enable samples the current action
without replaying old impact cues. Restart detaches the old encounter; removal
or scene unload destroys owned graphs, actors, effects, material, and voices.

Damage, defense admission, phase transitions, resources, seeded selection, and
victory/defeat remain in the existing combat domain/application. The only
application additions are accepted-action notifications. Animation events,
weapon collisions, audio completion, and camera callbacks do not resolve
gameplay. Hit stop and slow motion remain deferred until physical timing
qualification.

## Verification

Tests ran in an isolated, graphics-backed Windows Unity 6000.6.0f1 Editor,
revision f7f8ed4d1e24. The operator's existing Editors were preserved. These
results establish automated behavior and Editor rendering, not mobile frame
budgets, perceived responsiveness, contact quality, or production art approval.

| Check | Result | Evidence |
| --- | --- | --- |
| Full Edit Mode suite | PASS, 693/693, zero failures/skips | [Native XML](evidence/task-12-editmode.xml) |
| Full Play Mode suite | PASS, 113/113, zero failures/skips | [Native XML](evidence/task-12-playmode.xml) |
| Focused motion/lifecycle checks | PASS, 14/14 | [Native XML](evidence/task-12-focused.xml) |
| Repository verifier regressions | PASS, 12/12 | [Python log](evidence/task-12-python.log) |
| Dependency, metadata, and Git checks | PASS, 50 direct/69 resolved packages, 189 unique GUIDs | [Baseline](evidence/task-12-baseline.json) |
| Original feedback reproduction | PASS, four WAVs and manifest match | [Recipe and provenance](../production/task-12-feedback-provenance.md) |

The focused tests move real imported hand bones, sample cut contact at logical
impact, ignore rejected dodge presses, preserve pose during suspension and
countdown, freeze an active camera impulse and effect, use the shifted live
warning, and verify restart/disable/removal/unload ownership. The original
offense replay compares enabled/disabled results at 30/60/120 FPS and jittered
delivery, with literal parry, attack damage, and resource assertions.

Portable original replay records:
[30 FPS](evidence/task-12-replay-30.json),
[60 FPS](evidence/task-12-replay-60.json),
[120 FPS](evidence/task-12-replay-120.json),
[jitter](evidence/task-12-replay-0.json).

Four additional native cases exercise 32 fresh-scene replays: guard, dodge,
defeat, and victory, each enabled/disabled at all four cadences. A separate
combat-domain recorder produces one unchanged command list per scenario.
Comparisons include accepted-action order, timeline records, phase identities,
resources, initial/current committed patterns, selector history/count/random
state, and terminal results. Literal outcome/resource assertions accompany the
equality checks. Both terminal cases sample real Death bones over time.
See [focused terminal XML](evidence/task-12-terminal-focused.xml) and the
`task12-terminal-<scenario>-<cadence>.json` receipts in the evidence directory.

## Failures caught and fixed

The first accepted-action/profile checks failed all four cases for missing
notifications and profile. The first presentation checks failed all ten cases
because the separate scene was absent. These expected failures are preserved
in [admission RED](evidence/task-12-admission-red.xml) and
[scene RED](evidence/task-12-scene-red.xml).

Review and stronger lifecycle checks exposed an old camera kick replaying after
re-enable and four audio objects surviving component removal. Native tests
failed for both before the fixes. Render inspection also caught reversed actor
facing, confirmed by a failing encounter-axis assertion. The corrected focused
run passes all three regressions:
[camera RED](evidence/task-12-camera-red.xml),
[audio cleanup RED](evidence/task-12-audio-cleanup-red.xml),
[facing RED](evidence/task-12-facing-red.xml).

A further removal/restart test exposed Unity's destroyed-object behavior:
C# null-conditional calls still reached the destroyed presenter. Unity-aware
null checks now preserve encounter restart and advancement after component
removal. [Restart RED](evidence/task-12-removed-restart-red.xml) records the
exception; the full Play Mode run passes the strengthened removal case.

Two initial test-constructor compile errors and one countdown fixture using
1.5-second jumps were corrected before final verification. The countdown error
was in the fixture: the existing 150 ms stall guard correctly rejected those
jumps. No production timing threshold was weakened.

## Preservation and provenance

The 362-file preflight snapshot preserves all existing files. Exactly five
existing files change: CombatEncounter and CombatOpeningController observation
hooks, GrayboxEncounterRoot and GrayboxEncounterView integration, and the build
list. All 51 Domain files, both package files, seven existing scene files,
seven existing SourceArt files, and the 23 prototype-pair files remain byte
identical. Removing the one new CombatPrototype build entry reproduces the
previous settings hash. An editor-added App UI config entry was removed from
the canonical settings to preserve that baseline.

Preservation starts at the Task 12 preflight, not Git HEAD. Accumulated prior
task changes remain in this checkout. All 112 Game C# sources match the native
tested project. All 689 prior Edit Mode and 95 prior Play Mode test identities
remain present. No commit or push was performed.

See [preflight fingerprints](evidence/task-12-before-source-hashes.json),
[representative pair provenance](../production/prototype-pair-provenance.md),
and [original feedback provenance](../production/task-12-feedback-provenance.md).
No package version, external asset, purchase, generative-model credit, loot
implementation, new weapon family, or campaign content was introduced.

![Native prototype combat render](evidence/task-12-combat.png)

This is a native Editor gameplay render of the original prototype pair and
existing HUD. Its geometry, material treatment, cues, and controls still need
the consolidated hands-on review.

## Remaining checkpoint

Run the [post-Task-12 checkpoint](post-task-12-checkpoint.md) before Task 13.
Task 09 same-hand control acceptance, Task 10 latency/performance/device
qualification, and final Task 11 visual/contact/deformation approval remain
UNRUN. Prototype tones need speaker/headphone review and later audio direction.
This technical task does not accept those gates or authorize release/publishing.

Implementation plan: [Task 12](../plans/2026-09-30-task-12-presentation.md).
Machine-readable receipt: [Task 12 evidence](task-12-evidence-2026-09-30.json).
Independent review: [review record](evidence/task-12-review.md).
