# Task 09 test-first and repair record

All tests and API expectations preceded implementation; native Unity operated serially with source writers frozen. Fresh run receipts validate zero skips and complete suites.

| Gate | Native result before repair | Repair and final result |
|---|---|---|
| Missing task APIs | Native compile exit1 missing layout, explorer, HUD and pause APIs | Implemented API and behavior, final 669/669Edit and 75/75Play |
| Exploration overflow | 668Edit, 665pass, 3 fail for held gameplay/excluded/UI repeat Begin | Bounded 32-identity quarantine until explicit Reset, all cases pass |
| Calibration mirror and camera | 73Play, 71 pass, 2 fail | Mirror Apply/Cancel and use bound camera, both pass |
| Portrait settings | 669Edit, 668 pass, 1 fail, actual AutoRotation | Installed API confirms Portrait=0, other rotation directions disabled; passes |
| Native UI press cancellation | 75Play, 73 pass, 2 fail, stale Ability and Resume clicks | Paired fresh pointer buttons invalidated at contact cancellation; both pass |

The first presentation compile caught Application namespace collision and Color/Color32 conditional ambiguity. A later sprite cleanup helper reintroduced an unqualified Application; fixed with UnityEngine.Application. The first Play run (70/71) needed a warmed camera frame after opening the new menu before raycast assertion; final fixture explicitly renders it. These were repaired, not suppressed. No previous tests were removed.

Task-scoped reviews and one broad final review now pass SPEC and QUALITY. The full branch reviewer checked installed InputSystemUIInputModule release behavior and the native RED/GREEN pair. Root final nativeEdit confirms 669/669 after the UI fix. Physical same-hand acceptance remains UNRUN.
