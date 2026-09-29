# Infinity Revival

**An original premium, offline action RPG built around portrait sword-and-shield combat, readable enemy patterns, and earned attack openings.** Developed by Praxen LLC in Unity.

Infinity Revival is a working title. The project is currently an application and input foundation: Tasks 01 through 04 are complete within their recorded acceptance contracts. The next task is the combat clock and command queue. A playable combat prototype, campaign, and release build are still planned work.

![Intended portrait game design for Infinity Revival](docs/media/infinity-revival-prototype.png)

*GPT-Image visual prototype of the intended game. This is concept art, not a gameplay screenshot or evidence of implemented combat. Its characters, environment, effects, and HUD represent design direction. See the [generation prompt and provenance](docs/media/infinity-revival-prototype.md).*

## The intended game

Fight through a damaged weather-control complex in an original industrial fantasy world. A storm-battered foundry and coastal machinery district lead into an elevated observatory and weather-engine complex. Ceramic armor, worked brass, dark stone, weathered fabric, and restrained luminous glass define the visual direction.

The player is an expedition fighter restoring access to the complex. The Refuge is the place to equip, upgrade, choose a route, and return after defeat. Rebirth preserves accumulated knowledge and reconstructs expedition equipment while introducing bounded difficulty progression.

Combat centers on one player and one active opponent at fixed encounter anchors. Read the tell, choose a defense, then exploit recovery or a balance break. Dodges, lunges, recoil, and finishers use authored local movement. Logical combat rules determine outcomes; weapon contact, animation, camera, audio, and effects present those outcomes.

The phone layout is designed for holding and playing with the same hand. Four-direction swipes, reachable defensive controls, mirrored layouts, and reach calibration support that goal. One-thumb usability requires physical-device acceptance and has not yet been demonstrated.

### Planned campaign scope

| Area | Design target |
| --- | --- |
| Business model | Premium purchase with the complete campaign available offline |
| Campaign | Two regions, branching authored routes, four bosses |
| Regular opponents | Four archetypes with elite variations |
| Player combat | One sword-and-shield style, three selectable active abilities |
| Equipment | 34 definitions: 12 swords, eight shields, four helmets, four armor sets, six talismans |
| Progression | Equipment upgrades and mastery, character XP, legacy perks, six bounded difficulty tiers including the base tier |
| Phones | Portrait presentation, one-thumb controls |
| iPad | Planned landscape layout using the same combat rules |
| Windows / Steam | Planned landscape presentation, mouse/keyboard and controller adapters |
| Performance | Sustained 60 FPS target; optional 120 FPS only on qualified devices |

These are production targets, not shipped features or claims of platform support. Character XP is designed to continue after equipped items are mastered. The launch design has no compulsory account, energy system, daily attendance requirement, or battle pass.

### Planned expedition loop

```mermaid
flowchart LR
    Refuge[Equip and upgrade at Refuge] --> Route[Choose a route]
    Route --> Explore[Travel through authored nodes]
    Explore --> Defend[Read and defend an enemy sequence]
    Defend --> Opening[Exploit an opening]
    Opening --> Defend
    Opening --> Victory[Win and commit rewards/checkpoint]
    Victory --> Explore
    Defend --> Defeat[Defeat and return to Refuge]
    Defeat --> Refuge
```

The [development plan](docs/production/development-plan.md) contains the full design, bounded work orders, content envelope, and acceptance gates. It is a planning baseline, not a completion record.

## What exists today

Status is grounded in the acceptance reports dated **September 29, 2026**.

| Task | Delivered scope | Evidence |
| --- | --- | --- |
| 01 | Toolchain and project inventory, canonical-root correction, readiness gaps | [Task 01 report](docs/acceptance/task-01-readiness-2026-09-29.md) |
| 02 | Repository structure, metadata and LFS rules, exact dependency baseline, source-only restoration checks | [Task 02 report](docs/acceptance/task-02-readiness-2026-09-29.md) |
| 03 | Eight assembly boundaries, application state machine, typed diagnostics, Bootstrap composition, empty Refuge, lifecycle and failure recovery | [Task 03 report](docs/acceptance/task-03-readiness-2026-09-29.md) |
| 04 | Timestamped touch capture, cardinal recognition, fixed pointer ownership and phase, cancellation, duplicate suppression, direction trace | [Task 04 report](docs/acceptance/task-04-readiness-2026-09-29.md) |
| 05 | Combat clock, timestamp mapping, ordered queue, milestones, tie rules, stall suspension | **NEXT, not implemented** |

