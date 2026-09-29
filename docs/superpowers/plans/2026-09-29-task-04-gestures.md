# Task 04 Gesture Capture Implementation Plan

> **For agentic workers:** Use Superpowers tests-first implementation, parallel ownership, independent review, and verification. Checkboxes form the execution ledger.

**Goal:** Preserve original touch timestamps and emit exactly one correctly owned, phase-bound cardinal command per eligible gesture.

**Architecture:** Domain contains an engine-free recognizer and immutable values. Application supplies phase and ownership ports. Presentation uses per-event EnhancedTouch capture, synchronous UI ownership, lifecycle cancellation, editable tuning, and a short direction trace. Bootstrap supplies explicit composition.

**Tech Stack:** Unity 6000.6.0f1 (f7f8ed4d1e24), Input System 1.20.0, uGUI 2.6.0, Test Framework 1.8.0; no new dependencies.

**Spec:** [Task 04 design](../specs/2026-09-29-task-04-gestures-design.md), original plan section 3.2 and task 04.

## Constraints and review focus

- Canonical root remains C:\Users\cld-main\Desktop\github-projects\infinity-redux.
- Preserve all dependencies, original GUIDs, the alternate project, and Refuge startup.
- Root exclusively runs Unity and owns shared scene/build settings. No commits, pushes, services, purchases, deployments, or later-task implementation.
- Test strict 6% threshold, inclusive 350000us deadline, ties, normalized rectangular coordinates, stale phases, UI crossings, second contacts, repeated Begin/Move/End, cancellation, and re-enable while held.
- Preserve every queued move and source timestamp; balance EnhancedTouch support and restore merge settings across multiple capture owners.
- Keep files under 500 lines and methods near/below 50 lines. Keep application and Domain engine-free.

## Shared contracts

Namespace `Praxen.Game.Domain.Input`:

- `NormalizedPoint(double x, double y)`: X/Y, finite coordinates.
- `ScreenMetrics(int width, int height)`: Width/Height, positive dimensions.
- `SamplePhase`: Began, Moved, Ended, Cancelled.
- `TouchSample(long pointerId, SamplePhase phase, NormalizedPoint position, long timestampUs)`; PointerId, Phase, Position, TimestampUs.
- `PointerOwnerKind`: Gameplay, Ui, Excluded.
- `PointerOwnership(PointerOwnerKind kind, ulong controlId = 0)`; Kind/ControlId; static Gameplay/Excluded. UI IDs preserve Unity's full 64-bit EntityId.
- `InteractionPhaseKind`: Inactive, EnemySequence, PlayerOpening.
- `InteractionPhase(long id, InteractionPhaseKind kind)`; Id/Kind; static Inactive.
- `SwipeDirection`: Left, Right, Up, Down; `GestureIntent`: Parry, Attack.
- `GestureTuning(double travelThreshold = 0.06, long maximumDurationUs = 350000)`; TravelThreshold/MaximumDurationUs; static Default.
- `GestureCommand`: Sequence, InputTimestampUs, PointerId, Direction, Intent, Phase, Start, End.
- `CardinalGestureRecognizer(GestureTuning tuning = null)`; `GestureCommand? Process(in TouchSample sample, in PointerOwnership ownershipAtBegin, in InteractionPhase currentPhase, in ScreenMetrics metrics)`; `void CancelAll()`; `void ResetContacts()` (preserves sequence).

Namespace `Praxen.Game.Application.Input`:

- `IInteractionPhaseSource.Current`: InteractionPhase.
- `IPointerOwnershipResolver.Resolve(in TouchSample begin, in ScreenMetrics metrics)`: PointerOwnership.
- `GesturePhaseContext`: Current; `void SetPhase(InteractionPhase phase)` accepts strictly increasing phase IDs; initial Inactive/0.

Namespace `Praxen.Game.Presentation.Input`:

- `TimestampedTouchCapture.Configure(IInteractionPhaseSource, IPointerOwnershipResolver, GestureTuning)`; `SetInputEnabled(bool)`; typed SampleCaptured and CommandProduced events; `Tracked` recognition private.
- `EventSystemPointerOwnership`: synchronous UI GraphicRaycaster ownership at Begin.
- `EnhancedTouchSession`: shared balanced support/merge lease, IDisposable.
- `GestureTraceView.Show(GestureCommand)` / `Clear()`; brief typed-direction trace, never a raycast target.

