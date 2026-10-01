# Anchored melee motion refinement, 2026-09-30

**Status:** technical acceptance PASS, 707/707 Edit Mode, 125/125 Play Mode, and 12/12 Python tests, with no failures or skips. Physical and final visual acceptance remain UNRUN. This record is separate from the historical Task 11 and Task 12 receipts.
**Scope:** original sword motion, braced block feedback, bounded combat camera, original lore/design proposals, and a revised README image prompt. Task 13 definitions and weapon-family activation remain next work.

## User feedback and inspected evidence

The operator supplied a 45.119979-second, 1912 x 1028, 30 FPS recording of
CombatPrototype in a landscape Editor Game view. The screenshot and sampled
recording show guard/dodge attempts, defeat/restart, and subsequent attacks/victory.
This is informal Editor feedback, not physical phone or final visual acceptance.
The operator selected anchored one-thumb duels with richer melee handling.

The original source held every cut's contact pose from frame 10 through frame 18,
then returned directly to idle. The motion mostly used the upper arm and forearm.
Confirmed blocks emitted their cue without a short character response; the camera
had only the existing small translational impact kick.

## Corrections

- Source bundle advances from prototype-pair-1.0.1 to 1.0.3. Motion-only
  iteration 1.0.2 preceded the inherited anatomical-side correction. Seven cut keys at
  frames 0, 4, 10, 14, 22, 34, and 50 add preparation, bent-elbow contact,
  directional follow-through, and recovery to a ready stance. Hips, chest,
  shoulder, elbow, wrist, and supporting shield contribute to the motion.
- CutUp remains a rising cut; CutDown descends, and the lateral cuts sweep in
  opposite directions. EnemyTell ends in EnemyAttack's starting pose. Guard is
  stable, Parry has a shield intercept, and Hit retains a braced shield.
- Anatomical handedness is corrected in both actors and all three LODs. The
  original rig placed named RightHand on the actor's left side after import.
  Mirrored bind/equipment positions and matching local pose reflection retain
  names, shared hierarchy, normalized weights, geometry counts and timing, while
  placing the sword in the physical right hand and shield in the left.
- Confirmed block uses a 180 ms braced Hit response, then returns to Guard when
  held. It does not change defense admission, health, guard cost, or recovery.
- Lateral cuts and dodges orbit about the authored camera focus ray. Default
  strength is 3 degrees, hard maximum 4 degrees; focus distance is 1 to 12 m,
  default 8 m. The orbit component preserves focus projection and adds no roll.
  The existing separately bounded impact translation remains and can slightly
  shift the projection. Pause/countdown freeze motion; disable, unbind, and
  restart restore the saved pose. Reenable suppresses an already-active action.

All 13 clip keys, 100 FPS source timing, frame-10 sword contact, clip durations,
23-bone hierarchy (with corrected lateral bind positions), geometry counts, material family, LODs, avatar import policy,
and existing asset GUIDs are preserved. No Domain/Application gameplay changes,
package changes, hit stop, paid draws, new resources, or active abilities were
introduced by this refinement.

## Native verification and evidence

The native Unity 6000.6.0f1 runs use a graphics-backed isolated scratch project.
The operator's open Unity and dirty Blender sessions were preserved. Original
source and exports were backed up under output/motion-review/source-backup.
A factory-startup Blender 5.2.1 LTS process exported into a fresh staging folder;
validated exports were copied while retaining existing Unity metadata.

Legitimate regression RED runs:

- Eight sword path cases failed because contact-to-follow-through displacement
  was below 0.000001 m, against a 0.025 m minimum.
- Nine initial camera cases failed because bounded profile settings did not exist.
- The oversized camera-distance case exposed 68.2847 m of displacement. The
  clamp limits orbit displacement below 0.84 m at the maximum yaw/distance.
- The confirmed-block case failed because the character remained in Guard.
- The first full native Edit run passed 697 of 701 cases. Its four failures
  identified reversed horizontal cut labels on both avatars after FBX axis
  conversion. The source rows were corrected to match imported Unity travel;
  directional assertions were retained unchanged.
