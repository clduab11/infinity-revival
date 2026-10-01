# Forever We Reign: melee combat direction

**Decision, 2026-09-30:** retain anchored, portrait, one-thumb duels. Combine
Infinity Blade's touch-first defense/opening rhythm with Chivalry II's readable
weapon handling, committed momentum, and varied melee silhouettes. The soldier
fights through the throne's guardians to reclaim his crown, then discovers the
throne is cursed. His equipment and enemies belong to that original story.

**Status:** this is the approved direction and a Task 13 authoring contract,
not a claim that the future mechanics or weapon families below are implemented.
The current sword presentation is being polished separately after the supplied
CombatPrototype recording. Physical timing, ergonomics, and final visual quality
still require the [checkpoint](../acceptance/post-task-12-checkpoint.md).

Primary-source findings and their limits are in the
[reference record](../research/2026-09-30-chivalry-melee-reference.md).
The [combat specification](../production/development-plan/03-combat-specification.md)
remains the implemented grammar unless a later bounded work order explicitly
changes it. This document records the operator's newer melee/camera direction;
it does not retrospectively accept the prototype's art or device behavior.

## Product boundaries

- One active opponent, fixed encounter anchors, bounded dodge/lunge/recoil.
- One thumb can perform every required action sequentially. No simultaneous
  guard-plus-swipe requirement and no mandatory camera-control gesture.
- Premium offline game. No paid gacha, paid randomized equipment, or online
  combat requirement. Earned reveals and progression follow the
  [loot specification](../production/melee-loot-and-upgrades.md).
- Launch player families: four swords, four one-handed axes, four one-handed
  maces/warhammers. With eight shields, four helmets, four armor definitions,
  and six talismans, the catalog remains **34 equipment definitions**.
- One shared humanoid rig and one-handed weapon/shield style. No ranged player
  weapons. Two-handed weapons, polearms, and paired weapons are separate future
  styles, not hidden additions to this work order.
- One integer-microsecond combat clock. Physics contacts, animation events,
  camera callbacks, and audio never resolve damage or resource expenditure.

## Gesture grammar and ownership

| Input and captured phase | Current meaning | Direction commitment |
| --- | --- | --- |
| Left swipe in PlayerOpening | CutLeft | Lateral slash following the screen-space swipe |
| Right swipe in PlayerOpening | CutRight | Opposite-side lateral slash |
| Down swipe in PlayerOpening | CutDown | Descending overhead or diagonal cut |
| Up swipe in PlayerOpening | CutUp | **Rising cut**, not a thrust |
| Cardinal swipe in EnemySequence | Timed parry attempt | Authored required direction, not an inferred opposite-blade rule |
| Hold/release shield control | Guard press/release | The control owns the complete touch |
| Tap a dodge control | Left/right dodge | Bounded local displacement and authored avoidance window |
| Tap the ability control | Ability intent only, no ability equipped or effects implemented | Selected ability, equip flow, and effects remain Task 22 work |

CutUp stays a rising cut through the present polish and the first family pilot.
A thrust would require an explicit versioned direction binding, teaching text,
tell conventions, four-direction coverage, and replay/device acceptance. A clip
named CutUp must not silently become a thrust. Chivalry's thrust is a useful
future handling reference, not authorization for that rebind.

Capture the pointer owner and phase at touch start. Commit once at the existing
recognition threshold; require finger release before another arena gesture.
An expired defensive gesture never becomes an offensive command. A touch begun
on guard remains UI-owned even when dragged into the arena. The player releases
guard, then starts a new arena touch to parry or attack.

Retain the single offensive buffer and its 120000 us expiry. A new weapon may
change accepted attack timing, but never adds an unbounded queue, reinterprets
old input, or admits a buffered attack into a different opening. Rejected input
does not restart a clip, emit attack audio, spend a resource, or move the camera.

## Motion staging

The existing domain's `WindupUs` means **start to logical impact**. It currently
contains both visual preparation and blade travel. It is not a separately
implemented damage-active release interval.

For authored motion, distinguish three stages without adding damage authority:

1. Anticipation: prepare the body and weapon, making attack direction readable.
2. Release toward contact: carry the weapon along its authored arc to the
   logical impact's contact pose. A visual release marker can be authored data.
3. Follow-through/recovery: continue beyond contact, decelerate, and return to
   a usable guard or the next admitted attack. Recovery ends at the domain time.

