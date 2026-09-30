# Task 06 independent review

**Final rereview:** No remaining P1/P2 defects found in the ownership and bounds fixes.
Reviewer was a separate read-only agent; root ran all Unity verification.
Six P2 findings were resolved and covered by behavior regressions.

| Finding | Correction | Regression evidence |
|---|---|---|
| Dispose left pending strikes | Cancel still-owned pending milestones | DisposeCancelsOnlyOwnedStrikeMilestonesAndAllowsReuse |
| Late stagger hit skipped hit recovery | Separate hit deadline; retain original stagger deadline | LateHitKeepsStaggerDeadlineThenCompletesFullHitRecovery |
| Capture shutdown stranded guard/contact | Explicit ContactsCancelled notification, balanced subscriptions | DisablingCaptureReleasesGuardAndAllowsFreshControlAfterReenable |
| Rejected terminal release stranded guard | Guard-only fallback after timeline; terminal admission under inbox pressure | RejectedTerminalReleaseClearsGuardWithoutRetroactiveImpactChanges; RejectedReleaseAtCapacityCannotLeaveAnEarlierQueuedPressHeld; FullDriverInboxRetainsTerminalGuardRelease |
| Reused resolved milestone ID lost ownership | Remove owned ID before routing; dispose only remaining IDs | ResolvedMilestoneIdCanBeReusedWithoutRoutingOrDisposalByOldOwner |
| Old rejected release cleared a new contact | Unique raw guard action ID, matched release ownership | StaleReleaseForOldGuardCannotClearAFreshContactsGuard; DelayedOldEndCannotReleaseAFreshGuardContact |

Additional fixes preserve a committed parry during guard release, accept safe
release in every state, coordinate encounter end, and continue the visual dodge
pose through its recovery tail. Tests require both combatants in the PNG.

Final reviewer conclusions:

- Milestone ownership is removed before routing; disposal cancels only remaining owned milestones.
- Guard action IDs survive validation and raw End handling; stale releases preserve fresh guards and armed parries.
- Capacity fallback runs after accepted inputs; the driver retains terminal releases within its fixed inbox bound.

Actual tests-first evidence:

- Initial missing-API Unity run exited 1 before production defense types existed.
- Review regressions: 376 tests, 368 passed, eight expected failures before fixes, [XML](task-06-review-red.xml).
- Missing guard-action API run exited 1 with CS1739 before adding the ownership field.
- Reused-ID regression: 381 tests, 380 passed, one expected failure before ownership fix, [XML](task-06-ownership-red.xml).
- Dodge pose regression: 47 tests, 46 passed, one expected failure before pose fix, [XML](task-06-pose-red.xml).
- Final restored implementation: [381/381 Edit Mode](task-06-editmode.xml) and [48/48 Play Mode](task-06-playmode.xml), zero failures/skips.

The first raw-touch fixture run failed because the camera-space canvas had not
rendered: its graphic depth was -1 and raycasts returned Gameplay. A real graphics
warm-up produced depth 8 and the correct UI control ID. The fixture now verifies
that owner before sending touches. Initial stage capture also required render
warm-up; installed URP GPU Resident Drawer source supports deferred runtime mesh
upload as the likely cause. This timing explanation is an inference, not an
instrumented renderer proof. The final portrait is pixel-checked and inspected.

No review tool ran an additional Unity Editor or changed files. Physical device,
release build, ergonomic, animation-contact and performance acceptance remain
outside this task.
