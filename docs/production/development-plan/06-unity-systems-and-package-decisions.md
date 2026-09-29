# 6. Unity systems and package decisions

## 6.1 Rendering: URP

**Use URP for every platform.** Unity’s 6000.6 matrix supports iOS and Android through URP and lists HDRP as unsupported on those platforms. One pipeline also keeps mobile and Steam content compatible. [Unity 6.6 pipeline matrix](https://docs.unity3d.com/6000.6/Documentation/Manual/render-pipelines-feature-comparison.html)

Use the URP Forward renderer with RenderGraph enabled. Begin with simple renderer features and add effects only against measured budgets.

## 6.2 Package baseline

| Package | Version or decision |
|---|---|
| Unity Editor | **6000.6.0f1** |
| URP | **17.6.0**, installed |
| Input System | **1.20.0**, installed |
| uGUI | **2.6.0**, installed |
| Timeline | **6.6.0**, installed |
| Cinemachine | **6.6.0**, verified in installed Editor |
| Animation Rigging | **6.6.0**, verified in installed Editor; add when contact correction is needed |
| Addressables | **2.11.2**, add |
| Test Framework | **1.8.0**, installed |
| Unity Newtonsoft JSON | **3.2.2**, already resolved; promote to direct dependency for persistence |
| Adaptive Performance | Installed core module; use supported platform capability adapters |
| AI Navigation | Remove direct dependency after confirming no authored content uses it |
| Visual Scripting | Remove direct dependency |
| AI Assistant / inference | Remove from the game’s required package set unless a concrete development dependency is demonstrated |
| Unity Pipeline | Installed **0.8.0-exp.1**; development automation only, with release exclusion checks |

Unity 6.6 makes Cinemachine, Timeline, and Animation Rigging editor-matched core packages. Do not apply older Cinemachine 3.x installation guidance to this baseline. [Unity 6.6 changes](https://docs.unity3d.com/6000.6/Documentation/Manual/WhatsNewUnity66.html)

All dependency changes update the manifest and lockfile together. Package upgrades occur in dedicated compatibility changes.

## 6.3 Addressables

Use **Addressables 2.11.2 with AssetBundles**.

Unity’s new content-directory workflow is local-only in the reviewed documentation. AssetBundles preserve the required remote-content and content-update path. [Unity 6.6 Addressables versions](https://docs.unity3d.com/6000.6/Documentation/Manual/com.unity.addressables.html), [Content build systems](https://docs.unity3d.com/Packages/com.unity.addressables@4.0/manual/content-build-systems.html)

Loading scopes:

1. Bootstrap and shared UI.
2. Refuge.
3. Current region.
4. Current encounter.

Each scope owns its handles and releases them explicitly. Shared dependencies belong in deliberate shared groups.

Launch ships the complete campaign locally. Future downloadable content uses:

- A compatible player-version range.
- Stable content IDs.
- Versioned catalogs.
- Preserved content-state build artifacts.
- Updates applied at a safe menu boundary.
- Recovery from failed or interrupted downloads.

Catalog updates never replace resources during an active encounter.

## 6.4 Animation and camera

Use:

- Humanoid retargeting for the shared skeleton family.
- Animator layers for locomotion-independent poses and reactions.
- Playables where explicit clip-time control is required.
- Animation Rigging for limited hand, shield, and weapon contact correction.
- Cinemachine for exploration, introductions, gameplay framing, and finishers.
- Timeline for skippable noninteractive sequences.

Combat animation is driven by the combat timeline. Animation events may trigger optional audiovisual accents, but cannot own damage, rewards, or progression.

Gameplay movement uses encounter anchors and authored offsets. Root motion must not drift combatants outside the validated contact envelope.

## 6.5 UI

Use **uGUI for all runtime UI** and **UI Toolkit for Editor authoring tools**.

Runtime screens:

- Bootstrap/recovery.
- Main menu.
- Refuge.
- Equipment comparison and loadout.
- Upgrade/mastery.
- Route selection.
- Combat HUD.
- Rewards.
- Defeat.
- Rebirth.
- Settings/accessibility.
- Credits and licenses.

Use separate canvases for frequently changing HUD elements and mostly static screens. Pool repeating inventory entries.

All player-facing strings use localization keys from the first UI task. Launch language is English; additional languages remain content work rather than a UI rewrite.

## 6.6 Audio and feedback

Use Unity AudioMixer with separate music, effects, UI, and ambience groups.

- Short combat sounds are preloaded.
- Long music and ambience use streaming where appropriate.
- Limit simultaneous voices.
- Pool common effects.
- Provide haptic intensity controls.
- Make every required tell readable with audio muted.
- Keep camera shake and flash intensity independently adjustable.

---


[Return to development plan](../development-plan.md)
