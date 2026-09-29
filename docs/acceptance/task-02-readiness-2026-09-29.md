# Task 02 acceptance: repository foundation

**Status: COMPLETE.** Verified on 2026-09-29 using Superpowers execution and independent review, native Windows Git/LFS, and the pinned Unity Editor. Task 03 has not started.

The canonical project uses the corrected outer directory. `Assets`, `Packages`, and `ProjectSettings` are directly beneath it:

```text
C:\Users\cld-main\Desktop\github-projects\infinity-redux
Unity 6000.6.0f1 (f7f8ed4d1e24)
```

## Delivered

- Initialized Git on an unborn `main` branch, with repository-local LFS filters and hooks. Nothing staged or committed; no remote configured.
- Added Unity ignore rules, binary asset LFS attributes, text attributes for Unity YAML and metadata, and the planned source/documentation folders.
- Preserved Force Text serialization and Visible Meta Files settings. Unity generated metadata for the new folders; 35 asset GUIDs validate.
- Added the [dependency policy](../production/dependency-policy.md), [asset provenance record](../production/asset-provenance.md), and [repository instructions](../../README.md).
- Added a read-only [baseline validator](../../tools/build/verify_baseline.py) and 12 regression tests. Checks cover exact direct pins, the complete resolved dependency graph, registry configuration, metadata, Git exclusions, and LFS attributes.

## Dependency result

The manifest contains **48 direct dependencies**, and Unity Package Manager generated a lockfile containing **65 resolved packages**. No retained package changed version.

| Change | Exact versions |
| --- | --- |
| Added direct dependencies | Addressables 2.11.2; Cinemachine 6.6.0 |
| Promoted existing dependency to direct | Newtonsoft JSON 3.2.2 |
| Removed direct dependencies | AI Assistant 2.20.0-pre.1; AI Inference 2.6.1; AI Navigation 2.0.14; Visual Scripting 1.9.12 |
| New transitive dependencies | Scriptable Build Pipeline 3.0.3; Settings Manager 2.1.1; Splines 2.9.0 |
| Removed unused transitive dependencies | 2D Sprite 1.0.0; App UI 2.1.11 |
| Deferred | Animation Rigging 6.6.0, until contact correction requires it |

Authored-reference inspection found no component or code dependency preventing the removals. Removed one stale App UI settings reference from `EditorBuildSettings.asset`; preserved scene and input settings and the inert AI Assistant settings file.

## Verification evidence

| Check | Result |
| --- | --- |
| Canonical Unity batch import and compilation | Exit 0 |
| Source-only restore into a directory without a project Library | Exit 0 |
| Manifest and lockfile after restore | Byte-identical to canonical source |
| Baseline validator | PASS on canonical and restored projects |
| Validator regression suite | 12 passed, 0 failed |
| Native Git/LFS attributes and exclusions | PASS |
| LFS clean/smudge round trip | 49 synthetic bytes reconstructed exactly |
| Independent review | One graph-drift validation gap fixed; four new cases reproduced the gap before the fix; targeted re-review clear |

The restore copied only `Assets`, `Packages`, and `ProjectSettings`. Machine-wide package caches could be reused. This establishes restoration with a fresh project Library, not a virgin-machine installation or a Git clone of a committed baseline.

Pipeline auto-start was disabled temporarily during batch verification. Its temporary canonical configuration was moved into ignored evidence storage afterward, restoring its original absence. No listener was observed in Pipeline's port range during verification. Both batch Editor processes exited; the Unity Hub helper remains running.

Both successful batch logs contain nonfatal startup diagnostics: an unavailable licensing refresh access token and a VisionOS native DLL load failure on Windows. No C# compiler errors were found. VisionOS is outside the approved launch targets.

## Preservation and limits

Of 132 preexisting source files, 129 remain byte-identical. The only changed originals are the manifest, Unity-generated lockfile, and the stale-reference correction in `EditorBuildSettings.asset`. **No original source files were removed. All 62 alternate-project files remain unchanged.** The alternate `My project` directory is excluded from Git.

Play Mode, player builds, and physical-device acceptance remain **UNRUN**. Release endpoint exclusion remains **UNVERIFIED**. This task establishes repository and dependency readiness only.

Use native Windows Git for LFS on this workstation; WSL Git has no usable LFS command. No global Git configuration was changed. No commit, push, purchase, or deployment was performed.

## Reproduce and inspect

Run from the canonical repository root:

```bash
python3 tools/build/verify_baseline.py
python3 -m unittest discover -s tools/build -p 'test_*.py' -v
```

The [machine-readable receipt](task-02-evidence-2026-09-29.json) records versions, hashes, package changes, and preservation results. Local raw evidence is intentionally ignored by Git: [canonical Unity log](../../output/task-02/canonical-import.log), [restore log](../../output/task-02/clean-restore.log), [restore comparison](../../output/task-02/restore-comparison.json), [regression results](../../output/task-02/validator-tests.log), and [LFS round trip](../../output/task-02/lfs-roundtrip.json).
