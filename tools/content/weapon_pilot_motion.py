"""Original axe/mace motion recipe, independent of Blender and Unity.

The accepted sword recipe supplies defensive poses, the ready pose and the
corrected rig reflection. Family cuts use original bone-local XYZ degree
tables, explicit release/contact keys and continuing recovery. Contact marks
presentation sampling only; the combat clock remains gameplay authority.
"""
import argparse
import hashlib
import importlib.util
import json
import math
from pathlib import Path

VERSION = "weapon-family-pilot-1.0.0"
SOURCE_MOTION_SHA256 = "90dbe95c550df413eb69fbd943d7c98801a7f20020f7f7e836819ceed4b8f807"
_SOURCE_PATH = Path(__file__).with_name("prototype_motion.py")


def _load_accepted():
    if hashlib.sha256(_SOURCE_PATH.read_bytes()).hexdigest() != SOURCE_MOTION_SHA256:
        raise ValueError("Accepted prototype motion differs from its pinned source")
    spec = importlib.util.spec_from_file_location("weapon_pilot_accepted_motion", _SOURCE_PATH)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


_accepted = _load_accepted()
# actor, contact, duration, release, two continued arc keys, relaxed return.
_TIMING = {"Axe": ("AxePilot", 15, 60, 9, 21, 33, 45),
           "Mace": ("MacePilot", 18, 70, 10, 25, 40, 54)}

# Rows are upper arm, forearm, wrist, torso yaw and lateral brace, in the raw
# accepted authoring basis. Axe preparations lift the head further and lead
# with torso rotation; its longer arc has a committed, measured return.
_AXE_ROWS = {
    "CutLeft": [
        ((-4, 0, 24), (28, 43, 18), (-4, 43, 2), 20, 3),
        ((-12, 0, 11), (36, 39, 17), (-2, 41, -5), 26, 4),
        ((44, 0, -20), (24, 48, 21), (-2, 43, 2), -6, -3),
        ((73, 0, -16), (26, 52, 24), (0, 43, 6), -17, -4),
        ((101, 0, -2), (34, 50, 28), (2, 35, 10), -26, -4)],
    "CutRight": [
        ((101, 0, -2), (34, 50, 28), (2, 35, 10), -26, -4),
        ((116, 0, 2), (40, 44, 29), (0, 38, 13), -32, -5),
        ((46, 0, -19), (26, 48, 22), (-2, 43, 2), 6, 3),
        ((12, 0, 23), (28, 52, 21), (-1, 42, -6), 17, 4),
        ((-18, 0, 7), (38, 49, 19), (-4, 32, -10), 26, 4)],
    "CutDown": [
        ((20, 0, 62), (28, 4, -69), (-3, 5, 2), 13, 0),
        ((17, 0, 69), (36, 4, -76), (-3, 7, -6), 19, 0),
        ((44, 0, 13), (31, 50, -9), (-3, 46, 1), -4, 0),
        ((50, 0, -24), (32, 67, -10), (-1, 59, 2), -9, 0),
        ((50, 0, -46), (37, 79, 9), (1, 62, 4), -15, 0)],
    "CutUp": [
        ((12, 0, -68), (34, 79, 12), (-3, 60, 0), -14, 0),
        ((7, 0, -73), (40, 74, 11), (-2, 55, -6), -19, 0),
        ((44, 0, -12), (31, 49, 12), (-3, 46, 2), 4, 0),
        ((40, 0, 25), (30, 25, -28), (-1, 22, 3), 9, 0),
        ((40, 0, 55), (32, 4, -68), (1, 6, 5), 15, 0)]}

