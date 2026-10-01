# Art restart prompt: Forever We Reign

Copy the message below into the first user message of the third conversation, using GPT-6.1-Sol with Ultra reasoning.

---

Resume **Forever We Reign** as the art owner. First establish the complete, current local and GitHub repository state. Then produce a reviewable asset plan and begin the reversible work for **VisualBenchmark01: one finished player, one finished armored captain, and one small coastal duel courtyard**, aiming at the supplied concept in actual Unity combat. You are working alongside a separate engineering conversation. Protect its work and the accepted technical prototypes.

I am Chris, Founder/CEO of Praxen LLC. Act as an embedded senior engineer and art-production peer. Prioritize shipped quality, reduced solo-founder friction, and evidence. Lead updates with the outcome, use plain language, no praise, no em dashes, and no invented completion percentages. State your opening plan in 3–5 bullets and execute. Ask exactly one highest-leverage question only when genuinely blocked. Use subagents for independent research or review when useful, with bounded file ownership; do not give them Palace tools.

## Authority and working roots

- Canonical Windows project: `C:\Users\cld-main\Desktop\github-projects\infinity-redux`.
- Canonical WSL project: `/mnt/c/Users/cld-main/Desktop/github-projects/infinity-redux`.
- GitHub repository: `https://github.com/clduab11/infinity-revival`.
- The canonical outer directory directly contains `Assets`, `Packages`, and `ProjectSettings`. There is no additional `infinity-redux` or `infinity-revival` suffix. A nested template or alternate Unity project is not the production project.
- The local folder name, repository name, game title, and `Praxen.Game` namespaces deliberately differ. Do not rename source, folders, or GUIDs to make them match.
- Read `docs/handoffs/repository-audit-protocol.md` and `docs/handoffs/parallel-development-contract.md`. Their shared audit and ownership rules apply. Critical requirements are repeated below so this message works without previous chat history.
- Discover the checkpoint SHA from the verified checkpoint record and current Git state. No SHA in this message establishes today's HEAD or remote baseline. Do not infer the checkpoint from a historical receipt or remembered remote commit. Report any mismatch before dependent work.

This message authorizes a current audit, public asset research, exact asset/trial planning, and reversible source/staging work for this bounded visual benchmark. It does not authorize purchases, paid generation, Unity AI or Venice credit expenditure, dependency changes, publication, commit, staging, push, merge, destructive cleanup, or starting an intentionally stopped service. Prior conversation authorization to commit or push does not carry into this art conversation. Stop before a destructive action, show the exact command, and ask. Do not create another chat. Never expose secrets, hub configuration, or supplied credentials.

## First gate: recursively audit local and GitHub evidence

Do this before changing assets or opening a writable canonical Unity project. Use the actual installed environment and configured tools; inherited chat capabilities, memory, and old acceptance numbers are leads, not current evidence.

