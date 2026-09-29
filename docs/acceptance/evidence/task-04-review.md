# Task 04 independent review

Reviewer: separate default agent task04_independent_review. Read-only source review against Task04 design and installed Unity Input System source. Root owned all Unity runs and fixes.

## Initial actionable findings, P2

1. Invalid editable tuning published InputCapture before validation, leaving a partial composition that could never retry. InputCompositionTests reproduced it. Validate tuning before creating/publishing capture.
2. Removing a held touchscreen delivered no finger terminal callback and permanently reserved its gameplay slot. RemovedTouchscreenReleasesOwnershipForNewDevice reproduced it. Track immutable contact samples and synthesize terminal cancellation only for the removed device.
3. An inactive Begin reserved the gameplay slot, excluding a fresh active contact. InactiveHeldContactDoesNotExcludeAFreshActiveGameplayContact reproduced it. Reserve the slot only for contacts begun in an active phase; inactive contacts remain blocked themselves.
4. InputSystem.settings replacement re-enabled merging and left the previous settings object mutated. SettingsReplacementPreservesSamplesAndRestoresBothObjects reproduced it. Observe settings changes, preserve sample fidelity, track prior flags on every settings object, restore surviving objects after last lease.

## Additional verified corrections

- Trace geometry retained its old absolute coordinates when the canvas resized. TraceRemapsNormalizedEndpointsWhenItsSurfaceResizes failed before retaining normalized endpoints and redrawing geometry on dimension changes.
- EnhancedTouch rebuilds held histories with current-time records on updateMode changes. UpdateModeChangesCancelHeldContactWithoutInventingSamples reproduced six recorded samples from two input events after two mode changes. Capture ignores rebuild callbacks until its subsequent settings observer records the new mode and cancels held recognition.
- UI fixture depth remained -1 before initial rendering. Converted the two crossing tests to wait for rendering and explicitly assert actual raycast ownership; resolver logic was unchanged.

## Scoped re-review result

No remaining actionable finding in the four fixes, trace resizing, or update-mode callback suppression. Settings restoration and callback ordering match installed Input System source. Device removal preserves other held contacts, inactive contacts no longer block eligible Begins, and trace resizing preserves expiry. Review was static only; root's fresh compiled suites establish automated acceptance.
