"""Original sword-and-shield motion recipe, independent of Blender.

Rotations are degrees in the existing rig's XYZ bone-local axes. Torso local Y
is world Z (yaw). Right arm X points up, left arm X points down; both arm Z
axes point backward. Facing -Y with Z up makes anatomical right -X. The raw
authoring tables retain their prior basis; clip_keys reflects their local poses
for the corrected rig. Clip names, durations and contact times stay unchanged.
Run this file with --check to validate the source contract without importing bpy.
"""
import argparse
import json
import math
from pathlib import Path

VERSION = "prototype-pair-1.0.3"
CLIPS = {"Idle": 100, "Guard": 100, "Parry": 30, "DodgeLeft": 36,
         "DodgeRight": 36, "CutUp": 50, "CutDown": 50, "CutLeft": 50,
         "CutRight": 50, "Hit": 30, "Death": 100, "EnemyTell": 60,
         "EnemyAttack": 50}


def base_pose():
    """Elbows bent, sword raised in front, shield face turned toward the threat."""
    return {"Spine": (3, 0, 0), "Chest": (2, 0, 0),
            "RightUpperArm": (24, 5, -52), "RightLowerArm": (42, 15, 42),
            "RightHand": (-8, 12, 4), "LeftUpperArm": (30, 0, 48),
            "LeftLowerArm": (35, 0, -45), "LeftHand": (-30, 0, 12)}


def guard_pose():
    return dict(base_pose(), LeftUpperArm=(45, 0, 30),
                LeftLowerArm=(35, 0, -38), LeftHand=(-40, 0, 8),
                RightUpperArm=(35, 5, -35), RightLowerArm=(45, 10, 40),
                RightHand=(-8, 15, 0), Spine=(4, 0, 0), Chest=(3, 0, 0))


def strike_pose(upper, lower, wrist, yaw, lean=0):
    """Small hip lead, ribcage turn and a supported shield during each cut."""
    return dict(base_pose(), Hips=(1, yaw * .25, lean * .15),
                Spine=(3, yaw * .30, -lean * .10),
                Chest=(4, yaw * .45, lean * .15),
                RightShoulder=(3, 0, -3), RightUpperArm=upper,
                RightLowerArm=lower, RightHand=wrist,
                LeftUpperArm=(38, 0, 38), LeftLowerArm=(35, 0, -40),
                LeftHand=(-36, 0, 10), Head=(0, -yaw * .20, 0))


def cut_keys(name):
    # Each row is upper arm, forearm, wrist, whole-body yaw, lateral brace.
    # Frame 10 is contact, with nonzero elbow flexion. Frames 14 and 22 continue
    # the arc instead of freezing at contact. Wrist roll follows forearm roll.
    rows = {
        # Reflecting both the anatomical rig and local poses restores these
        # original directional names after FBX forward-axis conversion.
        "CutLeft": [
            ((0, 0, 20), (25, 45, 20), (0, 40, 0), 16, 2),
            ((-8, 0, 5), (32, 40, 18), (0, 36, -4), 20, 3),
            ((40, 0, -15), (18, 45, 20), (0, 40, 0), -4, -2),
            ((65, 0, -10), (20, 48, 22), (0, 40, 4), -12, -3),
            ((94, 0, 5), (30, 45, 25), (0, 32, 8), -20, -3)],
        "CutRight": [
            ((94, 0, 5), (30, 45, 25), (0, 32, 8), -20, -3),
            ((106, 0, 5), (35, 40, 25), (0, 34, 10), -24, -4),
            ((40, 0, -15), (18, 45, 20), (0, 40, 0), 4, 2),
            ((16, 0, 20), (22, 48, 20), (0, 38, -4), 12, 3),
            ((-8, 0, 5), (32, 45, 18), (0, 28, -8), 20, 3)],
        "CutDown": [
            ((25, 0, 55), (25, 0, -65), (0, 0, 0), 10, 0),
            ((22, 0, 62), (32, 0, -72), (0, 2, -4), 14, 0),
            ((40, 0, 15), (25, 45, -8), (0, 40, 0), -2, 0),
            ((45, 0, -15), (25, 60, -8), (0, 55, 0), -6, 0),
            ((45, 0, -40), (30, 75, 10), (0, 55, 0), -10, 0)],
        "CutUp": [
            ((15, 0, -60), (30, 75, 10), (0, 55, 0), -10, 0),
            ((10, 0, -65), (35, 70, 8), (0, 50, -4), -14, 0),
            ((40, 0, -10), (25, 45, 10), (0, 40, 0), 2, 0),
            ((35, 0, 20), (25, 20, -25), (0, 20, 0), 6, 0),
            ((35, 0, 50), (25, 0, -65), (0, 0, 0), 10, 0)],
    }
    keys = [(frame, strike_pose(*row)) for frame, row in
            zip((0, 4, 10, 14, 22), rows[name])]
    # Relax the blade and elbow before settling into the same ready pose used
    # by Idle, allowing a clean subsequent blend without a straight-arm snap.
    settle = dict(base_pose(), RightUpperArm=(32, 8, -36),
                  RightLowerArm=(40, 15, 34), RightHand=(-5, 12, 3))
    return keys + [(34, settle), (50, base_pose())]


