# Task 03 design: application foundation

Task 03 implements section 5.3 and row 03 of the supplied development plan: assembly boundaries, an explicit bootstrap composition root, application states, and typed diagnostics. Acceptance requires a Domain assembly with no UnityEngine dependency and a real Play Mode run that opens an empty Refuge screen.

## Scope and architecture

Use the eight prescribed assemblies: Domain, Application, Content, Presentation, Infrastructure, Editor, Tests.EditMode, and Tests.PlayMode, prefixed `Praxen.Game`. Domain and Application disable engine references. Runtime assemblies point inward; Editor and tests do not enter players.

The pure C# application state machine starts at Bootstrap. Legal transitions are Bootstrap to Loading, Loading to Refuge, Loading or Refuge to Suspended, and Suspended back to its captured prior state. Active states may fail or shut down. Failed may retry Loading or shut down. Shutdown is terminal. Rejected and duplicate transitions leave state unchanged and emit typed diagnostics.

`DiagnosticEvent` carries an enum code plus previous/current application states. `IDiagnosticsSink.Record(in DiagnosticEvent)` is the application port. Infrastructure implements a bounded ring buffer with capacity 128 and chronological snapshot copies. No string event bus, file export, endpoint, or logging service is needed.

Presentation owns an explicit Bootstrap MonoBehaviour and a Refuge view. Awake composes the state machine and diagnostic buffer, enters Loading, constructs the minimal uGUI screen, then enters Refuge. Focus loss or pause suspends once; return resumes only when both pause and focus permit it. Disposal reaches Shutdown. Failed initialization stays visible as a generic error state; retry rebuilds the view and reaches Refuge when successful.

`Assets/Game/Scenes/Bootstrap.unity` is the first enabled build scene. Preserve SampleScene as an asset and as a disabled build entry. The screen contains a portrait-safe background and Refuge title only. No input recognizer, combat, inventory, persistence, route, Addressables resource ownership, or polished art is included.

## Verification

- Edit Mode tests exercise legal/rejected transitions, suspension restoration, failure/retry, terminal shutdown, chronological bounded diagnostics, snapshot isolation, and actual assembly references.
- Play Mode tests load the saved Bootstrap scene, assert Refuge and one visible view, exercise pause/focus overlap and teardown, and capture a local screenshot where the host supports graphics.
- Tests are written and observed failing before implementation. Unity generates metadata and the saved scene through an Editor authoring command.
- Keep Task 02 dependencies exact, preserve the alternate project, and compare preexisting source hashes. No commit or push.

## Execution decisions

The existing development plan and explicit request to begin Task 03 supply scope and execution authorization. Skill approval and commit steps defer to that authorization and the no-commit boundary. The repository has no initial commit, so operate at the requested root rather than creating an unavailable worktree. Desktop verification covers this empty screen; physical-device acceptance remains later work.
