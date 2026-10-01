# Task 09 domain and exploration review

Scope: Task 1 layout/calibration and Task 3 exploration owner, owned implementation, tests and reports, plus referenced value types and recognizer. No production edits, Unity runs, broad root/UI review, commits or subagents.

## SPEC verdict: PASS

Task 1 passes scoped static review. The normalized cluster maintains pixel-square targets, semantic dodge identity, mirrored X and lower-45% bounds. Independent arithmetic checked all 216 combinations represented by four reference/extremal aspects, three scales, three X offsets, three Y offsets and both hands. Every interactive pair stays disjoint. Clear gesture region derives from the leftmost cluster edge and mirrors; tells remain in y .70-.85. Calibration accepts precisely three finite bounded samples, averages against the hand-correct default guard center, preserves hand/scale, clamps offsets and resets sums/count.

Task 3 passes normal-contact ownership, release-only tap, inclusive short-dimension threshold, irreversible drag, End-without-Moved, cancellation, latched metric/phase invalidation and timestamp inversion inspection. Public input boundary reconstructs OrderedTouchRecord and InteractionPhase, rejecting invalid default sample/metrics and treating default inactive phase as non-exploration. Global timestamp high-water handling is appropriate for ordered records. Constructor/Reset require positive phase IDs.

Prior P2 resolved in scoped re-review. `Assets/Game/Runtime/Application/Input/ExplorationInputController.cs:15,29,36,75-82` now invalidates the 32 stored contacts and latches overflow quarantine. Process still validates public inputs before ignoring further events; explicit Reset clears identities, ownership and quarantine. No post-overflow Begin, Move or End can emit a destination or inspection event or promote held gameplay, excluded or UI contacts. The dictionary remains bounded to 32 entries. Explicit Reset recovery is the deliberate root-approved ledger ruling for abnormal overflow, not a change to normal excluded-until-lift semantics.

The immutable sealed HudSettings implementation is an acceptable adjustment to the brief's readonly wording: get-only fields preserve immutability and parameterless construction correctly yields scale 1 rather than struct zero initialization. No public struct default hazard is introduced by this adjustment.

## QUALITY verdict: PASS

No remaining actionable findings in scope. `Assets/Game/Tests/EditMode/ExplorationInputTests.cs:174-195` adds three parameterized regressions for held gameplay owner, excluded second contact and UI contact. Each triggers overflow, attempts duplicate Begin/End on the held identity, checks no events, attempts a fresh drag while quarantined, checks no events again, then explicitly resets and verifies a new tap. This directly catches the original defect and exercises both quarantine persistence and recovery. Root reports all three regression cases failed before the fix and layout/exploration cases now pass in the native run.

Remaining code is small, engine-free, dependency-neutral and readable. Geometry tests check actual rectangle fit, pairwise overlap, square pixels, semantic mirroring and extreme offset/scale combinations rather than only snapshots. Exploration tests cover independently observable events and irreversible state transitions. Nonblocking coverage opportunities: diagonal Euclidean travel just above threshold, gameplay Begin whose End is labelled UI (ownership remains latched), default OrderedTouchRecord rejection, equal timestamp acceptance, and Reset/constructor invalid IDs. Boundary validation exists in implementation; these are not additional defects.

## Evidence and limits

Read task-1-brief.md, task-3-brief.md, task-1-report.md, task-3-report.md and docs/plans/2026-09-29-task-09-portrait-hud-spec.md. Independent Python arithmetic verification passed 216 combinations; this is a structural check, not native test execution. Reviewed native attempt output/task-09/editmode-green-1.log, which records presentation compiler errors and exit 1; it does not establish GREEN. For this scoped re-review, root reports native overflow RED for all three new cases, followed by a run with 668 passing tests out of 669 and its sole failure in a separate portrait-setting case, subsequently fixed. Layout and exploration tests therefore passed in that reported run; aggregate final GREEN remains root-owned and is not independently claimed here. Task 1 implementation and prior verdict are unchanged; it was not re-reviewed beyond the retained evidence. Re-review inspected only the updated exploration owner, overflow regressions and this report.

Physical same-hand hold-and-operate acceptance remains UNRUN. No device/OS qualification, performance soak, campaign traversal, ability effects or resource spending is inferred from this review. Those future boundaries remain intact.
