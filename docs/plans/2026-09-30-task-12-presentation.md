# Task 12: combat presentation

Execute the approved Task 12 work order: animation, camera, audio hooks, and
effects consume authoritative combat events. Presentation cannot apply damage,
admit actions, advance the clock, or alter progression. Verify the same recorded
commands produce the same outcomes with presentation enabled and disabled.

## Bounded implementation

1. Add observational application notifications for accepted defense/parry and
   admitted player attacks. Buffered or rejected intent must not start a motion.
   Preserve the combat domain and input ordering.
2. Build a presentation observer that copies accepted actions and resolved
   outcomes into immutable motion/cue data. Enemy timing reads live milestones,
   including the warning shift on resume, rather than cached authored schedules.
3. Sample real Task 11 Humanoid clips through manual PlayableGraphs, using combat
   time and bounded blends. Use in-place motion and authored anchor offsets;
   animation events and weapon collisions cannot resolve gameplay.
4. Add bounded camera impulses, pooled impact effects/trails, and optional audio
   hooks driven by resolved results. Freeze motion/effect/camera progression while
   combat time is suspended or counting down. Stop audio on suspension.
5. Save a separate CombatPrototype scene using the existing portrait HUD and
   original pair. Preserve GrayboxEncounter and existing scene identities. Add
   only the new scene to the end of the existing build list.
6. Run native accepted-action, actual bone-pose, resume/restart/unload, and
   enabled/disabled replay checks at 30/60/120 FPS and jittered delivery. Preserve
   previous test identities and package pins. Record rendered Editor evidence,
   source provenance, and the consolidated hands-on checkpoint status.

## Rulings

- Ruling: execute the already approved task in the existing feature checkout,
  preserving its accumulated uncommitted work. The operator explicitly requested
  completion; an additional design approval is redundant. Cost: changes remain
  reviewable together until a later authorized commit/push.
- Ruling: create a separate playable CombatPrototype scene and retain the
  primitive GrayboxEncounter as a regression fixture. Cost: one extra scene and
  a shared root configuration; benefit: preserved earlier acceptance evidence.
- Ruling: add accepted-action observation hooks rather than reconstructing
  admission from gesture notifications or multicast subscriber ordering. Cost:
  small application interface additions, tested for exact admission timestamps.
- Ruling: use original short procedural feedback tones and optional clip hooks;
  no external generation, purchases, or production audio claims. Cost: prototype
  audio needs later art direction and physical listening review.
- Ruling: leave hit stop and slow motion deferred until the basic timing model
  passes physical-device acceptance, as required by the combat specification.

## Completion boundary

Technical Task 12 acceptance requires presentation-independent recorded outcomes,
valid imported motion sampled from combat time, and correct resource lifecycle.
Founder visual/contact/deformation review and Task 09/10 physical qualification
remain UNRUN at the checkpoint immediately after Task 12 and before Task 13.
This task does not implement loot, weapon upgrades, abilities, saves, campaign
content, new player weapon assets, or additional combat styles. No commit or push
is authorized by this task.
