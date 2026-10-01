# Two development conversations, one game

The operator requested complete bootstraps for **GPT-6-Astra, Ultra** (technical)
and **GPT-6.1-Sol, Ultra** (art, third conversation) on 2026-10-01. These are
desired conversation settings. The operator should select them in the app;
writing a prompt does not change the actual model or reasoning setting.

Both conversations use `clduab11/infinity-revival`, the published checkpoint,
the [repository audit protocol](repository-audit-protocol.md), and this contract.
Use isolated working copies for concurrent edits. This document does not create
chats/worktrees, grant purchases, or grant future publication authority.

## Product and visual constraints

**Forever We Reign:** a soldier fights through bosses to regain his throne, then
discovers the throne is cursed, setting up a sequel. Original lore and assets;
Chivalry II informs grounded melee handling and Infinity Blade informs anchored
duels/cyclical appeal. Do not copy those games' art, names, lore, or choreography.

Portrait, one-thumb, anchored duels with richer one-handed sword/axe/mace handling
and left-hand shields; no ranged weapons, free movement, or player-controlled aim.
One shared humanoid skeleton family, two region kits, four bosses, four regular
archetypes, and 34 equipment items, including 12 melee weapons. Offline premium,
earned reward reveals, no paid gacha. Active abilities remain Task 22; the wider
catalog remains Task 33. Do not add mechanics or rigs to accommodate a donor.

The selected [duel concept](../media/forever-we-reign-duel-concept.png) is a visual
target, not a runtime asset or gameplay screenshot. Preserve weathered ivory
armor, a dark captain, coherent right-hand weapons/left-hand shields, wet coastal
stone, warm lamps, and a storm-bound fortress. Ceramic/brass/weather machinery
and new lore names in the lore bible are proposals, not approved changes to canon.

Use a practical hybrid of compatible licensed assets and original/customized
work. Blender and URP define an authoring/rendering pipeline, not guaranteed
quality. Do not require every production asset to be built from procedural
primitives. AI concept/surface work needs provenance and actual visual review.

## Ownership and first work orders

| Owner | Exclusive integration responsibilities | First bounded deliverable |
| --- | --- | --- |
| Technical, GPT-6-Astra Ultra | `Assets/Game/Runtime/`, `Editor/`, `Tests/`, production `Content/` and `Scenes/`, import policy, catalog/bindings, `Packages/`, `ProjectSettings/`, integration and native verification | Task 14, owned Addressables loading scopes and leases; load/cancel/failure/retry/release must not retain encounter handles |
| Art, GPT-6.1-Sol Ultra | New production source bundles, dedicated art export recipes, preview content in its isolated worktree, art references/provenance and asset handoff manifest | VisualBenchmark01: one finished player, one captain, one compact coastal courtyard, reviewed in actual Unity combat |

Art-owned new paths:

```text
SourceArt/Characters/VisualBenchmark01/
SourceArt/Equipment/VisualBenchmark01/
SourceArt/Environments/VisualBenchmark01/
tools/content/art-benchmark-01/
Assets/ArtStaging/ForeverWeReignBenchmark01/
docs/handoffs/art-batches/visual-benchmark-01/
```

New Unity staging assets require metadata and provenance. Art does not edit
production catalog/scene/settings assets in the canonical project. Technical
imports the reviewed bundle through a separate bounded production integration
assignment. Art may assemble NEW preview scenes, prefabs and data bindings
entirely under its isolated `Assets/ArtStaging/ForeverWeReignBenchmark01/`, using
unchanged existing runtime code and unique IDs for any new definitions. This
permits actual combat proof without expanding Task 14. Art specifies scene
composition, lighting and materials, supplies previews, and reviews gameplay
output with technical. Claim shared documentation before
editing it. A second chat is not permission to run two writers on the same file.

Do not run `Task13ContentAuthoring.Build` as an art-only operation: it rewrites
the production catalog and saves CombatPrototype. Preserve the accepted original
prototype pair and pilot source as comparison inputs; create a new art batch.

The benchmark brings visual-quality proof forward from Task 24. It does not
complete Task 24's boss phases, rewards, exploration route, or acceptance suite.
Task 14 continues independently. Neither track silently advances unrelated tasks.

## Stable interface

- Domain combat clock/resolver alone decides gameplay. Animation, rendering,
  physics and camera callbacks cannot decide damage, defense or progression.
- Current shared hierarchy has 23 bones, 21 Humanoid mappings and weapon/shield
  sockets. Preserve compatible scale, axes, bind pose, Humanoid validity, exact
  marker/socket paths, renderer references and the sole LOD0 Animator.
- Thirteen motion keys: `Idle`, `Guard`, `Parry`, `DodgeLeft`, `DodgeRight`,
  `CutUp`, `CutDown`, `CutLeft`, `CutRight`, `Hit`, `Death`, `EnemyTell`,
  `EnemyAttack`. `CutUp` is a rising cut. Weapon right, shield left.
- Record clip duration/contact/release independently of logical combat timing.
  Root movement is baked into bone pose and `applyRootMotion` remains false.
  Animation events do not apply gameplay damage. Match existing loop policy.
- Stable namespaced content IDs differ from Unity GUIDs. Retain existing `.meta`
  files/GUIDs; move source and metadata together. Binary files use LFS; YAML and
  metadata remain text. Do not merge binary source by accepting an arbitrary side.
- Immutable encounter/presentation capture changes on deliberate restart. An
  invalid assigned catalog fails admission rather than silently falling back.

The [Task 13 guide](../production/task-13-content-authoring.md) and current code
are the detailed contract. Document proposed interface changes before either
track edits them; technical owns final compatibility approval.

## Handoff bundle and acceptance

Each art batch provides editable sources, exports, textures/material specifications,
LODs, rig and attachment details, animation/contact table, file hashes, Unity GUID
mapping, provenance/license evidence, import instructions, and review captures.
Name the exact source and integration revisions. Technical records importer,
validator, scene and gameplay verification, then returns an actual gameplay
capture for art review. A pretty standalone render is not a playable visual pass.

Reference target: 60 FPS default, optional qualified 120 FPS. Existing physical
thresholds remain authoritative. Do not invent triangle/texture budgets: measure
the representative duel, record memory/frame time/thermals and revise the bundle.
Editor rendering and replay equivalence do not qualify an iPhone or Android phone.

At publication the technical evidence is 841 Edit Mode, 155 Play Mode and 24 Python
tests, with 53 definitions, three weapon pilots and four teaching decks. Final art,
contact/deformation review and physical phone qualification remain **UNRUN**.
Task 10 was deferred by the operator; report the gap without blocking independent
engineering or pretending the gate passed.

## Coordination and budgets

Repository documents are the shared source of decisions. Each chat writes a
short status/handoff naming changed paths, source revision, verification, next
step and blockers. Reconcile its incoming bundle before integration. Cross-chat
messaging requires direct human authorization; the other agent cannot grant it.
If authorized, use app messaging so the operator is not a manual courier.

Do not spend Unity/Venice credits, purchase Asset Store assets, start stopped
services, change budgets, or publish future work without applicable authorization.
Research and prepare concrete candidates, costs, compatibility trials and exact
generation prompts first. The operator-reported 1,000 Unity credits are not a
verified balance or approved spend cap. Preserve dirty interactive editor work.

When context compacts, reload this contract, the bootstrap receipt and the latest
track handoff, rather than reconstructing project state from conversation prose.
