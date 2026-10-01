# Original weapon-family pilot provenance

- Status: approved for bounded technical prototype use; final visual/contact approval UNRUN.
- Owner: Chris, Praxen LLC. Source/export ownership for this work order: root Codex agent.
- Recorded and created: 2026-09-30.
- Acquisition: original procedural geometry, original family motion tables, and project-authored URP materials.
- Creator/source: repository recipes `tools/content/build_weapon_pilots.py` and `weapon_pilot_motion.py`, reusing read-only low-level helpers from the accepted project-original `prototype-pair-1.0.3` bundle.
- Intended use: one representative axe and one representative mace on the existing soldier rig, with one shield and three LODs each. The captain remains the accepted sword actor.
- External models/textures/animation donors: none. No copied game assets or choreography.
- Tools: isolated Blender 5.2.1 LTS factory process; isolated Unity 6000.6.0f1 native Humanoid importer and URP 17.6.0 material authoring.
- AI image/animation generation: none for the runtime pilot. Unity credits spent: zero. Asset Store purchases: zero.
- Authoring source: `SourceArt/Equipment/WeaponFamilyPilot/WeaponFamilyPilot.blend`.
- Configuration: `weapon-pilot-export.json` beside the source, reproduced from `tools/content/weapon-pilot-export.json`.
- Generated source record: `weapon-family-pilot-source.json` beside the source, with original source/export hashes, upstream protection hashes, bind hierarchy, family timings, geometry counts, and motion key ranges.
- Source record schema: `SourceArt/Equipment/WeaponFamilyPilot/source-manifest.schema.json`.
- Commercial use/modification/player distribution: project-original work intended for this commercial game. No third-party runtime license has been introduced. Formal project license, final ownership/legal approval, and release eligibility remain UNVERIFIED; technical approval is not a license grant.
- Attribution: no external runtime asset attribution introduced by this bundle. Blender/Unity/package terms remain separately applicable.
- Generation inputs: the accepted original geometry/motion source, explicit original equipment outlines and family bone-local tables. The supplied concept guides documentation/art direction; it is not sampled into runtime textures.
- Modification record: new compact asymmetric axe and mace heads, rigid right-hand weights, left shield, distinct four-direction arcs, continuing recovery, explicit clip/contact data, exact scoped imports, LOD assembly and family material variations. The accepted sword source, six FBXs, prefabs, materials and import policy remain byte-identical to the work-order preflight.
- Review: [Task 13 acceptance](../acceptance/task-13-content-authoring.md), [art production brief](realistic-art-production-brief.md), [authoring guide](task-13-content-authoring.md).

## Imported artifacts

| Artifact | Unity GUID | SHA-256 |
| --- | --- | --- |
| AxePilot_LOD0.fbx | `af28e58b14a935f48afc4ecd19aa761d` | `bef29332721295fe40d514cc24fde70f5a7559233cd027f026f509634f9322d6` |
| AxePilot_LOD1.fbx | `a85d97e6538b1014abb9dcdcda218b7f` | `089e5ddec233501da0f73e7ba561502219b29500b2a5bb5d4ef86105c562dadb` |
| AxePilot_LOD2.fbx | `96030666adfaa3f4ca935b7c54e38cce` | `2918911d937cd9d9f2f753c3e837211163694ede1a58d9d9e8aea2e0cce23a59` |
| MacePilot_LOD0.fbx | `071d52e7d4c6f2a48b2f998131968bcb` | `17758500fba176c5041c9e9bc7b2c0de1c6952c5f3d52880894ef133c921987a` |
| MacePilot_LOD1.fbx | `66a34eac4641b0741a2a8e3b5ee03a61` | `99504308ff171c8f619039c6a4e5a45e644734258cfa2f61ec52496556c57b82` |
| MacePilot_LOD2.fbx | `c9dccb96bd630434f97a74dda4ba48ec` | `3c0d0de62337e8d05bca9ca5bfc969be4ddbf711f7214234fb4c25912c223296` |
| AxePilot.prefab | `11eab70dec7e1f741975a150bc217a07` | `ff94f6d0d7bc4dd96bf98fc1129bc2fbbee86fbe48123133bfe9824ad81744c8` |
| MacePilot.prefab | `d90fbb8a79240fc48a9c8c79545a6ed3` | `a6d6c974855d879d7c1b6a1048c5844db5e6c63a6dc9d0be08198b97c6ebbded` |

Source record SHA-256: `471c79ca75cb17b5c81b1d1ec2942159c9555c025617914d1f589b46225bac93`.

The historical source record inventories the local `PrototypePair.blend1` backup
to demonstrate that export preserved surrounding files. Backups remain ignored
and are not a required source/export dependency in a fresh clone.
