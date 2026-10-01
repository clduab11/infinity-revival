# Task 13: content authoring and weapon-family pilot

**Technical acceptance: PASS, 2026-09-30.** Native Unity verification passes
841/841 Edit Mode and 155/155 Play Mode tests; Python passes 12/12 existing
repository checks and 12/12 new family-motion checks. No failures or skips.
All 707 previous Edit Mode and 125 previous Play Mode identities are preserved.
**Physical qualification and final art/contact acceptance: UNRUN.**

The operator explicitly requested the next task after supplying the new concept.
This authorizes Task 13 development despite the outstanding consolidated hands-on
checkpoint. It does not accept that checkpoint or authorize purchases, AI credit
spend, dependency changes, commit, or push.

## Delivered behavior

- Nine ScriptableObject definition types, explicit stable IDs and versions,
  diagnosable whole-graph validation, and engine-free immutable attack/weapon/
  encounter conversion. The catalog admits 53 authored definitions.
- Three representative one-handed weapon definitions with four cardinal attacks
  each; all retain the left shield. Sword uses the accepted source; separate
  original axe/mace geometry, material variations, family-specific arcs, and
  three LODs each live in the new pilot bundle.
- Four asset-authored teaching decks preserve the code fixtures' identities,
  rules, timings, content revision, and balance revision. The shared intro attack
  uses one shared definition.
- Explicit thirteen-key motion profiles, exact LOD0 marker/socket paths,
  Humanoid/root-bake checks, contact/release/duration data, and bounded camera data.
- Prepared presentation replacement validates actor/graph resources before
  changing root state. Invalid content or presentation leaves the running
  encounter intact. Binding/audio/camera values remain frozen through graph
  recreation; live accessibility preferences use a separate API.
- An Editor validation menu and build preprocessor reject invalid definitions
  and duplicate IDs. Runtime admission revalidates assigned catalogs, with no
  fallback to code fixtures on failure. Graybox's unassigned legacy path remains.
- README uses the unedited supplied image. Provenance and a realistic-art brief
  connect its coastal stone, storm light, ceramic armor, brass, and duel framing
  to later original Blender and URP production work.

No inventory/progression UI, loot transactions, persistence, feints, counters,
riposte rewards, unified stamina, active abilities, free aiming, or production
courtyard has been added. Addressables ownership/loading is Task 14; finished
courtyard work is Task 24 and the remaining equipment catalog is Task 33.

## Native and source evidence

| Check | Result | Portable evidence |
| --- | --- | --- |
| Missing schema/pilot tests before implementation | 53 expected failures | [RED XML](evidence/task13-schema-pilot-red.xml) |
| Missing admission/contact/presentation API | 5 expected failures | [RED XML](evidence/task13-presentation-red.xml) |
| Final native Edit Mode | 841 passed, 134 added, no prior identities lost | [XML](evidence/task13-editmode.xml) |
| Final native Play Mode | 155 passed, 30 added, no prior identities lost | [XML](evidence/task13-playmode.xml) |
| Existing Python validator suite | 12 passed | [Output](evidence/task13-baseline-python.txt) |
| New pure family motion suite | 12 passed | [Output](evidence/task13-pilot-python.txt) |
| Repository baseline and native Git/LFS rules | PASS, 50 direct/69 resolved packages, 295 GUIDs | [JSON](evidence/task13-baseline-final.json) |
| Per-family replay, 30/60/120 FPS and jitter, presentation enabled/disabled | 24 equivalent replays | [Sword](evidence/task13-family-replay-sword.json), [axe](evidence/task13-family-replay-axe.json), [mace](evidence/task13-family-replay-mace.json) |
| Read-only review and failure resolution | Findings resolved and regression covered | [Review record](evidence/task13-review.md) |

The [machine-readable receipt](task-13-evidence-2026-09-30.json) includes test
identity comparison, production/source hashes, source provenance, concept hash,
protected sword files, and all portable evidence hashes. Existing Task 11/12 and
melee-refinement receipts remain unchanged.

Native tests cover invalid/overflow timing and damage, masks, nested references,
duplicate IDs/directions/keys, malformed paths and actors, camera/contact finite
bounds, unsupported capabilities, array-copy isolation, exact teaching-deck
parity, and atomic admission. Native family checks cover all six FBX imports,
shared bind hierarchy, rigid right-hand vertex influences, real head travel for
all four cuts, follow-through, and Humanoid retargeting on both accepted avatars.
Logical impact samples 100/150/180 ms contacts for sword/axe/mace respectively.

## Native pilot captures

<img src="evidence/task13-sword-idle.png" alt="Task 13 sword pilot in the native prototype" width="250">
<img src="evidence/task13-axe-idle.png" alt="Task 13 axe pilot in the native prototype" width="250">
<img src="evidence/task13-mace-idle.png" alt="Task 13 mace pilot in the native prototype" width="250">

These are graphics-enabled desktop Editor renders at 941 x 1672, not phone
captures or final art. Contact captures are also retained in the evidence
folder. The technical pilots intentionally retain faceted procedural geometry
while their import, timing, grip, and arc contracts are established.

## Production and tooling bounds

Native checks used an isolated Windows Unity 6000.6.0f1 Editor project, revision
f7f8ed4d1e24, with graphics enabled. The original weapon-family source was created
by a separate Blender 5.2.1 LTS factory process. The user's interactive Blender
file was dirty when inspected through the connected MCP and was preserved.
The open operator Unity project was not automated or closed.

The accepted sword source bundle, six FBXs, materials, prefabs, import policy,
recipe/config/motion, and their preflight hashes are preserved. Dependencies and
ProjectSettings did not change. No purchase, generated image/animation, Unity AI
credit expenditure, commit, or push occurred.

Use the [authoring guide](../production/task-13-content-authoring.md),
[pilot provenance](../production/weapon-family-pilot-provenance.md), and
[realistic art production brief](../production/realistic-art-production-brief.md)
for the next production steps. Original Blender geometry and URP materials are
the recommended route. No new external asset is required for Task 14. Optional
Unity AI surface trials and donor-asset requirements are prepared in the brief;
no paid operation has been requested.

The [physical checkpoint](post-task-12-checkpoint.md) still owns final grip and
contact review, deformation, portrait readability, physical reach and touch
latency, sustained-device performance, and platform qualification.
