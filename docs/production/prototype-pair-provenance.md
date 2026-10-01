# Asset: original soldier/captain prototype pair

- Status: APPROVED FOR PROJECT USE, bounded technical prototype only.
- Owner: Chris, Praxen LLC.
- Recorded and created on: 2026-09-30.
- Creator: project-authored procedural geometry and motion, implemented by Codex for Praxen.
- Acquisition: original procedural content, not an asset-store download or an image-to-mesh conversion.
- Source delivery: repository authoring source, executable recipe, and export manifest below.
- License evidence: no external geometry, images, textures, animations, or licensed asset packages were used. No separate third-party asset license or attribution was introduced.
- Commercial use, modification, and player redistribution: internal prototype use is authorized by the operator. Final release eligibility is UNVERIFIED pending the normal source/rights and visual acceptance review; this record is not a legal clearance opinion.
- Purchase authorization/receipt: not applicable; no purchases or paid generations.
- Generator: Blender 5.2.1 LTS, original Python geometry/motion recipe. Initial inspection and creation used the connected Blender MCP; the v1.0.3 refinement used an isolated native background export to preserve the dirty interactive session.
- External generative image/video model: none. Unity AI, Venice, and ChatGPT image credits were not used.
- Inputs: authored dimensions, bone coordinates, material values, and pose keys in the source recipe. The existing GPT-Image README concept was not imported or sampled as texture/geometry.
- Intended use: one representative player soldier and armored captain with sword/shield equipment, establishing the shared humanoid pipeline before campaign content expansion.
- Dependencies: Blender FBX exporter, pinned Unity 6000.6.0f1 and existing URP 17.6.0; no new package dependencies.
- Review: native import/retarget tests in the [Task 11 record](../acceptance/task-11-prototype-pair.md). Founder visual, contact, and deformation approval remains UNRUN at the [post-Task-12 checkpoint](../acceptance/post-task-12-checkpoint.md).
- Release eligibility: UNVERIFIED.

## Source bundle and controlled export

[Authoring source](../../SourceArt/Characters/PrototypePair.blend),
[source manifest](../../SourceArt/Characters/prototype-pair-source.json),
[recipe](../../tools/content/build_prototype_pair.py), and
[export configuration](../../tools/content/prototype-export.json) belong to one
versioned bundle, currently `prototype-pair-1.0.3`. The original source and acceptance
receipt retain their v1.0.1 identity. The [motion refinement record](../acceptance/melee-motion-refinement-2026-09-30.md)
records the subsequent source, export, and native verification. Binary source and FBXs use the existing
Git LFS rules; YAML prefabs, materials, metadata, and records remain text.

The original Blender `Scene` and its Cube, Camera, and Light were preserved.
The new scene is `FWR_PrototypePair`, with a shared 23-bone hierarchy: 21
humanoid bones and two equipment sockets. Both actors are approximately 1.8 m.
Blender authoring uses metres, Z-up, and forward -Y. FBX export uses forward -Z,
up Y, unit conversion, no leaf bones, and named NLA strips on LOD0 only.

Each actor has three body/equipment LODs and at most two normalized vertex
influences in the generated source. Soldier triangle counts are 1832/1208/740;
captain counts are 1892/1268/800. Ceramic, brass, and cloth use controlled
procedural PBR values without texture files. Production texture admission and
compression remain future work; these materials do not prove that workflow.

## Import modifications and Unity ownership

The scoped [import policy](../../Assets/Game/Editor/PrototypePairImportPolicy.cs)
admits only the six named FBXs. Postprocessor version 2 restores rest transforms
from actual skin bind matrices. This corrects the animation exporter leaving
LOD0's default node channels posed while lower LODs used rest channels. The
native regression compared every mesh's bind matrices, named bone hierarchy,
local rest transform, and scale across both actors and all LODs.

The importer uses explicit Humanoid mapping, controlled loops/root settings,
no cameras/lights/embedded materials, no runtime CPU mesh reads, a four-influence
cap, and addressable sockets. Idle and Guard loop; the other eleven actions do
not. Clips have no gameplay animation events. FBX source bytes remain unchanged
by import normalization; Unity model metadata records the import settings.

The [authoring command](../../Assets/Game/Editor/PrototypePairAuthoring.cs)
creates Soldier/Captain prefabs with three LODs sharing one animated skeleton
per actor and three URP/Lit materials. It creates the separate
[PrototypePairReview scene](../../Assets/Game/Scenes/PrototypePairReview.unity).
It leaves GrayboxEncounter, Bootstrap, the existing build list, and combat
resolution code unchanged. Task 12 supplies event-driven presentation wiring.

