# Task 13 review and verification findings

Read-only integration review identified concrete admission failures; the root
agent resolved them before final native verification.

| Finding | Resolution | Regression evidence |
| --- | --- | --- |
| Root committed selection and disposed combat before replacement actor/graph validation | Prepare replacement resources first, commit only after unbinding, release uncommitted resources | Invalid catalog, legacy camera, legacy avatar, enemy motion, and uncommitted preparation tests; final Play Mode PASS |
| Temporary HideAndDontSave profile could leak on gesture/presentation failure | Convert gesture data before allocating the profile, destroy temporary profile on failed preparation, retain ownership after successful commit | Failure paths preserve active session/graphs; preparation disposal is covered |
| Observer derives `Cut` + direction, so arbitrary authored motion keys would be ignored | Both authoring and immutable attack constructors enforce exact current direction keys | ArbitraryMotionKeyCannotSilentlyOverrideTheCurrentDirectionResolver PASS |

The initial native authorer exposed a false root-motion validator: all controlled
Humanoid clips have root curves even with movement baked into bone pose. The
validator now checks exact imported clip bake settings and disabled Animator root
motion. The second native authoring run validated all 53 definitions. Unity's
[root-motion documentation](https://docs.unity.com/en-us/engine/6000.3/manual/animation-section/animation-mecanim/avatar-creationand-setup/root-motion)
describes how bake settings separate bone pose from the root trajectory.

Fourteen pilot failures in the first focused Edit Mode run led to fixture fixes:
trajectory/retarget fixtures use the production AlwaysAnimate policy off-screen;
rigidity checks inspect each vertex's actual RightHand influence rather than the
23-bone shared palette; silhouette checks compare rest mesh bounds rather than
LOD0's full animation culling envelope. No export, source pose, distance threshold,
or test identity was changed to make these assertions pass. Final native checks
prove actual imported head travel, follow-through, rigid weights, retargeting and
LOD silhouette consistency.

Final results: 841/841 Edit Mode, 155/155 Play Mode, 24/24 Python, zero failures or
skips. All 707/125 previous native test identities are preserved. The accepted
sword bundle and dependencies remain unchanged. Physical/final-art acceptance
stays UNRUN. No unresolved correctness finding remains in this work order.