1. Resolve the physical canonical root, Git executable, repository identity, branch, HEAD, default remote branch, remotes, dirty index/worktree, untracked files, and all existing worktrees. Record current processes and open Unity/Blender projects. Check path case, links, nested `.git` directories, alternate/template projects, and whether another writer holds the canonical project. Preserve all existing and unsaved work. Do not stash, reset, clean, overwrite, close, or save the operator's open project merely to simplify your audit.
2. Recursively inventory the local canonical tree, including dotfiles and ignored paths. Separate versioned source, modified versioned source, untracked source, ignored caches/build outputs/raw evidence/editor backups, nested templates, and external attachments referenced by documents. Record counts and sizes by category with the commands/tool and UTC timestamp. Aggregate caches where appropriate; do not confuse cache bytes with source completeness. Do not read or print secret values. Referenced external files are not magically included in a Git checkpoint.
3. Independently inspect GitHub, its advertised refs and default branch, and freshly fetched refs. Fetch through a verified working Git/LFS executable without changing the active checkout or merging. Inspect complete remote trees, not just the README or latest commit. Compare the discovered local HEAD, checkpoint, relevant remote branches, and actual local working files. Include branch divergence and worktree ownership. If network/authentication blocks remote verification, mark it UNVERIFIED, preserve the exact failure, and continue independent local inventory.
4. Classify every relevant source/evidence difference as **same, local-only, remote-only, different, or unverified**. Classify storage separately as **versioned, ignored cache/output, untracked source, nested template, or external attachment**. Use path and byte/hash evidence for comparisons. A tracked file name does not prove the content arrived, and an untracked file is not automatically disposable. Include `SourceArt`, recipes, manifests, imported binaries, textures, motions, `.meta` files, scenes, catalogs, packages/settings, documentation, and portable acceptance evidence.
5. Verify Git LFS attributes and actual payloads. For each required LFS source/import file, distinguish pointer text from materialized bytes, read the pointer's SHA-256 OID and size from Git, and verify the real payload hash and size. Inspect missing objects, pointer-only working files, and remote payload availability. Use an isolated comparison/materialization location where necessary; do not replace dirty working files. `.meta`, Unity YAML, licenses, and provenance records remain ordinary Git text. Native Windows Git was required on this workstation because WSL Git lacked LFS; validate that fact on arrival rather than assuming it remains true.
6. Verify Unity asset and folder metadata: each required imported asset has its matching `.meta`, GUIDs are unique and references resolve. Preserve existing GUIDs during moves or controlled integration. Do not regenerate missing metadata speculatively or copy assets without their metadata. Derive current source, package, definition, rig, clip, asset, GUID, and evidence counts from the audited tree, not memory or a hardcoded former milestone.
7. Discover current skills and configured MCP capabilities, installed Blender/Unity versions and paths, native command availability, active service state, and licensing/entitlement evidence. Use read-only live probes before relying on a connector. Tool catalog presence is not a successful connection; package presence is not generator access. Do not start a stopped connector or service without permission.

Produce a concise audit receipt with the verified root, executables, timestamps, branch/HEAD/checkpoint/default remote SHA, source counts, dirty/worktree/process state, local/remote comparison, and payload/GUID findings. Include a gaps table with **path or capability, evidence, classification, impact, owner, safe repair, and acceptance check**. Then give an ordered safe repair plan. Distinguish read-only investigation, reversible isolated repair, engineering-owned changes, and actions needing explicit authorization. Do not silently fix discrepancies across ownership boundaries. A checkpoint only captures saved disk files; unsaved interactive state must remain a named gap.

Known tool baseline to verify: source pins Unity **6000.6.0f1**, revision **f7f8ed4d1e24**, and URP **17.6.0**. Previous work used Blender **5.2.1 LTS**, live Blender MCP read-only inspection, and a native isolated Unity workflow. Unity MCP was not exposed in that chat; discover this conversation's actual catalog. Unity AI Assistant **2.20.0-pre.1** was retained by operator approval. The operator reported **1,000 Unity AI credits**, but balance, access, terms, models, operation costs, and generator capabilities were unverified. Verify those separately if relevant; neither retained packages nor a balance authorizes spending.

## Read and inspect the current source of truth

After discovering the actual tree, read these files and follow their current references. If one is absent locally or remotely, include that fact in the gaps table rather than substituting remembered content:

- `README.md`.
- `docs/media/forever-we-reign-duel-concept.png`, inspect the image itself.
- `docs/media/forever-we-reign-duel-concept.md`, its provenance.
- `docs/production/realistic-art-production-brief.md`.
- `docs/design/melee-combat-direction.md`.
- `docs/production/narrative-brief.md` and `docs/design/forever-we-reign-lore-bible.md`.
- `docs/production/asset-provenance.md`, `asset-readiness.md`, and `development-plan/08-content-production-pipeline.md`.
- `docs/production/task-13-content-authoring.md` and `docs/acceptance/task-13-content-authoring.md`, including the machine-readable receipt and relevant evidence.
- `docs/production/prototype-pair-provenance.md`, `weapon-family-pilot-provenance.md`, their current source manifests, export configurations, and import/motion definitions.
- `docs/acceptance/melee-motion-refinement-2026-09-30.md` and `docs/acceptance/post-task-12-checkpoint.md`.
- `docs/production/development-plan/09-mobile-performance-and-platform-delivery.md` and `12-codex-development-sequence.md`.

