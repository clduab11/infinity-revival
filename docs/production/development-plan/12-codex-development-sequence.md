# 12. Codex development sequence

## 12.1 Execution rules

Apply this instruction to every implementation prompt:

> Work only on the named task and its necessary dependencies. Read the current architecture and inspect existing state first. Preserve Unity 6000.6.0f1 and the approved package pins. Implement the smallest reviewable change, including the task’s specified verification. Do not purchase assets, commit, push, publish, start services, or change infrastructure without authorization. Report changed files, verification evidence, and remaining gaps. A successful compile does not establish gameplay or device acceptance.

Before scene mutations, confirm the target Unity project and editor instance. Before asset mutations, inspect current state.

Use separate agents for independent code or content work. Serialize modifications to shared Unity assets.

## 12.2 Sequential implementation prompts

Each row is a bounded work order. Proceed only after its stated exit condition is met.

**Operator schedule override, 2026-09-30:** the earlier hands-on checkpoint after Task 12 remains outstanding. The operator's later "Complete next Task" instruction authorizes bounded Task 13 ScriptableObject authoring, immutable conversion, validation, and the representative sword/axe/mace pilot to proceed. Task 09 physical acceptance, Task 10 device qualification, and final Task 11 visual/contact/deformation/device approval remain **DEFERRED, execution UNRUN**. Automated checks continue; this exception changes sequencing, not acceptance thresholds or supported-device claims. It does not authorize a broad production-art batch, purchases, AI-credit spend, commits, or pushes. See the [checkpoint record](../../acceptance/post-task-12-checkpoint.md) and [Task 13 execution plan](../../plans/2026-09-30-task-13-content-authoring.md).

