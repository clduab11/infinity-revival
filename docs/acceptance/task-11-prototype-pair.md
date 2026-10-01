# Task 11: original prototype pair and import pipeline

Recorded 2026-09-30. **Technical pipeline verification PASS; final visual,
contact, deformation, and physical-device acceptance DEFERRED, execution UNRUN.**
The operator moved hands-on testing to the checkpoint after Task 12 and before
Task 13. Task 10 remains deferred, not accepted. This report does not establish
finished character art, production weapon compatibility, or release eligibility.

## Delivered scope

Blender 5.2.1 LTS and the connected Blender MCP authored an original soldier and
armored captain in `FWR_PrototypePair`. The original `Scene`, Cube, Camera, and
Light were preserved. The saved source, recipe, export configuration, FBXs, and
fingerprints are linked in the [bundle provenance](../production/prototype-pair-provenance.md).
No asset-store content, external images/textures, paid generations, or AI credits
were required. Unity MCP was unavailable; validation used the installed native
Unity 6000.6.0f1 Editor in an isolated project, preserving the open operator Editor.

The pair has one shared 23-bone hierarchy, including 21 Humanoid bones and two
equipment sockets, normalized metre scale, original sword/shield meshes, three
LODs per actor, and three procedural material treatments. Each actor has 13
representative clips. Soldier triangles are 1832/1208/740; captain triangles are
1892/1268/800. Source weights use at most two normalized influences; Unity caps
the admitted bundle at four. No production texture/compression workflow was
accepted by these untextured prototypes.

The scoped import policy maps Humanoid bones explicitly, excludes embedded
materials/cameras/lights and runtime CPU mesh reads, preserves socket access,
controls loop/root settings, and excludes animation from lower LOD imports.
Postprocessor version 2 restores rest transforms from actual skin bind matrices.

Two prefabs share their animated LOD0 skeleton with all lower LOD renderers.
Native authoring creates three URP/Lit materials and a separate
[PrototypePairReview scene](../../Assets/Game/Scenes/PrototypePairReview.unity).
Bootstrap, GrayboxEncounter, the build list, existing combat code, and the
approved package/settings bytes remain unchanged from this task's preflight.
Task 12 event-driven animation, camera, audio, and effects are not implemented.

![Original soldier/captain source preview](evidence/task-11-blender-prototype-pair.png)

*Blender source render, not a Unity gameplay screenshot or final visual approval.*

## Verification

| Check | Result | Evidence |
| --- | --- | --- |
| Initial native import regressions, before assets | 17 failed as expected | [Import RED](evidence/task-11-import-red.xml) |
| Imported pair before prefab assembly | 17 passed, two missing-prefab failures | [Prefab RED](evidence/task-11-prefab-red.xml) |
| Expanded shared bind-pose regression | 688 passed, one real LOD mismatch | [Bind-pose RED](evidence/task-11-bind-pose-red.xml) |
| Focused native import checks after correction | 20/20 PASS | [Focused imports](evidence/task-11-import-focused.xml) |
| Final full native Edit Mode | 689/689 PASS | [Edit Mode](evidence/task-11-editmode.xml) |
| Final full native Play Mode | 95/95 PASS | [Play Mode](evidence/task-11-playmode.xml) |
| Python structural validator tests | 12/12 PASS | [Python results](evidence/task-11-python.log) |
| Canonical structural/dependency/Git checks | PASS, 50 direct packages, 69 resolved packages, 172 unique GUIDs | [Baseline](evidence/task-11-baseline.json) |

Final suites have zero failures or skips. All 669 previous Edit Mode identities
and all 95 previous Play Mode identities remain; Task 11 adds 20 native Edit Mode
cases. All 102 Game C# files match the native test project. All 323 pre-existing
asset, package, and settings files match the [preflight fingerprints](evidence/task-11-before-source-hashes.json).
Seventeen new asset GUIDs are unique and preserved across native authoring copies.
The final logs contain no C# compiler errors, remaining clock exception,
missing-reference exception, or matched import failure.

The native checks establish:

- Valid Humanoid avatars and controlled import settings on the actual FBXs.
- Matching named hierarchy, local rest transforms, scales, and mesh bind matrices
  across both actors, all LODs, and every skinned mesh.
- Actual Humanoid clip identity, loop policy, bounded timing, no gameplay events,
  360 ms dodge clips, and 500 ms player cut clips.
- Reduced lower-LOD geometry, equipment markers, and one animated skeleton per
  assembled prefab.
- Both characters' LOD0 bounds fit the separate review camera at synthetic 9:16.
- The four soldier cuts retarget onto both avatars: the hand moves, the actor
  root remains stationary, and baked mesh vertices stay finite.

These checks do not measure attractive deformation, perceived weight, readable
weapon contact, every pose's portrait safety, or physical-device performance.

## Corrections found by verification and review

Native batch authoring initially failed because an untitled startup scene cannot
accept an additive scene. Isolated batch authoring now uses Single mode. Explicit
interactive authoring saves/checks every loaded scene before asset writes, then
creates its own additive review scene. The corrected native authoring exited 0.

The stronger bind-pose test caught LOD0 left-arm rotations differing by 72 degrees
from lower LODs. Exporting from frame-zero rest channels alone did not fix the
FBX animation baker's default node channels. Restoring from authoritative skin
bind matrices and increasing the postprocessor version to 2 forced the corrected
import. The failing regression passed without removing or weakening assertions.

Independent read-only review after those fixes reported no actionable P0/P1/P2
findings. Maintainability follow-up: before expanding the generator, extract the
per-actor build from its approximately 60-line entrypoint. The file is below 500
lines; no unrelated refactor was added to this bounded task.

## Loot and weapon documentation

The requested [melee loot and upgrade specification](../production/melee-loot-and-upgrades.md)
records 12 launch melee weapons: four swords, four one-handed axes, and four
maces/warhammers. The full equipment budget stays at 34 definitions. Ranged
weapons and additional launch combat styles are excluded.

The design includes earned, freely skippable equipment reveals, equal authored
pool weights, visible current probabilities, duplicate currency conversion,
initial protection after two duplicate equipment grants, fixed purchases and
milestone choices, three upgrade levels, and one-time definition mastery. Rewards
freeze their seed/pool/revision/protection state and commit atomically with the
result. Numerical thresholds and weapon-family tradeoffs are tuning hypotheses.
The document cites primary research and developer precedents, labels inferences,
and separates planned systems from implemented behavior. Loot, upgrades,
temporary weapon effects, new stats, axes, and maces are not implemented here.

## Deferred checkpoint and next task

Founder visual/contact/deformation review, physical same-thumb controls, input
latency, recognition/readability, sustained frame times, memory, thermals, and
platform identity remain deferred to the [post-Task-12 checkpoint](post-task-12-checkpoint.md).
The original thresholds stay binding. Synthetic camera bounds and automated
retargeting do not accept that gate. Final campaign art and release eligibility
remain unapproved.

Task 12 connects presentation to the existing combat events and verifies that
disabling presentation leaves combat outcomes unchanged. It must precede the
consolidated checkpoint; content expansion in Task 13 follows accepted evidence.
No commit or push was performed during Task 11. The [machine-readable receipt](task-11-evidence-2026-09-30.json)
records source hashes, GUIDs, preservation, native results, and remaining gates.