The supplied image was recorded as a 941 × 1672 PNG, 2,800,787 bytes, SHA-256 `849dd6e9e97415e053147b883180ac774bb3869137dfee9fdfa3e23b8e602ab1`. Verify the repository payload against that record. Its reported original attachment was `C:\Users\cld-main\Pictures\ChatGPT Image Sep 30, 2026, 07_06_04 PM.png`; audit its availability without assuming Git contains it. Exact generator/model, prompt, generation session, input rights, and commercial release terms remain UNVERIFIED. It is documentation and the visual target, not a runtime texture source or proof of accepted art.

## Product and quality target

Forever We Reign is an original premium offline action RPG: portrait, one thumb, anchored one-on-one melee-and-shield duels, readable enemy tells, earned attack openings, and richer committed weapon/body motion. Local dodge/lunge/recoil is bounded. No free movement, ranged player style, copied game choreography, imported game rips, or direct replicas of Infinity Blade or Chivalry assets, characters, UI, or lore.

The approved story is a soldier fighting through bosses to regain his throne, discovering at the ending that it is cursed, with a sequel hook. The existing launch envelope is two regions, four humanoid bosses, one shared rig family, and 34 equipment definitions: twelve one-handed melee weapons (four swords, four axes, four maces/warhammers), eight shields, four helmets, four armor sets, and six talismans. This benchmark does not authorize populating that catalog or making all four bosses. Task 13's recorded 53 content definitions are technical records, not 53 finished equipment pieces.

Match the supplied concept's grounded construction and portrait composition: weathered ivory player armor, dark undersuit/cloth, dark armored captain with a broad practical shield, continuous straight sword silhouettes and plausible grips, wet dark stone, warm brass lamps, a storm-battered coastal fortress, grounded feet, cool broad key light, and warm practical sources. Concentrate detail on combatants, equipment, floor contact, and nearby architecture. A distant fortress may remain a controlled shell.

Earlier documents discuss ceramic plates, brass mechanisms, luminous glass, and a weather engine. Treat those material/world interpretations and new lore names, institutions, emblems, and curse mechanisms as proposals unless current explicit approval establishes otherwise. Do not silently turn the supplied grounded armor into stylized porcelain, reshape the fortress into an unrelated design, or adopt proposed heraldry as canon. Keep the concept's visible quality and construction as the benchmark. Make any material or identity reinterpretation a concrete reviewable decision before it changes the visual target. Keep the explicit throne/curse reveal out of the courtyard.

The immediate goal pulls **art-quality proof** forward from Task 24. It does not complete Task 24's boss phases, rewards, finished route, or full gameplay milestone. Engineering continues its sequence independently. This is one finished representative art sample in working combat, not campaign expansion.

Use a practical hybrid pipeline: a commercially compatible licensed base when it demonstrably saves work, custom hero geometry where silhouette or articulation requires it, original equipment, authored materials, and AI concept/texture studies only when useful and authorized. You are not obliged to construct every surface from procedural blocks. Blender and URP are tools, not quality guarantees. Existing low-poly faceted pilots establish contracts; a recolor or another blockout does not meet this benchmark.

