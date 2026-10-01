# Task 11: shared prototype pair and import pipeline

The operator authorized moving development forward and consolidating hands-on
checks after Task 12. Task 10 is deferred, not accepted. Available tooling was
confirmed with Blender 5.2.1 LTS and live Blender MCP inspection of the original
startup scene. Unity remains pinned at 6000.6.0f1 with the approved package set.

## Scope

Create original procedural soldier/captain prototypes with a single humanoid
skeleton, a sword/shield equipment convention, three LODs, and representative
presentation clips. Preserve the original Blender scene and all existing Unity
combat code, scenes, settings, package pins, and metadata. These are prototypes,
not approved final campaign art. No asset purchase or paid image/video generation
is needed to establish the technical workflow.

The shared rig has 21 humanoid bones and two equipment sockets. Normalize to
metres and export Blender Z-up authoring into Unity Y-up using a fixed FBX preset.
Both models use the same skeleton and bind pose. Skin weights are normalized
with at most four influences. Keep materials to ceramic, brass, and cloth.

Representative actions: Idle, Guard, Parry, DodgeLeft, DodgeRight, four cardinal
Cuts, Hit, Death, EnemyTell, and EnemyAttack. Dodge clips cover 360 ms and Cut
clips 500 ms with a 100 ms authored contact pose. Clips remain in-place. They
carry no damage/defense events and cannot determine combat outcomes.

## Execution

1. Write native import regressions before introducing the pair/import policy.
2. Create the source scene and FBX exports through live Blender MCP, preserving
   the default scene. Save the authoring source and export/provenance receipt.
3. Apply a scoped Unity importer only under the admitted prototype pair folder:
   explicit Humanoid mapping, no embedded material import, no cameras/lights,
   capped skin influences, no runtime CPU mesh read, and controlled loop/root
   settings. Lower LODs exclude animation. Restore imported rest transforms
   from the skin bind matrices so FBX animation takes cannot leave LOD0 posed
   differently from its lower LODs; version the postprocessor to force reimport.
4. Assemble reusable prefabs with shared bones, LOD groups, and URP materials;
   create a separate asset review scene without changing GrayboxEncounter.
5. Verify actual imported avatars, shared hierarchy/bind pose, clip identity and
   timing, LOD reduction, and cross-avatar retargeting through native Unity.
6. Record automated evidence and deferred visual/contact/deformation/portrait
   review. Task 12 connects the assets to combat presentation events.

## Decisions

- Ruling: defer physical Task 09/10 acceptance until after Task 12, before Task
  13, under the operator's explicit schedule change. Cost if wrong: later reach,
  performance, or readability findings may require asset/presentation revision.
- Ruling: use original procedural sources to establish the pipeline without
  spending AI credits or acquiring external models. Cost: prototype art and
  motion require founder review and later refinement before final content.
- Ruling: current task authorization and the existing production plan cover the
  bounded pair/import workflow. Additional design approval does not add useful
  evidence before constructing this reversible prototype. The written plan is
  the reviewable record, not permission to expand the campaign or publish.

## Completion reporting

Distinguish automated import/retarget evidence from founder visual acceptance,
physical device qualification, release eligibility, and Task 12 event wiring.
Keep deferred checks UNRUN and link the
[consolidated checkpoint](../acceptance/post-task-12-checkpoint.md).
