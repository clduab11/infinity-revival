# Task 12 independent review

Recorded 2026-09-30. Read-only source review by a separate agent, followed by
native verification by the root agent. No outstanding source blockers remain.

## Findings closed

1. P2: disabling presentation retained the camera impulse timestamp, replaying
   an old kick on re-enable. Disable now clears that timestamp.
2. P2: removing the presenter component retained four owned audio objects.
   Destroy now releases each owned voice object.
3. Root inspection and independent confirmation: C# null-conditional calls
   reached the destroyed Unity component on restart. The composition root now
   checks Unity object validity before bind, render, and unbind.
4. Native render inspection: the original pair faced away from its opponent.
   Corrected actor orientations and an encounter-axis test cover the fix.

Native RED receipts preserve each failure. The final 113/113 Play Mode run
passes all strengthened lifecycle, facing, and removal/restart assertions.

## Requirements checked

- Accepted-action notifications follow actual combat admission. Buffered and
  rejected intent do not create an early motion.
- Enemy motion reads shifted live milestones after resume.
- Manual graphs and immutable motion cues preserve one combat clock. No
  damage, phase, progression, or collision authority enters presentation.
- Disable, restart, component removal, and scene unload release or reset their
  owned resources and detach the old encounter.
- The terminal fixture records one command list per scenario, preserves phase
  identities, reloads equivalent roots, and compares admission, timeline,
  resources, initial/current AI schedules, history/count/random state, and
  literal guard/dodge/victory/defeat outcomes across all four cadences.
- Real hand/contact and Death bone progression tests sample imported clips.

## Verification boundary

Final root execution passes 693/693 Edit Mode, 113/113 Play Mode, and 12/12
Python tests, with zero failures or skips. All 112 Game C# sources match the
native tested scratch project; previous test identities remain present.

The reviewer did not run Unity or mutate files. Physical phone performance,
same-hand usability, final contact/deformation/readability, audio levels, and
production art approval remain UNRUN at the consolidated checkpoint.