| Motion | Anticipation | Contact and follow-through |
| --- | --- | --- |
| Lateral cuts | Coil chest/hips toward the originating side; chamber elbow | Unwind the torso, then shoulder/elbow; sweep across the target line and settle rather than reverse abruptly |
| Descending cut | Raise weapon above the shoulder/head with a distinct silhouette | Drive through a vertical or diagonal plane; finish below contact, then recover |
| Rising cut | Chamber low and to the originating side | Carry a visible low-to-high arc, then return without flipping the wrist or blade |
| Held guard | Shield covers the incoming line while sword remains usable | Confirmed block produces brief forearm/chest recoil and stays braced while held |
| Timed parry | Deliberate compact intercept, distinct from passive guard | Deflect outward on success, then recover or express the earned opening |
| Hit/guard break | Readable disruption proportional to the resolved result | Pose recovery follows the domain's recovery/stagger timestamp |
| Dodge | Shift weight and bend joints into the chosen side | Bounded displacement, stable feet, and controlled return, with no new avoidance rule |

These are original animation directions inferred from the references. They are
not copied joint curves, motion-capture data, or measured Chivalry angles.
Arm-only rotations and sword-head spinning will not establish apparent weight.
Author coordinated chest, shoulder, elbow, wrist, and lower-body motion while
keeping the root bounded. Contact must remain aligned when retargeted to both
the soldier and captain. Camera tricks cannot hide a broken grip or poor arc.

## Mechanics: current behavior and planned evolution

| Reference lesson | Current implementation | Committed adaptation and activation boundary |
| --- | --- | --- |
| Alternate slash direction | Four cardinal cuts and one-command buffering | Improve distinct left/right paths and recovery continuity now; no new button |
| Initiative and flow | EnemySequence then earned PlayerOpening; recovery prevents spam | Preserve phase rhythm and make each accepted action feel prompt; no freeform simultaneous exchange |
| Held block | Finite guard; depletion staggers | Braced pose and confirmed-impact recoil; shield dimensions/efficiency become item data later |
| Directional counterplay | Timed directional parry builds balance and Focus | Current parry supplies the skillful defensive role. A future Counter tag/rule must be explicit, single-use, and replay-tested |
| Riposte after block | Blocking contributes momentum toward a later opening; no separate riposte command or speed bonus | Reserve a next-opening riposte opportunity after a confirmed defense. It consumes no extra touch; accelerated timing requires a future domain rule |
| Feints | AI commits selected attacks without reacting to unfinished input; player attacks cannot currently be replaced | Author readable enemy anticipation now. Player attack replacement/cancellation is a future rule and remains disabled |
| Heavy attacks | Authored enemy categories can have higher costs/different defense masks | No required hold-to-attack input. A future heavy player attack needs an explicit one-thumb mapping and authored timing/cost, not longer press inference |
| Drag/acceleration | No user-controlled attack arc or free look | Express lean and hip torque in authored clips. Do not let camera/thumb dragging alter contact time |
| Footwork | Bounded dodge movement | Use authored lunge/recoil for weight and distance; no locomotion stick |
| Stamina economy | Guard, dodge charges, and Focus already exist | Keep these resources. A unified endurance bar is not added by this design or Task 13 |
| Hit stop/slow motion | Deferred by combat specification | Only after physical timing qualification; route through the same combat clock, never independent `Time.timeScale` damage timing |
| Jabs, kicks, crouch, throws | Not required launch player actions | Do not spend thumb reach or content budget on these systems in this pilot |

A riposte opportunity must never make a stale EnemySequence gesture an attack.
If later implemented, mark the confirmed result, transfer the opportunity only
at a safe PlayerOpening boundary, and consume it on the first eligible admitted
attack. Any windup bonus is positive, bounded authored timing. Its cost is the
already resolved guard expenditure and use of the opportunity; it does not
quietly introduce stamina or grant immunity. An opening that closes discards it.

A future counter can use the existing defensive swipe grammar and authored
direction matching, without requiring guard to be held. Its extra reward or
active-defense interval would be a new domain capability. Until implemented,
parry remains parry and no active-parry invulnerability is implied.

