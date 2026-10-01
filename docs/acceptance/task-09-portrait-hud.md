# Task 09 acceptance: portrait HUD and input ownership

**Implementation verified by automated checks, 2026-09-29. Physical exit: UNRUN.**
The portrait HUD is implemented in the canonical Forever We Reign graybox.
Task 09 cannot claim its physical exit until an operator can hold and perform
all required actions on a phone using the same hand. No phone is connected;
no ADB listener or associated services were started. Native Editor evidence
establishes automated behavior and layout only.

## Delivered scope

Mobile settings select portrait orientation; the desktop Editor keeps a portrait
HUD wrapper when its Game view is wider. This is configuration evidence, not a
mobile player build.

- Right thumb is the default. Left-hand mode mirrors control placement while
  preserving the meanings of left and right dodge. HUD honors the supplied
  safe area, separates controls from tells, and supports scale 0.85 through 1.20.
  These bounds are prototype tuning, not validated ergonomic limits.
- Guard hold, separate dodge taps, ability intent, pause, phase-owned cardinal
  swipes, resource readouts, direction traces and countdown are presented in
  a dark stone, bone, brass and restrained teal treatment. New visible copy
  uses localization keys with English fallback. Dynamic HUD and menu use
  separate canvases; placeholder geometry and procedural icons require no art batch.
- Reach calibration suspends combat, clears held guard and incomplete gestures,
  and accepts three comfortable thumb taps in the lower 45% of the safe area.
  Drags and second contacts are rejected. Averaged offsets are bounded to
  plus/minus 0.08 safe width and 0.06 safe height. Apply or cancel returns to
  the paused menu. Hand/layout changes cancel contacts.
- Explicit pause survives focus loss and return. Only explicit resume leaves
  user pause, followed by a three-second countdown. Restart creates a fresh
  encounter. Settings are session-local; persistence belongs to Task 15.
- Graybox exploration preview ends combat and establishes a fresh Exploration
  phase. A tap with travel at most 0.025 of the shorter screen dimension selects
  a bounded hotspot. Longer movement becomes inspection only, with yaw bounded
  to plus/minus 12 degrees and pitch to plus/minus 8 degrees. UI ownership is
  latched at Begin; a second contact is excluded until lift. Mode and lifecycle
  transitions reset contacts. This preview does not implement Task 21 traversal,
  campaign rewards, or checkpoints. Bootstrap retains its minimal Refuge.
- The ability control emits a gated intent only. Ability effects, resource
  spending, loadout and progression remain Task 22. Task 10 qualification,
  asset generation and production art approval are outside this work order.

Authority: [Task 09 specification](../plans/2026-09-29-task-09-portrait-hud-spec.md),
[implementation plan](../plans/2026-09-29-task-09-portrait-hud-plan.md), and
[development sequence](../production/development-plan/12-codex-development-sequence.md).

## Automated evidence

Final native Unity **6000.6.0f1, revision f7f8ed4d1e24** results are
**669/669 Edit Mode**, **75/75 graphics-backed Play Mode**, and **12/12 Python**
validator tests, with zero failures or skips. All **632 Edit Mode and 60 Play Mode
Task 08 test identities** are preserved; Task 09 adds 37 and 15 cases respectively.
The final native runs occurred September 30 UTC, still September 29 in Chicago.

| Evidence | Status |
| --- | --- |
| [Edit Mode receipt](evidence/task-09-editmode.xml) | PASS, 669/669 |
| [Play Mode receipt](evidence/task-09-playmode.xml) | PASS, 75/75 |
| [Structural validator](evidence/task-09-baseline.json) and [Python suite](evidence/task-09-python.log) | PASS, 48 direct/65 resolved packages, 155 unique GUIDs; 12/12 Python |
| [Source preservation](evidence/task-09-source-preservation.json) | PASS, 463/473 preexisting files unchanged; 10 intended changes; 62 alternate-project sources preserved |
| [Task reviews](evidence/task-09-domain-review.md), [UI review](evidence/task-09-ui-review.md) and [whole-branch review](evidence/task-09-final-review.md) | SPEC PASS, QUALITY PASS; all findings resolved |
| Physical same-hand hold-and-operate exit | UNRUN |
| Built-player platform/OS qualification | UNRUN |
| Task 10 latency, recognition, readability and frame-time qualification | UNRUN, future work |

[Test-first and repair evidence](evidence/task-09-test-first.md) records native
RED/GREEN regressions for overflow, mirroring, bound-camera use, portrait settings,
and stale native button presses. A button press invalidated by focus or layout
cannot emit a click on release; a fresh press is required. Existing scenes, meta
GUIDs, package pins, historical PNGs and pipeline-control bytes are unchanged.
The historical Task 08 bundle retains 48/51 hashes; its root-composition source,
README and asset-readiness document are the three intended Task 09 updates.

[Machine-readable receipt](task-09-evidence-2026-09-29.json) lists exact hashes,
source changes and gate status. Code is local on branch task-09-portrait-hud,
based on 9ced64410bf9d5a6dd164d28455fb25ac2faf902. No Task 09 commit or push was
performed. Earlier scope and evidence remain in the [Task 08 report](task-08-enemy-patterns.md).

## Reference profiles and native captures

| Profile | Reference pixel dimensions | OS qualification |
| --- | --- | --- |
| iPhone 17 Pro Max | 1320 x 2868 | User target iOS 27.0.1; installed device OS and build UNVERIFIED |
| Galaxy S26 Ultra | 1440 x 3120 | Actual installed Android OS and build pending physical inventory |
| Smaller portrait preview | 720 x 1280 | Editor layout scenario only |