- Visual review then exposed inherited mirrored anatomical hands. Six additional
  rest-pose cases failed at RightHand x = -0.700 m on both actors and every LOD;
  the expected physical right side is positive actor-local X. This triggered
  the 1.0.3 source correction before weapon-family expansion.

Fixture setup mistakes were repaired before using RED as implementation evidence:
the Edit test initially referenced an unreferenced Presentation assembly, the
tip lookup included all three LOD markers, and the guard command initially
arrived after its same-timestamp frame had already closed. None was treated as
proof of a production defect. An independent audit also corrected a camera test
that assumed exactly 12 degrees of baseline pitch instead of the authored pose.

Final suite results, preservation checks, current hashes, blade trajectories,
and portrait captures are recorded in the [machine-readable receipt](melee-motion-evidence-2026-09-30.json).
The source and live export manifest retain the final bundle identity.
All 693 prior Edit and 113 prior Play identities are retained; 14 new Edit and
12 new Play cases cover imported blade travel, anatomical rest sides, confirmed
block response, bounded camera behavior, and portrait evidence. The full runs
repeat the existing enabled/disabled 30/60/120/jitter replays without changing
their combat or AI outcomes. All 265 protected preflight files remain identical.
Structural checks pass with 192 unique asset GUIDs, 115 Game C# sources, and
the unchanged 50 direct/69 resolved package baseline.

![Current corrected-handedness prototype](evidence/melee-motion-current-combat.png)

*Native 720 x 1280 Editor evidence, prototype geometry/materials. Cardinal windup
and follow-through frames, guard and block recoil, trajectories, XML receipts,
and replay JSON are retained with the melee-motion evidence prefix.*

## Design and narrative handoff

The [melee direction](../design/melee-combat-direction.md) adapts the readable
body momentum and weapon commitment described in [Torn Banner's official FAQ](https://chivalry2.com/faq/)
and [combat recap](https://chivalry2.com/2022/03/24/advanced-combat-twitch-stream-recap/).
These are handling references, not copied animations or measured joint angles.
Reference timings and camera degrees in our design are original project choices.

Task 13 owns stable weapon IDs, immutable conversion, validated timing/contact
bindings, and a representative sword/axe/mace pilot. Family-specific skins,
grips, silhouettes and four-cut sets require actual native import and contact
review. Full twelve-weapon catalog production remains Task 33. Feints, riposte
bonuses, active counters, heavy inputs, and unified stamina remain disabled
future capabilities, not behavior activated by this document.

The [original lore bible](../design/forever-we-reign-lore-bible.md) proposes
Aulden Reach, soldier-king Iven Sarr, four wardens, and the Anchor Seat. Its curse
binds rulers to conflicting unfulfilled civic vows, preserving the approved
final revelation and sequel hook. Equipment names and earned pattern
reproductions fit the existing 34-definition envelope. New details are writing
proposals; the approved soldier/throne premise stays authoritative.

The [revised GPT-Image prompt](../media/forever-we-reign-image-prompt.md) specifies
right-hand sword and left-arm shield for both actors, clear grips, plausible
blade alignment, a pre-contact duel, and original fantasy atmosphere. No image
was generated or replaced; no image/video/Unity AI credits were spent.

## Remaining acceptance

The [consolidated physical checkpoint](post-task-12-checkpoint.md) remains due.
Phone reach, input latency, sustained performance/thermals, final contact and
deformation, skin/material quality, and all weapon-family acceptance remain
UNRUN. Native sword-path and camera tests cannot establish those results.
The export recipe is 425 lines with a separate 188-line motion helper. Its
58-line export orchestrator should split per-actor construction from manifest
writing when Task 13 expands the pipeline. No larger runtime abstraction was
introduced for this motion correction.

The current geometry/materials remain prototype content. The README illustration
is a concept target, not current rendered quality.

The open Blender session retains unsaved operator edits and therefore may show
the earlier in-memory source until the updated on-disk bundle is opened. It was
not saved or replaced through the interactive MCP session. Historical Task 11
and Task 12 reports retain their original test counts and hashes.
No commit, push, purchase, or dependency change was performed.
