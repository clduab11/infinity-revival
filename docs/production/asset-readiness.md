# Forever We Reign: asset readiness

**Historical decision (2026-09-29):** Continue the 3D graybox prototype before producing a character or environment asset batch.
Tasks 08 and 09 do not require generated sprites, finished images, or production materials.
Task 08 extends the combat foundation with seeded pattern selection. Task 09 adds
a portrait HUD using procedural placeholder controls, with 669 Edit Mode, 75 Play Mode and 12 Python tests passing; physical
one-thumb acceptance remains UNRUN.
**Recorded:** 2026-09-29, against the existing development sequence and current user direction.

**Historical update (2026-09-30):** The operator authorized an original Task 11
soldier/captain prototype pair and deferred physical qualification until after
Task 12. The pair establishes the shared humanoid import and retarget workflow.
Its visual quality, contact, deformation, and device performance still require
review. It does not approve final campaign art or a broader asset batch.

Task 12 now connects that existing pair to observed accepted actions and resolved
combat events in the separate
[CombatPrototype scene](../../Assets/Game/Scenes/CombatPrototype.unity). Its saved
presentation profile selects the 13 representative motions per actor; animation,
bounded camera response, pooled effects, and four quiet original feedback tones
consume combat time and cues. Focused automated replays pass at 30/60/120 FPS and
jittered intervals with presentation enabled/disabled. Those results establish
the tested replay contracts, not physical frame-rate qualification or final
whole-suite acceptance. The [feedback provenance](task-12-feedback-provenance.md)
records the source recipe, hashes, and imports. No extra asset batch, purchase,
external sample, or generative-model usage was introduced.

**Current update (2026-10-01):** Task 13 delivers 53 definitions, immutable
admission, and sword/axe/mace pilots; its automated suite passes 841 Edit Mode,
155 Play Mode and 24 Python tests. Physical and final art acceptance remain
UNRUN. The operator requested a technical/art handoff and publication checkpoint.
The [parallel development contract](../handoffs/parallel-development-contract.md)
separates Task 14 engineering from a bounded first finished visual benchmark.
Blender/URP are the production tools; the faceted pilots are not finished assets.
Compatible licensed bases and original/customized work remain available under
the provenance and budget rules. Purchases and AI-credit spend are not authorized.

## Historical prototype needs and production roles

| Asset type | Task 09 need | Production role later |
|---|---|---|
| 2D character sprites | None | The planned combatants are rigged 3D characters |
| Character concept sheets | Optional reference | Approve silhouette, equipment, and material direction |
| Arena concepts | Optional reference | Guide the representative finished arena |
| Rigged meshes | None for graybox resolution | Required for the Task 11 player/enemy pair |
| Textures and PBR materials | Placeholder treatment is enough | Required for the representative pair and finished content |
| Combat animations | Graybox patterns and defense/offense timing can prove the domain | Representative retargeted clips must pass Task 11 |
| UI imagery and icons | No finished art batch required | Follow the approved Task 09 HUD and later content screens |
| Audio and effects | No production batch required | Task 12 prototype hooks and four original feedback tones are connected; final Foley and readability require acceptance |

The Task 09 portrait HUD remains a prototype presentation tool.
The generated concept's controls were rejected by the user and are not an approved HUD specification.
Task 09 implements the right-hand default, mirrored left-hand layout, scale 0.85
through 1.20, and three-tap reach calibration. Qualification still requires the
same-hand physical-phone checklist in the [Task 09 acceptance record](../acceptance/task-09-portrait-hud.md).
Native Editor captures with synthetic safe areas do not pass this gate. Final
production art and a broader concept batch remain unapproved; the original
Task 11 prototype pair is the authorized exception.
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
Task 13 now supplies ScriptableObject content authoring and validation. The encounter can end in victory
or defeat; it does not qualify production assets. Focus accumulation and selection
tier input do not implement Task 22 ability effects, equip flow, persistence, or
difficulty progression. See the [enemy pattern contract](../architecture/enemy-pattern-selection.md).

Task 08 passes 632/632 Edit Mode, 60/60 Play Mode, and 12/12 Python tests, with
zero failures or skips. The [Task 08 report](../acceptance/task-08-enemy-patterns.md)
records these automated contracts; Task 09 passes 669/669 Edit Mode, 75/75
Play Mode and 12/12 Python tests. Its physical one-thumb exit remains
UNRUN; full prototype device qualification remains Task 10.

The operator deferred Task 10 physical acceptance on 2026-09-30, authorizing the
Task 11 representative pair and Task 12 presentation first. Consolidated hands-on
acceptance was initially scheduled immediately after Task 12. The later explicit
Task 13 request changed sequencing without accepting the gate. Physical checks
remain DEFERRED, execution UNRUN; the thresholds remain unchanged.
See the [consolidated checkpoint](../acceptance/post-task-12-checkpoint.md).
Open CombatPrototype for the consolidated presentation checks; GrayboxEncounter
remains the primitive fixture. Task 09/10 physical checks and Task 11 final
visual/contact/deformation/device approval remain UNRUN. Task 13 content
authoring and the representative weapon pilots are technically delivered; broader
campaign assets remain future work.
Task 11 is the first representative 3D asset pipeline gate.
Task 24 is the first finished boss/route production milestone.
The separate art conversation can establish visual quality earlier through
VisualBenchmark01; that benchmark does not complete Task 24 gameplay or route work.
The throne and curse ending belongs to Task 32, after the representative workflow is proven.
These are planned acceptance requirements, not claims that any gate has already passed.

## Candidate visual references after checkpoint acceptance

The following bounded visual references remain unapproved future work. Task 12
does not authorize their generation, purchase, or production:

1. One soldier concept sheet: readable silhouette, sword/shield, armor, and material cues.
2. One first-boss concept sheet: existing armored captain role and compatible humanoid proportions.
3. One arena moodboard: the existing foundry/coastal machinery direction and portrait combat framing.
4. One style board and UI icon brief: approved materials, contrast, icon language, and Task 09 layout constraints.

Keep this batch original and consistent with ceramic armor, worked brass, dark stone,
weathered fabric, and restrained luminous glass.
It should clarify the representative pair, not design the entire campaign.
The sheets do not assign personal names, boss backstories, or a curse mechanism.
Use these references to refine the existing procedural pair. Approve the visual
direction before expanding it into final campaign content.

## Representative 3D pair for Task 11

The original soldier/captain pair now provides the technical foundation. See
its [source and import provenance](prototype-pair-provenance.md) and
[Task 11 evidence](../acceptance/task-11-prototype-pair.md).
Final visual approval remains deferred. A production-ready pair must include:

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
