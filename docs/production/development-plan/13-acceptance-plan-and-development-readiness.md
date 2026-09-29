# 13. Acceptance plan and development readiness

## 13.1 Required acceptance scenarios

### Combat and controls

- Equivalent recorded commands at 30, 60, and 120 FPS presentation.
- Input arriving exactly at an impact boundary.
- Swipe beginning on UI and ending over gameplay.
- Swipe crossing an encounter-phase transition.
- Multiple touch contacts with only one accepted gameplay pointer.
- Guard depletion, exhausted dodge charges, and interrupted recovery.
- Death coinciding with a phase threshold.
- Camera transitions with no hidden mandatory attack.
- Large-phone one-thumb reachability.
- Left-handed layout and accessibility assists.

### Persistence

- Termination before and after every reward commit.
- Interrupted equipment purchase or upgrade.
- Interrupted rebirth.
- Duplicate encounter-result delivery.
- Duplicate tier-clear claim.
- Corrupt newest save generation.
- Unsupported future schema.
- Missing equipment definition.
- Failed migration.
- Storage full.
- Application suspension during an active encounter.

### Content and performance

- Failed and canceled asset loads.
- Repeated enter/exit without growing memory.
- First-use shader and effect behavior.
- Thirty-minute thermal runs.
- Low-power mode.
- Low-memory warning.
- Offline startup and complete offline campaign.
- Device refresh-rate changes.
- Clean checkout and dependency restoration.
- Release build with no active automation endpoint.

## 13.2 Evidence standard

For each milestone, retain:

- Editor and package versions.
- Build identifier.
- Device and OS.
- Reproduction procedure.
- Results and logs.
- Frame-time and memory captures where relevant.
- Save fixtures.
- Short gameplay recordings.
- Open defects and their severity.

Compilation, automated checks, Editor play, and physical device acceptance are separate evidence categories.

## 13.3 Readiness conclusion

**The architecture is ready to guide development of the ergonomic prototype and representative boss pipeline.**

Before committing to the campaign schedule, establish:

1. Working Unity and Blender automation connections.
2. Access to the required physical device matrix.
3. A functioning Mac/Xcode signing path.
4. One approved skeleton and animation family.
5. Measured boss-authoring throughput.
6. Sustained mobile performance and reliable save transactions.

Research used official game and engine sources, Exa, Context7, local project manifests, and independent research/review checks. The named researcher and reviewer presets failed because of account/model compatibility; replacement agents completed those checks.

No implementation, package changes, builds, or gameplay tests were performed for this plan.

[Return to development plan](../development-plan.md)