A future player feint needs an explicit early cancellation window, at most one
replacement command, cancellation of the old owned milestones, and the same
opening identity. That conflicts with today's committed attack and buffer
contract. Task 13 may reserve a disabled capability reference; it must not
activate that behavior from the presence of optional fields.

## Minimum shared-rig weapon pilot

Task 13 establishes data/import support, followed by **one sword, one axe, and
one mace/warhammer pilot**. It does not require all twelve finished launch assets.
Reuse the existing shared 23-bone hierarchy (21 humanoid bones plus equipment
sockets), Humanoid avatars, bounded root policy, and controlled LOD workflow.

Each pilot needs original geometry, grip proportions, a readable silhouette,
material/skin treatment, and provenance. A recolored sword does not prove an axe
or mace family. Rigid equipment can attach to the validated WeaponSocket;
equipment exported in a skinned mesh needs stable rigid weights to its proper
hand/socket. Do not deform a metal weapon to imitate flexible anatomy.

Keep ShieldSocket and WeaponSocket roles, and one unambiguous WeaponTip/contact
marker per equipped weapon instance. Assign and validate exact references or
paths at import; do not choose whichever substring search finds first. All LODs
must retain the same grip, equipment scale, contact marker, and shared skeleton.

| Pilot | Original handling goal | Minimum clip work |
| --- | --- | --- |
| Sword | Balanced flowing arcs, blade alignment, controlled extension | Four distinct cuts; polished guard/parry; coherent recovery into idle or next admitted cut |
| One-handed axe | Head-led chop, committed travel, more deliberate return | Four family-specific cuts that keep the axe head facing the strike; validate grip and shield clearance |
| One-handed mace/warhammer | Compact preparation, visible striking head, heavier terminal follow-through | Four family-specific blows; validate head contact, shorter apparent reach, and controlled return |

Each family must provide all thirteen current motion keys: Idle, Guard, Parry,
DodgeLeft, DodgeRight, CutUp, CutDown, CutLeft, CutRight, Hit, Death, EnemyTell,
and EnemyAttack. Shared defensive clips may be referenced when they pass that
family's grip/clearance review. Sharing a rig does not automatically accept them.
Family-specific defensive variants are allowed without new gameplay rules.

### Provisional pilot timing

**Project tuning choices, not Chivalry measurements or accepted balance.**
These defaults establish concrete validation fixtures. The sword retains the
current logical timing; axe/mace timing is future data and is not activated by
this document. Each of the four cuts initially uses its family's default.

| Family | Visual release start offset us | Start-to-impact `WindupUs` | Post-impact `RecoveryUs` | Clip contactSeconds | Clip durationSeconds |
| --- | ---: | ---: | ---: | ---: | ---: |
| Sword | 60000 | 100000 | 400000 | 0.10 | 0.50 |
| One-handed axe | 90000 | 150000 | 450000 | 0.15 | 0.60 |
| One-handed mace/warhammer | 100000 | 180000 | 520000 | 0.18 | 0.70 |

Logical impact equals accepted start plus WindupUs. Recovery end equals impact
plus RecoveryUs. Visual release start is a presentation landmark, not a second
damage event. ContactSeconds is the authored clip pose at that impact, not an
unconditional delay read from Animator. Different clip durations can be sampled
piecewise before/after contact while preserving these logical timestamps.

Current Task 12 sampling uses a fixed 0.10-second contact pose. Task 13 must
replace that assumption with validated motion-binding data before an axe or mace
pilot is admitted. Never adjust a clip's playback multiplier to change domain
timing. Per-direction timing overrides can be added as explicit authored data,
not inferred from mesh length, mass labels, or animation length.

Longer recovery is a real loadout concession. Keep health damage, balance, and
Focus tradeoffs within the separate provisional family limits in the loot
specification; measure complete opening output before declaring a family balanced.
Character Speed/endurance and timing power-ups remain unresolved future stats.
They cannot silently modify these durations or shrink parry/dodge windows.

## Task 13 data contract

Implement ScriptableObject authoring, immutable conversion, stable IDs, and
validators. The following is a **schema requirement**, not new runtime code.