[Apple hardware specification](https://support.apple.com/en-hk/125091) and
[Samsung hardware announcement](https://news.samsung.com/global/samsung-unveils-galaxy-s26-series-the-most-intuitive-galaxy-ai-phone-yet)
support the hardware profiles. These pages do not establish the tested OS,
a successful player build, installed behavior, or input acceptance.

Capture safe rectangles use bottom-left origin in pixels and are **SYNTHETIC**:

| Scenario | Safe rectangle (x, y, width, height) |
| --- | --- |
| iPhone reference | (0, 100, 1320, 2600) |
| Android reference | (0, 80, 1440, 2960) |
| 720 preview | (0, 24, 720, 1200) |

These are test inputs, not measured physical-device insets. Native Unity Editor
captures show a placeholder graybox rather than finished characters or runtime
art. These are the final root-operated native capture files:

- [iPhone right hand](evidence/task-09-iphone17-Right.png)
- [iPhone left hand](evidence/task-09-iphone17-Left.png)
- [Android right hand](evidence/task-09-android-Right.png)
- [Android left hand](evidence/task-09-android-Left.png)
- [Pause menu](evidence/task-09-pause-menu.png)
- [Reach calibration](evidence/task-09-reach-calibration.png)
- [Exploration preview](evidence/task-09-exploration-preview.png)

## Physical same-hand operator checklist

For a device preview, use a local Android or iOS Build Profile whose scene-list
override starts with GrayboxEncounter. The canonical list still starts with the
minimal Bootstrap Refuge, which has no duel navigation yet. Record the profile
and scene GUID with the build evidence. Android player build/deployment and
Mac/Xcode/Apple signing are not qualified in this task.

Run on both reference phone classes, with each hand and the operator's normal
secure grip. Hold and operate using that same hand throughout required actions.
Record unsupported configurations or grip changes as findings, not passes.
Repeat layout inspection at scales 0.85, 1.00 and 1.20, then exercise the full
sequence at the operator's chosen scale. Retain a run record for each device,
hand and scale tested. Do not infer one phone's result for the other.

1. Record installed OS/build, player build identity, orientation, measured
   Screen.safeArea and display dimensions. Confirm every control remains inside
   measured safe bounds and tells/readouts stay legible with the thumb present.
2. From a secure normal grip, hold guard through an enemy tell and impact,
   release cleanly, and tap both dodge controls. Verify semantic dodge sides in
   both layouts. Record every reposition, missed activation and unintended touch.
3. Perform all four cardinal parries and all four attack directions in their
   permitted phases. Confirm the lower gesture region is reachable and a swipe
   starting on UI cannot become an arena command. Record wrong-direction or
   unintended actions rather than attributing them to operator error silently.
4. Tap ability intent in its permitted state. Confirm no ability effect or
   resource spending is implied. Confirm pause is reachable with the same hand.
5. Open calibration. Confirm guard releases and combat stops. Make three
   comfortable taps, apply, and inspect placement. Repeat cancel, drag rejection,
   second-contact rejection and mirrored-hand calibration. Confirm bounded
   offsets and no control overlap. Record rejected comfortable taps explicitly.
6. Adjust scale and handedness while a contact is active. Confirm cancellation
   without latent guard, dodge, attack or ability action after the layout changes.
7. Pause explicitly, background and return to the app, then wait. Confirm it
   stays paused. Resume explicitly and verify the full three-second warning
   before combat continues. Repeat warm resume from each relevant menu state;
   record unintended touches or actions during and immediately after countdown.
8. Open exploration preview, tap each bounded destination and drag to both
   inspection limits. Confirm drags do not select destinations, UI contacts do
   not enter exploration, and a second finger does not steal ownership. Return
   to duel and confirm a fresh encounter without stale input or camera state.
9. Restart and repeat a complete defense-to-opening cycle. Check portrait
   safe-area changes and interruption recovery on the physical device. Record
   occlusion, reach, stability and unwanted touches for every required action.
10. Attach observations and evidence. Mark PASS only when all required actions
    are completed with the same hand and secure grip, without unresolved
    unwanted-action or reach failures. A failed item blocks the Task 09 exit.

Copy this record for each physical run:

| Field | Recorded value |
| --- | --- |
| Device/model | UNRUN |
| Installed OS/version/build | UNRUN |
| Player build/commit or source identifier | UNRUN |
| Operator/date | UNRUN |
| Hand and scale | UNRUN |
| Grip, case and any required reposition | UNRUN |
| Display dimensions and measured safe area | UNRUN |
| Guard, both dodges, four parries, four attacks | UNRUN |
| Ability intent and reachable pause | UNRUN |
| Calibration apply/cancel and rejection cases | UNRUN |
| Hand/scale changes and contact cancellation | UNRUN |
| Explicit pause, background return and warm resume | UNRUN |
| Exploration tap, drag, ownership and return | UNRUN |
| Unwanted touches/actions and missed activations | UNRUN |
| Evidence files and unresolved findings | UNRUN |
| Outcome and acceptance owner | UNRUN |

Task 09 physical reach and ownership acceptance is distinct from Task 10's
full input-latency, recognition, readability and sustained frame-time campaign.
No production-asset expansion or device-support claim follows from Editor captures.
