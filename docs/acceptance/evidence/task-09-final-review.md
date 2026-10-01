# Task 09 whole working-branch final review

Reviewed 2026-09-29. Base and current HEAD: `9ced64410bf9d5a6dd164d28455fb25ac2faf902`; branch `task-09-portrait-hud`; changes are uncommitted. This is an independent whole-branch source review, including untracked sources and tests, against `docs/plans/2026-09-29-task-09-portrait-hud-spec.md`, its implementation plan, development sequence Task 09, combat specification 3.2 and Unity systems 6.5. The review package was read first. No production/test assets were edited, no Unity process was launched, and no commits or subagents were created. Only this report was written.

## Strengths

- Layout and reach calibration are engine-free domain code; exploration input remains in the engine-free Application assembly. Both assembly definitions retain `noEngineReferences: true`. The HUD extracts presentation from the existing encounter view without changing stage geometry or semantic guard/dodge objects.
- Combat still uses the existing clock and ordered encounter resolver. Explicit pause has its own durable flag, gates capture, clears pending input and held guard, survives focus loss/return, and resumes through the existing three-second countdown. It does not replace the clock or replay already resolved damage/resources.
- Exploration ends the duel, advances phase identity, accepts only contacts owned by gameplay at Begin, uses one latched gameplay pointer, rejects stale phase/metrics/timestamps and post-drag taps, and bounds camera movement. The dedicated overflow quarantine, mirrored calibration actions and bound-camera repairs address the three earlier scoped findings.
- New visible HUD/menu/feedback/exploration text uses `HudText` keys. Dynamic HUD and static menu use separate camera canvases, with raycasting menu coverage. Settings remain session-local, ability intent does not spend resources or apply effects, and prototype destination selection is clearly separated from future traversal.
- Documentation consistently distinguishes implementation and Editor captures from physical same-hand acceptance. Synthetic safe areas are labelled as scenarios, and physical and built-player qualification remain UNRUN.

## Findings disposition

### [RESOLVED P2, SPEC/QUALITY] Cancel native UI button presses when layout changes

The following trigger/evidence records the initial reviewed implementation before the repair. The scoped re-review below verifies its resolution.

**Evidence:** `Assets/Game/Runtime/Presentation/Combat/PortraitHudCoordinator.cs:121-126` cancels custom defense/duel/calibration/exploration contacts and toggles `TimestampedTouchCapture`, but does not invalidate the native UI module's held button press. Ability uses the standard `Button.onClick` listener at lines 28 and 62; `RequestAbility` at lines 63-69 has state gates but no contact/layout generation check. `PortraitCombatHud.cs:228-232` changes control geometry and emits `LayoutChanged` while combat can remain running.

**Trigger and impact:** Begin a native touch on Ability, let `InputSystemUIInputModule.Process()` latch the press, change the safe-area/layout by a small amount that leaves that point inside the same Ability rectangle, then release at that point. The custom capture contact has been cancelled, but the standard UI press remains eligible, so release still emits `AbilityRequested`. The same uncancelled UI path applies to menu button presses when geometry/settings change. This violates the explicit spec requirement that layout changes cancel contacts and permits a stale action after the layout transition.

**Installed runtime evidence:** `Library/PackageCache/com.unity.inputsystem@7a4e1a2a8194/InputSystem/Runtime/Plugins/UI/InputSystemUIInputModule.cs:649` marks a press eligible; lines 681-682 save its press/click handlers; lines 702-727 invoke the click on release when the same handler remains under the pointer. `TimestampedTouchCapture.cs:59-68` only resets its recognizer and ordered owners, with no native UI reset. These installed-source observations established the missing cancellation path. The subsequent native RED receipt reproduced it and the scoped repair passed GREEN, as recorded below.

**Fix and verification:** Invalidate held UI presses alongside the existing contact cancellation, using a contact/layout generation gate or a supported native UI cancellation path that requires a fresh Begin. Add a native touch regression: down on Ability and process UI, apply a small viewport/safe-area change, release at the same screen point and process UI, assert zero intents; then assert one intent for a fresh down/up. Include a menu action to ensure the repair is not ability-only. Do not disable the whole UI persistently or suppress fresh interactions.

No other new P0-P2 finding was identified in the reviewed source.

## Coverage and evidence limits

Reviewed all changed existing production C# files, all new production C# files, all new EditMode and PlayMode tests/capture helpers, the orientation settings change, README, asset-readiness changes, acceptance draft and the authored spec/plan. Relevant unchanged touch capture, native pointer ownership, defense controls, clock and installed UI release code were traced to assess integration behavior.

The new test sources cover layout extremes, pixel-square geometry, mirrored semantics, three-sample calibration, bounds, exploration owner latching, irreversible dragging, cancellation, chronology/metrics/phase rejection, overflow quarantine, native calibration input, native ability clicking, user-pause/focus behavior and bound-camera selection. Existing tests were not modified. The default portrait orientation has an installed API regression test. Scope-restricted review reports were also read after independent inspection of their repaired code.