| ID | Implementation prompt | Required exit condition |
|---|---|---|
| **01** | Inventory the canonical Unity project, installed Editor, packages, build modules, signing dependencies, accessible devices, Unity MCP, Blender MCP, and experimental automation features. Produce a readiness ledger. | Facts distinguish installed, connected, unverified, and unavailable capabilities. |
| **02** | Initialize the repository structure, Unity ignore rules, LFS rules, metadata settings, and dependency policy. Preserve the alternate project. Apply the approved package baseline. | Canonical project restores exact dependencies; no unrelated files removed. |
| **03** | Create assembly boundaries, bootstrap composition root, application states, and typed diagnostics. | Domain has no UnityEngine dependency; bootstrap reaches an empty refuge screen. |
| **04** | Implement timestamped touch capture and the four-direction gesture recognizer with pointer ownership, phase capture, cancellation, and one-command-per-gesture behavior. | Gesture tests cover boundaries, UI crossings, cancellation, and duplicate suppression. |
| **05** | Implement the combat clock, timestamp mapping, ordered command queue, event milestones, tie rules, and stall suspension. | Recorded event streams produce equivalent outcomes at 30/60/120 presentation rates. |
| **06** | Implement guard, dodge charges, parry windows, damage, stagger, recovery, and death in a graybox encounter. | Every attack/defense combination resolves according to its authored mask. |
| **07** | Implement player openings, directional attacks, one-command buffering, enemy balance, and Focus accumulation. | No attacks leak across phases; openings and resource changes are reproducible. |
| **08** | Implement regular-enemy pattern decks, cooldowns, selection history, encounter seeds, and safe phase boundaries. | AI never changes a committed attack in response to unfinished input. |
| **09** | Build the portrait HUD, mirrored layouts, reach calibration, pause/resume, and exploration input ownership. | All required actions can be performed with one thumb on physical phones. |
| **10** | Run the prototype device qualification and produce input-latency, recognition, readability, and frame-time evidence. Fix failed contracts before proceeding. | Prototype gate accepted with recorded evidence. |
| **11** | Establish the shared skeleton and asset-import pipeline using approved assets. Validate retargeting, weapon contact, deformation, portrait framing, and export presets. | One player/enemy pair passes the asset acceptance checklist. |
| **12** | Connect animation, camera, audio hooks, and effects to combat events. Keep gameplay outcomes in the combat domain. | Disabling presentation does not change combat outcomes. |
| **13** | Implement ScriptableObject definitions, immutable runtime conversion, stable IDs, and authoring validators. Include the [anchored melee direction](../../design/melee-combat-direction.md) for validated weapon-family timing/contact bindings and a representative sword/axe/mace pilot. | Invalid attack timing, duplicate IDs, and broken references fail validation. Unsupported future mechanics stay disabled; family imports and arcs require native validation. Technical delivery: [Task 13 acceptance](../../acceptance/task-13-content-authoring.md). |
| **14** | Implement Addressables loading scopes and owned leases for bootstrap, refuge, region, and encounter resources. | Load, cancel, failure, retry, and release cases leave no retained encounter handles. |
| **15** | Implement the save envelope, serialized writer, recoverable generations, integrity checks, and load recovery. | Interrupted writes and corrupt newest generations recover safely. |
| **16** | Add sequential save migrations and missing-content fallback behavior. | Old fixtures migrate without destroying the previous valid generation. |
| **17** | Implement encounter checkpoints and atomic victory, defeat, purchase, and route transactions. | Termination around each commit cannot duplicate rewards or erase prior committed progress. |
| **18** | Implement equipment ownership, upgrades, statistics, duplicate conversion, and definition-based mastery. | Duplicate equipment cannot repeat mastery grants. |
| **19** | Implement character XP, levels, attribute allocation, and free refuge respec. | Mastered equipment does not prevent character advancement; stat caps hold. |
| **20** | Implement equipment comparison, loadout, upgrade, and mastery screens with localization keys. | UI cannot equip incompatible or unavailable content, or spend uncommitted currency. |
| **21** | Implement authored exploration nodes, route alternatives, travel transitions, caches, and checkpoint return. | Navigation and rewards survive save/load at every node boundary. |
| **22** | Implement active abilities, legacy perks, bounded tiers, first-clear claims, and voluntary rebirth. | Persistence matrix and one-time tier claims pass verification. |
| **23** | Implement audio mixing, haptics, reduced effects, alternate tells, text scaling, and control options. | A complete encounter is playable without audio or color-only information. |
| **24** | Produce the representative captain boss and a short finished region route through the content pipeline. | Boss has complete presentation, rewards, phases, and content validation. |
| **25** | Profile and optimize the vertical slice, then perform the complete slice acceptance suite. Record actual boss production hours. | Sustained device target and persistence gate pass; campaign estimate is revised. |
| **26** | Produce the sword-guard archetype using the approved content workflow. | Its teaching purpose, patterns, rewards, and device behavior pass review. |
| **27** | Produce the shield-guard archetype using the same workflow. | Same acceptance contract. |
| **28** | Produce the polearm-guard archetype using the same workflow. | Same acceptance contract. |
| **29** | Produce the hammer-guard archetype using the same workflow. | Same acceptance contract. |
| **30** | Produce the polearm boss and finish region one’s branching route content. | Region one is playable end to end with validated assets and checkpoints. |
| **31** | Produce region two’s environment kit, exploration graph, and counter-fencer boss. | Region transitions and the third boss pass content and memory checks. |
| **32** | Produce the final humanoid guardian, campaign ending, and elite variations. | Full campaign and all rebirth tiers are playable. |
| **33** | Populate the remaining equipment catalog in small reviewed batches, using the same item validation contract. | All 34 definitions have valid visuals, icons, statistics, costs, and provenance. |
| **34** | Analyze progression and economy over full expeditions and repeated tiers. Tune tables while preserving system contracts. | No mandatory progression dead end, unlimited mastery exploit, or unbounded tier inflation. |
| **35** | Finish iPad layout and Android device profiles, including thermal behavior, memory limits, interruption, and high-refresh qualification. | Each claimed device profile has physical acceptance evidence. |
| **36** | Implement Steam mouse/keyboard and controller adapters, landscape camera profiles, desktop pacing, and reconnect behavior. | Controls emit the same semantic combat commands; supported controllers are documented. |
| **37** | Build reproducible release pipelines and enforce content validation, save compatibility, licensing records, and automation exclusion. | Clean checkout produces release candidates without development endpoints. |
| **38** | Run Beta acceptance across clean installs, upgrades, long sessions, interrupted saves, unavailable storage, and supported devices. | No unresolved critical save, progression, control, or stability defects. |
| **39** | Prepare signed release candidates, store materials, support documentation, release archives, and rollback constraints. | Concrete release package is ready for explicit publication approval. |

### PC command mappings

Use the same semantic command layer:

- Mouse drag: directional Cut.
- Keyboard arrows: directional Cut.
- Space: guard.
- A/D: left/right dodge.
- E: active ability.
- Controller right-stick flick: directional Cut.
- Left trigger: guard.
- Shoulder buttons: left/right dodge.
- South face button: ability.

Require stick recentering before another flick. Avoid stacked deadzone processing. Validate controller disconnects, reconnects, and input-device switching.

Steam Input gamepad emulation is the initial compatibility path; universal controller support is not assumed. [Valve gamepad emulation guidance](https://partner.steamgames.com/doc/features/steam_controller/steam_input_gamepad_emulation_bestpractices)

---


[Return to development plan](../development-plan.md)
