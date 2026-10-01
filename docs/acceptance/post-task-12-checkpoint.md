# Consolidated checkpoint after Task 12

The operator initially deferred hands-on qualification until after Task 12 on
2026-09-30, then explicitly requested "Complete next Task" after supplying the
new duel concept. That later instruction authorizes bounded Task 13 content
authoring and the representative sword/axe/mace pilot while this checkpoint is
outstanding. Task 09 physical acceptance and Task 10 device qualification remain
**DEFERRED, execution UNRUN**. Final Task 11 visual/contact/deformation/device
approval also remains UNRUN. The schedule exception does not accept the
prototype, change its thresholds, or authorize a broad production-art batch,
purchases, AI-credit spend, commits, or pushes. See the [Task 13 execution plan](../plans/2026-09-30-task-13-content-authoring.md).

## Current presentation and automated evidence

Task 12's separate
[CombatPrototype scene](../../Assets/Game/Scenes/CombatPrototype.unity) binds the
existing Task 11 Soldier/Captain pair and their 13 representative motions per
actor. Accepted actions, telegraphs, and resolved impacts drive animation,
bounded camera response, audio, and pooled effects. The four original feedback
tones have an explicit [source and provenance record](../production/task-12-feedback-provenance.md);
no extra asset batch, purchases, external samples, or generative-model usage
was introduced.

Task 12 passes 693 Edit Mode, 113 Play Mode, and 12 repository verifier tests,
with zero failures or skips. Automated combat replays pass at 30/60/120 FPS and
jittered intervals, including enabled/disabled guard, dodge, offense, victory,
and defeat comparisons. See the [Task 12 evidence](task-12-combat-presentation.md).
These are controlled replay results, not physical device performance, perceived
contact, or visual approval. All physical statuses below remain
UNRUN. Task 09/10 and final Task 11 visual/contact/deformation/device approval
must be recorded at this checkpoint before claiming physical acceptance or
supported-device performance. Continuing Task 13 does not change those results.

## Operator procedure

1. In the canonical Unity project, open `Assets/Game/Scenes/CombatPrototype.unity`
   for the Task 11 pair with Task 12 presentation. Use GrayboxEncounter only
   when the primitive fixture is required for a comparison.
2. Record the source/build identity and phone details before each device run.
   Exercise both hand layouts, the three scales, accepted and rejected combat
   inputs, interruption/resume, and restart using the procedures below.
3. Review both actors through tells, cuts, guard, parry, both dodges, hit, death,
   and recovery. Record weapon/shield contact, deformation, portrait framing,
   effects clarity, and cue levels on the actual output device. Prototype
   feedback tones do not constitute final Foley approval.
4. Compare recorded combat outcomes with presentation enabled and disabled,
   then capture the sustained profiling and latency evidence. Record each
   failure and its owner; fix and retest before accepting the checkpoint or
   expanding the finished campaign-art batch.

## Required evidence

| Area | Procedure and acceptance evidence | Status |
| --- | --- | --- |
| Build and device identity | Source/build identity, actual phone model, installed OS/build, display/refresh and measured safe area; reference profiles are not device evidence | UNRUN |
| Same-hand controls | Both hands and scales 0.85/1.00/1.20; guard, both dodges, cardinal swipes, ability intent, calibration, exploration, pause/background/resume and restart; record misses, unwanted actions, occlusion and grip changes | UNRUN |
| Recognition and visible response | Repeatable inputs, wrong directions, duplicate/unwanted actions, recordings and timestamps; p95 recognition-to-visible response <=50 ms at 60 FPS, <=35 ms at qualified 120 FPS | UNRUN |
| Sustained performance | 30-minute encounter loop per qualified mode; frame intervals 16.67/8.33 ms; typical GPU <=12/6 ms and main thread <=8/4 ms; continuous game-attributable combat allocations 0 B/frame | UNRUN |
| Memory and interruptions | Platform measurements including native/graphics allocations; lower-tier resident target <=1.2 GB and transition peak <=1.5 GB; thermal, low power, refresh changes and interruption behavior | UNRUN |
| Representative pair | Retargeting, weapon contact, deformation, skinning, LOD/export settings, approved visual direction and provenance; readability and portrait framing with presentation active | UNRUN |
| Presentation independence | Compare recorded combat results with animation, camera, audio and effects enabled/disabled; presentation must not decide damage, defense or progression | UNRUN |

Record procedures, captures, gameplay recordings, profiling logs, findings,
severity, and an owner in one acceptance report. Fix failed contracts and retest
before accepting the checkpoint. An unavailable device class stays UNRUN with no
support claim.
There is no invented numeric recognition-error-rate threshold; preserve the
functional recognition contract and record all errors.

60 FPS remains the default. Optional 120 FPS requires display, quality profile,
thermal, and power qualification. CPU and GPU budgets overlap. Reference iPhone
17 Pro Max and Galaxy S26 Ultra layouts do not establish actual device access,
OS support, or measured safe areas.

Tradeoff: deferring the checkpoint may require revising assets, framing, effects,
or reach layouts if the physical measurements expose failures.

Sources: [Task 09 physical checklist](task-09-portrait-hud.md),
[performance targets](../production/development-plan/09-mobile-performance-and-platform-delivery.md),
[sequence](../production/development-plan/12-codex-development-sequence.md),
and [asset readiness](../production/asset-readiness.md).

## Informal Editor feedback and refinement, 2026-09-30

The operator supplied a 45.12-second landscape Editor recording of CombatPrototype
and requested more coherent sword arcs, distinct melee families, block response,
and helpful fight-camera rotation. Anchored one-thumb duels remain the selected
model. The [refinement record](melee-motion-refinement-2026-09-30.md) documents the
source corrections and native checks. This feedback is not a physical phone reach,
latency, thermal, or final art acceptance result. Those gates remain UNRUN.