Source modifications on 2026-09-30: widen the Blender review camera to retain
both actors' equipment; export all LODs from frame-zero rest channels; normalize
Unity rest transforms from authoritative bind matrices after native regression
exposed the remaining 72-degree default-pose mismatch. Recipe/configuration and
source hashes are recorded in the manifest; Unity-side hashes are in the Task 11
receipt. Blender editor backups are preserved locally and ignored by Git.

## Task 11 baseline identity, v1.0.1 historical record

| File | SHA-256 | Unity GUID |
| --- | --- | --- |
| [PrototypePair.blend](../../SourceArt/Characters/PrototypePair.blend) | `354ac5f41976927361a98cdf4a07b2be910b672609451c080b813deeba6f0a6d` | `Not a Unity import` |
| [Soldier_LOD0.fbx](../../Assets/Game/Content/Characters/PrototypePair/Soldier_LOD0.fbx) | `3c6486421476f9f6f396ebffb819feb01f328707e76ed5ce5447f96d4e6ee4cd` | `99b43fd108a64735b610166d9e1af65e` |
| [Soldier_LOD1.fbx](../../Assets/Game/Content/Characters/PrototypePair/Soldier_LOD1.fbx) | `ce2483d84a555a57accc8ea7bb74c0766df579648ed7b5a92e5cb1f5719f7af6` | `9dc43f2f859042baa9d1e13df3c80c2a` |
| [Soldier_LOD2.fbx](../../Assets/Game/Content/Characters/PrototypePair/Soldier_LOD2.fbx) | `4248ca3ea09e68fbb55b0756d605d30a97814f489a12fdf2801e7da9481c9581` | `749e529ca36c4783b49bd5c6e7face46` |
| [Captain_LOD0.fbx](../../Assets/Game/Content/Characters/PrototypePair/Captain_LOD0.fbx) | `e8030fada5998779e5ac813ee5e5fb8cd3422f50024e38e5a53f65b7f54c8f6e` | `68fded6541ef4a218d2134aebd8cf6de` |
| [Captain_LOD1.fbx](../../Assets/Game/Content/Characters/PrototypePair/Captain_LOD1.fbx) | `0c80caa5d6036e85bbc42191777835678898d90a40bc499e3fda50148eaf7a3f` | `bc45bb80c22042fc927976a7d2323bc8` |
| [Captain_LOD2.fbx](../../Assets/Game/Content/Characters/PrototypePair/Captain_LOD2.fbx) | `ef422f5f0124cacdcfe3fc7dc53badc09c478bfab5b0da94685ee2a9ad3c1a96` | `1bdf83d55e84448db76b79d0b3526772` |

The table above records Task 11 bytes, not the revised current bundle.
Current v1.0.3 hashes are in the live source manifest and refinement receipt.
Prefab/material GUIDs and the Task 11 source/import-policy hashes are recorded in
the [machine-readable Task 11 receipt](../acceptance/task-11-evidence-2026-09-30.json).
The manifest records recipe/configuration fingerprints and full clip timing.

## Anatomical-side correction in v1.0.3

Native portrait review exposed inherited mirrored side naming in the initial
bundle. With Blender forward -Y and up Z, anatomical right belongs on -X.
The new source reflects lateral bind/equipment positions and matching local
pose rotations; Unity now validates named RightHand on positive actor-local X,
with the sword attached there and the shield on the left. All names, hierarchy
relationships, LOD counts, materials, clip durations and GUIDs stay stable.
Bind coordinates and animation bytes intentionally differ from the Task 11
baseline. Native retargeting and rest-side checks are repeated after this repair.

## Remaining acceptance

The 13 representative actions are Idle, Guard, Parry, DodgeLeft, DodgeRight,
CutUp, CutDown, CutLeft, CutRight, Hit, Death, EnemyTell, and EnemyAttack.
Native tests establish import validity and the four soldier cuts retargeting to
both avatars. They do not approve apparent weight, shield contact, tell clarity,
recovery continuity, joint deformation, or skin quality during every motion.
Sword compatibility does not accept axes/maces. Validate their grips and weapon
arcs when those assets are authored.

Founder review, physical portrait framing, sustained device performance, and
final release rights/content approval remain UNRUN or UNVERIFIED as appropriate.
The consolidated checkpoint was initially scheduled after Task 12. The operator
subsequently authorized Task 13's bounded content/pilot work before that outstanding
gate. Physical and final visual approval remain UNRUN. Do not expand the character
family from this technical prototype alone.
