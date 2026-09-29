# Task 01: Unity project and toolchain readiness

**Audit date:** September 29, 2026, approximately 14:05–14:09 America/Chicago (19:05–19:09 UTC).
**Result at original audit:** Task 01 inventory complete. The canonical Editor is running and Android/Windows toolchain artifacts are installed. Physical mobile qualification, Apple build/signing access, and Unity/Blender MCP connectivity are not established.

Task 01 completion means the readiness facts and gaps are recorded. It does not establish a successful build, a playable prototype, or supported devices. Task 02 has not started.

**Path corrected September 29, 2026, at 19:43 UTC:** the canonical Unity project now lives directly at the workspace root, [Assets](../../Assets), [Packages](../../Packages), and [ProjectSettings](../../ProjectSettings). The former nested project directory no longer exists. See the [relocation receipt](project-path-relocation-2026-09-29.json).

The inventory below describes the original 19:05–19:09 UTC session. Its PID, endpoint observations, and original nested layout are historical. Source links now resolve to relocated files; the original JSON snapshot and hash keys retain their historical meaning and include a relocation mapping. This path correction does not claim new build, test, or device acceptance.

The user's path correction supersedes the original plan's nested layout. Subsequent tasks must use Assets, Packages, and ProjectSettings directly under the workspace root.

Unity reopened from the corrected root as PID 54764 and completed startup compilation without reported compilation failure. All 132 source/config files match their original hashes after startup, including the entire alternate project. EditorBuildSettings.asset temporarily differed during import, then returned to its original hash without agent edits. The relocation receipt records the final verification.



**Publication note:** this report preserves the original audit findings. Workstation-source links were made portable for GitHub; installed-tool/cache evidence remains historical and is summarized in the JSON snapshot. See the [README](../../README.md) for the current implementation state.

## Scope and evidence rules

The work order is row 01 of the [supplied development plan](../production/development-plan.md). The plan's SHA-256, package inventory, sanitized observations, and project-source hashes are retained in the [evidence snapshot](task-01-evidence-2026-09-29.json).

| Label | Meaning |
|---|---|
| INSTALLED | Files, binary metadata, or resolved package records demonstrate local presence; execution is not implied. |
| CONNECTED | A read-only live request succeeded against the named capability. |
| UNVERIFIED | Available evidence cannot establish the capability or acceptance outcome. |
| UNAVAILABLE | The capability could not be used in this session, with the failure or absence stated. |
| UNRUN | No fresh execution of this acceptance check was performed. |

Only audit documents were created. No Unity assets/settings/packages were changed. No application or service was started, no build or gameplay test ran, and Git was not initialized. No credential values were reported or copied to the audit artifacts; signing profiles and keystores were not opened. Configuration findings use allowlisted non-secret fields.

## 1. Canonical project and active Editor

| Item | Status and evidence |
|---|---|
| Workspace | `C:\Users\cld-main\Desktop\github-projects\infinity-redux`; not a Git repository. |
| Canonical Unity project at original audit | Nested `infinity-redux` directory, confirmed by its version file, active Editor instance descriptor, window title, and Pipeline descriptor. |
| Required Editor pin | **6000.6.0f1**, revision **f7f8ed4d1e24**, matches ProjectVersion.txt lines 1–2. |
| Installed Editor | Unity.exe product version **6000.6.0f1_f7f8ed4d1e24**, file version **6000.6.0.16251117**. |
| Editor at original audit | PID **60472**; window identifies **infinity-redux / SampleScene / Unity 6.6 / DX11**. Two other processes using the same Editor executable were present; no scene mutation was attempted. |
| Serialization | Force Text, `m_SerializationMode: 2`, EditorSettings.asset line 7. |
| Metadata | Visible Meta Files, VersionControlSettings.asset line 6. |
| Scene/build baseline | One enabled scene, `Assets/Scenes/SampleScene.unity`; no custom build-profile assets found. |
| Gameplay implementation | No custom gameplay C# found. Assets contains the two TutorialInfo Readme scripts, sample scene, URP template settings, and input-actions asset. |
| Input baseline | Input System actions assigned in EditorBuildSettings; Player/UI maps and Keyboard&Mouse, Gamepad, Touch, Joystick, XR schemes exist. No game-specific touch recognition is implemented. |
| Alternate project | Sibling `My project` preserved. Its ProjectVersion.txt is absent; dependencies differ. It is not a fallback canonical project. |

The canonical project is still configured with template identity and platform defaults. Company is DefaultCompany, version is 0.1.0, and the Standalone identifier is the URP template identifier. Android min SDK is 26 and target SDK is serialized as 0; iOS target is 15.0. These are current settings, not the plan's approved OS/support claims. Platform configuration remains later work.

