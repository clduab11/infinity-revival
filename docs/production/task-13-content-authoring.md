# Task 13: authored combat content

Task 13 admits explicit content definitions at an encounter boundary. It does
not add progression, inventory persistence, rewards, active abilities, or a
production art pass. The representative sword, axe, and mace remain one-handed
weapons with a left-hand shield and a shared Humanoid rig.

## Files and entrypoints

- `Assets/Game/Content/Definitions/catalog.prototype.asset`: three weapons,
  four teaching decks, defense, and camera references.
- `Assets/Game/Runtime/Content/Definitions`: authoring records, diagnostics,
  whole-graph validation, motion and camera snapshots.
- `Assets/Game/Runtime/Domain/Content`: engine-free immutable attack, weapon,
  and encounter values.
- `Assets/Game/Content/Characters/WeaponFamilyPilot`: separate axe/mace FBXs,
  prefabs, and URP material variations.
- `SourceArt/Equipment/WeaponFamilyPilot`: original Blender source, export
  configuration, generated provenance, and accepted-source protection hashes.
- `tools/content/build_weapon_pilots.py` and `weapon_pilot_motion.py`: separate
  geometry/export and pure motion recipes.

Use **Praxen > Content > Validate Authored Definitions** after editing assets.
The same whole-graph and global-ID validation runs before a Player build.
Native pilot authoring runs in an isolated batch Editor through
`Praxen.Game.Editor.Task13ContentAuthoring.Build`; the generated assets are
reviewed before their controlled paths are copied into the canonical project.

Open `Assets/Game/Scenes/CombatPrototype.unity`. On the encounter root, the
**Weapon Id** field selects the pilot for the next deliberate restart:

| Stable ID | Family | Logical windup / recovery | Authored clip contact / duration | Visual release |
| --- | --- | --- | --- | --- |
| `weapon.pilot.sword` | Sword | 100 / 400 ms | 100 / 500 ms | 40 ms |
| `weapon.pilot.axe` | Axe | 150 / 450 ms | 150 / 600 ms | 90 ms |
| `weapon.pilot.mace` | Mace | 180 / 520 ms | 180 / 700 ms | 100 ms |

These are pilot values. Damage remains 20 for all three families so the pilot
isolates their geometry, handling, and timing. The catalog contains one example
per family; the remaining launch equipment is Task 33. GrayboxEncounter retains
the legacy code fixture when no catalog is assigned. An assigned invalid catalog
never falls back silently.

## Authoring rules and diagnostics

Every definition has an explicit lowercase namespaced ID, positive version,
and localization key. File renames and Unity GUID changes cannot substitute for
content identity. Repeated references to the same definition are legal; distinct
definitions with one ID fail admission. Diagnostics identify the asset ID,
authored field, and repair needed.

Four cardinal attacks must use `CutUp`, `CutDown`, `CutLeft`, and `CutRight`.
The existing resolver has one offense tuning record per weapon, so directional
timing/damage overrides are rejected. Unsupported capabilities, two-handed or
ranged families, invalid masks, overflow, broken references, and incomplete
motion profiles fail validation. Production-ready weapons additionally require
icons; all current pilots declare `ProductionReady = false`.

Motion definitions contain thirteen explicit keys, a Humanoid prefab, exact
LOD0 tip/socket paths, three weapon renderer paths, clip duration, contact,
release, and loop policy. Imported clips must have their rotation, vertical,
and horizontal root motion baked into bone pose. The Animator never applies
root motion. The presence of Humanoid root curves alone is not a movement test.
See Unity's [root motion explanation](https://docs.unity.com/en-us/engine/6000.3/manual/animation-section/animation-mecanim/avatar-creationand-setup/root-motion).

Logical combat and visual time remain separate: the combat clock admits and
resolves the attack, then piecewise clip sampling maps its impact to the frozen
contact pose and its recovery end to the end of the clip. Clip contact/duration
can differ from logical timing. Neither a marker nor an animation event applies
damage. Visual release data is an authoring landmark, not a new command-cancel
or feint window.

The four asset-authored teaching decks preserve the code fixture's attack IDs,
patterns, selection rules, balance/content revisions, and timing. The shared
`teaching.overhead` attack uses one shared asset rather than duplicate identities.

## Encounter capture and ownership

Restart validates and converts the whole catalog, validates motion data, and
prepares replacement actors/graphs before changing the running encounter.
Failed preparation releases its temporary resources and leaves the current
session, selection, and animation graphs intact. The replacement commits only
after the old presentation is unbound.

Running encounters use copied gameplay values and copied presentation binding,
camera, and audio records. Editing an authoring asset affects the next restart;
disabling/re-enabling presentation recreates graphs from the captured records.
Imported Unity asset references are retained, not duplicated into new meshes or
clips per encounter. Live camera accessibility preferences use a separate API
and do not rewrite frozen authoring.

Provenance must use a canonical project-relative path. Editor validation checks
that its source record exists. Player validation does not require SourceArt or
documentation to be distributed with the build. Addressables loading scopes and
owned resource leases remain Task 14.

## Art production boundary

The [supplied concept](../media/forever-we-reign-duel-concept.md) defines the
realistic visual reference; the pilot remains faceted technical art. Use the
[realistic art production brief](realistic-art-production-brief.md) for later
character surfaces, motion refinement, and the reusable coastal court. No Asset
Store purchase or Unity AI credit expenditure is required for this milestone.
Final grip/contact quality, deformation, one-thumb reach, touch latency, and
sustained physical-device performance remain at the outstanding checkpoint.