# Mace preparation stays closer to the ready guard, retaining elbow flexion.
# Its contact-to-terminal travel is larger, with later recovery/settle keys
# and stronger body follow-through to communicate the heavier head.
_MACE_ROWS = {
    "CutLeft": [
        ((17, 0, 15), (38, 40, 16), (-8, 34, 0), 13, 2),
        ((11, 0, 10), (44, 36, 18), (-8, 32, -4), 18, 3),
        ((43, 0, -18), (27, 46, 20), (-5, 38, 1), -5, -2),
        ((77, 0, -15), (25, 53, 24), (-2, 43, 6), -18, -4),
        ((113, 0, -1), (32, 55, 29), (2, 41, 11), -32, -5)],
    "CutRight": [
        ((84, 0, 0), (38, 42, 24), (-8, 30, 6), -16, -2),
        ((94, 0, 1), (45, 38, 26), (-8, 31, 9), -21, -3),
        ((42, 0, -17), (28, 46, 21), (-5, 38, 1), 5, 2),
        ((7, 0, 22), (25, 54, 20), (-2, 43, -6), 18, 4),
        ((-24, 0, 3), (33, 56, 17), (2, 38, -12), 32, 5)],
    "CutDown": [
        ((29, 0, 41), (37, 5, -48), (-8, 8, 0), 9, 0),
        ((25, 0, 48), (43, 5, -55), (-8, 11, -4), 14, 0),
        ((43, 0, 8), (29, 46, -7), (-5, 38, 1), -3, 0),
        ((48, 0, -29), (27, 62, -6), (-2, 53, 4), -10, 0),
        ((51, 0, -57), (35, 83, 12), (2, 65, 8), -22, 0)],
    "CutUp": [
        ((22, 0, -42), (39, 69, 12), (-8, 46, 0), -8, 0),
        ((18, 0, -48), (45, 64, 9), (-8, 41, -4), -13, 0),
        ((42, 0, -8), (29, 42, 11), (-5, 35, 1), 3, 0),
        ((37, 0, 31), (28, 19, -27), (-2, 17, 4), 10, 0),
        ((37, 0, 64), (33, -4, -75), (2, -2, 8), 22, 0)]}
_ROWS = {"Axe": _AXE_ROWS, "Mace": _MACE_ROWS}


def _timing(family):
    if not isinstance(family, str) or family not in _TIMING:
        raise ValueError("Unknown weapon family: " + str(family))
    return _TIMING[family]


def clips_for(family):
    """Fresh thirteen-clip map with accepted defensive clip durations."""
    duration = _timing(family)[2]
    return {name: duration if name.startswith("Cut") or name == "EnemyAttack" else frames
            for name, frames in _accepted.CLIPS.items()}


def _cut_keys(family, name):
    _, contact, duration, release, follow, terminal, settle_frame = _timing(family)
    keys = [(frame, _accepted.strike_pose(*row)) for frame, row in
            zip((0, release, contact, follow, terminal), _ROWS[family][name])]
    settle = dict(_accepted.base_pose(), RightUpperArm=(34, 8, -35),
                  RightLowerArm=(43, 16, 33), RightHand=(-6, 13, 3))
    if family == "Mace":
        settle.update(RightUpperArm=(31, 8, -34), RightLowerArm=(45, 17, 31),
                      RightHand=(-7, 13, 2), Chest=(3, 1, 0))
    return keys + [(settle_frame, settle), (duration, _accepted.base_pose())]


def _authored_keys(family, name, duration):
    clips = clips_for(family)
    if (not isinstance(name, str) or name not in clips or
            not isinstance(duration, int) or isinstance(duration, bool) or duration != clips[name]):
        raise ValueError("Unknown clip or changed family timing contract: " + str(name))
    if name.startswith("Cut") or name == "EnemyAttack":
        return _cut_keys(family, "CutDown" if name == "EnemyAttack" else name)
    if name == "EnemyTell":
        guard = _accepted.guard_pose()
        wind = _cut_keys(family, "CutDown")[0][1]
        gathering = dict(guard, Chest=(3, 5 if family == "Axe" else 3, 0),
                         RightUpperArm=(25, 0, 24 if family == "Axe" else 17),
                         RightLowerArm=(35, 4, -34), RightHand=(-5, 9, 0))
        return [(0, guard), (18, gathering), (36, wind), (60, wind)]
    return _accepted._authored_keys(name, duration)


def _validate_pose(pose, name):
    if not isinstance(pose, dict) or not pose:
        raise ValueError("Invalid bone pose: " + name)
    for bone, degrees in pose.items():
        if (not isinstance(bone, str) or not isinstance(degrees, (tuple, list)) or
                len(degrees) != 3):
            raise ValueError("Invalid bone rotation: " + name)
        for value in degrees:
            try:
                valid = isinstance(value, (int, float)) and not isinstance(value, bool) and math.isfinite(value)
            except (TypeError, OverflowError):
                valid = False
            if not valid:
                raise ValueError("Invalid finite bone rotation: " + name)


