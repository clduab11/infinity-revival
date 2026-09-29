# 5. Technical architecture and Unity project

## 5.1 Canonical project and verified foundation

The Git repository and Unity project share one root. `Assets/`, `Packages/`, and `ProjectSettings/` are directly beneath it. A fresh clone from GitHub is named `infinity-revival`; the existing Windows working directory remains `infinity-redux`.

[ProjectVersion.txt](../../../ProjectSettings/ProjectVersion.txt) pins **6000.6.0f1**, revision **f7f8ed4d1e24**. The [manifest](../../../Packages/manifest.json) and [lockfile](../../../Packages/packages-lock.json) contain the approved exact package graph.

The original nested-directory planning baseline was corrected before Task 02. Do not create a duplicated `infinity-redux/infinity-redux` path. Tasks 01 through 04 are verified; see the [current README](../../../README.md) and [Task 04 acceptance](../../acceptance/task-04-readiness-2026-09-29.md).

The local `My project` alternate remains preserved and excluded from tracked content. It is not part of a GitHub clone and must not be merged into the canonical package set.

## 5.2 Repository organization

Keep Git and Unity source at the same outer project root.

```text
infinity-revival/
├── .gitignore
├── .gitattributes
├── README.md
├── Assets/
│   ├── Game/
│   │   ├── Runtime/
│   │   │   ├── Domain/
│   │   │   ├── Application/
│   │   │   ├── Content/
│   │   │   ├── Presentation/
│   │   │   └── Infrastructure/
│   │   ├── Editor/
│   │   ├── Tests/
│   │   ├── Content/
│   │   └── Scenes/
│   └── ThirdParty/
├── Packages/
├── ProjectSettings/
├── docs/
│   ├── architecture/
│   ├── design/
│   ├── production/
│   ├── media/
│   └── acceptance/
├── tools/
│   ├── content/
│   └── build/
└── SourceArt/
    ├── Characters/
    ├── Equipment/
    ├── Environments/
    └── Audio/
```

Within runtime layers, group code by gameplay feature: Combat, Equipment, Progression, Exploration, Persistence, and Platform.

### Git rules

- Commit Assets, metadata, Packages, and ProjectSettings.
- Use visible `.meta` files and Force Text serialization.
- Exclude Library, Temp, Logs, build outputs, credentials, and local user settings.
- Use Git LFS for large source art, meshes, textures, and audio.
- Retain license and provenance records beside asset-family documentation.
- Assign one writer at a time to a scene, prefab, Animator Controller, or shared material.
- Prefer prefab composition over concurrent edits to large scenes.
- Require explicit authorization for commits and pushes.

## 5.3 Assembly boundaries

| Assembly | Responsibility |
|---|---|
| `Praxen.Game.Domain` | Combat, statistics, progression, reward rules; no UnityEngine dependency |
| `Praxen.Game.Application` | Use cases, transaction orchestration, service interfaces |
| `Praxen.Game.Content` | ScriptableObject definitions and conversion to runtime definitions |
| `Praxen.Game.Presentation` | Input, animation, camera, UI, audio, effects |
| `Praxen.Game.Infrastructure` | Filesystem, Addressables, platform lifecycle, diagnostics |
| `Praxen.Game.Editor` | Authoring tools and validators; Editor only |
| `Praxen.Game.Tests.EditMode` | Domain, content, persistence tests |
| `Praxen.Game.Tests.PlayMode` | Unity integration and presentation tests |

Dependency direction points toward Domain and Application. Domain must never reference Presentation or Infrastructure.

Use explicit composition roots in the bootstrap scene. Constructor injection is sufficient for the C# services. A dependency-injection framework is unnecessary.

Use typed, bounded events within systems. Avoid a project-wide string-based event bus.

## 5.4 Core interfaces

| Contract | Responsibility |
|---|---|
| `ICombatSession` | Accept commands, advance combat time, expose snapshots, emit outcomes |
| `ICombatClock` | Map input time, pause/resume, and advance active combat time |
| `IProfileStore` | Load, validate, migrate, and commit save generations |
| `IContentProvider` | Acquire encounter resources and return an owned loading lease |
| `IPlatformLifecycle` | Report focus, suspension, resume, and low-memory events |
| `IPerformancePolicy` | Select supported frame rate and quality profile |
| `IDiagnosticsSink` | Record bounded diagnostic events and export support information |

Minimum public behavior:

```csharp
public interface ICombatSession
{
    void Enqueue(in CombatCommand command);
    void AdvanceTo(long combatTimeUs, ICombatEventSink events);
    CombatSnapshot Capture();
}

public interface IProfileStore
{
    Task<LoadResult> LoadAsync(CancellationToken cancellationToken);

    Task<CommitResult> CommitAsync(
        SaveEnvelope next,
        long expectedGeneration,
        CancellationToken cancellationToken);
}
```

`CombatCommand` carries sequence number, combat timestamp, command kind, direction, and captured interaction-phase ID.

`CommitResult` distinguishes success, stale generation, validation failure, storage failure, and cancellation.

## 5.5 Data architecture

ScriptableObjects author:

- Equipment.
- Abilities.
- Combatants.
- Attacks.
- Patterns.
- Boss phases.
- Encounters.
- Routes.
- Reward tables.
- Difficulty tiers.
- Quality profiles.

At load time, convert these into immutable runtime definitions. Runtime health, cooldowns, inventory ownership, and progression never mutate ScriptableObject assets.

Every authored definition receives a stable content ID. Saves reference those IDs, not Unity object references, display names, paths, or asset GUIDs.

Maintain separate versions for:

- Application build.
- Save schema.
- Content catalog.
- Balance configuration.

An encounter captures its content and balance versions when it starts.

---


[Return to development plan](../development-plan.md)
