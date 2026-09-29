# 8. Content production pipeline

## 8.1 Launch content envelope

| Content | Budget |
|---|---:|
| Player combat style | 1 |
| Shared humanoid skeleton family | 1 |
| Regions/environment kits | 2 |
| Bosses | 4 |
| Regular archetypes | 4 |
| Elite variations | 4 |
| Equipment definitions | 34 |
| Active abilities | 3 |
| Legacy perks | 6 |
| Difficulty tiers | 6, including base tier |

Each region contains approximately twelve authored nodes: entrance/exit, junctions, encounter alternatives, two mandatory boss nodes, and optional cache or narrative nodes.

Route alternatives reuse region assets while changing encounter order, rewards, and presentation.

## 8.2 Character workflow

1. Select a compatible licensed base or create an original mesh.
2. Record license and provenance.
3. Normalize scale, axes, skeleton naming, and bind pose.
4. Validate retargeting with the representative combat clips.
5. Create original silhouette and equipment combinations.
6. Produce LODs.
7. Validate skinning and deformation.
8. Build materials and texture variants.
9. Import through controlled presets.
10. Assemble the prefab.
11. Validate contact, portrait framing, and performance.
12. Approve the asset family before expanding it.

A licensed animation family must pass this process before further assets from that family are purchased.

## 8.3 Equipment workflow

Weapons and shields must fit a validated size envelope. Equipment variation cannot require different hit-resolution rules.

Armor uses compatible attachment and body-mask conventions. Avoid arbitrary mixing of unrelated modular armor sets.

Every item includes:

- Definition.
- Visual reference.
- Icon.
- Display/localization keys.
- Statistics.
- Mastery requirements.
- Upgrade costs.
- Source/license record.

## 8.4 Enemy creation workflow

Create enemies through a reusable prefab and definition workflow:

1. Select skeleton and visual assembly.
2. Assign combat statistics.
3. Select validated attacks.
4. Author pattern deck.
5. Set defensive masks.
6. Add phase behavior where required.
7. Attach camera and audio profiles.
8. Assign rewards.
9. Run content validation.
10. Play the encounter on device.

Editor tooling must flag:

- Missing IDs or duplicate IDs.
- Missing animations.
- Invalid timing order.
- Impossible defensive masks.
- Broken phase references.
- Missing reward entries.
- Addressable dependency problems.
- Missing licensing records.

## 8.5 AI and MCP use

| Tool | Production use |
|---|---|
| Codex | Gameplay code, validators, import tools, build scripts, documentation |
| Unity MCP | Scene/prefab inspection and bounded authoring after connection verification |
| Blender MCP | Repetitive modeling operations, transforms, material setup, LOD/export scripts, previews |
| Image generation | Original concepts, icons, decals, supporting textures, UI illustrations |
| Desktop Commander | Controlled file/application operations where a direct integration is unavailable |
| Research connectors | Version-specific documentation and source verification |

Generated raster images do not substitute for rigged combatants or coherent motion.

Keep generation prompts, selected outputs, edits, and provenance alongside asset records. Review generated imagery for consistency with the original art direction.

**Current connection status:** Unity MCP is not exposed in this chat. Blender tools are registered, but the read-only connection check failed. Both integrations require a verified handshake before their automation is scheduled as available capacity.

---


[Return to development plan](../development-plan.md)