def clip_keys(family, name, duration):
    """Reflect raw family tables once into the accepted anatomical rig basis."""
    keys = _authored_keys(family, name, duration)
    for _, pose in keys:
        _validate_pose(pose, name)
    return [(frame, _accepted.reflect_pose(pose)) for frame, pose in keys]


def _validate_entry(entry):
    fields = ("family", "actor", "contact_frame", "duration_frames", "release_frame")
    if not isinstance(entry, dict) or any(field not in entry for field in fields):
        raise ValueError("Each family requires family/actor/contact/duration/release fields")
    actor, contact, duration, release, *_ = _timing(entry["family"])
    if entry["actor"] != actor:
        raise ValueError("Actor identity differs for " + entry["family"])
    for field, expected in zip(fields[2:], (contact, duration, release)):
        value = entry[field]
        if not isinstance(value, int) or isinstance(value, bool) or value != expected:
            raise ValueError("Family requires its authored finite integer " + field)
    if not 0 <= release < contact < duration:
        raise ValueError("Release/contact/duration landmarks are out of order")
    return entry["family"]


def _validate_keys(family, name, duration, keys):
    frames = [frame for frame, _ in keys]
    if (not frames or any(not isinstance(frame, int) or isinstance(frame, bool) for frame in frames)
            or frames != sorted(set(frames)) or frames[0] != 0 or frames[-1] != duration):
        raise ValueError("Invalid key range: " + family + "/" + name)
    if name.startswith("Cut") or name == "EnemyAttack":
        _, contact, _, release, follow, terminal, _ = _timing(family)
        if not 6 <= len(keys) <= 8 or any(frame not in frames for frame in (release, contact, follow, terminal)):
            raise ValueError("Family cut requires release/contact/continuing recovery keys")
        poses = dict(keys)
        if (poses[contact].get("RightLowerArm") == (0, 0, 0) or
                any(poses[contact] == poses[frame] for frame in (follow, terminal))):
            raise ValueError("Contact requires elbow flexion and moving follow-through")
        if keys[-1][1] != _accepted.reflect_pose(_accepted.base_pose()):
            raise ValueError("Family cut must settle into the shared ready pose")


def _validate_clips(family):
    clips = clips_for(family)
    if len(clips) != 13 or set(clips) != set(_accepted.CLIPS):
        raise ValueError("Family requires the accepted thirteen motion keys")
    for name, duration in clips.items():
        _validate_keys(family, name, duration, clip_keys(family, name, duration))
    if (clip_keys(family, "EnemyTell", 60)[-1][1] !=
            clip_keys(family, "EnemyAttack", clips["EnemyAttack"])[0][1]):
        raise ValueError("Enemy tell differs from its family attack wind-up")
    guard = clip_keys(family, "Guard", clips["Guard"])
    if guard[0][1] != guard[-1][1]:
        raise ValueError("Guard must remain stable")


def validate(config):
    """Validate authored motion before native export; ignore unrelated export keys."""
    required = ("source_version", "fps", "families")
    if not isinstance(config, dict) or any(field not in config for field in required):
        raise ValueError("Motion configuration requires source_version/fps/families")
    if config["source_version"] != VERSION:
        raise ValueError("Pilot motion source and export configuration differ")
    fps = config["fps"]
    if not isinstance(fps, int) or isinstance(fps, bool) or fps != 100:
        raise ValueError("Pilot motion requires 100 integer frames per second")
    entries = config["families"]
    if not isinstance(entries, list) or len(entries) != len(_TIMING):
        raise ValueError("Pilot configuration requires both Axe and Mace families")
    families = [_validate_entry(entry) for entry in entries]
    if len(set(families)) != len(families) or set(families) != set(_TIMING):
        raise ValueError("Pilot family identities must be unique and complete")
    for family in families:
        _validate_clips(family)
    return {"source_version": VERSION, "clips": 13, "fps": 100,
            "families": sorted(families), "status": "PASS",
            "upstream_motion_sha256": SOURCE_MOTION_SHA256}


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--check", action="store_true", required=True)
    parser.parse_args()
    config = json.loads(Path(__file__).with_name("weapon-pilot-export.json").read_text())
    print(json.dumps(validate(config), sort_keys=True))
