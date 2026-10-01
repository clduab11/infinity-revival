# Task 13: content authoring and weapon-family pilot

**Authorization:** on 2026-09-30 the operator supplied the completed duel concept
and requested "Complete next Task." Task 13 proceeds under the approved
architecture and [melee direction](../design/melee-combat-direction.md). This
later instruction overrides the earlier schedule requiring the physical
checkpoint before Task 13. It does not accept that checkpoint: Task 09/10
physical qualification and final Task 11 visual/contact/deformation/device
approval remain **DEFERRED, execution UNRUN**.

**Status:** execution plan. Native checks, acceptance evidence, and final results
are recorded separately when actually run. No commit, push, purchase, dependency
change, AI-credit spend, or broad campaign-art batch is authorized. Preserve
Unity 6000.6.0f1, URP 17.6.0, and the existing package pins.

## Outcome

Author combat content as ScriptableObjects with explicit stable IDs, validated
references, and atomic conversion into immutable runtime records. Connect one
sword, one axe, and one mace/warhammer pilot to the existing encounter at a
deliberate restart. The captain retains the accepted sword setup. All game
outcomes retain the integer-microsecond combat clock and current gesture grammar.

The supplied [concept and provenance](../media/forever-we-reign-duel-concept.md)
guide visual direction. They do not establish a finished Unity environment or
character-quality acceptance. The [realistic art brief](../production/realistic-art-production-brief.md)
defines the later short courtyard and optional surface-material trial.

## Work and ownership boundaries

| Area | Bounded change |
| --- | --- |
| Domain content | Engine-free WeaponFamily, AttackSnapshot, WeaponSnapshot, and EncounterContentSnapshot values with defensive collection copies |
| Content authoring | Definitions, validation diagnostics, and immutable conversion for weapons, attacks, defense, enemy attacks/patterns/decks, motion profiles, camera, and catalog |
| Presentation integration | Validate and capture the entire encounter before replacing a session; freeze timing/contact, motion, camera, and audio authoring data per encounter |
| Pilot source/import | New original source recipe/config/manifest and six allowlisted axe/mace FBXs, three LODs per family, with separate prefabs and import policy |
| Native verification | Tests first for missing schema and pilot assets; native RED before production; focused checks, replay checks, then final applicable suites |
| Documentation | Execution and provenance records, current schedule exception, actual evidence, and retained physical UNRUN status |

Independent domain/content and pilot work can proceed in parallel. Serialize
shared authored Unity assets, Blender exports, native Unity processes, scratch
syncing, and encounter integration. Preserve the accepted PrototypePair source,
its six FBXs, prefabs, materials, original import policy, metadata, and GUIDs.
The operator's interactive `PrototypePair.blend` is dirty; live Blender MCP path
inspection succeeded and that scene is preserved. Use isolated authoring for the
new pilots. No Unity MCP tools are callable in this chat; use repository tooling
and isolated native Unity validation.

## Definition and validation contract

All definitions have explicit lowercase namespaced IDs, a positive version, and
localization keys. Renaming a file, display name, or Unity asset must not change
content identity. Repeated references to one object are legal. Distinct objects
sharing an ID fail catalog validation atomically.

| Definition | Required content and rejection rules |
| --- | --- |
| Attack | Four unique cardinal directions, positive nonoverflowing windup/recovery, supported damage/bonus, release landmark and motion key. Current resolver tuning is shared across directions: reject unsupported directional overrides |
| Defense | Current guard/dodge/parry/recovery tuning; reuse existing domain constructor validation |
| Enemy attack/pattern/deck | Authored allowed defenses, safe dodge side, parry direction, costs/timing, complete steps, selection metadata, archetype/lesson, and explicit content/balance revisions; copy arrays into existing immutable domain records |
| Motion binding/profile | Exactly thirteen unique required keys, valid human clips/avatar, exact socket/tip/renderer paths, finite scale/bounds, correct loop policy, no gameplay animation events, explicit duration/contact/release values |
| Camera | Finite bounded orbit and focus envelope, plus reduced-motion configuration; presentation data only |
| Weapon | Sword/Axe/Mace family, one-handed shield compatibility, four attacks, motion/material/provenance references, prototype or production readiness; production tier also requires an icon |
| Catalog | Full validation with AssetId/Field/Message diagnostics before atomic runtime conversion; invalid content cannot produce a partial usable encounter |

For each cut and EnemyAttack, contact is finite and strictly inside the clip
duration; visual release lies before contact. Duration matches the native clip.
Root motion is bounded and the expected Humanoid rig is compatible. Paths are
exact references under the sole LOD0 Animator, with three weapon renderer paths;
substring search must not pick whichever marker happens to appear first.

Unsupported two-handed/ranged player families and active feint, counter, riposte,
heavy, or other future capabilities fail validation. Reserving a field is not
activation. CutUp remains a rising cut. No physics or animation event becomes
damage authority, and the existing one-command offensive buffer remains bounded.

## Runtime admission and restart

1. Validate the full assigned catalog and presentation snapshot before changing
   seed, archetype, tier, or selected weapon, or disposing the current session.
