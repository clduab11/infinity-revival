# 9. Mobile performance and platform delivery

## 9.1 Qualification targets

Use a device matrix rather than a single flagship demonstration.

Initial qualification candidates:

- A15-class iPhone as the lower iPhone performance target.
- A 120 Hz Pro iPhone.
- M1-class iPad.
- Snapdragon 8 Gen 2-class Android.
- A separate Android GPU family for driver coverage.
- A representative Windows gaming PC.

Actual owned or accessible devices are recorded during preflight. Device support is claimed only after measurement.

Proposed minimum OS targets:

- iOS/iPadOS 17.
- Android 12, ARM64.
- Windows 10/11, x64.

These are product targets, distinct from engine compatibility floors and store SDK requirements.

## 9.2 Initial budgets

All values below are **engineering targets requiring device validation**.

| Metric | 60 FPS mode | Optional 120 FPS mode |
|---|---:|---:|
| Frame interval | 16.67 ms | 8.33 ms |
| Typical GPU budget | ≤12 ms | ≤6 ms |
| Typical main-thread budget | ≤8 ms | ≤4 ms |
| Continuous combat allocations | 0 B/frame attributable to game code | Same |
| Recognition-to-visible-response target | ≤50 ms at p95 | ≤35 ms at p95 |
| Sustained qualification | 30-minute encounter loop | Same |
| Initial resident-memory target on lower tier | ≤1.2 GB | Same |
| Peak transition-memory target | ≤1.5 GB | Same |

CPU and GPU budgets overlap; they are not added together.

Memory measurement must use platform tools and include native allocations, graphics resources, and transition peaks. These values are not OS termination guarantees.

## 9.3 Rendering strategy

Start with:

- Baked environment lighting.
- One main shadow-casting light.
- Short shadow distance.
- Limited cascades.
- Baked or inexpensive additional lights.
- Reflection probes.
- SRP Batcher.
- Restrained transparent effects.
- Compressed textures with mipmaps.
- Pooled combat effects.
- Aggressive shader-variant stripping.
- Dynamic resolution through quality profiles.
- No gameplay depth of field or motion blur.

Initial asset guidelines:

| Asset | Starting budget |
|---|---|
| Player or boss LOD0 | Approximately 50–80k triangles |
| Regular enemy LOD0 | Approximately 30–50k triangles |
| Visible scene geometry | Approximately 400–600k triangles |
| Character materials | Prefer 2–3 |
| Character texture sets | Usually 2K |
| Small equipment | Usually 512–1K |
| Maximum texture | 4K only with measured justification |
| Skinning | Prefer four influences per vertex |
| Simulated cloth | Excluded from required launch presentation |

These budgets may tighten after the first representative boss.

## 9.4 Battery and thermal policy

Default to 60 FPS.

Offer 120 FPS only when:

- The display supports it.
- The device has passed qualification.
- The selected quality profile fits the budget.
- Thermal and power conditions permit it.

Use hysteresis when changing quality. Avoid oscillating between modes.

Lower rendering cost before reducing combat readability:

1. Resolution scale.
2. Shadows.
3. Secondary effects.
4. Background detail.
5. Frame-rate fallback.

Menus can use a lower frame rate. Backgrounding stops gameplay rendering and simulation.

Unity mobile frame pacing uses `Application.targetFrameRate`; mobile ignores `vSyncCount`. iOS high-refresh operation requires ProMotion support to be enabled. Desktop needs a separate pacing implementation. [Unity frame-rate behavior](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Application-targetFrameRate.html)

## 9.5 Platform toolchains

- Use Hub-provided Android SDK 36, NDK r27c, and OpenJDK 17.
- Use Metal for Apple platforms.
- Use Vulkan for the qualified Android device set.
- Use Direct3D 11 as the initial Windows baseline.
- Use IL2CPP release builds and validate stripping against serialization and package requirements.

As of the research date, Apple submissions require Xcode 26 or later with the iOS/iPadOS 26 SDK or later. Google Play requires target API 36 or higher. Recheck both at Beta and submission. [Apple requirements](https://developer.apple.com/news/upcoming-requirements/?id=04282026a), [Google Play requirements](https://support.google.com/googleplay/android-developer/answer/11926878?hl=en)

A working Mac/Xcode signing path is a required iOS production dependency.

## 9.6 Exact engine risks

Track these Unity 6000.6.0f1 issues:

- **UUM-149540:** slow `Resources.UnloadUnusedAssets()` in Player builds.
- **UUM-149781:** D3D12 crash involving shader warmup followed by shader unloading.

Use explicit asset ownership, avoid blanket unload calls during interactive transitions, and keep warmed shaders alive for their content lifetime. [6000.6.0f1 release notes](https://unity.com/releases/editor/whats-new/6000.6.0f1)

The editor pin remains exact. A blocking engine or submission issue requires an explicit upgrade decision and compatibility review.

---


[Return to development plan](../development-plan.md)
