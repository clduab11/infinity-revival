#!/usr/bin/env python3
"""Read-only Task 02 repository checks. Does not run Unity or mutate Git."""
import argparse
import hashlib
import json
import re
import shutil
import subprocess
import sys
from pathlib import Path

REPOSITORY = Path(__file__).resolve().parents[2]
POLICY = Path(__file__).with_name("dependency-baseline.json")
GAME_FOLDERS = (
    "Game/Runtime/Domain", "Game/Runtime/Application", "Game/Runtime/Content",
    "Game/Runtime/Presentation", "Game/Runtime/Infrastructure", "Game/Editor",
    "Game/Tests", "Game/Content", "Game/Scenes", "ThirdParty",
)
IGNORED = (
    "Library/probe", "Temp/probe", "Obj/probe", "Logs/probe", "UserSettings/probe",
    "Build/probe", "Builds/probe", "My project/Assets/probe", "output/probe",
    ".superpowers/probe", ".env", "release.keystore", "release.p12",
    "Assembly-CSharp.csproj", "infinity-redux.slnx",
)
TRACKABLE = (
    "Assets/Scenes/SampleScene.unity", "Assets/Scenes/SampleScene.unity.meta",
    "Packages/manifest.json", "Packages/packages-lock.json",
    "ProjectSettings/ProjectVersion.txt", "tools/build/verify_baseline.py",
    "SourceArt/Characters/example.fbx", "docs/production/dependency-policy.md",
)
BINARY_SAMPLES = (
    "SourceArt/Characters/example.fbx", "SourceArt/Characters/EXAMPLE.FBX",
    "Assets/Game/Content/example.png", "Assets/Game/Content/EXAMPLE.PNG",
    "SourceArt/Audio/example.wav", "SourceArt/Characters/example.blend",
)
TEXT_SAMPLES = (
    "Assets/Game/Scenes/example.unity", "Assets/Game/Content/example.prefab",
    "Assets/Game/Content/example.asset", "Assets/Game/Content/example.png.meta",
)


def read_json(path, errors):
    try:
        value = json.loads(path.read_text(encoding="utf-8-sig"))
        if not isinstance(value, dict):
            raise ValueError("expected a JSON object")
        return value
    except (OSError, ValueError) as exc:
        errors.append(f"{path.name}: {exc}")
        return {}


def check_lock_fingerprint(document, policy, errors):
    normalized = json.dumps(document, sort_keys=True, separators=(",", ":")).encode("utf-8")
    fingerprint = hashlib.sha256(normalized).hexdigest()
    if fingerprint != policy.get("resolved_lock_fingerprint_sha256"):
        errors.append("Resolved lock graph differs from the approved fingerprint "
                      "(versions, sources, registry URLs, depths, and dependency edges).")


def check_packages(root, policy, errors):
    manifest_document = read_json(root / "Packages/manifest.json", errors)
    lock_document = read_json(root / "Packages/packages-lock.json", errors)
    options = {k: v for k, v in manifest_document.items() if k != "dependencies"}
    if options != policy.get("manifest_options", {}):
        errors.append("Unapproved manifest options, including registry configuration.")
    check_lock_fingerprint(lock_document, policy, errors)
    manifest = manifest_document.get("dependencies", {})
    locked = lock_document.get("dependencies", {})
    expected = policy["direct_dependencies"]
    if not isinstance(manifest, dict) or not isinstance(locked, dict):
        errors.append("Package dependencies must be JSON objects.")
        return 0, 0
    for name, version in expected.items():
        if manifest.get(name) != version:
            errors.append(f"Manifest requires {name} {version}; got {manifest.get(name)!r}.")
    for name in sorted(set(manifest) - set(expected)):
        errors.append(f"Unapproved direct dependency: {name}.")
    for name, version in manifest.items():
        entry = locked.get(name)
        if not isinstance(entry, dict) or entry.get("version") != version or entry.get("depth") != 0:
            errors.append(f"Lock must resolve direct dependency {name} to {version} at depth 0.")
    for name, entry in locked.items():
        if not isinstance(entry, dict):
            errors.append(f"Invalid lock entry: {name}.")
            continue
        deps = entry.get("dependencies", {})
        if not isinstance(deps, dict):
            errors.append(f"Invalid lock dependencies: {name}.")
            continue
        for dependency in deps:
            if dependency not in locked:
                errors.append(f"Unresolved lock dependency: {name} -> {dependency}.")
    for name in policy["forbidden_packages"]:
        if name in manifest or name in locked:
            errors.append(f"Removed package remains: {name}.")
    return len(manifest), len(locked)


