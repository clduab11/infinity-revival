# Application foundation

Task 03 establishes eight assembly boundaries and an explicit scene composition root. Domain and Application are pure C#; no dependency-injection framework or global event bus is used.

| Assembly | References and responsibility |
| --- | --- |
| Praxen.Game.Domain | No game or engine references; marker anchors the boundary until domain rules are added. |
| Praxen.Game.Application | Domain; application state machine and typed diagnostic contracts. Engine references disabled. |
| Praxen.Game.Content | Domain and Application; boundary reserved for future authoring definitions. |
| Praxen.Game.Infrastructure | Application; bounded diagnostic history. Engine references disabled. |
| Praxen.Game.Presentation | Domain, Application, Infrastructure, and uGUI; explicit composition, lifecycle callbacks, and Refuge view. |
| Praxen.Game.Editor | Application and Presentation; Editor-only scene authoring. |
| Praxen.Game.Tests.EditMode | Domain, Application, Infrastructure, and test runners; Editor-only behavioral tests. |
| Praxen.Game.Tests.PlayMode | Application, Infrastructure, Presentation, uGUI, rendering, and test runners; test-only scene/lifecycle checks. |

Content currently has no implementation. Test assemblies require `UNITY_INCLUDE_TESTS` and are not automatically referenced. Runtime assemblies never reference Editor or tests. Compiled-reference tests enforce the inward dependencies and engine exclusion in Domain, Application, and Infrastructure.

## State contract

| Current state | Allowed next state |
| --- | --- |
| Bootstrap | Loading, Failed, Shutdown |
| Loading | Refuge, Suspended, Failed, Shutdown |
| Refuge | Suspended, Failed, Shutdown |
| Suspended | Captured Loading or Refuge state, Failed, Shutdown |
| Failed | Loading (retry), Shutdown |
| Shutdown | None |

Repeated, undefined, and disallowed transitions leave state unchanged and record `TransitionRejected`. Successful transitions record `StateChanged` with typed previous/current states. The diagnostic buffer retains the newest 128 events in chronological order; snapshots are independent copies. Access is confined to the application thread.

## Scene ownership and lifecycle

`BootstrapCompositionRoot.Awake` constructs the diagnostic sink and state machine, enters Loading, shows the serialized Refuge view, then enters Refuge. The scene owns its camera and view as children of Bootstrap. Calling Initialize again does nothing.

Pause or focus loss captures the current active state and suspends once. Resume requires both focus and an unpaused application. The Refuge view remains visible during suspension; future combat work must consume the application state rather than infer active time from rendering.

Missing view/camera wiring enters Failed and shows a camera-independent generic error overlay. Retry requires corrected bindings and returns through Loading. Rebinding hides the previous fallback view. Destroying the composition component hides its current view and enters terminal Shutdown; destroying the owner also destroys its children.

The Refuge screen contains only a background and title. It adapts to screen safe-area anchors; physical notch, rotation, gesture, and ergonomic acceptance remain later device work.

## Open and verify

Open `Assets/Game/Scenes/Bootstrap.unity` and enter Play Mode. Bootstrap is the first enabled build scene; the preserved SampleScene entry is disabled.

From a Windows PowerShell terminal at the repository root:

```powershell
./tools/build/run_unity_checks.ps1 -Mode EditMode
./tools/build/run_unity_checks.ps1 -Mode PlayMode
```

The runner pins the Editor, temporarily disables Pipeline auto-start, waits for the Editor process, validates fresh nonempty all-passing XML receipts, and restores the prior Pipeline configuration. Close the canonical Editor before these batch checks.

Scene authoring is repeatable through `Praxen/Create Bootstrap Scene` or the runner's `AuthorScene` mode. It replaces Bootstrap and updates the build list, so use it deliberately when changing authored Bootstrap content.
