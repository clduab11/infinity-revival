# Forever We Reign: realistic art production brief

**Initial production recipe, 2026-09-30:** build the representative art with original Blender
geometry, the shared humanoid rig, and Unity URP materials and lighting. The
[new supplied concept](../media/forever-we-reign-duel-concept.md) sets the visual
reference. The next asset batch is one sword, one axe, and one mace/warhammer
pilot. A short coastal courtyard becomes the first finished environment at
Task 24. No Asset Store purchase or AI-credit spend is required for Task 13.

**Scope:** a production recipe and tool assessment, not completion of the
realistic character/environment pass. The operator requested the next task;
this brief does not turn all future art into an authorized production batch.
Physical qualification and final art acceptance retain their recorded status.

**Continuation update, 2026-10-01:** Blender and URP describe production tools,
not finished visual quality. Use compatible licensed bases and original/customized
assets where they save practical work, rather than requiring all geometry to be
procedural or original. A separate art conversation will establish one finished
player/captain/coastal-court benchmark under the
[parallel development contract](../handoffs/parallel-development-contract.md),
bringing visual-quality proof forward without claiming Task 24's complete boss
and route delivery. This publication prepares handoffs, not purchases or credit spend.

## Art target and original identity

Aim for believable construction, material response, and readable motion at the
actual portrait camera. A small finished duel space can carry the quality target
while the distant fortress remains a simplified silhouette. Reserve detail for
the player, opponent, weapon, shield, floor contact, and nearby architecture.

| Supplied concept cue | Production treatment |
| --- | --- |
| Weathered ivory plate and dark service cloth | Original articulated ceramic armor, visible thickness and repairs, dark undersuit and a narrow repaired sash |
| Dark captain with a broad shield | A distinct silhouette on the existing shared rig; darker ceramic, worked brass, weathered cloth, practical shield construction |
| Tarnished metallic edges and lamps | Controlled brass and steel response; warm local light with an authored source |
| Rain-wet stone and coastal spray | One stone material with authored wetness variation, shallow puddle accents, sparse rain and mist behind combatants |
| Monumental coastal fortress | Reusable foundry/seawall modules and a distant shell, with purposeful weather-control machinery |
| Blue architectural glazing | Restrained luminous glass records and storm channels, with readable brightness below the combat silhouettes |
| Repeated heraldic banners | Original company/service marks after review; the generated crown-and-tree emblem does not create approved canon |

The [approved melee direction](../design/melee-combat-direction.md) governs
hands, grips, arcs, contact, and the one-thumb grammar. The [lore proposal](../design/forever-we-reign-lore-bible.md)
can guide repairs and civic machinery; its new names and curse details remain
proposals. Preserve two environment kits, four humanoid bosses, and one shared
skeleton family. Do not add a cathedral interior or a new creature rig to match
background detail in a concept image.

## Next asset batch: Task 13 pilot

Task 13 needs meaningful equipment differences and validated motion bindings.
Its schema and import proof do not need finished scenery or twelve launch
weapons. Use the existing soldier/captain pair and shield.

| Deliverable | Concrete work | Evidence needed |
| --- | --- | --- |
| One sword | Refine the existing straight blade, continuous hilt, neutral grip, contact marker, and flowing directional arcs | Four distinct cuts with coherent contact/recovery and sword/shield clearance |
| One one-handed axe | Original compact head and haft proportions; rigid attachment at the established right-hand socket | Head faces the strike in all four cuts; grip, contact marker, and shield clearance remain stable |
| One mace/warhammer | Original compact striking head, plausible grip and shorter visible reach | Four family-specific blows, readable head contact, controlled heavier follow-through |
| Three motion profiles | Thirteen current motion keys per family, validated timing/contact data and shared defensive clips only when appropriate | Humanoid compatibility, bounded root, precise hand/socket references, no damage events in clips |
| Source and imports | Original source recipe, controlled exports, consistent LODs, metadata, stable IDs, provenance | Native import and replay validation; visual/contact/deformation review stays a separate gate |