Opening Bootstrap in Play Mode reaches an empty Refuge screen containing a background and title. The application handles pause/focus overlap, failure/retry, and teardown. Input capture is composed, but the interaction phase starts **Inactive**, so the screen does not run a duel or accept combat commands by default.

<img src="docs/media/refuge-current.png" alt="Current empty Refuge rendered by the Unity Editor test" width="240">

*Current Refuge foundation, captured by an explicit graphics-backed Editor test render at 720 x 1280. This is current implementation evidence, not physical-device acceptance.*

The latest recorded suites pass **148 Edit Mode tests, 24 Play Mode tests, and 12 Python validator regression tests**. Task 04 includes 89 new Edit Mode and 16 new Play Mode cases alongside Task 03's 59 and eight existing cases. These counts establish the tested contracts above, not full gameplay completion.

Player builds, physical touch latency, one-thumb ergonomics, device safe areas/rotation, sustained mobile performance, and gameplay outcomes remain **UNRUN or UNVERIFIED**. The prototype milestone is not accepted yet. Release exclusion of development automation also requires built-player evidence.

## Open the project

### Prerequisites

- Unity **6000.6.0f1**, revision **f7f8ed4d1e24**. Install and select this exact Editor; do not upgrade the project on import.
- Git and Git LFS for binary content.
- Python 3 for the structural validator and its regression suite.
- Windows PowerShell for the provided Unity batch-test runner. Mobile and Apple build/signing paths have separate acceptance requirements.

### Clone and restore LFS files

```bash
git lfs install
git clone https://github.com/clduab11/infinity-revival.git
cd infinity-revival
git lfs pull
```

The cloned **outer directory** is the Unity project root. It contains these directories directly:

```text
infinity-revival/
├── Assets/
├── Packages/
└── ProjectSettings/
```

In Unity Hub, choose **Add project from disk**, select that outer directory, and assign Unity 6000.6.0f1. Allow the pinned packages to restore, then open the scene below and enter Play Mode:

```text
Assets/Game/Scenes/Bootstrap.unity
```

Bootstrap is the first enabled build scene. The preserved template SampleScene is disabled in the build list. The locally excluded alternate project is not the canonical project root.

The existing development checkout is still named infinity-redux, and the C# namespaces remain Praxen.Game. The public repository name and working game title do not require a source or asset-GUID rename.

```text
Windows development root: C:\Users\cld-main\Desktop\github-projects\infinity-redux
WSL development root:     /mnt/c/Users/cld-main/Desktop/github-projects/infinity-redux
Editor pin:              6000.6.0f1 (f7f8ed4d1e24)
```

## Controls and input scope

### Implemented recognition contract

The recognizer uses the original Input System event timestamp, normalized screen coordinates, and four cardinal directions. It commits at the first sample whose travel is strictly greater than **6% of the shorter screen dimension**, within **350 ms** of Begin. Horizontal classification wins diagonal ties. Defaults are editable in the tuning asset:

```text
Assets/Game/Content/GestureTuning.asset
```

Pointer ownership and interaction phase are captured at Begin and stay fixed. UI-owned contacts stay UI-owned when crossing into gameplay. An eligible gameplay contact produces at most one command and requires terminal release before another gesture. Expired phases, stale timestamps, timeouts, screen changes, device removal/reset, suspension, disable, and teardown cancel incomplete recognition.

An EnemySequence phase yields a typed Parry command; a PlayerOpening phase yields Attack; Inactive yields no command. A brief noninteractive trace displays accepted direction. The current Bootstrap starts Inactive, and no combat phase owner has been implemented.

See the [touch pipeline contract](docs/architecture/touch-gestures.md) for timestamp, ownership, lifecycle, and event-merging details.

### Planned player controls

| Input | Intended action | Current status |
| --- | --- | --- |
| Directional swipe during an enemy sequence | Parry attempt | Recognition exists; timing judgments and defense resolution are planned |
| Directional swipe during a player opening | Attack | Recognition exists; openings, damage, and buffering are planned |
| Hold shield control | Guard | Planned |
| Tap left/right dodge control | Dodge to that side | Planned |
| Tap equipped ability control | Activate the selected ability | Planned |
| Tap exploration destination | Travel to an authored node | Planned |
| Drag exploration view | Limited camera inspection | Planned |

