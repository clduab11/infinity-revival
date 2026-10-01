# Task 09, Tasks 2 and 4 UI review

SPEC verdict: PASS for scoped source re-review, prior mirror finding resolved.
QUALITY verdict: PASS for scoped source re-review, prior bound-camera finding resolved.

Read-only review of Task 2/4 briefs, portrait HUD specification, HUD/menu/view/root/coordinator/calibration/exploration/timing code, ownership capture, clock, and PortraitHudIntegrationTests. No Assets edits, Unity execution, commits, or delegated work. Native receipts were pending when written: native compilation, geometry, input, screenshots, and previous-test retention are UNVERIFIED. Physical hold-and-operate acceptance remains deliberately UNRUN. Session-local preferences and an ability intent without effects are intentional scope.

## Original findings, both resolved in scoped re-review

### [RESOLVED P2, SPEC] Mirror calibration Apply and Cancel with handedness

Evidence: Assets/Game/Runtime/Presentation/Combat/PortraitHudMenu.cs:52-53 creates Cancel at x=.08-.47 and Apply at x=.53-.92. ShowSettings at lines 86-94 mirrors Resume, Restart, Hand, Calibrate, Scale, and Explore only. Calibration buttons keep their right-hand layout after choosing Left. The specification requires mirroring all HUD controls. Apply remains on the opposite thumb side during left-hand calibration.

Fix: mirror existing calibration rectangles when settings change while preserving semantic identity and vertical placement. Add geometry assertions for both calibration controls under Left and Right; the current mirror test only checks the guard object and hand value.

### [RESOLVED P2, QUALITY] Use the encounter's configured camera for exploration

Evidence: Assets/Game/Runtime/Presentation/Combat/PortraitHudCoordinator.cs:97 passes Camera.main to ExplorationPreview. Assets/Game/Runtime/Presentation/Combat/ExplorationPreview.cs:19 immediately dereferences camera.transform. GrayboxEncounterRoot.Bind explicitly accepts a camera and Awake builds View with encounterCamera. A valid bound untagged camera produces a functioning HUD but selecting Explore throws NullReferenceException if no MainCamera exists. With another MainCamera, exploration rotates the wrong camera and restoration targets the wrong object. Combat has already ended before construction throws, leaving a partially switched state.

Fix: obtain the camera used to build the HUD (or expose root's encounter camera), validate it before ending the encounter, and use it for preview movement and restoration. Verify a bound untagged camera scenario and a scene with a distinct MainCamera.

## Reviewed behavior and remaining evidence

- HUD preserves public guard/dodge semantic objects, resource/status getters and stage construction; layout mirrors positional dodge controls while keeping LEFT/RIGHT labels and control identity stable. Two camera canvases have GraphicRaycasters, with the full-screen menu above the HUD. New HUD/menu/exploration strings use HudText keys. Existing combat prompt strings forwarded by the root predate this task.
- Timing.IsUserPaused independently gates input, survives focus return, and explicit combat Resume begins the three-second countdown. Layout cancellation clears held defense, duel contacts, calibration contact and exploration contacts, then toggles capture to exclude old contacts. AbilityRequested checks exploration/calibration, lifecycle/countdown/user pause and duel outcome; it spends no resources and applies no effects.
- Calibration listens to raw samples while combat capture is disabled, resolves ownership at Begin against the calibration surface's exact Image entity ID, ignores other contacts while one is active, rejects travel over threshold and backwards timestamps, accepts only Ended, and delegates lower-safe-area bounds and clamped averaging to ReachCalibration. Exploration receives only contacts owned by gameplay at Begin, disposes combat, advances phase, resets on cancellation/lifecycle/layout, and restores camera rotation on restart/disposal.
- Integration tests include InputSystem touch records for guard, calibration drag rejection, three accepted taps, exploration tap/drag and native UI ability click, plus scripted pause/countdown. Native receipts remain unverified. Missing integration scenarios relevant to requirements: concurrent second calibration pointer, native exploration second-pointer exclusion, focus/layout cancellation during calibration and exploration, and mirrored calibration buttons. Domain tests can cover portions, but cannot establish the UI/capture boundary.

## Scoped re-review

Scope: only the two prior P2 findings and their regression tests. This is not a whole-branch review.

- Mirror finding resolved: PortraitHudMenu.cs:94-95 now passes Cancel and Apply through SetCalibrationColumn. Lines 97-104 derive mirrored anchors from the original fixed extents, preserving button identity and vertical placement. The new CalibrationActionsMirrorWithoutChangingApplyAndCancelMeaning regression (PortraitHudIntegrationTests.cs:223) asserts Apply's left-hand mirrored anchor. Cancel uses the same helper, with its own original extents.
- Bound-camera finding resolved: GrayboxEncounterRoot.cs:27 exposes EncounterCamera and PortraitHudCoordinator.cs:97 passes it into ExplorationPreview. This is the camera used by View.Build, so valid explicitly bound untagged cameras no longer depend on Camera.main. ExplorationUsesBoundCameraWithAnotherMainCameraPresent (PortraitHudIntegrationTests.cs:234) verifies movement of the bound camera and preservation of the distinct main camera.
- Parent reports review-red.xml had 73 total tests, 71 passed and the two exact regression failures before fixes. That receipt was not independently located/inspected during this scoped re-review. Final native run is still pending, so final native results and rendered acceptance remain UNVERIFIED.

Both prior source findings are resolved. No remaining actionable finding in this scoped re-review. Native acceptance and physical UNRUN boundaries still apply; the source PASS verdicts do not establish device qualification.
