# Task 05 independent review

Review covered integer clock mapping, chronological and tied ordering, sealed
horizons, bounded queues, before/after Input System hooks, pause and countdown,
warning retiming, held-gesture cancellation, composition and test coverage.
Reviewers were read-only; root exclusively ran Unity and changed integration.

## Findings and resolution

1. P2: Re-enabling an inactive driver left Refuge capture disabled. `OnDisable`
   disabled input; `OnEnable` restored hooks without restoring eligibility.
   Fixed by calling `RefreshInput` on enable. The behavioral regression delivers
   a real queued gesture after disabling/re-enabling the inactive driver.
2. P2: A stall occurring during input processing froze the session without
   notifying suspension observers. Fixed by detecting the newly suspended state
   after frame advancement and emitting `Suspended`. The regression changes
   source time across the before/after hooks, requiring no impact and one notice.

Command reception also filters gameplay update types consistently with the hooks.
Public test settings select the player stream in a batch Editor and restore prior
focus/update policies afterward. Fake Touchscreen creation follows that temporary
policy configuration to avoid inherited background-device disable state.

## Verification

- Initial intended missing-API red: Unity Edit Mode exit 1.
- Repaired complete Play Mode suite: 33/33 passed.
- Focused independent rereview: both P2 findings resolved; no remaining material
  Task 05 finding. Timeline-only independent review also found no defect.
- Mutation sensitivity: temporarily remove only the two lifecycle fixes, retaining
  the corrected fixture. Exactly the two new regressions fail, 31/33 pass.
  Accepted driver restored byte-for-byte before the final full suite.

Final fresh XML and source hashes are recorded in the [Task 05 evidence
receipt](../task-05-evidence-2026-09-29.json). Later encounter phase ownership,
damage/guard mechanics, HUD rendering, save/restart and physical-device acceptance
remain outside this review's completion claim.