Namespace `Praxen.Game.Content.Input`: `GestureTuningAsset` with editable defaults and `ToRuntime()`.

Bootstrap exposes InputCapture and InteractionPhase context, initializes an owned EventSystem where needed, configures capture/trace, and gates capture alongside existing lifecycle state. Root scene authoring binds the tuning asset while preserving scene GUID/build list.

## A: pure recognizer (worker)

Files: Domain/Input types and recognizer, Application/Input ports/context, EditMode/Gesture* tests.

- [x] Write meaningful behavioral tests and notify root; wait for intended red Unity run.
- [x] Implement the shared pure contracts and pass the tests.

## B: real input adapter and integration (root)

Files: Presentation/Input; Content/Input; BootstrapCompositionRoot, RefugeScreen, BootstrapSceneAuthoring; affected assembly references; PlayMode/TouchCapture* tests; tools/build/run_unity_checks.ps1.

- [x] Write public InputSystem queued-event integration tests; observe missing implementation.
- [x] Implement timestamp capture, UI ownership, leases, contacts/lifecycle, tuning, and trace.
- [x] Bind tuning through Editor authoring. Keep Refuge startup and original scene GUID.
- [x] Run all Edit Mode/Play Mode tests and inspect trace feedback.

## C: review and acceptance

- [x] Independent review; reproduce/fix material findings and re-review.
- [x] Verify unchanged package baseline and original source preservation.
- [x] Record exact test receipts, known acceptance limits, architecture/README updates.

## Ledger and rulings

- Preflight: Task 02 validator passes, 48 direct/65 resolved packages, 61 metadata GUIDs; snapshot 204 preexisting source files. No canonical Editor is running.
- Ruling: the existing work order and explicit execute request authorize implementation without repeated skill approval gates. No commits/worktrees. Cost if wrong: reversible design edits may need revision.
- Ruling: use supported current-model agents because historical worker/reviewer presets are unavailable on this account. Root owns Unity integration; worker owns pure code; independent reviewer verifies both. Cost: higher model cost, avoids failed dispatches.
- Plugins: Superpowers and Plugin Management used; Desktop Commander live ping succeeded. No usable Unity integration returned. Computer Use previously rejected this unchanged workspace URI, so use installed Editor and package-source evidence.
- Input API investigation: EnhancedTouch preserves source times and reset cancellation; redundant event merging must be disabled during capture. Public-device queued tests avoid manifest testables changes.
- Missing-API red: Unity exit 1, expected absent runtime boundaries. First authoring compile then caught the installed Unity 6.6 GetInstanceID obsolete-error; preserve full EntityId with ulong rather than truncating to an int.
- Review red reproduced invalid-tuning partial composition, removed-device pointer lock, and inactive contacts occupying the eligible gameplay slot. Eligibility red: 143/148 passing, one intended behavior failure plus four NUnit int-to-ulong fixture conversion failures; all repaired without weakening assertions.
- Settings/trace red: 19/23 passing, four intended failures for tuning retry, device removal, settings replacement, and resized trace geometry. All fixed. Actual UI fixture depth was -1 before rendering, so tests now render first and assert ownership.
- History-rebuild red: 23/24 passing; update-mode changes expanded two source samples into six recorded samples. Adapter filters synthetic rebuild callbacks and cancels held recognition. The independent re-review confirms installed callback ordering and reports no remaining actionable finding.
- Final acceptance: 148/148 Edit Mode, 24/24 Play Mode, and 12/12 existing validator tests pass. Portrait trace render visually inspected. Baseline passes with 48 direct/65 resolved packages and 82 unique GUIDs.
- Preservation: 196/204 original source files byte-identical, eight intended modifications, zero removed. All 62 alternate files, packages, original metadata, and build settings remain unchanged. No staging/commit/push or later task started.
- Evidence and remaining limits: docs/acceptance/task-04-readiness-2026-09-29.md and task-04-evidence-2026-09-29.json. Player builds, physical touch latency, rotation/safe areas, ergonomics, release controls, and gameplay outcomes remain unrun or unverified.