The thirteen keys are Idle, Guard, Parry, DodgeLeft, DodgeRight, CutUp, CutDown,
CutLeft, CutRight, Hit, Death, EnemyTell, and EnemyAttack. CutUp is a rising cut.
The axe and mace need their own geometry and handling; changing a sword's color
does not demonstrate another family. All families use the existing 23-bone
hierarchy, anatomical right weapon hand, left shield, and exact contact marker.

Keep the pilot's current procedural material treatment while proving its data,
grip, and arc contracts. A production material pass can then refine the existing
pair without creating another rig or animation library.

## First finished space: Task 24 courtyard

Build one short approach and one anchored captain duel court. Proposed starting
kit: twelve reusable modules, one floor slab, one step, one parapet, one arch
pier, one arch span, one gate, one banner assembly, one lantern, one brass
channel, one weather-engine prop, one distant fortress shell, and one cliff/rock
piece. Vary placement, trim, and material values before adding modules.

Use three common surface families, dark stone, ivory/dark ceramic, and worked
brass, plus shared steel, cloth, and glass accents. Author consistent texel
density, UVs, beveled edges and silhouettes in Blender. Bake normal detail where
it helps at the portrait camera; remove small geometry that disappears at that
distance. Fit weapon grips and armor articulation before adding surface wear.

In Unity, build URP materials explicitly. Review albedo under neutral light;
import normal maps as normals and data maps with the appropriate linear color
handling. Check the shader's channel packing and smoothness convention before
assigning generated maps. Wetness should alter surface response with a controlled
mask; painted highlights in base color will fight the scene lighting.

Start lighting with a cool broad key, warm practical lamps, baked static light,
and reflection probes. Keep real-time shadows concentrated on the combatants and
nearby geometry. Use modest fog, a distant storm backdrop, and sparse particles
so tells and weapon silhouettes remain legible. Treat real-time water, heavy
screen-space effects, and dense rain as measured additions after the base arena
meets the existing sustained-device target.

Reuse this kit for the foundry and seawall routes. Region two can reuse the
structural scale, materials, rig, and import conventions while introducing its
limited observatory/glass/weather-engine modules. Bosses vary silhouette,
equipment, materials, and authored moves within the four-boss envelope.

## Tool choices and actual limits

| Capability | Project state and useful role | Decision |
| --- | --- | --- |
| Blender MCP | Live `get_blendfile_summary_path_info` inspection succeeded on 2026-09-30. The operator's open canonical `PrototypePair.blend` had unsaved changes and was preserved | Use original geometry, UVs, material assignments, LOD/export automation, and previews in an isolated authoring process; do not overwrite the operator's dirty scene |
| Unity Editor / URP | Project pins Unity 6000.6.0f1 and URP 17.6.0; an isolated native Unity verification workflow is available | Use controlled import tooling and native Editor checks for materials, lighting, retargeting, and profiling |
| Unity MCP | No Unity MCP tools are exposed in this chat's current catalog | Continue through repository tooling and the native Editor; catalog absence does not establish the organization's configuration |
| Unity AI | Manifest includes Assistant 2.20.0-pre.1. Generator access, accepted terms, entitlement, remaining credits, and installed generator capabilities are unverified here | Optional material/concept trial after the operator checks access and runs it within an approved credit cap |
| Unity Asset Store | Supplies licensed assets; it is distinct from Unity AI generation. No package has been selected or purchased | Consider a compatible animation donor or a focused environment/weather aid only after a named bottleneck and a measured integration trial |

