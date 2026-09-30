# Task 07 independent review record

Review was read-only source inspection by separate agents. Native verification
was run serially by the root agent in the pinned Editor; final suite counts are
recorded in the Task 07 readiness report and XML receipts.

## Findings and repairs

- A ready request tied to recovery could mutate offense without reserving its
  impact/recovery records. All ready admission now reserves the complete batch.
- A first swipe during inherited defense recovery had no consumption trigger.
  It now owns a buffered-offense opportunity at the shared action deadline.
- Early buffer consumption discarded an eligible command while still busy.
  It now preserves the first command until readiness, expiry, or invalidation.
- Decay tuning unnecessarily added delay and step, rejecting a valid maximum
  delay. Elapsed-time decay needs no absolute deadline addition.
- A late or capacity-rejected terminal raw sample retained gameplay ownership.
  Session cancellation now occurs after accepted records resolve. Driver inbox
  overflow and terminal eviction also cancel after the batch; device removal
  resets contacts outside input updates.
- Next enemy admission could fail once at opening close and leave combat idle.
  A failed capacity admission now retries through application frame advancement
  after the batch drains, at a fresh future telegraph timestamp. Observer-only
  callers retain one readiness notification.
- Zero enemy recovery opened before its tied impact and cleared armed defense.
  Inclusive enemy defense and impact now resolve before the opening starts at
  that same timestamp.
- Current milestone IDs became reusable before pre-resolution owners completed
  their handoff, allowing disposal to cancel reused foreign work. The timeline
  now reserves the current ID throughout BeforeResolve, releases it in finally,
  and permits reuse during Resolved.

The raw-parry Play Mode fixture was also corrected from Right to the first
fixed strike's authored Up direction. This was a test fixture error, not a
change to the fixed attack catalog.

## Evidence

- [Missing API RED](task-07-missing-api-red.log): native compiler exit 1 before implementation.
- [Buffer RED](task-07-buffer-red.xml): 483 cases, 480 passed, three reproduced failures.
- [Admission RED](task-07-admission-red.xml): 487 cases, 483 passed, four reproduced failures.
- [Touch RED](task-07-touch-red.xml): 56 cases, 53 passed; two cancellation defects plus the incorrect parry fixture.
- [Ownership RED](task-07-ownership-red.xml): 488 cases, 487 passed, current-ID handoff reproduced.
- [Final Edit Mode](task-07-editmode.xml) and [Play Mode](task-07-playmode.xml): fresh verification after all repairs.

## Final source signoff

No remaining P1/P2 findings. The last focused rereview confirmed the current-ID
reservation and its regression. Review does not establish physical-phone,
player-build, final animation, final HUD, or campaign acceptance.