The alternate project has URP 17.3.0, Timeline 1.8.12, uGUI 2.0.0, and Test Framework 1.6.0. Its 48 manifest dependencies match its 63-entry lock. Its additional HubForceResolve Editor helper calls package resolution on Editor initialization. It was neither opened nor modified.

Evidence: [ProjectVersion.txt](../../ProjectSettings/ProjectVersion.txt#L1), [EditorSettings.asset](../../ProjectSettings/EditorSettings.asset#L7), [VersionControlSettings.asset](../../ProjectSettings/VersionControlSettings.asset#L6), [EditorBuildSettings.asset](../../ProjectSettings/EditorBuildSettings.asset#L7), [captured Editor identity in the original evidence snapshot](task-01-evidence-2026-09-29.json).

## 2. Packages and approved-baseline delta

**49/49 direct dependency versions match the lock.** The lock contains **66 packages**, with 52 builtin and 14 registry sources; depth counts are 49 direct, 13 at depth 1, and 4 at depth 2. This is structural consistency, not a fresh dependency restore.

| Package | Observed state | Plan disposition |
|---|---|---|
| URP | 17.6.0, direct and locked | Preserve. |
| Input System | 1.20.0, direct and locked | Preserve. |
| uGUI | 2.6.0, direct and locked | Preserve. |
| Timeline | 6.6.0, direct and locked | Preserve. |
| Test Framework | 1.8.0, direct and locked | Preserve; tests are UNRUN. |
| Unity Pipeline | 0.8.0-exp.1, direct and locked | Development automation only; release exclusion remains to verify. |
| Newtonsoft JSON | 3.2.2, indirect at depth 1 | Promote to direct dependency in task 02. |
| Addressables | Absent from project manifest and lock | Add 2.11.2 in task 02. The installed Editor catalog lists that version. |
| Cinemachine | 6.6.0 installed in Editor BuiltInPackages; absent from project manifest/lock | Available in Editor installation; project integration not established. |
| Animation Rigging | 6.6.0 installed in Editor BuiltInPackages; absent from project manifest/lock | Add when contact correction is needed, per plan. |
| AI Navigation | 2.0.14, direct and locked | Planned removal after confirming no authored references. That removal check was not executed. |
| Visual Scripting | 1.9.12, direct and locked | Planned removal in task 02. |
| AI Assistant | 2.20.0-pre.1, direct and locked | Planned removal unless a concrete development dependency is established. |
| AI Inference | 2.6.1, direct and locked | Same conditional removal policy. |
| Collab Proxy / Rider / Visual Studio integrations | 2.13.6 / 3.0.38 / 2.0.26 | Inventory only; no additional removal decision. |

Removing AI dependencies alone would not remove Newtonsoft while Pipeline remains. Manifest and lockfile must change together in task 02. No package versions changed in this audit.

Evidence: [manifest.json](../../Packages/manifest.json#L3), [packages-lock.json](../../Packages/packages-lock.json#L1), Editor package catalog (historical local evidence; see [audit snapshot](task-01-evidence-2026-09-29.json)), Cinemachine manifest (historical local evidence; see [audit snapshot](task-01-evidence-2026-09-29.json)), Rigging manifest (historical local evidence; see [audit snapshot](task-01-evidence-2026-09-29.json)).

## 3. Build modules and platform tools

The Editor root is `C:\Program Files\Unity\Hub\Editor\6000.6.0f1`. The following PlaybackEngines directories are present: AndroidPlayer, iOSSupport, AppleTVSupport, VisionOSPlayer, MetroSupport, WebGLSupport, and windowsstandalonesupport. This inventory does not expand the approved product platforms.

| Capability | Status | Evidence / qualification limit |
|---|---|---|
| Android platform support | INSTALLED | AndroidPlayer artifacts present; no APK/AAB build or deployment. |
| Android NDK | INSTALLED | r27c, **27.2.12479018**; clang++.exe present. |
| Java | INSTALLED | Temurin **17.0.18+8**; java.exe present. |
| Android SDK | INSTALLED | Platform tools **36.0.0**, build tools **36.0.0**, command-line tools **16.0**, CMake **3.22.1**. |
| SDK platforms | INSTALLED | API 34 rev 2, API 36 rev 2, API 37.0 rev 2. Effective build target has not been tested. |
| Windows IL2CPP | INSTALLED | Windows IL2CPP support and Editor/Data/il2cpp/build/deploy/il2cpp.exe present. |
| Windows compiler | INSTALLED | VS Community tools **14.51.36231**, cl.exe file version **19.51.36257.0**. Insiders tools **14.50.35717** also present. |
| Windows SDK | INSTALLED | **10.0.26100.0** headers, libraries, and resource compiler present. |
| Apple Unity export support | INSTALLED | iOSSupport present. No export was produced. |
| Mac / Xcode / Apple SDK | UNVERIFIED | No Mac or Xcode access established through this audit. Windows export support does not supply the signing toolchain. |
| Release signing | UNVERIFIED | Canonical project has no configured Apple team ID/manual profile ID or Android keystore/key-alias names. Credential ownership and external signing paths were not inspected. |
| Apple Windows device tooling | UNAVAILABLE in checked locations | No Apple Devices/iTunes package, relevant service, or standard device-support installation path found. This does not establish whether a separate Mac/device is accessible. |
| Blender application | INSTALLED | Blender **5.2** binary and file metadata; no Blender process observed. |

Hub's modules.json is not sufficient installation evidence: it largely omits isInstalled and reports Visual Studio false despite compiler artifacts being present. Filesystem and binary metadata were used instead.

The delegated read-only vswhere invocation failed with CantActivateDocumentInPipeline. Compiler files were inspected successfully, but Visual Studio registration and Unity's external-tool selections remain unverified. No repair or build was attempted.

Evidence: modules.json (historical local evidence; see [audit snapshot](task-01-evidence-2026-09-29.json)), NDK properties (historical local evidence; see [audit snapshot](task-01-evidence-2026-09-29.json)), JDK release (historical local evidence; see [audit snapshot](task-01-evidence-2026-09-29.json)), MSVC default version (historical local evidence; see [audit snapshot](task-01-evidence-2026-09-29.json)).

## 4. Accessible devices

| Plan qualification target | Current evidence | Acceptance |
|---|---|---|
| A15-class iPhone | Physical access UNVERIFIED; no current Windows phone/tablet enumeration | UNRUN |
| 120 Hz Pro iPhone | Physical access UNVERIFIED | UNRUN |
| M1-class iPad | Physical access UNVERIFIED | UNRUN |
| Snapdragon 8 Gen 2-class Android | No device reported by existing ADB daemon; access elsewhere UNVERIFIED | UNRUN |
| Second Android GPU family | No device reported by existing ADB daemon; access elsewhere UNVERIFIED | UNRUN |
| Windows PC | Local machine available: i5-12600KF, 10 cores/16 threads, 47.75 GiB RAM, RTX 4060 Ti, driver 32.0.15.9186 | UNRUN |

The pre-existing ADB daemon, PID 51000 on Windows 127.0.0.1:5037, answered a read-only smart-socket host:devices-l request with OKAY and **zero devices** (zero authorized, unauthorized, or offline). The adb CLI was not invoked, avoiding its automatic server-start behavior.

Windows Get-PnpDevice -PresentOnly yielded zero phone/tablet candidates under the audit's device filter. This establishes no currently enumerated device in those checks, not that the user owns or can access no phones.

Desktop Commander reports one online Windows desktop registration and one offline registration with the same desktop name. The online desktop answered ping. Those registrations are not evidence of a phone, tablet, or Mac.

## 5. MCP and experimental automation

| Capability | Installation/configuration | Live result |
|---|---|---|
| Unity MCP | Codex configuration enables unityMCP at http://127.0.0.1:8090/mcp | **UNAVAILABLE in this session:** zero exposed Unity MCP tools; no Windows listener observed on 8090. No MCP handshake could be performed. |
| Blender MCP | Enabled stdio registration; Blender tools are exposed | **UNAVAILABLE at audit:** get_blendfile_summary_path_info failed to connect to localhost:9876. No Blender process or Windows listener on 9876 observed. |
| Desktop Commander | Registered connector, desktop app 0.2.51 | **CONNECTED:** list_devices succeeded; ping returned pong at 19:06:52 UTC. |
| Unity Pipeline | Project package 0.8.0-exp.1 | Existing **loopback listener observed** on 127.0.0.1:7801, owned by canonical Editor PID 60472. Project descriptor agrees. Authenticated command handshake **UNRUN**. |

Unity Pipeline and Unity MCP are separate capabilities. A Pipeline listener does not establish a Unity MCP connection. The audit did not start, reconfigure, or repair any server.

### Pipeline configuration and release implications

- EditorPipelineConfig.json is absent. Installed source defaults Editor AutoStart to true. The already-running endpoint matches that default behavior.
- RuntimePipelineConfig.json is absent. Installed source defaults enableInBuilds to false and autoStart to true. These defaults are not a release-build acceptance result.
- Runtime assembly inclusion is controlled by **UNITY_EDITOR || ENABLE_PROFILER || ENABLE_RUNTIME_PIPELINE**.
- The build processor accounts for Development Build, scripted development options, managed code instrumentation, and the explicit runtime symbol. In this version, an instrumented build can contain runtime Pipeline code while Development Build is off.
- The current Standalone custom symbol list contains SENTIS_ANALYTICS_ENABLED, with no explicit ENABLE_RUNTIME_PIPELINE. Compiler-generated ENABLE_PROFILER cannot be excluded merely by checking custom symbols.
- Installed source binds the Pipeline HTTP listener to 127.0.0.1 and checks bearer authentication. This audit checked the observed listener and source, not authentication behavior or the security of the full package.
- Existing compilation-status telemetry reports completed with compilationFailed false. It is historical Editor telemetry, not a fresh compile or gameplay test.

**Release-exclusion TODO:** before release acceptance, verify non-development and non-instrumented build settings, absence of explicit runtime-enabling symbols, and actual player artifacts/behavior for automation endpoints and code reload. No release build exists in this audit to verify those conditions.

Evidence: runtime assembly definition (historical local evidence; see [audit snapshot](task-01-evidence-2026-09-29.json)), Editor defaults and settings path (historical local evidence; see [audit snapshot](task-01-evidence-2026-09-29.json)), runtime defaults (historical local evidence; see [audit snapshot](task-01-evidence-2026-09-29.json)), build inclusion checks (historical local evidence; see [audit snapshot](task-01-evidence-2026-09-29.json)).

## 6. Readiness gaps and next gates

| ID | Gap / one-line diagnosis | Required evidence / owner |
|---|---|---|
| R01 | Unity MCP is configured but not callable; no observed Windows 8090 listener. | Approved connection setup, tool exposure, and read-only handshake tied to canonical project. Operator/integration work; no service start authorized here. |
| R02 | Blender MCP connection fails and Blender is not running. | Approved application/add-on startup and successful read-only scene identity query. Operator/integration work. |
| R03 | Physical phone/tablet access is not documented and no device is enumerated. | Founder supplies available models/OS and device access; task 10 must retain real ergonomic/performance evidence. |
| R04 | Mac/Xcode, Apple signing, and Android release signing are unverified. | Establish accessible build/signing path before iOS device iteration and signed delivery. Keep credentials out of audit documents. |
| R05 | Canonical project remains a template; package additions/removals are unapplied. | Task 02: preserve alternate project, initialize repository, apply pins with lock updates, and verify restoration. |
| R06 | Installed compiler/SDK artifacts have not been exercised. | Build and deployment receipts under the applicable later work order; verify Unity external-tool selections and VS registration. |
| R07 | Pipeline runtime exclusion has no built-player evidence. | Enforce and verify the release-exclusion conditions above in the release pipeline and acceptance work. |
| R08 | No approved skeleton/animation family or measured encounter production throughput is recorded. | Tasks 11, 24, and 25 establish asset acceptance and production evidence; no purchases authorized. |

These gaps do not prevent recording task 01 as complete. They prevent claiming device, automation, signing, prototype, or release readiness. The next numbered work order is task 02; it remains unexecuted.

## 7. Verification receipt

| Check | Outcome |
|---|---|
| Canonical editor/version identity | Confirmed from project pin, executable metadata, active window, process/instance records |
| Direct manifest/lock version consistency | **PASS, 49/49**; no restore performed |
| Existing project preservation | **PASS, 132/132** source/config files across both projects unchanged by SHA-256; zero added or removed within Assets, Packages, and ProjectSettings |
| Blender read-only connection | **FAIL**, connection unavailable, recorded as R02 |
| Desktop Commander live read-only ping | **PASS** |
| Existing ADB daemon query | **PASS**, zero enumerated devices |
| Audit artifact validation | **PASS**, JSON parses, local links resolve, source hashes match; independent review found no material issues |
| Unity MCP handshake | **UNAVAILABLE**, recorded as R01 |
| Fresh compile, EditMode/PlayMode tests, builds, signing | **UNRUN** |
| Physical controls, thermals, latency, save robustness | **UNRUN** |

Hash comparison covers both projects' Assets, Packages, and ProjectSettings. Generated Library, Temp, Logs, and UserSettings were excluded because the pre-existing Editor can update them independently.

Method adaptation: the execution workflow was limited to the user's selected inventory task. Git/worktree setup, implementation tests, commits, and later task execution would exceed task 01, so none was performed. The initial low-cost subagent preset failed account/model compatibility; replacement agents completed the independent project and host inventories.
