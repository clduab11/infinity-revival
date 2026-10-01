# Saved-source checkpoint and continuation handoff

**Recorded:** 2026-10-01. **Saved-source verification:** PASS.
**Publication:** pending the requested push and independent remote verification.

This checkpoint preserves the canonical project through Task 13, its original
art sources, imports, provenance, concept image, portable acceptance evidence,
and complete technical/art continuation prompts. It is not a campaign release
or physical/visual acceptance milestone.

## Identity

- Repository: `https://github.com/clduab11/infinity-revival`.
- Canonical project: `C:\Users\cld-main\Desktop\github-projects\infinity-redux`.
- WSL path: `/mnt/c/Users/cld-main/Desktop/github-projects/infinity-redux`.
- Pre-checkpoint local and freshly fetched `origin/main`:
  `9ced64410bf9d5a6dd164d28455fb25ac2faf902`, the Task 08 publication.
- Unity: `6000.6.0f1 (f7f8ed4d1e24)`; URP: `17.6.0`.
- Approved baseline: 50 direct/69 resolved packages, including Unity AI Assistant
  `2.20.0-pre.1` and Inference `2.6.1`; no dependency change in this publication.

The current publication carries previously approved local package changes since
Task 08. Their lockfile and baseline are included; this checkpoint does not add
another package or authorize generation spending.

## Fresh verification

| Check | Result | Evidence |
| --- | --- | --- |
| Native Edit Mode | 841/841, zero failures/skips | [XML](../acceptance/evidence/checkpoint-2026-10-01-editmode.xml) |
| Native Play Mode | 155/155, zero failures/skips | [XML](../acceptance/evidence/checkpoint-2026-10-01-playmode.xml) |
| Existing Python verifier tests | 12/12 | Fresh native-workstation run |
| Family motion Python tests | 12/12 | Fresh native-workstation run |
| Package/metadata verifier | PASS, 295 GUIDs | 50 direct/69 resolved packages |
| Source-motion and tone checks | PASS | Three recipe `--check` commands |
| Production source guard | 621 files unchanged since test snapshot | SHA-256 comparison |

The full native suites ran in an isolated saved-source Unity copy with graphics
enabled. The operator's canonical Editor and dirty Blender session were preserved.
The [machine-readable receipt](../acceptance/repository-checkpoint-2026-10-01.json)
records exact times and portable XML hashes. Historical task receipts are retained,
including intentional red-test evidence; do not mistake those files for new failures.
Git attributes explicitly describe Unity's generated empty YAML fields and verbatim
Windows receipt formatting. This keeps source/provenance and evidence bytes intact;
ordinary handwritten code and documentation retain normal whitespace checks.

## What is deliberately outside GitHub

- Open Blender `SourceArt/Characters/PrototypePair.blend` had `is_dirty=true`.
  The saved disk file is included; unsaved in-memory edits are not represented by
  a Git commit and were neither overwritten nor silently saved.
- `My project/` and the embedded `infinity-game/` are unrelated local Unity
  projects, preserved and excluded. Do not integrate their source or caches.
- Library, Logs, Temp, UserSettings, local output, ignored Blender backups and
  `.superpowers` scratch state remain local. Required source and portable
  evidence have versioned locations; historical backups are not clone dependencies.
- Tool credentials, installed programs, MCP connections, generator entitlements,
  and actual devices are instance capabilities, not repository payloads.

Task 09/10 phone qualification and final character/contact/deformation/art review
remain **UNRUN**. No Asset Store purchases or Unity/Venice generation spend took
place. The current concept is a reference image, not an in-game quality claim.

## Full standalone prompts

- [GPT-6-Astra Ultra: technical bootstrap](2026-10-01-technical-bootstrap.md).
- [GPT-6.1-Sol Ultra: art bootstrap](2026-10-01-art-bootstrap.md).
- [Shared recursive audit protocol](repository-audit-protocol.md).
- [Shared ownership and integration contract](parallel-development-contract.md).
- [Checkpoint source manifest](checkpoint-source-manifest.json).

Set the actual model and Ultra effort in each conversation's UI, then paste the
entire corresponding bootstrap file as the first user message. The prompts are
complete operator instructions, not abbreviated summaries. They require fresh
local/GitHub tree, LFS payload, metadata, source and capability reconciliation,
a gap table, and a durable track-specific audit receipt before work continues.

Technical begins Task 14 loading scopes/leases. Art establishes VisualBenchmark01
under its new owned source and preview paths, with actual Unity combat proof.
The benchmark does not complete Task 24 boss/reward/route work. Production art
integration is a separate bounded technical assignment after the art handoff.

Separate working copies and a single owner per shared Unity asset are required
for concurrent authoring. The repository is the durable source of decisions.
New conversations must verify their own capabilities and cannot assume that this
conversation's tools, temporary workspaces, authorization or unsaved state transfer.