def check_settings(root, policy, errors):
    expected = {
        "ProjectVersion.txt": (
            f"m_EditorVersion: {policy['unity_version']}",
            f"m_EditorVersionWithRevision: {policy['unity_version']} ({policy['unity_revision']})",
        ),
        "EditorSettings.asset": ("m_SerializationMode: 2",),
        "VersionControlSettings.asset": ("m_Mode: Visible Meta Files",),
    }
    for filename, values in expected.items():
        path = root / "ProjectSettings" / filename
        if not path.is_file():
            errors.append(f"Missing settings file: {filename}.")
            continue
        lines = {line.strip() for line in path.read_text(encoding="utf-8-sig").splitlines()}
        for value in values:
            if value not in lines:
                errors.append(f"{filename}: required setting {value!r} is missing.")


def check_assets(root, errors):
    assets = root / "Assets"
    for relative in GAME_FOLDERS:
        if not (assets / relative).is_dir():
            errors.append(f"Missing Unity folder: Assets/{relative}.")
    guids = {}
    for path in sorted(assets.rglob("*")):
        if any(part.startswith(".") for part in path.relative_to(assets).parts):
            continue
        if path.suffix == ".meta":
            continue
        meta = Path(str(path) + ".meta")
        if not meta.is_file():
            errors.append(f"Missing Unity metadata: {path.relative_to(root)}.")
            continue
        match = re.search(r"^guid: ([0-9a-fA-F]{32})$", meta.read_text(encoding="utf-8-sig"), re.M)
        if not match:
            errors.append(f"Invalid Unity GUID: {meta.relative_to(root)}.")
            continue
        guid = match.group(1).lower()
        if guid in guids:
            errors.append(f"Duplicate Unity GUID: {meta.relative_to(root)} and {guids[guid]}.")
        guids[guid] = str(meta.relative_to(root))
    return len(guids)


def git_binary():
    native = Path("/mnt/c/Program Files/Git/cmd/git.exe")
    return str(native) if sys.platform != "win32" and native.is_file() else shutil.which("git")


def check_git(root, errors):
    executable = git_binary()
    if not executable:
        errors.append("Git is unavailable.")
        return
    def run(*args, stdin=None):
        return subprocess.run([executable, *args], cwd=root, input=stdin,
                              text=True, capture_output=True, check=False)
    if run("rev-parse", "--git-dir").returncode:
        errors.append("Project root is not a Git repository.")
        return
    result = run("check-ignore", "--stdin", stdin="\n".join(IGNORED + TRACKABLE) + "\n")
    if result.returncode not in (0, 1):
        errors.append("Git ignore check failed: " + result.stderr.strip())
        return
    ignored = set(result.stdout.splitlines())
    for path in IGNORED:
        if path not in ignored:
            errors.append(f"Generated/private path is not ignored: {path}.")
    for path in TRACKABLE:
        if path in ignored:
            errors.append(f"Required source path is ignored: {path}.")
    attrs = run("check-attr", "--stdin", "filter", "diff", "merge", "text",
                stdin="\n".join(BINARY_SAMPLES + TEXT_SAMPLES) + "\n")
    if attrs.returncode:
        errors.append("Git attributes check failed: " + attrs.stderr.strip())
        return
    values = {}
    for line in attrs.stdout.splitlines():
        path, attribute, value = line.split(": ", 2)
        values.setdefault(path, {})[attribute] = value
    for path in BINARY_SAMPLES:
        if values.get(path) != {"filter": "lfs", "diff": "lfs", "merge": "lfs", "text": "unset"}:
            errors.append(f"Binary asset lacks complete LFS attributes: {path}.")
    for path in TEXT_SAMPLES:
        if values.get(path, {}).get("filter") == "lfs" or values.get(path, {}).get("text") != "set":
            errors.append(f"Unity YAML/metadata must remain text, outside LFS: {path}.")
    if run("lfs", "version").returncode:
        errors.append("Git LFS is unavailable to the selected Git executable.")
    if run("config", "--local", "--get", "filter.lfs.required").stdout.strip() != "true":
        errors.append("Run git lfs install --local before working with binary assets.")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project-root", type=Path, default=REPOSITORY)
    parser.add_argument("--skip-git", action="store_true", help="For source-only Unity restore copies.")
    args = parser.parse_args()
    errors = []
    policy = read_json(POLICY, errors)
    if errors:
        print(json.dumps({"ok": False, "errors": errors}, indent=2))
        return 1
    root = args.project_root.resolve()
    direct, resolved = check_packages(root, policy, errors)
    check_settings(root, policy, errors)
    metas = check_assets(root, errors)
    if not args.skip_git:
        check_git(root, errors)
    print(json.dumps({"ok": not errors, "project_root": str(root),
                      "direct_packages": direct, "resolved_packages": resolved,
                      "asset_guids": metas, "git_checked": not args.skip_git,
                      "errors": errors}, indent=2))
    return int(bool(errors))


if __name__ == "__main__":
    raise SystemExit(main())
