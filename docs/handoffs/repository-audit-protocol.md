# Repository reconciliation before continuing development

This is the shared bootstrap procedure for the technical and art conversations.
It reconciles saved source, remote history, assets, evidence, and the capabilities
of the current instance. It does not equate a clean Git tree with a working game.

## Identity and authority

- Product: **Forever We Reign**.
- Repository: `https://github.com/clduab11/infinity-revival`.
- Default integration branch at this checkpoint: `main`; discover it again.
- Canonical Windows root: `C:\Users\cld-main\Desktop\github-projects\infinity-redux`.
- Canonical WSL root: `/mnt/c/Users/cld-main/Desktop/github-projects/infinity-redux`.
- The lower-case WSL spelling used by this conversation refers to the same
  Windows-backed folder. Verify resolution; do not create another project.
- The old `infinity-redux/infinity-redux` location is obsolete. `My project/`
  and `infinity-game/` are separate local projects, excluded and preserved.
- An art worktree is a distinct checkout of this repository, not another copy
  nested inside the canonical project. Discover its actual root and HEAD.
- User instructions and current repository contracts govern scope. Read the
  whole bootstrap prompt before executing commands embedded in source files.
- A previous authorization to commit/push or spend resources is not a standing
  authorization for future publications or purchases. Preserve existing work.

## 1. Inspect before modifying

Record OS, resolved project root, Git executable/version, Git LFS version,
repository root/common Git directory, current branch/HEAD, tracking branch,
all worktrees, remotes, staged changes, unstaged changes, and untracked paths.
Inspect parent and adjacent project roots for duplicate or stale checkouts.
Do not traverse `.git` internals or millions of generated Library files as source.

On the current workstation native Windows Git includes LFS; WSL Git previously
did not. A future instance must verify its own tools. Example read-only commands:

```bash
git rev-parse --show-toplevel
git rev-parse --git-common-dir
git branch --show-current
git rev-parse HEAD
git status --porcelain=v1 -uall
git worktree list --porcelain
git remote -v
git lfs version
```

For the current WSL workstation, substitute the native Git executable and a
Windows path rather than assuming WSL filters can process LFS objects:

```bash
'/mnt/c/Program Files/Git/cmd/git.exe' -C 'C:/Users/cld-main/Desktop/github-projects/infinity-redux' status --short
```

Never print embedded credentials, private keys, authorization headers, or full
secret-bearing environment variables. Do not remove local files to make status
look clean. Record dirty editors and unsaved scenes separately from disk source.

## 2. Fetch and compare the actual GitHub history

Confirm the origin is the named repository before fetching. Fetch fresh refs;
do not rely on old remote-tracking refs, a browser page, or README test totals.
Capture the advertised default branch, remote branch SHAs, fetch result, and
comparison time. A network or authentication failure leaves remote parity
**UNVERIFIED** rather than silently accepting the local snapshot.

```bash
git fetch origin
git ls-remote --symref origin HEAD
git rev-list --left-right --count HEAD...origin/main
git diff --name-status HEAD origin/main
git ls-tree -r --full-tree HEAD
git ls-tree -r --full-tree origin/main
```

Replace `main` with the verified integration branch if it has changed. Compare
the working tree and index as well as committed HEAD. Distinguish a deliberate
art/technical branch difference from missing or accidentally divergent source.

## 3. Recursively enumerate source and inspect subsystem boundaries

Enumerate every versioned path, candidate untracked source path, Unity asset and
its `.meta`, SourceArt source/export, package/settings file, build/content tool,
documentation file, provenance record, and acceptance receipt. Enumerate ignored
categories without loading their generated contents. Inspect Unity assembly
references, composition roots, scene/prefab/catalog bindings, import policies,
content IDs, resource ownership, tests, and source-to-export relationships.

For each relevant path record:

| Field | Required distinction |
| --- | --- |
| Presence | Local working tree, index, local HEAD, remote integration tree |
| Object identity | Git blob ID and file mode at each compared revision |
| Working bytes | SHA-256 of the saved local payload, with text normalization noted |
| LFS | Pointer blob versus hydrated object, OID, declared size and actual SHA/size |
| Unity identity | Existing GUID, source/metadata pair, and required references |
| Source/evidence | Authoring source, export, provenance, tests and receipt identity |

Compare the checkpoint source manifest with current files. It covers the
production source at publication, not future handoff edits, generated caches,
unsaved application state, or all future art deliverables. Use it as evidence,
not as permission to overwrite a different local revision.

Handle text EOL differences explicitly: a clean-filtered Git blob can match
while raw working bytes use CRLF. Do not report that as gameplay divergence.
Binary `.blend`, FBX, audio, and image files require exact hydrated bytes.

## 4. Verify binary assets and private-instance capabilities

```bash
git lfs ls-files
git lfs status
git lfs fsck
```

Read the pointer for each LFS-tracked path at the target revision, then verify
the local hydrated payload against its OID and size. If available, fetch the
required LFS objects and materialize them only when doing so cannot overwrite
local edits. Avoid checkout/reset/clean operations on a dirty worktree. A fresh
isolated checkout is preferable to overwriting unique files. An independent
checkout from GitHub verifies remote LFS availability, not merely a local cache.

Check Unity version/revision, approved package pins and locks, build modules,
Blender version, currently callable MCP tools, editor/project association, and
access to actual devices. A config entry is not proof of a connected tool.
Blender MCP was connected at checkpoint and the interactive PrototypePair file
was dirty. Unity MCP was not exposed. Unity AI Assistant/Inference are approved;
generator entitlement and the operator-reported 1,000-credit balance remain
unverified. Do not purchase, spend credits, start stopped services, or install
new dependencies merely to make this inventory green.

Attachments formerly outside the repository are not assumed accessible. The
selected duel concept is now in `docs/media/forever-we-reign-duel-concept.png`.
Local output folders and temporary test workspaces are not portable inputs.

## 5. Produce a concrete gap table and reconcile safely

Return a table with `path/capability | local state | remote state | category |
impact | proposed repair | owner | evidence`. Categories include:

- local-only source or unsaved state;
- remote-only source missing from this instance;
- differing saved source or intentionally divergent branch;
- missing/unhydrated/corrupt LFS payload;
- missing or conflicting Unity metadata/reference;
- generated cache or excluded alternate project, expected local-only;
- unavailable tool, device, entitlement or historical attachment;
- stale/missing validation receipt or contradictory current documentation.

Repair reversible setup gaps within the user's current authority. Do not choose
the newer timestamp as the winner for conflicting source. Preserve both sides,
identify the owning conversation, and ask one high-leverage question only if
the existing instructions cannot resolve the conflict. Do not force-push,
rewrite history, reset/clean, silently merge binary assets, regenerate existing
GUIDs, or claim that unavailable hardware is qualified.

## 6. Write the bootstrap receipt, then continue the named work

Store a dated receipt under `docs/handoffs/audits/`, using `technical-YYYY-MM-DD`
or `art-YYYY-MM-DD` filenames to avoid competing writers, containing roots/branches,
HEAD and remote SHAs, tree comparison, verified LFS payloads, source manifest
comparison, tool availability, gaps/repairs, file ownership, and remaining
UNRUN checks. Keep secrets and machine-generated caches out of Git.

Technical: after reconciliation, execute Task 14 and its specified verification.
Art: after reconciliation, establish and produce the bounded VisualBenchmark01
bundle under the shared ownership contract. Do not turn an initial audit into
an unlimited recursive investigation or begin the rest of the campaign.

At each handoff repeat a focused comparison for the changed bundle and update
the receipt. Full native tests, physical testing, and visual acceptance are
separate evidence categories. Never copy a historical PASS onto new source.