Unity's official [Material Generator description](https://unity.com/blog/unity-ai-material-generator)
supports text/reference input, tileable surfaces, and generated PBR maps for
prototype materials. Its extra PBR/upscale steps are separate operations. The
[Unity 6000.6 AI menu documentation](https://docs.unity.com/en-us/engine/6000.6/manual/unity-ai/ai-menu-access)
lists generators for materials, textures, animation, sound, and sprites. These
published capabilities do not verify access in this project or make generated
motion ready for weapon contact.

Unity AI operations consume credits. Rates vary with model and operation;
documented examples are not fixed quotations. Check the current balance and
cost before a bounded run, record the selected model/settings and before/after
usage, then retain the output metadata. No credits were spent for this brief.
See [Unity Credits documentation](https://docs.unity.com/en-us/ai/credits/credits-about).

Use a licensed animation donor only if it saves more time than retargeting and
repair consume. Trial the current four cuts, guard/parry, reactions, bounded
root, contact, and recovery on both original combatants before expanding a
license family. For an environment aid, require the pinned URP version, mobile
quality controls, source/provenance, and a measured advantage over the simple
authored effect. Avoid a full medieval art pack that dictates another rig or
replaces the ceramic/brass/weather-engine identity.

## Optional Unity AI material trial briefs

These are prepared instructions for an operator-run trial, not a generation
request or approved credit budget. Start with one selected result per surface;
review tiling and material response before requesting variations or upscaling.
Use the existing image for art direction, not as an unverified rights-cleared
texture input.

### Dark coastal stone

```text
Generate a realistic seamless dark basalt paving surface for a storm-battered coastal fantasy foundry. Large worn stone faces, fine salt abrasion, shallow repaired chips, muted charcoal and cool gray color variation. Orthographic surface texture, no perspective, no cast shadows, no painted reflections or light sources, no puddles baked into the color, no text or emblems. Use restrained detail that stays readable at a mobile portrait duel camera. Prepare consistent PBR maps with physically plausible normal depth; wetness will be authored as a separate mask in Unity.
```

### Ivory ceramic armor

```text
Generate a realistic seamless ivory fired-ceramic surface for original articulated fantasy armor. Dense matte ceramic, subtle glaze variation, fine age crazing and sparse abrasion, repaired surfaces rather than shattered porcelain. Warm ivory with restrained gray weathering. No metal, anatomy, armor silhouette, gold filigree, writing, symbols, directional lighting or cast shadows. Keep base color free of highlights. Prepare coherent PBR maps suitable for small curved armor plates; brass fasteners and repair seams will be separate authored elements.
```

### Worked brass

```text
Generate a realistic seamless worked-brass surface for coastal weather-engine fittings. Muted warm brass with directional hand-tool marks, shallow scratches, sparse salt patina in recesses, and readable large-scale variation. No gear shapes, pipes, runes, emblems, embossed objects, lighting, perspective, or painted highlights. Metal response must remain coherent; no glowing color or mirror finish. Prepare consistent PBR maps. Mechanical geometry, contact wear masks, and luminous glass are authored separately in Blender and Unity.
```

## Review before expanding the batch

1. Check the saved native scene at the portrait camera: correct hands, continuous
   grips, shield straps, visible tell silhouettes, contact, recovery, and feet.
2. Review neutral and final lighting on the same assets. Ceramic, metal, stone,
   and cloth must remain distinguishable without relying on bloom or color alone.
3. Run the existing combat equivalence checks with presentation enabled/disabled.
   Art, camera, animation, and particles do not become damage authority.
4. Measure the short duel on the target phones, including sustained frame time,
   memory, thermals, readability, and one-thumb acceptance. Editor rendering and
   concepts do not satisfy those gates.
5. Record actual character/boss/environment authoring and review hours. Expand
   within the two-region/four-boss budget only after the representative workflow
   earns its cost.

Source records follow the [asset provenance policy](asset-provenance.md). The
[asset readiness ledger](asset-readiness.md), [production sequence](development-plan/12-codex-development-sequence.md),
and [consolidated physical checkpoint](../acceptance/post-task-12-checkpoint.md)
remain the authority for staged acceptance.
