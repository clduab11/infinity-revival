# Task 03 Bootstrap Implementation Plan

> **For agentic workers:** Use superpowers:subagent-driven-development with tests first and independent review. Steps use checkboxes as the execution ledger.

**Goal:** Boot the corrected Unity project into an empty Refuge screen with enforced assembly boundaries.

**Architecture:** Pure C# state and diagnostic contracts live in Application, with a Unity-free Domain boundary. Infrastructure supplies a bounded diagnostic buffer. Presentation explicitly composes services in the Bootstrap scene; Editor owns repeatable scene authoring.

**Tech Stack:** Unity 6000.6.0f1 (f7f8ed4d1e24), existing uGUI 2.6.0 and Test Framework 1.8.0, C# and Python standard library tooling.

**Spec:** [Task 03 design](../specs/2026-09-29-task-03-bootstrap-design.md), derived from original plan section 5.3 and task 03. The user's root-path correction takes precedence.

## Global constraints

- Canonical root: C:\Users\cld-main\Desktop\github-projects\infinity-redux.
- Keep Unity and all Task 02 dependency versions exact.
- No commits, pushes, new dependencies, server starts, purchases, or deployments.
- Preserve all existing assets and the alternate My project directory.
- One owner for scene/build settings. Only root runs Unity against the canonical project.
- No implementation from Tasks 04 onward. No physical-device acceptance claims.
- No file above 500 lines; keep methods below approximately 50 lines.

## Review focus

- Domain/Application engine dependency leaks, proven with compiled assembly references.
- Invalid and duplicate transitions, failure/retry, and terminal shutdown.
- Overlapping focus and pause events, repeated bootstrap, and view teardown.
- Diagnostics overflow order and mutable snapshot aliases.
- Saved build scene identity, existing source preservation, and test-only assembly exclusion.

## Shared interfaces

- `Praxen.Game.Application.ApplicationState`: Bootstrap, Loading, Refuge, Suspended, Failed, Shutdown.
- `ApplicationStateMachine(IDiagnosticsSink sink)`, `Current`, `TryTransition(ApplicationState next)`.
- `DiagnosticEvent`: `DiagnosticCode Code`, `ApplicationState PreviousState`, `ApplicationState CurrentState`.
- `DiagnosticCode`: StateChanged, TransitionRejected.
- `IDiagnosticsSink.Record(in DiagnosticEvent diagnosticEvent)`.
- `Praxen.Game.Infrastructure.BoundedDiagnosticsSink(int capacity = 128)`, `Count`, `TotalRecorded`, `Snapshot()`.
- `Praxen.Game.Presentation.BootstrapCompositionRoot`: `CurrentState`, `Diagnostics`, `void Initialize()`, `bool Retry()`, `void BindView(RefugeScreen)`, `void SetPaused(bool)`, `void SetFocused(bool)`.
- `RefugeScreen`: `IsVisible`, `void SetCamera(Camera)`, `void Show(ApplicationState)`, `void Hide()`.
- `Praxen.Game.Editor.BootstrapSceneAuthoring.CreateScene()` saves Bootstrap and makes it first enabled in build settings.

## Task A: state and diagnostics

Owner: application worker. Files: Runtime/Domain and Application; Infrastructure/BoundedDiagnosticsSink.cs; EditMode/ApplicationStateTests.cs and DiagnosticsTests.cs; associated assembly definitions.

- [x] Write Edit Mode behavioral tests and await the root's failing Unity test run.
- [x] Implement the shared interfaces and inward assembly boundaries.
- [x] Pass state, diagnostics, and compiled dependency tests.

## Task B: scene and presentation

Owner: root. Files: Runtime/Presentation; Editor; Tests/PlayMode; Content assembly definition; Bootstrap scene and EditorBuildSettings.

- [x] Write scene and lifecycle tests; observe missing bootstrap implementation in Unity.
- [x] Implement explicit composition and minimal Refuge/error view with deterministic teardown.
- [x] Generate Bootstrap via the Editor authoring method, preserving SampleScene.
- [x] Run Edit Mode and Play Mode suites; inspect a rendered screenshot if available.

## Task C: review and receipt

- [x] Independently review implementation and test coverage; fix material findings with covering tests.
- [x] Verify Task 02 baseline and preexisting source hashes; record intentional settings changes only.
- [x] Write Task 03 acceptance report and machine-readable evidence; update README.

## Ledger

- Preflight: Task 02 baseline passes, 48 direct/65 resolved packages, 35 asset GUIDs.
- Ruling: execute the requested task without redundant skill approval gates; no commits or worktree creation. Cost if wrong: design choices may need revision, all source edits remain reviewable and uncommitted.
- Ruling: native execution owns scene/Unity integration while an independent worker owns pure C# behavior. Cost if wrong: shared interfaces require coordinated edits; the interface block above is authoritative.
- Plugin discovery: Superpowers, Plugin Management, and available desktop tools are relevant. Unity search returned no usable plugin; use the installed Editor directly. No exposed MemPalace tool, so current repository evidence supplies context.
- Computer Use initialization failed with sandboxCwd/local-file-URI rejection; no UI input attempted. Desktop Commander live ping succeeded.
- Reviewer preset model unavailable on this account; an independent agent on the current supported model reviewed source, receipts, scene bindings, and preservation.
- Initial test configuration duplicated TestRunner references; corrected before intended red. Red then exited 1 for missing runtime APIs.
- AuthorScene exited 0; initial green: EditMode 59/59, PlayMode 6/6; portrait render inspected.
- Review found two P2 lifecycle gaps: blank failure UI when view/camera unbound, and a visible view after component-only teardown. Three tests reproduced them (review-red: 5 pass, 3 fail). Fix supplies camera-independent failure overlay, hides replaced fallback view, and hides view on component destruction.
- Final PlayMode: 8/8 passed, exit 0; scoped re-review resolved both findings with no new actionable issue. Task 02 validator passes with 61 valid metadata GUIDs and unchanged package graph.
- Preservation: 153/154 preexisting source files byte-identical; only EditorBuildSettings changed intentionally. Zero removals; all 62 alternate files unchanged. Fifty new Unity source/metadata files.
- Complete: Task 03 acceptance receipt and architecture notes written. No staged files, commits, remotes, pushes, or later-task implementation. All batch Editors exited; Pipeline's prior configuration restored.