Mirrored layouts, adjustable control scale, reach calibration, and the portrait combat HUD remain planned. The design requires no simultaneous two-finger action. Future desktop adapters will emit the same semantic combat commands; mouse/keyboard, controller support, and landscape combat presentation are not implemented.

## Architecture

The project uses an explicit scene composition root and a small C# core. Runtime code does not depend on Editor or test assemblies. Domain, Application, and Infrastructure disable engine references, with compiled-reference tests enforcing the boundaries.

| Assembly | Current responsibility |
| --- | --- |
| Praxen.Game.Domain | Engine-free touch values, commands, tuning, and cardinal recognizer; future combat rules |
| Praxen.Game.Application | Application states, typed diagnostic contracts, input ports, and interaction phase context |
| Praxen.Game.Content | Unity authoring definitions, currently the gesture tuning asset |
| Praxen.Game.Infrastructure | Engine-free bounded diagnostic history |
| Praxen.Game.Presentation | Bootstrap composition, lifecycle, Refuge, Input System adapters, UI ownership, and direction trace |
| Praxen.Game.Editor | Editor-only scene/input authoring |
| Praxen.Game.Tests.EditMode | Pure behavior and compiled assembly-boundary checks |
| Praxen.Game.Tests.PlayMode | Saved-scene, lifecycle, real Input System, UI-raycast, and presentation integration checks |

Application states are Bootstrap, Loading, Refuge, Suspended, Failed, and Shutdown. Legal transitions are explicit; invalid transitions leave state unchanged and record a typed diagnostic. The diagnostic buffer keeps the latest 128 events in chronological order and returns independent snapshots.

See the [application foundation contract](docs/architecture/application-foundation.md) for transitions, failure recovery, scene ownership, and deterministic teardown. Some early architecture descriptions record their task's original scope; the Task 04 report and touch contract describe the later input additions.

### Repository map

```text
Assets/
├── Game/
│   ├── Runtime/{Domain,Application,Content,Infrastructure,Presentation}/
│   ├── Editor/
│   ├── Content/GestureTuning.asset
│   ├── Scenes/Bootstrap.unity
│   └── Tests/{EditMode,PlayMode}/
└── ...                         Preserved Unity template assets
Packages/                       Manifest and Unity-generated lockfile
ProjectSettings/                Canonical Unity project settings
SourceArt/                      Characters, equipment, environments, audio sources
docs/
├── acceptance/                 Dated reports and machine-readable receipts
├── architecture/               Application and input contracts
├── design/                     Design documentation space
├── media/                      Intended visual prototype and provenance
├── production/                 Development plan, dependency policy, provenance
├── research/                   Research documentation space
└── superpowers/                Implementation plans and task designs
tools/
├── build/                      Validator, regression tests, Unity runner
└── content/                    Content tooling space
```

Generated caches, local editor state, root build outputs, raw task evidence under output, and the alternate project are ignored. The build tooling directory is versioned source.

## Dependency baseline

The manifest has **48 direct dependencies** and the Unity-generated lockfile has **65 resolved packages**. The principal exact pins are:

| Package | Version |
| --- | --- |
| Universal Render Pipeline | 17.6.0 |
| Input System | 1.20.0 |
| Unity UI (uGUI) | 2.6.0 |
| Timeline | 6.6.0 |
| Cinemachine | 6.6.0 |
| Addressables | 2.11.2 |
| Test Framework | 1.8.0 |
| Newtonsoft JSON | 3.2.2 |
| Pipeline | 0.8.0-exp.1 |

Animation Rigging 6.6.0 is deferred until contact correction requires it. Pipeline is an experimental development dependency; its presence does not establish release suitability or enable a service. Launch campaign content is intended to remain local and offline.

Use the [dependency policy](docs/production/dependency-policy.md) for changes. Preserve the Editor and unrelated pins, resolve through the pinned Unity Editor, and review the manifest, generated lockfile, and validator baseline together. Record transitive changes and major-version migration risks.

## Verification

Run structural checks from the repository root:

```bash
python3 tools/build/verify_baseline.py
python3 -m unittest discover -s tools/build -p 'test_*.py' -v
```

Run Unity suites from Windows PowerShell after closing the Editor for this project:

```powershell
./tools/build/run_unity_checks.ps1 -Mode EditMode -EvidenceTask task-04
./tools/build/run_unity_checks.ps1 -Mode PlayMode -EvidenceTask task-04
```

The runner uses the following fixed Editor path, validates the version/revision, rejects an active canonical Editor, temporarily disables Pipeline auto-start, and restores its prior configuration. It requires fresh, nonempty, all-passing XML receipts. It derives the project root from the script location, so a clone can use a different directory name.