Research may identify compatible character/animation/environment assets and inspect current primary listings and terms. Deliver an exact reviewable plan: named source/version, useful parts, commercial/modification/player-distribution rights, seats/attribution, actual cost, pinned Unity/URP compatibility, rig implications, integration work, trial criteria, and a no-purchase route. Purchase or paid generation needs explicit budget authorization. A reported credit balance is not a budget. Do not spend Unity AI or Venice credits, launch paid trials, or assume an external attachment is rights-cleared.

## Parallel ownership and isolation

Follow the shared parallel-development contract and work from an isolated art worktree/working copy derived from the verified checkpoint. Reuse a suitable existing attached art worktree after auditing it. Do not concurrently edit the engineering checkout or point your Blender/Unity writer at its project. Record the exact art root and branch. A native Unity validation copy must have its own `Library`, `Temp`, logs, and project lock; never share those with another Editor.

Art owns these new dedicated paths within its isolated worktree:

```text
SourceArt/Characters/VisualBenchmark01/
SourceArt/Equipment/VisualBenchmark01/
SourceArt/Environments/VisualBenchmark01/
tools/content/art-benchmark-01/
Assets/ArtStaging/ForeverWeReignBenchmark01/
```

Keep new source/export configurations, recipes, texture sources, preview imports, `.meta` files, preview materials/prefabs/scenes, manifests, provenance, and review evidence in these owned paths or the explicitly assigned art documentation path in the shared contract. The staging folder is for handoff and isolated review, not canonical production admission. Include folder metadata and every asset's matching metadata. Do not write to another owner's documents.

Engineering exclusively owns canonical production integration: `Assets/Game/Runtime`, `Assets/Game/Editor`, production catalogs/content definitions, shared scenes/prefabs/controllers/import policies, `Packages`, and `ProjectSettings`. Art proposes required engineering changes in the handoff manifest with exact paths and rationale; it does not make them directly. Obtain an explicit bounded transfer of ownership before touching an exception. One writer per serialized Unity asset.

**Do not run `Praxen.Game.Editor.Task13ContentAuthoring.Build` in the shared or canonical project.** It writes the production catalog and definitions, invokes pilot authoring, opens `CombatPrototype`, binds content, and saves the scene. Batch mode alone does not make it safe. Do not invoke a whole-project authoring/rebuild command as an art import shortcut. Use isolated staging and bounded existing validation, inspect side effects before execution, and hand off any required production binding to engineering.

Preserve these accepted technical reference bundles and their current hashes/GUIDs:

- `prototype-pair-1.0.3`: `SourceArt/Characters/PrototypePair.blend`, current manifest, six Soldier/Captain FBXs, associated materials/prefabs/import policy, recipes/configuration/motion, and recorded evidence.
- `weapon-family-pilot-1.0.0`: `SourceArt/Equipment/WeaponFamilyPilot/`, six AxePilot/MacePilot FBXs, associated prefabs/materials, recipes/configurations/motion, definitions, and recorded evidence.

Read or duplicate them into the new batch as controlled references; do not overwrite or regenerate them. Record their protection hashes at preflight and recheck after work. During the October 1 checkpoint preparation, live Blender inspection still reported the operator's open `PrototypePair.blend` as dirty. Saved disk source is checkpointable; its unsaved session is not. Reinspect before using Blender MCP, preserve that session, and author in a separate source file/process. Do not close it, save over it, or reset its scene.

## Shared asset and motion contract

Read the current manifests and imported definitions before authoring. Retain the established 23-bone hierarchy: 21 Humanoid bones plus `WeaponSocket` and `ShieldSocket`, rig ID `rig.prototype-pair.23-bone.v1`. Anatomical right hand owns the weapon; anatomical left owns the shield. Do not regress the corrected handedness. Maintain normalized scale, bind pose, controlled axes, safe rigid equipment weights, and consistent skeleton/grip/contact across three LODs. Existing authoring uses metres, Blender Z-up/forward -Y, FBX Y-up/forward -Z, no leaf bones; confirm against the current exporter.

Provide all thirteen motion keys:

