# Task 12: original prototype feedback and presentation asset authoring

Recorded 2026-09-30. **Original source tones generated and reproducible;
native import, scene configuration, and gameplay integration require the root
Task 12 evidence. Perceived readability and final Foley acceptance are UNRUN.**

## Ownership and scope

- Owner: Chris, Praxen LLC.
- Creator: project-authored sine/chirp recipe, implemented by Codex for Praxen.
- Acquisition: original procedural synthesis using Python 3 standard-library
  `math`, `struct`, and `wave`; no external audio, recordings, samples, music,
  asset-store content, paid generations, or generative-model credits.
- Intended use: four quiet, short prototype cues for combat feedback. These are
  technical feedback tones, not a finished weapon/material Foley library.
- Use authorization: bounded project prototype use is authorized by the
  operator. No third-party asset license or attribution is introduced.
- Release eligibility: UNVERIFIED pending the normal source/rights and content
  acceptance process. This provenance record is not a legal clearance opinion.
- Dependencies: Python standard library, pinned Unity 6000.6.0f1 and existing
  URP 17.6.0. No new package, service, purchase, or subscription.

The [recipe](../../tools/content/build_feedback_tones.py) and
[source manifest](../../SourceArt/Audio/task12-feedback-source.json) constitute
the versioned bundle `task12-feedback-tones-1.0.0`. The manifest records every
frequency, partial weight, envelope, gain, sample count, peak/RMS measurement,
SHA-256, and preassigned Unity GUID. Rerunning the recipe preserves existing
Unity metadata and refuses an unexpected GUID.

## Synthesis and bounded level

All four files are mono, 48 kHz, signed 16-bit little-endian PCM WAVs. Each
waveform combines explicitly listed sine partials around a linear frequency
chirp. A 6 ms attack, 20 ms release, and authored power decay taper the result.
The first and last samples are zero. Weighted partials are divided by the sum
of their absolute weights, then multiplied by the gain and envelope; there is
no peak normalization, dithering, noise, or random input.

| Tone | Duration | Chirp | Gain ceiling | Measured peak | Intended mapping |
| --- | --- | --- | --- | --- | --- |
| Tell | 100 ms | 880 to 1040 Hz | 0.100 | 0.08194 | Telegraph, Opening |
| Swing | 110 ms | 700 to 140 Hz | 0.085 | 0.07263 | Attack, Dodge |
| Clash | 140 ms | 2100 to 1700 Hz | 0.095 | 0.07520 | Parry, Block, Guard |
| Impact | 180 ms | 160 to 70 Hz | 0.100 | 0.08578 | Hit, Death |

Peak fractions are relative to full-scale PCM. Every measured peak is below
0.1, approximately -20 dBFS. Playback volume and device output still require
the consolidated physical checkpoint; numerical level does not establish a
comfortable or distinctive cue on speakers/headphones.

## Source identity

| File | SHA-256 | Unity GUID |
| --- | --- | --- |
| [Recipe](../../tools/content/build_feedback_tones.py) | `e093890a042936b5fd9ab5c2f444ff0248d444624bdcbaa39c9af5e79ebe57dd` | Not a Unity import |
| [Manifest](../../SourceArt/Audio/task12-feedback-source.json) | `a4b627580a91aaabc6fc35d924255ae328591d11a45d1b5e456409271ee2a64a` | Not a Unity import |
| [Tell](../../Assets/Game/Content/Audio/Task12Tell.wav) | `3cc7257911cdc71a8dfb908ecc8bfb5d502bf060f07ff7e54bcc9f5ac6d75e5f` | `c8314bb10b0657bc9fe0d80175371ff7` |
| [Swing](../../Assets/Game/Content/Audio/Task12Swing.wav) | `b0779d878a35b1ff9946e4781c888da3857abefe1fd87d2364b1753c45f4d5a4` | `929f4a716ba154b381c7da206c95e477` |
| [Clash](../../Assets/Game/Content/Audio/Task12Clash.wav) | `ddce06c3fccf28f09692a8740da162cb5f9fd4282b3f5cb4e39165ccd96e3957` | `dc8abf96f5e55ddab916c97a0d50a901` |
| [Impact](../../Assets/Game/Content/Audio/Task12Impact.wav) | `f604a9c2f9055f4cbe43d1ea5264bc179e0e4f4e3d0a9870484aead8384279dc` | `0f3fe31f529453a0bffb2d49a6f571c3` |

The audio folder GUID is `ba09b1384f505bbab672d74380d8ac09`. WAV bytes and
GUIDs are fixed by the recipe; native Unity authoring fills in the import
metadata while preserving these identities.

## Native presentation authoring

[CombatPresentationProfile](../../Assets/Game/Runtime/Presentation/Combat/CombatPresentationProfile.cs)
holds Task 11 Soldier/Captain prefab references, all 13 exact embedded motion
clips per actor, and the nine cue-to-tone mappings above. Motion lookup first
tries the requested exact key, then the same actor's Idle clip. It holds no
combat rules, timings, damage values, or animation events.

The menu `Praxen/Assets/Build Combat Prototype` invokes
[CombatPrototypeAuthoring.Build](../../Assets/Game/Editor/CombatPrototypeAuthoring.cs).
The root task runs this in an isolated native Editor project. It configures
only these four audio imports to DecompressOnLoad, PCM, mono, preload enabled,
and background loading disabled; all other importer defaults remain intact.
It loads both valid Humanoid prefabs and the exact 13 motions from each LOD0
FBX, then creates or updates the saved presentation profile.

Authoring opens GrayboxEncounter as the source, binds the presentation profile,
and saves the separate CombatPrototype scene. It appends that new scene to the
build list only if absent, preserving every existing entry and its state. It
does not write GrayboxEncounter, Bootstrap, PrototypePairReview, or any other
project setting. The root task verifies preserved bytes after native execution.

Interactive invocation first saves/checks every loaded scene. It rejects
unsaved or dirty scenes and a loaded source/target scene before asset edits.
Otherwise it uses an additive source and save-as-copy, restores the previous
active scene, and closes the temporary loaded source. Play Mode authoring is
rejected. An isolated batch Editor remains the intended execution path.

## Reproduction and remaining acceptance

From the repository root:

```bash
python3 tools/content/build_feedback_tones.py
python3 tools/content/build_feedback_tones.py --check
```

The second command compares generated WAV and manifest bytes plus Unity GUIDs
without writing. Structural source checks can verify format, duration, finite
samples, level, and endpoint taper. Native AudioImporter settings, saved
profile references, scene binding, combat isolation, event counts, lifecycle
behavior, and camera/effect limits require Unity evidence in the root Task 12
receipt. Source reproduction alone does not accept those behaviors.

Founder visual/contact/deformation review, cue clarity, speaker/headphone
levels, physical portrait controls, sustained device performance, and release
content approval remain at the [post-Task-12 checkpoint](../acceptance/post-task-12-checkpoint.md).