```text
C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe
```

If that Editor is installed elsewhere, review the runner's path deliberately. Its default evidence label remains task-03, so pass task-04 explicitly for the current suites. New logs and XML are written under the ignored task output directory.

The [Task 04 machine-readable receipt](docs/acceptance/task-04-evidence-2026-09-29.json) records counts, hashes, and preservation evidence. Portable copies of the final [Edit Mode XML](docs/acceptance/evidence/task-04-editmode.xml), [Play Mode XML](docs/acceptance/evidence/task-04-playmode.xml), [baseline result](docs/acceptance/evidence/task-04-baseline.json), and [review record](docs/acceptance/evidence/task-04-review.md) are included for inspection.

Earlier receipts establish source-only package restoration, lifecycle behavior, and assembly boundaries. Additional raw logs referenced by historical reports remain local ignored evidence and may be absent from a clone. A synthetic Editor render does not establish physical-device acceptance.

## Development roadmap

| Stage | Remaining work and acceptance gate |
| --- | --- |
| Prototype, Tasks 05–10 | Combat clock/queue, graybox defense, openings/resources, enemy patterns, portrait HUD, then recorded physical-phone input/readability/frame-time evidence |
| Vertical Slice, Tasks 11–25 | Approved shared rig and assets, presentation, content definitions, Addressables, recoverable saves/transactions, progression, exploration, accessibility, one finished boss/route, sustained device and persistence acceptance |
| Alpha, Tasks 26–34 | Four regular archetypes, remaining bosses and regions, equipment catalog, campaign/rebirth completion, progression and economy validation |
| Platform and release readiness, Tasks 35–38 | iPad/Android profiles, Steam controls, reproducible release builds, automation exclusion, clean-install/upgrade/interruption/device acceptance |
| Launch preparation, Task 39 | Signed candidates, store materials, licensing records, support documentation, release archive, and explicit publication approval |

Task 05 must show equivalent recorded event outcomes at 30/60/120 presentation rates. The first finished boss must establish actual animation and encounter-production effort before the campaign schedule is treated as credible. Mobile release leads the plan; Steam follows its own acceptance gate.

No release date or supported-device list is declared. Installed build modules and successful compilation do not qualify a platform. Consult the [full development plan](docs/production/development-plan.md) before expanding a work order.

## Working on the repository

- Version complete source under Assets, Packages, and ProjectSettings, including every Unity metadata file. Move each asset with its matching metadata and preserve its GUID.
- Keep Unity YAML, scenes, prefabs, settings, and metadata as ordinary Git text. Binary art, meshes, textures, audio, video, and authoring files use the existing Git LFS rules.
- Use one writer at a time for a shared scene, prefab, controller, settings asset, or other serialized Unity asset. Coordinate ownership before editing.
- Keep authoring sources in SourceArt, content tooling under tools/content, and decisions/evidence in docs.
- Keep changes bounded to their work order. Add verification for behavior changes and preserve UNRUN/UNVERIFIED for missing acceptance evidence.
- Dependency changes include the generated lockfile and baseline review. Preserve exact pins unless the change explicitly requires otherwise.
- Keep credentials, signing keys, local configuration secrets, generated caches, and raw task output out of source control.

On the original workstation, use native Windows Git for LFS; WSL Git has no installed LFS command. Other contributors need a working Git LFS installation for their chosen Git executable.

```powershell
git lfs version
```

```bash
"/mnt/c/Program Files/Git/cmd/git.exe" lfs version
```

## Asset provenance and licensing

World, characters, writing, equipment, environments, audio, and visual identity are intended to be original. Genre references are research inputs. Do not import copied game assets, traced environments, reproduced UI, copied choreography, or derivative character designs.

Record original, commissioned, licensed, and generated content before integration using the [asset provenance requirements](docs/production/asset-provenance.md). Records must cover creator/source, commercial use, modifications, player redistribution, attribution, original hashes, authoring sources, and imported asset GUIDs. Unclear rights remain UNVERIFIED and block release eligibility.

The [GPT-Image prototype record](docs/media/infinity-revival-prototype.md) documents the illustration's prompt and available generation provenance. The image is documentation for intended design, not an imported Unity production asset or evidence of a cleared campaign asset set.

**No project license has been selected or added.** Package and third-party components have their own terms. Working-title clearance and production asset licensing remain separate release requirements.