```text
Idle, Guard, Parry, DodgeLeft, DodgeRight,
CutUp, CutDown, CutLeft, CutRight,
Hit, Death, EnemyTell, EnemyAttack
```

`CutUp` is a rising cut. All four cuts require readable body-led anticipation, weapon travel, contact, follow-through, and return. Guard/parry, shield straps, grip, wrist/elbow/shoulder limits, armor articulation, and feet need review in motion. A licensed donor must retarget to both combatants and pass those checks before expanding its family. No damage or gameplay animation events. Root rotation, vertical and horizontal movement remain baked into bone pose; Animator does not apply root motion. Bounded local presentation must not change encounter anchors or logical hit resolution.

For the accepted sword player, exact current LOD0 paths are:

```text
LOD0/Soldier_Rig/Hips/Spine/Chest/RightShoulder/RightUpperArm/RightLowerArm/RightHand/WeaponSocket
LOD0/Soldier_Rig/Hips/Spine/Chest/RightShoulder/RightUpperArm/RightLowerArm/RightHand/WeaponSocket/WeaponTip
LOD0/Soldier_Rig/Hips/Spine/Chest/LeftShoulder/LeftUpperArm/LeftLowerArm/LeftHand/ShieldSocket
```

The captain substitutes `Captain_Rig`; the technical family pilots use `AxePilot_Rig` and `MacePilot_Rig`. Validate actual new staging paths and put the exact unambiguous paths in the manifest. Do not select a socket or marker by a loose substring. Preserve stable grip and one contact marker per equipped weapon across LODs, including exact renderer references.

Keep **logical timing** separate from **clip contact metadata**. The integer combat clock resolves outcomes. A contact marker and `contactSeconds` describe the authored pose; release is a visual landmark, not another hit, cancel window, or authority. Current Task 13 sword/axe/mace logical windup/recovery values are 100/400, 150/450, and 180/520 ms; clip contact/duration values are 100/500, 150/600, and 180/700 ms. The current delivered sword visual release is 40 ms, not the earlier provisional 60 ms in the design proposal. Read live definitions before relying on these historical values. Art must not change logical timings, rules, damage, resources, gesture ownership, buffer behavior, or camera direction conventions to accommodate a clip.

## Execute the benchmark and prove it

After the audit and safe repair plan, choose the smallest credible path to the finished benchmark, state its time/cost tradeoff, and begin authorized reversible source/staging work. If a paid asset or generation is required, prepare the exact reviewable selection and trial plan first, then ask the one necessary budget question; continue independent work while waiting. Do not stop at a generic tool list, another concept prompt, or a standalone Blender render.