2. Convert values and stable keys into engine-free domain snapshots. Keep Unity
   clips, prefabs, and materials in immutable presentation snapshots.
3. Apply a selected family only at a deliberate encounter restart. This is a
   pilot selection path, not the Task 20 equipment/persistence interface.
4. Freeze content revisions, weapon identity, contact data, clips, camera, and
   audio arrays for the admitted encounter. Authoring edits affect a later
   restart; they cannot retime an active attack or a recreated animation graph.
5. Preserve the legacy null-catalog prototype path. An assigned invalid catalog
   must fail visibly and preserve the active session, never silently fall back.

Legacy serialized motion bindings with zero contact data retain the old
0.10-second sampling only through the legacy profile path. New definitions must
provide explicit validated contact. Live accessibility camera preferences can
remain separate from frozen authored camera settings.

## Original pilot assets

Create a new source/config/manifest bundle under
`SourceArt/Equipment/WeaponFamilyPilot/`. New actor exports live under
`Assets/Game/Content/Characters/WeaponFamilyPilot/` as
`AxePilot_LOD0.fbx` through `AxePilot_LOD2.fbx` and
`MacePilot_LOD0.fbx` through `MacePilot_LOD2.fbx`, with separate AxePilot and
MacePilot prefabs. The new importer admits exactly those six FBXs; do not widen
the accepted PrototypePair importer.

Reuse the low-level shared-rig construction contract, not its main authoring
entry point or saved scene. The new geometry needs a distinct asymmetric axe
head and compact mace/hammer head, rigid right-hand weights, the established
left shield, and exact head-based contact markers. All LODs retain shared bind
pose, hierarchy, grip scale, sockets, and contact references.

Author all thirteen motion keys at 100 fps. Sword contact/duration retain
10/50 frames. Axe uses 15/60 and mace uses 18/70. These are project pilot tuning,
not accepted balance. Give all four family cuts distinct directional head travel
and coherent recovery. Shared defensive clips are allowed after family grip and
shield-clearance validation. Retarget the family clips to both existing avatars
in native tests.

Record source inputs, recipe/config hashes, Blender/export versions, file
hashes, Unity GUIDs, import settings, triangle/LOD counts, hierarchy, clip timing,
contact paths, and modifications in a provenance record before admission.
Original geometry and source recipes establish this bounded technical pilot;
they do not establish final skinning, perceived contact, art approval, or phone
performance.

## Asset Store and Unity AI follow-through

Task 13 can finish with original geometry and existing procedural materials.
Before any later external asset or generated material is admitted:

1. Name the specific production bottleneck and intended asset role. Keep it
   within the shared rig, short courtyard, two-region/four-boss envelope.
2. For an Asset Store donor, verify the pinned URP/mobile requirements, license,
   creator, commercial modification/redistribution rights, seat restrictions,
   dependencies, and source or durable terms evidence. Prepare a concrete trial
   and obtain purchase authorization before acquiring a paid asset.
3. For Unity AI, verify installed generator access, accepted terms, entitlement,
   current model, credit balance and expected operation cost. The manifest's
   Assistant package alone proves none of those. Obtain a bounded credit-spend
   authorization before generation; no subscription or budget change is implied.
4. Start with the three prepared stone/ceramic/brass briefs in the
   [art-production brief](../production/realistic-art-production-brief.md). Save
   the exact prompts, model/settings, references, generation metadata, source
   hashes, usage terms, and actual before/after credit usage.
5. Trial tiling, neutral lighting, PBR channel packing, final portrait framing,
   and device cost before requesting variations or a wider batch. Animation
   donors additionally need Humanoid retargeting, four-cut contact/recovery,
   bounded root, and shield clearance on both combatants.
6. Retain source files and complete the [asset-provenance record](../production/asset-provenance.md)
   before import/release admission. Unknown rights or missing evidence remain
   UNVERIFIED. A generated raster image does not provide a combat-ready rig.

## Verification and remaining acceptance

First establish native failures for missing schema and pilot assets. Then check
bad/nonfinite/overflow timing, duplicate IDs/directions/keys, null/broken paths,
references and masks, unsupported families/capabilities, defensive collection
copies, rename-stable identity, and atomic invalid conversion preserving the
current session. Verify authored deck parity with the accepted prototype decks.

Native import checks cover all three LODs, distinguishable family silhouettes,
shared bind/hierarchy, valid Humanoid retargeting, and directional head travel.
Runtime checks cover family contact at logical impact, suspension/reenable,
snapshot edits/restarts, cleanup, and presentation-enabled/disabled replay at
30/60/120 FPS and jittered intervals. Finish with applicable native Edit Mode,
Play Mode, and the twelve existing Python verifier tests; record actual counts,
failures, and skips rather than predicting results.

The [consolidated physical checkpoint](../acceptance/post-task-12-checkpoint.md)
still requires recorded phone controls, timing, sustained performance, memory,
thermal behavior, visual/contact/deformation review, and presentation
independence. Automated import, replay, and Editor evidence cannot pass those
physical gates. The schedule tradeoff is possible revision of the new family
assets or presentation after those measurements arrive.