Existing `output/task-09` iPhone-right, pause-menu and exploration-preview images were visually inspected as native Editor layout artifacts. They show the intended prototype layout and readable controls in those scenarios; they do not establish reach, device safe-area measurements, player qualification or runtime responsiveness. The exploration instructions appear twice, which is a minor presentation polish item, not an acceptance-blocking P0-P2 defect.

Final native runs and preservation checks are root-owned. This reviewer has now independently inspected the final PlayMode receipt and baseline validator receipt, recorded below. The final EditMode rerun after the UI repair remains pending at this report revision; earlier counts are not promoted to that final result. The accepted compiler pin is Unity 6000.6.0f1, revision f7f8ed4d1e24, read from `ProjectSettings/ProjectVersion.txt`. Windows Git reports only the expected source/config/doc changes and new Task09 files. WSL Git's missing git-lfs executable and filter-disabled apparent PNG modifications were tooling artifacts; no binary modification claim is made from those results.

## Declined to judge

- Physical same-hand ergonomics, missed/unwanted touches, actual OS/device support and measured safe insets: no physical phone is connected, so these remain UNRUN and block the full Task09 exit rather than constituting a source defect.
- Sustained latency, recognition, readability and frame-time qualification: the authored sequence assigns the complete device campaign to Task10; Editor captures are insufficient.
- Ability effects, spending, equip flow and progression: explicitly reserved for Task22; the Task09 gated intent event is the required boundary.
- Authored traversal, route rewards and checkpoints: explicitly reserved for Task21; the bounded Graybox preview satisfies the current input-plumbing scope.
- Preference persistence and save migration: explicitly reserved for Task15; session-local settings match the current spec.
- Finished art, animation, material/content approval and campaign completion: outside the frozen Task09 work order.
- Final receipt links/count placeholders in documentation: root is still assembling final evidence; the final published acceptance record must replace pending fields only with verified evidence.

## Scoped final-fix re-review

This follow-up inspected only the new `CancellableHudButton.cs` and its metadata, the HUD button factory and UI cancellation method, the coordinator cancellation hook, the two added native regressions, and the corresponding RED/GREEN and baseline receipts. It does not duplicate the broad review above. Production/test assets remained untouched by this reviewer.

- `CancellableHudButton.cs:11-24` records an eligible left-button pointer down, requires the same pointer for a click, and consumes the token before invoking normal Button behavior. Lines 27-32 clear the token on explicit cancellation and disable. A cancelled native module click therefore cannot invoke the HUD action; a fresh down can establish a new token. Normal `Button.OnSubmit` remains unchanged, an intentional future keyboard/controller path outside this mobile pointer repair.
- `PortraitCombatHud.cs:192-195` cancels pending tokens on every custom HUD button, including inactive menu descendants. The shared factory at line 312 creates the subclass for Ability, Menu and all menu actions, so the repair is not ability-only. `PortraitHudCoordinator.cs:127-130` invokes this cancellation alongside existing calibration/exploration resets. Existing layout, lifecycle, suspension and capture cancellation paths already reach that method.
- `LayoutChangeCancelsHeldNativeAbilityPress` at `PortraitHudIntegrationTests.cs:200-212` uses a native down/UI processing, a small viewport change and native up/UI processing, then asserts zero ability intents. `FocusCancellationRequiresFreshNativeResumePress` at lines 215-229 presses native Resume, loses and regains focus, releases and verifies the explicit user pause and suspended clock remain intact. The ordinary native Ability click regression also still passes, confirming valid pointer clicks remain functional.
- Independently parsed `output/task-09/ui-cancellation-red.xml`: 75 total, 73 passed, 2 failed, zero skipped/inconclusive. The exact failures were stale Ability producing 1 intent instead of 0 and stale Resume clearing user pause. This directly reproduces the reported defect before the repair.
- Independently parsed `output/task-09/playmode-final.xml`: 75/75 passed, zero failed, skipped or inconclusive. Both cancellation regressions and the ordinary native Ability click pass. Compared full test identities against `docs/acceptance/evidence/task-08-playmode.xml`: all 60 previous identities are preserved, 15 added, zero missing.
- Independently read `output/task-09/baseline-final.json`: `ok: true`, 48 direct packages, 65 resolved packages, 155 asset GUIDs, Git checked, no errors. The root reports 669 passing EditMode tests before this UI-only repair; the final EditMode rerun remains pending and is not claimed verified here.

The sole whole-branch P2 is resolved. There is no remaining actionable P0-P2 finding in the reviewed branch plus this scoped repair.

## Verdicts

**SPEC: PASS for the reviewed implementation scope.** The whole-branch review and scoped final repair now satisfy the authored Task09 source specification. The physical same-hand acceptance gate remains UNRUN independently and still blocks the full Task09 exit.

**QUALITY: PASS.** The missing native UI contact boundary is repaired and reproduced by meaningful RED/GREEN native regressions. Final PlayMode evidence is 75/75 passing with all 60 legacy identities preserved. Aggregate task verification still requires the root's final EditMode and preservation evidence, and neither source PASS nor Editor tests establish physical-device qualification.
