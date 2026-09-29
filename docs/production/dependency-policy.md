# Dependency policy

The baseline is Unity **6000.6.0f1 (f7f8ed4d1e24)** at the outer repository root. Dependencies must serve an approved project requirement and remain reproducible from source control.

## Exact package pins

| Manifest ID | Version |
| --- | --- |
| `com.unity.render-pipelines.universal` | `17.6.0` |
| `com.unity.inputsystem` | `1.20.0` |
| `com.unity.ugui` | `2.6.0` |
| `com.unity.timeline` | `6.6.0` |
| `com.unity.cinemachine` | `6.6.0` |
| `com.unity.addressables` | `2.11.2` |
| `com.unity.test-framework` | `1.8.0` |
| `com.unity.nuget.newtonsoft-json` | `3.2.2` |
| `com.unity.pipeline` | `0.8.0-exp.1` |

`com.unity.animation.rigging` at `6.6.0` is explicitly deferred until contact correction needs it. The experimental Pipeline package remains an exact baseline pin; its presence is not evidence of production suitability or authorization to enable services.

Pin direct dependencies to exact versions. Do not use floating Git refs, version ranges, or unreviewed registry changes. Preserve the existing exact engine-module and editor-tool pins unless an approved change requires them.

## Changing dependencies

1. State the concrete need, affected workflows, license, transitive changes, and whether the package can enter a runtime build.
2. Inspect the current manifest and lockfile. Make the smallest approved change; do not opportunistically upgrade unrelated packages.
3. Let the pinned Unity editor resolve packages and generate `Packages/packages-lock.json`. Do not hand-edit resolved versions, dependency hashes, or registry metadata to imitate successful resolution.
4. Review manifest and lockfile together, and update `tools/build/dependency-baseline.json` alongside approved package pins. Record a major version jump and its migration risk in one line, and retain a reversible baseline.
5. Run the structural validator and relevant editor or build checks. Record the actual result and evidence. Preserve **UNRUN** for checks that were not performed.

Track the complete manifest and Unity-generated lockfile. Approval for a local dependency edit does not authorize a commit, push, purchase, service activation, infrastructure change, or production deployment. Obtain explicit authorization for those actions.

The baseline also records approved manifest options and a SHA-256 fingerprint of the entire parsed lockfile, serialized as sorted-key compact JSON. This pins transitive versions, registry URLs, package sources, depths, and dependency edges while tolerating JSON whitespace changes. After an approved package change has resolved and passed review, update that fingerprint in dependency-baseline.json along with the direct pins. Do not refresh the fingerprint merely to silence a failed check.

## Release runtime boundary

Release builds must have no active automation endpoints, runtime code reload, Development Build connections, or profiler connections. Reject `ENABLE_RUNTIME_PIPELINE` in release compilation. A package appearing in the editor or package cache does not establish that its runtime behavior is acceptable.

The Pipeline guard `UNITY_EDITOR || ENABLE_PROFILER || ENABLE_RUNTIME_PIPELINE` requires checking the actual compilation symbols and included assemblies. Instrumented managed-code builds can activate profiler-dependent code even when Development Build is off. Inspect the build configuration, generated artifacts, and relevant runtime behavior; an unchecked Development Build box or a static scan alone does not establish acceptance.

The launch campaign and its content remain local and offline. Approved future remote Addressables content remains possible with compatibility checks, versioned catalogs, and updates applied at menu boundaries. This scaffold does not enable or authorize that delivery path.

Record the actual artifact inspection and runtime checks for automation endpoints, code reload, and development or profiler connections. Keep release acceptance **UNRUN** until the relevant player has been built and exercised with evidence recorded.

## Shared asset ownership

Use one writer per shared Unity scene, prefab, controller, settings asset, or other serialized Unity asset at a time. Claim the file before editing and release ownership with the evidence or handoff. Preserve asset GUIDs and move Unity assets together with their `.meta` files. Resolve conflicting asset edits deliberately; do not regenerate metadata or accept a merge blindly.

Binary source content uses Git LFS. Unity YAML, scenes, prefabs, `.asset` files, and `.meta` files remain ordinary text in Git. Follow the [asset provenance template](asset-provenance.md) before integrating external or generated content.

Use the native Windows Git installation for LFS operations on the current workstation. WSL Git does not have Git LFS installed; switching Git executables can bypass the available LFS tooling. The [README](../../README.md) includes the native invocation for both terminals.
