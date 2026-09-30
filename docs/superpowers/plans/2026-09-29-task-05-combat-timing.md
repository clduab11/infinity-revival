# Task 05 Combat Timing Implementation Plan

Use Superpowers tests-first implementation, bounded parallel ownership,
independent review and verification before completion. Checkboxes form the ledger.

**Goal:** Ordered event-time outcomes independent of 30/60/120 presentation rates,
with suspension before unseen catch-up attacks.

**Spec:** [Timing design](../specs/2026-09-29-task-05-timing-design.md).

**Stack:** Unity 6000.6.0f1, existing Input System 1.20.0 and Test Framework 1.8.0.
No package changes. Canonical checkout stays at the corrected outer directory.

## Ownership and execution

- Clock worker: Domain/Combat/CombatClock.cs and EditMode/CombatClockTests.cs.
- Timeline worker: Domain/Combat/CombatTimingValues.cs, CombatTimeline.cs and
  EditMode/CombatTimelineTests.cs.
- Root: Application/Combat/CombatSession.cs, Presentation/Combat/CombatTimingDriver.cs,
  Bootstrap composition, session and integration tests, metadata and evidence.
- Root alone runs the installed Unity Editor and modifies shared integration.
- Independent read-only review after full green suites; fix material findings.

## Checklist

- [x] Inspect exact timing spec and input lifecycle seams.
- [x] Record design and bounded contracts; compose an inactive session driver.
- [x] Write missing-API tests and observe intended Unity compilation failure.
- [x] Implement clock, ordering, mapping, warning retiming and lifecycle adapter.
- [x] Replay fixtures at 30/60/120 and jittered rates with explicit outcomes.
- [x] Run all Edit Mode, Play Mode and Python baseline checks.
- [x] Review independently, repair findings and repeat affected checks.
- [x] Export portable receipts and update README/architecture/acceptance report.

## Ledger and rulings

- Explicit "Begin Task 05" authorizes reversible implementation without another
  design approval gate. Scope excludes Task 06 mechanics and Task 09 control UI.
- Use the corrected checkout on local branch task-05-combat-timing. A new worktree
  would reintroduce the path friction the user explicitly asked to remove.
- No commit, push, dependency update, new service, purchase or later task.
- Supported default agents replace historical unsupported model presets.
- Superpowers, Plugin Management and Desktop Commander were used. Plugin search
  found no usable Unity integration; the installed Editor provides runtime proof.
- Input API investigation established before-update stall gating and after-update
  resolution. Delivery-time phase attribution is explicitly reserved for the
  encounter resolver rather than misrepresented as solved by typed milestones.
- Three-second countdown is a reversible default because the original plan
  specifies a countdown without its duration. The public clock accepts tuning.

- Missing-API red observed: Unity Edit Mode exit 1, absent Combat contracts.
- Final Edit Mode: 213/213. Play Mode: 33/33 after lifecycle repairs.
- Public fixture policies select player updates in batch mode and restore afterward;
  fake Touchscreen creation follows configuration to avoid background disable.
- Two independent P2 findings fixed and rereviewed. With corrected fixtures, removing
  those two fixes yields exactly two failures, accepted code restored byte-for-byte.
- Actual replay receipts match nine events and two defended/one undefended impacts
  at 30/60/120 and jittered delivery. Physical acceptance remains unrun.
- Baseline passes: 48 direct, 65 resolved packages, 94 unique GUIDs; Python 12/12.
- Portable evidence and README updated. No later task, commit or push performed.