def _authored_keys(name, duration):
    if name not in CLIPS or duration != CLIPS[name]:
        raise ValueError("Unknown clip or changed timing contract: " + str(name))
    base, guard = base_pose(), guard_pose()
    if name == "Idle":
        breathe = dict(base, Chest=(3, 0, 0), Spine=(3.5, 0, 0))
        return [(0, base), (50, breathe), (100, base)]
    if name == "Guard":
        return [(0, guard), (100, guard)]
    if name == "Parry":
        intercept = dict(guard, LeftUpperArm=(65, -8, 8),
                         LeftLowerArm=(30, 0, -25), LeftHand=(-65, 0, 5),
                         Hips=(1, 2, 0), Spine=(4, 3, 0), Chest=(3, 5, 0))
        follow = dict(intercept, LeftUpperArm=(72, -8, 6),
                      LeftLowerArm=(35, 0, -24), Chest=(4, 7, 0))
        return [(0, base), (4, guard), (8, intercept), (12, follow),
                (20, guard), (30, base)]
    if name.startswith("Dodge"):
        side = 1 if name == "DodgeLeft" else -1
        lean = dict(guard, Hips=(1, 0, side * 14),
                    Spine=(3, 0, side * 8), Chest=(3, 0, -side * 7),
                    Head=(0, 0, -side * 9))
        deepest = dict(lean, Hips=(2, 0, side * 17),
                       Spine=(4, 0, side * 7), Head=(0, 0, -side * 11))
        return [(0, base), (8, lean), (16, deepest), (25, lean), (36, base)]
    if name.startswith("Cut") or name == "EnemyAttack":
        return cut_keys("CutDown" if name == "EnemyAttack" else name)
    if name == "EnemyTell":
        wind = cut_keys("CutDown")[0][1]
        gathering = dict(guard, Chest=(3, 4, 0), RightUpperArm=(26, 0, 18),
                         RightLowerArm=(30, 0, -30), RightHand=(0, 8, 0))
        return [(0, guard), (18, gathering), (36, wind), (60, wind)]
    if name == "Hit":
        braced = dict(base, LeftUpperArm=guard["LeftUpperArm"],
                      LeftLowerArm=guard["LeftLowerArm"],
                      LeftHand=guard["LeftHand"])
        recoil = dict(braced, Hips=(-3, 0, 2), Spine=(-8, 0, 3),
                      Chest=(-7, 0, -2), Head=(-5, 0, 0),
                      LeftUpperArm=(39, 0, 34), LeftLowerArm=(40, 0, -40))
        follow = dict(recoil, Spine=(-5, 0, 2), Chest=(-4, 0, -1))
        return [(0, braced), (6, recoil), (12, follow), (20, braced), (30, base)]
    fallen = dict(base, Hips=(22, 0, 8), Spine=(48, 0, 15),
                  Chest=(40, 0, 0), Head=(20, 0, 0),
                  LeftUpperLeg=(-25, 0, -8), RightUpperLeg=(-25, 0, 8),
                  LeftLowerLeg=(55, 0, 0), RightLowerLeg=(55, 0, 0))
    return [(0, base), (20, dict(base, Chest=(-20, 0, 0))),
            (60, fallen), (100, fallen)]


def reflect_pose(pose):
    """Conjugate XYZ rotations by local lateral reflection S=diag(-1,1,1).

    Reflecting the rig in world X changes each bind basis by M * bind * S.
    The matching local rotation S * Euler * S is (x, -y, -z), including torso
    yaw, so joint flexion and shield orientation survive the chirality repair.
    """
    return {bone: (x, -y, -z) for bone, (x, y, z) in pose.items()}


def clip_keys(name, duration):
    return [(frame, reflect_pose(pose)) for frame, pose in _authored_keys(name, duration)]


def validate(config):
    """Check the independent source contract before expensive native export."""
    if config["source_version"] != VERSION or config["clips"] != CLIPS:
        raise ValueError("Motion source and export configuration differ")
    if config["fps"] != 100 or config["cut_impact_frame"] != 10:
        raise ValueError("Combat contact must remain at 100 ms")
    for name, duration in CLIPS.items():
        keys = clip_keys(name, duration)
        frames = [frame for frame, _ in keys]
        if frames != sorted(set(frames)) or frames[0] != 0 or frames[-1] != duration:
            raise ValueError("Invalid key range: " + name)
        for _, pose in keys:
            for degrees in pose.values():
                if len(degrees) != 3 or not all(math.isfinite(v) for v in degrees):
                    raise ValueError("Invalid bone rotation: " + name)
        if name.startswith("Cut") or name == "EnemyAttack":
            if not 6 <= len(keys) <= 8 or 10 not in frames:
                raise ValueError("Cut requires contact and continuing recovery keys")
            contact = dict(keys)[10]
            if contact["RightLowerArm"] == (0, 0, 0) or contact == dict(keys)[14]:
                raise ValueError("Contact must retain elbow flexion and follow-through")
            if keys[-1][1] != reflect_pose(base_pose()):
                raise ValueError("Cut must settle into ready")
    if clip_keys("EnemyTell", 60)[-1][1] != clip_keys("EnemyAttack", 50)[0][1]:
        raise ValueError("Enemy wind-up seam differs")
    if clip_keys("Guard", 100)[0][1] != clip_keys("Guard", 100)[-1][1]:
        raise ValueError("Guard must be stable")
    return {"source_version": VERSION, "clips": len(CLIPS), "fps": 100,
            "contact_frame": 10, "cuts": 5, "status": "PASS"}


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--check", action="store_true", required=True)
    parser.parse_args()
    config = json.loads(Path(__file__).with_name("prototype-export.json").read_text())
    print(json.dumps(validate(config), sort_keys=True))
