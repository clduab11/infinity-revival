# Task 06 Defense Implementation Plan

> **For agentic workers:** Use Superpowers subagent-driven development,
> tests first, bounded ownership and independent review. Checkboxes are the ledger.

**Goal:** Every graybox attack/defense combination resolves according to its
authored mask, on Task 05's timestamped combat timeline.

**Architecture:** Pure defense model and data in Domain; encounter coordination
in Application; timestamped test controls, primitives and explicit scene ownership
in Presentation. Root serializes all Unity and shared scene edits.

**Stack:** Unity 6000.6.0f1, existing Input System/uGUI/Test Framework; no new packages.

**Spec:** [Defense design](../specs/2026-09-29-task-06-defense-design.md).

## Constraints and ownership

- Preserve the corrected checkout and all uncommitted Task 05 work on its branch.
- No commit, push, purchase, image generation, service or dependency mutation.
- Domain worker: new defense values/model/tuning and pure rule/timing tests.
- View worker: new GrayboxEncounterView and optional widget helper only.
- Narrative worker: narrative-brief.md and asset-readiness.md only.
- Root: existing timeline/session/driver extensions, encounter coordinator,
  raw control capture, composition, authoring, integration tests and evidence.
- Reviewer: read-only whole feature review, independent of implementers.
- Files under 500 lines and focused methods; keep Domain/Application engine-free.

## Review focus

- Cross-type input ties must preserve arrival, with defense before impact.
- Shifted strike times after resume must not use stale cached parry windows.
- Raw control ownership must survive movement and suppress held contacts on resume.
- Charge expenditure and recovery must remain frozen and never refund on pause.
- Damage-induced death must be terminal at the same timestamp as later milestones.

## Execution

- [x] Inspect exact task scope, timing seams and asset pipeline.
- [x] Snapshot existing work and record design, defaults and narrative scope.
- [x] Write missing-API tests; observe actual Unity red before implementation.
- [x] Implement pure authored defense rules and exact resource/timing boundaries.
- [x] Extend one mixed queue and connect the application encounter.
- [x] Implement timestamped temporary controls, primitive view and saved scene.
- [x] Run all suites, render/inspect graybox and verify outcome independence.
- [x] Independent review, reproduce/fix findings and rerun affected checks.
- [x] Record final portable receipts, source preservation and README/title updates.

## Rulings

- User's explicit Begin Task 06 authorizes reversible implementation. Additional
  skill approval/commit gates do not override that instruction or prior scope.
- Keep current branch/checkout to preserve uncommitted Task 05; no worktree move.
- Use supported default-model agents because historical presets are unavailable.
- Prototype health and recovery tuning were unspecified; choose 100 HP, 250ms
  parry recovery and 200ms hit recovery. These remain editable, not final feel.
- Minimum post-dodge recovery is the 230-360ms portion of its existing action.
- Story changes are saved in project docs, not a memory database. Historical
  acceptance records and the previously generated image/prompt keep provenance.
- Image generation is useful later for concepts/icons/supporting textures; it is
  not necessary for Task 06 and cannot replace the approved rigged asset pipeline.
- Plugin Management finds no usable Unity/Blender plugin; Desktop Commander ping
  succeeded. Use installed Editor and package-source evidence for execution.

## Final verification

381/381 Edit Mode, 48/48 Play Mode, 12/12 Python checks. Six independent P2
findings fixed, final focused rereview clear. The saved portrait is graphics-backed
and pixel-checked for both combatants. No physical acceptance, commit, push or
asset generation. See the dated Task 06 acceptance report for portable receipts.