| Record | Required data |
| --- | --- |
| WeaponDefinition | Stable ID, content version, localization keys, family, one-handed-shield compatibility, geometry/prefab/icon/provenance refs, immutable statistics, motion-profile ID |
| WeaponMotionProfile | Stable ID, compatible rig/handedness/socket roles, thirteen unique motion-key bindings, bounds/scale contract, exact tip/contact marker reference |
| AttackDefinition | Stable ID, captured direction/binding version, positive WindupUs/RecoveryUs, damage and supported cost fields, presentation motion key; enemy attacks additionally require telegraph/defense masks/safe dodge/parry direction |
| MotionBinding | Motion key, clip reference, finite contactSeconds and durationSeconds, optional visual release landmark, loop policy, compatible avatar/rig, bounded-root setting |
| DefenseDefinition | Authored current guard costs, allowed defense masks, required parry direction, safe dodge side, recovery/stagger values; no inferred rule from clip or weapon skin |
| CameraProfile | Stable ID, bounded angles/offsets/envelope, framing target, reduced-motion variant; presentation-only values |
| Optional future capability | Versioned ID with explicit runtime capability requirement; unsupported activation is a validation error, not a fallback behavior |

Use stable namespaced IDs, for example `weapon.sword.pilot` and
`motion.sword.pilot.cut-left`. Their spelling is an authoring choice. Renaming
an asset or localization text must not change its ID. Never derive save identity
from array position, display name, Unity instance ID, or a newly generated GUID.

The Unity authoring layer resolves object references and builds an immutable
runtime snapshot. Domain records contain values and stable keys only; they have
no UnityEngine dependency or asset mutation port. Presentation bindings retain
their own validated clips/prefabs. Conversion runs before encounter admission,
not per frame. Active attacks retain their admitted snapshot if an Editor asset
changes; reload only at a safe encounter boundary.

Validation rejects at least:

- Duplicate/missing stable IDs and duplicate direction or motion-key bindings.
- Nonpositive or overflowing logical timing, contact outside clip duration,
  nonfinite seconds, release landmark outside the pre-contact interval.
- Missing required cuts/clips, invalid Humanoid/avatar compatibility, root-motion
  drift, gameplay animation events, or unexpected loops on attack actions.
- Missing/ambiguous sockets or tip marker, invalid scale/bounds, broken prefab,
  icon, material, or provenance references for the declared readiness tier.
- A ranged or unsupported two-handed player family in the launch catalog.
- Invalid defense masks/directions/costs, or an unsupported optional capability
  requested as active. Reserved feint/counter/riposte/heavy specifications do
  not pass through as gameplay behavior.

Authoring validators report an asset ID, field, and repairable diagnosis. Avoid
silent normalization of an invalid duration/reference. Conversion is atomic:
invalid content never produces a partially usable runtime record.

## Camera direction

Allow bounded fight rotation when it helps expose body motion or weapon contact.
Keep the opponent, its tell, and the gesture convention visible. A small event
response can lean or yaw around the established view without crossing behind a
combatant or swapping screen-space left/right. Introductions and finishers may
use wider authored motion only outside active defensive input.

The current polish implements only its explicitly bounded presentation response.
Per-weapon camera profiles, more elaborate shot selection, and reduced-motion
options are later authored work. Rotation must freeze with the combat clock,
reset on restart/disable/unbind, and not replay an old impulse after reenable.
No camera delta changes attack direction, timing, dodge side, or damage.

During input-critical telegraphs, constrain camera movement more tightly than
during a player opening. HUD stays screen-space. Reduced motion must keep every
required tell readable with camera impulses removed. Camera shake, zoom, trails,
and audio are supplements to the weapon/body pose, not the sole defense cue.

## Acceptance before family expansion

1. Verify the sword polish in the user's recorded scenario and every cardinal
   cut, guard/parry, dodge, hit, death, pause/resume, and restart path.
2. Run enabled/disabled presentation replays at 30/60/120 and jittered rates.
   Compare admitted commands, impacts, resources, phase IDs, and AI decisions.
3. Validate each pilot's geometry, skin/material, grip, contact pose, sockets,
   LODs, recovery continuity, and both-avatar retargeting in native Unity.
4. Review portrait framing and reduced-motion readability on physical devices,
   including one-thumb guard-release-to-swipe travel and repeated-input pacing.
5. Measure opening output and progression tradeoffs for the three pilots before
   populating the remaining twelve-weapon catalog in Task 33.

Passing schema/native automation does not accept visual quality, physical input
latency, thermal performance, or all twelve finished weapons. The README concept
is an art target, not evidence that the prototype has reached that quality.
