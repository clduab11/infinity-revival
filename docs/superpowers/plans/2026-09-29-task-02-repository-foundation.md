# Task 02 Repository Foundation Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans for execution and superpowers:dispatching-parallel-agents for independent inventory/scaffolding. Steps use checkboxes as the persistent execution ledger.

**Goal:** Establish the corrected root as a reproducible Unity repository with the approved package baseline.

**Architecture:** Assets, Packages, and ProjectSettings live directly at the workspace root. Native Windows Git manages local LFS; Unity owns dependency resolution and lockfile generation. A small read-only Python validator checks the baseline.

**Tech Stack:** Unity 6000.6.0f1 (f7f8ed4d1e24), Windows Git 2.55.0, Git LFS 3.7.1, Python 3 standard library.

**Spec:** User-supplied development plan, sections 5.2, 6.2 and task 02; the user's subsequent path correction supersedes the original nested layout. Source: /mnt/c/Users/cld-main/.codex/attachments/e927b2d0-6749-47d2-a7dd-eb2feda74ddc/Pasted text.txt.

## Global constraints

- Work at C:\Users\cld-main\Desktop\github-projects\infinity-redux.
- Preserve Unity 6000.6.0f1 and revision f7f8ed4d1e24.
- Preserve the alternate My project directory and all unrelated existing files.
- No commits, pushes, purchases, publishing, gameplay, or service starts.
- Add Addressables 2.11.2, Cinemachine 6.6.0 and direct Newtonsoft JSON 3.2.2.
- Remove AI Assistant, AI Inference, AI Navigation and Visual Scripting only after authored-reference inspection.
- Defer Animation Rigging 6.6.0 until contact correction is required.
- Keep Pipeline 0.8.0-exp.1 for development; batch verification must not start its server.
- Unity regenerates packages-lock.json. No hand-authored resolved dependency graph.
- Compilation and dependency restoration do not establish gameplay or device acceptance.

## Review focus

- Generated artifacts, alternate project, credentials and backups cannot enter Git accidentally.
- Unity YAML and metadata remain text; binary art receives LFS filters.
- Asset GUID references into removed packages do not survive unnoticed.
- A fresh project-local Library restores the same locked package graph.
- Editor-generated file changes are distinguished from intentional changes and unrelated-file removal.

## Execution

- [x] Inventory exact versions, project state, process state, existing settings and native Git/LFS.
- [x] Initialize an unborn main branch at the user-selected root; install LFS hooks/filters locally. Do not stage or commit.
- [x] Create Unity ignore/LFS rules, documentation and planned empty folders.
- [x] Write the baseline validator and observe it reject the unapplied package baseline.
- [x] Verify no authored references prevent the four package removals. Preserve removed-package settings as backup if cleanup is necessary.
- [x] Apply manifest changes and create Unity folder metadata without changing existing GUIDs.
- [x] Run canonical Unity batch import/compile with temporary Pipeline auto-start disabled.
- [x] Verify Unity-generated lock versions and structural baseline.
- [x] Restore a source-only verification copy with no project Library; verify identical manifest/lock and successful compilation.
- [x] Run negative validator cases, Git ignore/LFS checks and preservation comparisons.
- [x] Independent review, fix material findings, write task-02 acceptance receipt.

## Files and ownership

- Scaffold agent: .gitignore, .gitattributes, README.md, dependency/provenance policy, non-Unity placeholders.
- Root: Packages/manifest.json, Unity-resolved Packages/packages-lock.json, Assets/Game and Assets/ThirdParty folders/metas, tools/build validator and baseline JSON.
- Root: docs/acceptance/task-02-readiness-2026-09-29.md and machine-readable receipt.
- Existing audit files and alternate project stay historical and unchanged.

## Verification commands

```bash
python3 tools/build/verify_baseline.py
python3 tools/build/verify_baseline.py --project-root output/task-02/restore-project --skip-git
```

Use native Windows Git for LFS checks. Run the pinned Unity.exe in batch mode with the explicit project path and per-run log file; check its exit code, package-resolution evidence and compiler output.

## Workflow rulings

- This is an approved setup work order, so execute it without another design approval cycle.
- Root initialization is the requested outcome; a separate worktree cannot represent an unborn repository without creating an unauthorized initial commit.
- No gameplay test code is needed for repository configuration. The executable baseline validator supplies a failing pre-change check and negative cases; actual Unity imports validate restoration.
- WSL lacks a usable Git LFS executable. Use the installed Windows Git/LFS and record that prerequisite instead of changing the machine-wide toolchain.


## Completion receipt

Task 02 completed. Both Unity imports exited 0, the source-only restore retained identical manifest/lock bytes, and all 12 verifier tests passed. Independent review identified one validator gap, corrected and re-reviewed. No files were staged or committed. See docs/acceptance/task-02-readiness-2026-09-29.md for evidence and remaining acceptance limits.
