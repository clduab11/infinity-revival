#!/usr/bin/env python3
"""Generate four original Task 12 feedback tones with Python's standard library."""

import argparse
import hashlib
import io
import json
import math
from pathlib import Path
import struct
import uuid
import wave


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = Path("Assets/Game/Content/Audio")
MANIFEST = Path("SourceArt/Audio/task12-feedback-source.json")
SAMPLE_RATE = 48000
TONES = {
    "Tell": {"duration_ms": 100, "start_hz": 880, "end_hz": 1040,
             "gain": .10, "decay": 1.2, "partials": [[1, 1], [2, .2]]},
    "Swing": {"duration_ms": 110, "start_hz": 700, "end_hz": 140,
              "gain": .085, "decay": 1, "partials": [[1, 1], [2, .15]]},
    "Clash": {"duration_ms": 140, "start_hz": 2100, "end_hz": 1700,
              "gain": .095, "decay": 3, "partials": [[1, 1], [1.4142, .35], [2.1, .15]]},
    "Impact": {"duration_ms": 180, "start_hz": 160, "end_hz": 70,
               "gain": .10, "decay": 3, "partials": [[1, 1], [2.7, .25]]},
}
CUES = {"Telegraph": "Tell", "Attack": "Swing", "Parry": "Clash", "Block": "Clash",
        "Dodge": "Swing", "Hit": "Impact", "Death": "Impact", "Opening": "Tell", "Guard": "Clash"}


def sha256(data):
    return hashlib.sha256(data).hexdigest()


def guid_for(path):
    return uuid.uuid5(uuid.NAMESPACE_URL, "praxen/infinity-redux/task12/" + path.as_posix()).hex


def synthesize(recipe):
    count = int(SAMPLE_RATE * recipe["duration_ms"] / 1000)
    duration = (count - 1) / SAMPLE_RATE
    slope = (recipe["end_hz"] - recipe["start_hz"]) / duration
    partial_weight = sum(abs(weight) for _, weight in recipe["partials"])
    samples = []
    for index in range(count):
        time = index / SAMPLE_RATE
        progress = index / (count - 1)
        envelope = min(1, time / .006) * min(1, (duration - time) / .020)
        envelope *= (1 - progress) ** recipe["decay"]
        phase = math.tau * (recipe["start_hz"] * time + .5 * slope * time * time)
        signal = sum(weight * math.sin(multiple * phase) for multiple, weight in recipe["partials"])
        value = recipe["gain"] * envelope * signal / partial_weight
        samples.append(round(32767 * value))
    pcm = struct.pack("<" + "h" * len(samples), *samples)
    output = io.BytesIO()
    with wave.open(output, "wb") as writer:
        writer.setnchannels(1)
        writer.setsampwidth(2)
        writer.setframerate(SAMPLE_RATE)
        writer.writeframes(pcm)
    return output.getvalue(), samples


def source_manifest(entries):
    return {
        "schema_version": 1,
        "source_version": "task12-feedback-tones-1.0.0",
        "created_on": "2026-09-30",
        "owner": "Chris, Praxen LLC",
        "creator": "Project-authored procedural tone recipe implemented by Codex for Praxen",
        "purpose": "Quiet prototype combat feedback, not final Foley or perceptual acceptance",
        "external_assets": [],
        "purchases": [],
        "generative_model_credits_used": 0,
        "generator": "Python 3 standard library math, struct, wave",
        "recipe_path": "tools/content/build_feedback_tones.py",
        "recipe_sha256": sha256(Path(__file__).read_bytes()),
        "format": {"encoding": "linear PCM signed little-endian", "sample_rate_hz": SAMPLE_RATE,
                   "channels": 1, "bits_per_sample": 16},
        "envelope": {"attack_seconds": .006, "release_seconds": .020,
                     "formula": "min(1,t/attack)*min(1,(duration-t)/release)*(1-progress)^decay"},
        "signal": "gain*envelope*sum(weight*sin(multiple*2*pi*(start_hz*t+0.5*slope*t*t)))/sum(abs(weight))",
        "phase_slope": "(end_hz-start_hz)/((sample_count-1)/sample_rate)",
        "quantization": "round(32767*value), no random input, dither, noise, external samples or normalization",
        "unity_import": {"load_type": "DecompressOnLoad", "compression": "PCM", "force_to_mono": True,
                         "preload_audio_data": True, "load_in_background": False,
                         "scope": "Only the four Task12*.wav files; other importer defaults retained"},
        "cue_mapping": CUES,
        "audio_folder_guid": guid_for(OUTPUT),
        "tones": entries,
        "acceptance": {"source_reproduction": "Verify with build_feedback_tones.py --check",
                       "native_import": "Requires isolated Unity authoring and root task evidence",
                       "perceived_readability_and_final_foley": "UNRUN"},
    }


def build_outputs():
    outputs, entries = {}, []
    for name, recipe in TONES.items():
        path = OUTPUT / ("Task12" + name + ".wav")
        data, samples = synthesize(recipe)
        outputs[path] = data
        entries.append({"name": name, "path": path.as_posix(), "sha256": sha256(data),
                        "unity_guid": guid_for(path), "sample_count": len(samples),
                        "peak_fraction": max(abs(sample) for sample in samples) / 32768,
                        "rms_fraction": math.sqrt(sum(sample * sample for sample in samples) / len(samples)) / 32768,
                        "recipe": recipe})
    manifest = json.dumps(source_manifest(entries), indent=2, ensure_ascii=True) + "\n"
    outputs[MANIFEST] = manifest.encode("utf-8")
    return outputs, entries


def ensure_meta(root, path, folder=False):
    meta_path = root / (path.as_posix() + ".meta")
    if meta_path.exists():
        if "guid: " + guid_for(path) not in meta_path.read_text(encoding="utf-8"):
            raise ValueError("Unexpected existing GUID: " + str(meta_path))
        return
    text = "fileFormatVersion: 2\nguid: " + guid_for(path) + "\n"
    if folder:
        text += "folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    meta_path.write_text(text, encoding="utf-8")


def run(check):
    outputs, entries = build_outputs()
    mismatches = [path.as_posix() for path, data in outputs.items()
                  if not (ROOT / path).exists() or (ROOT / path).read_bytes() != data]
    if check:
        for path in [OUTPUT] + [path for path in outputs if path.suffix == ".wav"]:
            meta_path = ROOT / (path.as_posix() + ".meta")
            if not meta_path.exists() or "guid: " + guid_for(path) not in meta_path.read_text(encoding="utf-8"):
                mismatches.append(path.as_posix() + ".meta")
        print(json.dumps({"status": "PASS" if not mismatches else "FAIL", "mismatches": mismatches,
                          "tones": [{"name": entry["name"], "sha256": entry["sha256"]} for entry in entries]}, indent=2))
        return 1 if mismatches else 0
    for path, data in outputs.items():
        (ROOT / path).parent.mkdir(parents=True, exist_ok=True)
        (ROOT / path).write_bytes(data)
    ensure_meta(ROOT, OUTPUT, folder=True)
    for path in outputs:
        if path.suffix == ".wav":
            ensure_meta(ROOT, path)
    print(json.dumps({"status": "GENERATED", "manifest": MANIFEST.as_posix(), "tones": entries}, indent=2))
    return 0


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="Verify recipe bytes and GUIDs without writing.")
    raise SystemExit(run(parser.parse_args().check))