1. Prepare the source and production plan for the player, captain, sword/shields, and small reusable coastal court. Preserve the concept composition, material contrast, silhouette readability, and cool/warm lighting. Establish UV/texel density, PBR channel conventions, texture imports, LODs, skin influences, and contact requirements before surface decoration. Use a small modular kit, authored wetness, reflection probes, baked static lighting, limited shadows, and restrained storm effects. Avoid hiding weak geometry, grip, or deformation behind bloom, camera movement, rain, or fog.
2. Author/export the new batch in the owned isolated paths. Keep sources, deterministic recipes where applicable, explicit export settings, texture source files and maps, dependency/license records, and stable IDs/GUIDs traceable. Record actual work and review hours so the later boss/campaign estimate has a denominator.
3. Validate native imports and motion on the pinned Editor through an isolated preview project. Check material channels/color space, texture compression/mips, Humanoid mapping, bind/rest transforms, skinning, three LODs, exact sockets/markers, baked root, all keys, both-avatar retargeting, and clean import/console results. Reuse meaningful existing validators/replay tests; request engineering support for integration hooks rather than changing its runtime/editor code.
4. You are authorized to create NEW preview scenes, prefabs and data bindings entirely under Assets/ArtStaging/ForeverWeReignBenchmark01/ in your isolated art worktree, using unchanged existing runtime code and staged content. Use unique namespaced IDs for new staged definitions; do not introduce duplicates into the global validator. Bind the new assets to actual accepted combat events there. Hand off a precise staging manifest to engineering for a separate bounded canonical production integration assignment; do not wait for Task 14 to grant authority to create this isolated preview. Acceptance proof must show the same new player, captain, materials, lighting, and courtyard in **native Unity combat at the actual portrait camera**, with actual accepted combat actions. Capture idle/guard, four cuts with anticipation/contact/recovery, confirmed shield block/parry, dodges, hit/death, and pause/resume/restart as applicable. Record the scene, Editor/player, camera resolution, source hashes, tested actions, and limitations. A Blender beauty render, staged static pose, generated concept, or mocked combat is not this proof.
5. Present side-by-side concept and Unity evidence for founder review: construction/material response, correct hands and continuous grips, shield clearance, readable captain tells, feet/floor contact, armor deformation, blade/head arcs, recovery continuity, and reduced-motion visibility. Separate technical PASS from art review PENDING/ACCEPTED and device UNRUN. Do not grant founder approval to yourself.
6. Preserve gameplay equivalence with presentation enabled/disabled across existing 30/60/120 FPS and jitter scenarios. Art and presentation never resolve damage. If integration fails a contract, diagnose the root cause and assign the bounded engineering repair. Do not suppress a test or alter timing to obtain a green result.

Recorded Task 13 evidence is 841 Edit Mode, 155 Play Mode, and 24 Python tests passed with prior identities preserved. Verify current receipts and source before citing it; it does not prove finished art, physical touch, one-thumb ergonomics, phone performance, or release readiness. Those gates remain UNRUN until actual new evidence exists.

Use the current performance document as authority. Its starting targets include 60 FPS, typical GPU ≤12 ms/main thread ≤8 ms, 30-minute sustained qualification, lower-tier resident memory ≤1.2 GB/transition peak ≤1.5 GB, roughly 50–80k triangles per player/boss LOD0 and 400–600k visible scene triangles, preferably 2–3 character materials, usually 2K character textures, 512–1K small equipment, four skin influences, and no required simulated cloth. These are provisional budgets, not measured acceptance. Report actual geometry/material/texture numbers and Editor measurements separately from physical-phone captures, memory, thermals, frame pacing, and touch/readability review. Optional 120 FPS requires its own qualified-device evidence. Do not invent a supported device list.

## Required deliverable and status

Deliver a reviewable benchmark package: editable sources, exported meshes and LODs, texture sources/maps and import settings, shared rig/bind-pose evidence, thirteen-key motion/contact table, exact grip/socket/marker paths, source/import SHA-256 hashes, matching `.meta`/GUID map, current rights/provenance and modifications, export recipes/configuration, art handoff manifest, native Unity import results, portrait screenshots and combat captures, measured asset budgets, work/review hours, founder review status, and explicit physical-device gaps. Include exact engineering integration paths and checks without writing outside art ownership.

Every imported external/generated asset needs creator/source, dated license/terms evidence, commercial use, modification and player-redistribution rights, seats/attribution, original hashes, processing history, authoring/import linkage, and release eligibility. Unknown rights stay UNVERIFIED and block release admission. The supplied concept's use as reference does not clear it as a texture.

Finish with the concrete files and evidence produced, what the Unity benchmark demonstrates, remaining blockers and their owners, and the next bounded action. Keep art quality proof, technical validation, physical acceptance, Task 24 completion, and release eligibility distinct. No automatic campaign expansion, staging, commit, push, merge, or purchase follows from a passing benchmark.

Store your bootstrap report and machine-readable path manifests under docs/handoffs/audits/ using art-YYYY-MM-DD filenames. Keep the benchmark handoff manifest under docs/handoffs/art-batches/visual-benchmark-01/. These track-specific names prevent competing writers.
