# Forever We Reign: asset readiness

**Decision:** Continue the 3D graybox prototype before producing a character or environment asset batch.
Task 08 does not require generated sprites, finished images, or production materials.
It extends Task 07's openings, offense, buffering, balance, and Focus with seeded
pattern selection using placeholder presentation.
**Recorded:** 2026-09-29, against the existing development sequence and current user direction.

## What is needed now

| Asset type | Task 08 need | Production role later |
|---|---|---|
| 2D character sprites | None | The planned combatants are rigged 3D characters |
| Character concept sheets | Optional reference | Approve silhouette, equipment, and material direction |
| Arena concepts | Optional reference | Guide the representative finished arena |
| Rigged meshes | None for graybox resolution | Required for the Task 11 player/enemy pair |
| Textures and PBR materials | Placeholder treatment is enough | Required for the representative pair and finished content |
| Combat animations | Graybox patterns and defense/offense timing can prove the domain | Representative retargeted clips must pass Task 11 |
| UI imagery and icons | No finished art batch required | Follow the approved Task 09 HUD and later content screens |
| Audio and effects | No production batch required | Connect through Task 12 presentation hooks |

The current debug UI is a prototype tool.
The generated concept's controls were rejected by the user and are not an approved HUD specification.
Task 09 must design and qualify the portrait HUD, mirrored layouts, and thumb reach.
Use the current title and [approved narrative](narrative-brief.md) in future art briefs.

## Gates that determine asset work

| Task | Required work | Evidence before expansion |
|---|---|---|
| 06 | Guard, dodge, parry, damage, stagger, recovery, and death in graybox | Authored attack/defense masks resolve correctly |
| 07 | Openings, directional attacks, buffering, balance breaks, and Focus in graybox | No phase leakage; reproducible openings and resources |
| 08 | Pattern decks, cooldowns, history, encounter seeds, and safe phase boundaries | Committed attacks remain stable during unfinished input |
| 09 | Portrait HUD, mirrored layouts, reach calibration, pause/resume, input ownership | Required actions work with one thumb on physical phones |
| 10 | Prototype qualification: latency, recognition, readability, frame times | Recorded physical device evidence accepts the prototype gate |
| 11 | Approved shared skeleton, import pipeline, and one player/enemy pair | Retargeting, contact, deformation, portrait framing, exports pass |
| 12 | Animation, camera, audio hooks, and VFX connected to combat events | Disabling presentation preserves combat outcomes |
| 24 | First finished boss and short region route | Presentation, phases, rewards, and content validation are complete |
| 32 | Final humanoid guardian, campaign ending, and elite variations | Full campaign and rebirth tiers are playable |

Tasks 07 and 08 continue to use the primitive encounter. Task 08 replaces the fixed
fixture cycle with four immutable code-authored prototype decks, three patterns
each, and commits complete patterns before one final player opening. These bounded
single-phase teaching decks are not the finished regular archetypes in Tasks 26–29.
ScriptableObject content authoring remains Task 13. The encounter can end in victory
or defeat; it does not qualify production assets. Focus accumulation and selection
tier input do not implement Task 22 ability effects, equip flow, persistence, or
difficulty progression. See the [enemy pattern contract](../architecture/enemy-pattern-selection.md).

Task 08 passes 632/632 Edit Mode, 60/60 Play Mode, and 12/12 Python tests, with
zero failures or skips. The [Task 08 report](../acceptance/task-08-enemy-patterns.md)
records these automated contracts; physical prototype acceptance remains Task 10.

Task 10 physical acceptance precedes expansion into the Task 11/12 3D pipeline.
Task 11 is the first representative 3D asset pipeline gate.
Task 24 is the first finished boss/route production milestone.
The throne and curse ending belongs to Task 32, after the representative workflow is proven.
These are planned acceptance requirements, not claims that any gate has already passed.

## Small next batch after Task 10 acceptance

Prepare one bounded visual review batch before expanding content:

1. One soldier concept sheet: readable silhouette, sword/shield, armor, and material cues.
2. One first-boss concept sheet: existing armored captain role and compatible humanoid proportions.
3. One arena moodboard: the existing foundry/coastal machinery direction and portrait combat framing.
4. One style board and UI icon brief: approved materials, contrast, icon language, and Task 09 layout constraints.

Keep this batch original and consistent with ceramic armor, worked brass, dark stone,
weathered fabric, and restrained luminous glass.
It should clarify the representative pair, not design the entire campaign.
The sheets do not assign personal names, boss backstories, or a curse mechanism.
Approve the visual direction before producing the corresponding 3D pair.

## Representative 3D pair for Task 11

Produce or admit one player/enemy pair using approved source content.
The pair must include:

- One shared humanoid skeleton and normalized scale, axes, naming, and bind pose.
- Rigged meshes with valid skinning and deformation.
- Compatible sword/shield or first-enemy equipment and attachment conventions.
- Textures and PBR materials using controlled import settings.
- Representative combat animations and a validated retargeting path.
- Weapon-contact and portrait-framing checks.
- Appropriate LODs and controlled export presets.
- Completed source, license, modification, and import provenance records.

Use representative clips covering the actions the encounter actually exercises.
Clip approval must include readable tells, impact alignment, recovery, and reaction continuity.
Record animation contact, deformation, and device behavior as evidence, not an assumed import success.
Approve this family before expanding to the remaining combatants or buying further animation assets.

## Where GPT-Image can help

GPT-Image is useful for original concepts, icons, decals, supporting textures, and UI illustrations.
Generation is optional; this readiness brief does not authorize a generation or purchase batch.
Generated 2D images do not provide rigged meshes, skinning, coherent combat animation, or retargeting.
Supporting textures still require material setup, import review, and runtime validation.
Selected outputs need prompts, source inputs, edits, generator/version information when available,
creation date, usage-term evidence, and provenance alongside the asset record.

Existing concept images remain documentation references with their historical records intact.
A concept image is not runtime art, an accepted control layout, or physical-device evidence.

## Acceptance and scope control

Keep two regions, four humanoid bosses, and one shared skeleton family.
Do not expand into new rig families before measuring the representative boss workflow.
Compile results, automated checks, Editor play, and physical-device acceptance remain separate evidence.
Unknown license or input rights stay UNVERIFIED until documented.
Record actual founder review and boss-production hours before scaling the campaign batch.

## Primary project sources

- [Content production pipeline](development-plan/08-content-production-pipeline.md)
- [Development sequence](development-plan/12-codex-development-sequence.md)
- [Production roadmap](development-plan/11-production-roadmap.md)
- [Acceptance plan](development-plan/13-acceptance-plan-and-development-readiness.md)
- [Asset provenance policy and template](asset-provenance.md)
