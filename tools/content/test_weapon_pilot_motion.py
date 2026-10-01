"""Pure source-contract checks, separate from native Humanoid acceptance."""
import copy
import hashlib
import math
from pathlib import Path
import unittest
from unittest.mock import patch

import prototype_motion as accepted
import weapon_pilot_motion as pilot


def configuration():
    return {"source_version": "weapon-family-pilot-1.0.0", "fps": 100,
            "families": [
                {"family": "Axe", "actor": "AxePilot", "contact_frame": 15,
                 "duration_frames": 60, "release_frame": 9},
                {"family": "Mace", "actor": "MacePilot", "contact_frame": 18,
                 "duration_frames": 70, "release_frame": 10}]}


def matrix_product(a, b):
    return tuple(tuple(sum(a[i][k] * b[k][j] for k in range(3))
                       for j in range(3)) for i in range(3))


def transform_vector(matrix, vector):
    return tuple(sum(matrix[i][j] * vector[j] for j in range(3))
                 for i in range(3))


def rotation(degrees):
    x, y, z = map(math.radians, degrees)
    cx, sx, cy, sy = math.cos(x), math.sin(x), math.cos(y), math.sin(y)
    cz, sz = math.cos(z), math.sin(z)
    return matrix_product(((cz, -sz, 0), (sz, cz, 0), (0, 0, 1)),
                          matrix_product(((cy, 0, sy), (0, 1, 0), (-sy, 0, cy)),
                                         ((1, 0, 0), (0, cx, -sx), (0, sx, cx))))


def contact_position(pose, head_height, lateral=-.09):
    """Independent arm FK in the pinned rig basis, converted to Unity axes.

    This proves authored directional intent without pretending to test the
    native FBX bake or Unity Humanoid retargeting. The sample point is rigid to
    RightHand, with a shorter family head offset than the accepted sword tip.
    """
    torso = ((1, 0, 0), (0, 0, -1), (0, 1, 0))
    arm = ((0, -1, 0), (0, 0, -1), (1, 0, 0))
    identity = ((1, 0, 0), (0, 1, 0), (0, 0, 1))
    bones = [("Hips", (0, 0, .91), torso),
             ("Spine", (0, 0, 1.01), torso),
             ("Chest", (0, 0, 1.19), torso),
             ("RightShoulder", (-.04, 0, 1.4), arm),
             ("RightUpperArm", (-.18, 0, 1.4), arm),
             ("RightLowerArm", (-.45, 0, 1.4), arm),
             ("RightHand", (-.70, 0, 1.4), arm)]
    point, previous = (0, 0, 0), (0, 0, 0)
    basis, bind_previous = identity, identity
    for name, head, bind in bones:
        displacement = tuple(a - b for a, b in zip(head, previous))
        parent_rotation = matrix_product(basis, tuple(zip(*bind_previous)))
        translated = transform_vector(parent_rotation, displacement)
        point = tuple(a + b for a, b in zip(point, translated))
        basis = matrix_product(matrix_product(parent_rotation, bind),
                               rotation(pose.get(name, (0, 0, 0))))
        previous, bind_previous = head, bind
    offset = transform_vector(matrix_product(basis, tuple(zip(*bind_previous))),
                              (lateral, 0, head_height))
    point = tuple(a + b for a, b in zip(point, offset))
    return -point[0], point[2], point[1]


class WeaponPilotMotionTests(unittest.TestCase):
    def test_accepted_source_is_the_pinned_read_only_recipe(self):
        path = Path(__file__).with_name("prototype_motion.py")
        self.assertEqual(hashlib.sha256(path.read_bytes()).hexdigest(),
                         pilot.SOURCE_MOTION_SHA256)
        self.assertEqual(accepted.VERSION, "prototype-pair-1.0.3")

    def test_same_thirteen_keys_preserve_shared_defensive_durations(self):
        for family, duration in (("Axe", 60), ("Mace", 70)):
            clips = pilot.clips_for(family)
            self.assertEqual(set(clips), set(accepted.CLIPS))
            self.assertEqual(len(clips), 13)
            for name, frames in accepted.CLIPS.items():
                expected = duration if name.startswith("Cut") or name == "EnemyAttack" else frames
                self.assertEqual(clips[name], expected)
                if not name.startswith("Cut") and name not in ("EnemyAttack", "EnemyTell"):
                    self.assertEqual(pilot.clip_keys(family, name, frames),
                                     accepted.clip_keys(name, frames))
        clips["Idle"] = 1
        self.assertEqual(pilot.clips_for("Mace")["Idle"], 100)
        self.assertEqual(accepted.CLIPS["CutUp"], 50)

    def test_validation_accepts_complete_config_and_ignores_export_fields(self):
        config = configuration()
        config.update({"fbx": {"axis_up": "Y"}, "export_directory": "unused"})
        result = pilot.validate(config)
        self.assertEqual(result["status"], "PASS")
        self.assertEqual(result["source_version"], pilot.VERSION)
        self.assertEqual(result["clips"], 13)
        self.assertEqual(result["families"], ["Axe", "Mace"])

    def test_cuts_use_accepted_reflection_once(self):
        for family in ("Axe", "Mace"):
            for name, duration in pilot.clips_for(family).items():
                authored = pilot._authored_keys(family, name, duration)
                expected = [(frame, accepted.reflect_pose(pose)) for frame, pose in authored]
                self.assertEqual(pilot.clip_keys(family, name, duration), expected)

    def test_family_head_travel_follows_all_four_swipe_directions(self):
        for family, height, lateral, follow in (("Axe", .48, -.355, 21), ("Mace", .54, -.09, 25)):
            for name in ("CutUp", "CutDown", "CutLeft", "CutRight"):
                with self.subTest(family=family, cut=name):
                    poses = dict(pilot.clip_keys(family, name, pilot.clips_for(family)[name]))
                    start = contact_position(poses[0], height, lateral)
                    continued = contact_position(poses[follow], height, lateral)
                    travel = tuple(b - a for a, b in zip(start, continued))
                    axis, sign = {"CutUp": (1, 1), "CutDown": (1, -1),
                                  "CutLeft": (0, -1), "CutRight": (0, 1)}[name]
                    self.assertGreater(travel[axis] * sign, .1)

    def test_contacts_keep_flexed_elbows_and_moving_recovery(self):
        for entry in configuration()["families"]:
            family = entry["family"]
            for name in ("CutUp", "CutDown", "CutLeft", "CutRight", "EnemyAttack"):
                keys = pilot.clip_keys(family, name, entry["duration_frames"])
                poses = dict(keys)
                frames = list(poses)
                self.assertIn(entry["release_frame"], frames)
                self.assertIn(entry["contact_frame"], frames)
                after = [frame for frame in frames if entry["contact_frame"] < frame < frames[-1]]
                self.assertGreaterEqual(len(after), 2)
                self.assertNotEqual(poses[entry["contact_frame"]]["RightLowerArm"], (0, 0, 0))
                contact = contact_position(poses[entry["contact_frame"]], .45)
                for frame in after[:2]:
                    recovery = contact_position(poses[frame], .45)
                    self.assertGreater(math.dist(contact, recovery), .025)
                self.assertEqual(keys[-1][1], accepted.reflect_pose(accepted.base_pose()))

    def test_family_tables_are_distinct_from_each_other_and_sword(self):
        for name in ("CutUp", "CutDown", "CutLeft", "CutRight"):
            axe = pilot.clip_keys("Axe", name, 60)
            mace = pilot.clip_keys("Mace", name, 70)
            sword = accepted.clip_keys(name, 50)
            self.assertNotEqual([pose for _, pose in axe], [pose for _, pose in mace])
            self.assertNotEqual([pose for _, pose in axe], [pose for _, pose in sword])
            self.assertNotEqual([pose for _, pose in mace], [pose for _, pose in sword])
        for family in ("Axe", "Mace"):
            contacts = [dict(pilot.clip_keys(family, name, pilot.clips_for(family)[name]))
                        [15 if family == "Axe" else 18] for name in
                        ("CutUp", "CutDown", "CutLeft", "CutRight")]
            self.assertEqual(len({repr(pose) for pose in contacts}), 4)

    def test_enemy_tell_matches_its_family_attack_start(self):
        for family in ("Axe", "Mace"):
            tell = pilot.clip_keys(family, "EnemyTell", 60)
            attack = pilot.clip_keys(family, "EnemyAttack", pilot.clips_for(family)["EnemyAttack"])
            self.assertEqual(tell[-1][1], attack[0][1])
            self.assertNotEqual(tell[-1][1], accepted.clip_keys("EnemyTell", 60)[-1][1])

    def test_bad_configuration_structure_and_identity_are_rejected(self):
        cases = [None, {}, {"source_version": "wrong", "fps": 100, "families": []}]
        for key in ("source_version", "fps", "families"):
            case = configuration()
            del case[key]
            cases.append(case)
        for families in (None, {}, [], [configuration()["families"][0]]):
            case = configuration()
            case["families"] = families
            cases.append(case)
        for family, actor in (("Sword", "SwordPilot"), ("Axe", "MacePilot")):
            case = configuration()
            case["families"][0].update({"family": family, "actor": actor})
            cases.append(case)
        duplicate = configuration()
        duplicate["families"][1] = copy.deepcopy(duplicate["families"][0])
        cases.append(duplicate)
        for case in cases:
            with self.subTest(config=case), self.assertRaises(ValueError):
                pilot.validate(case)

    def test_invalid_and_nonfinite_timing_is_rejected(self):
        for key in ("contact_frame", "duration_frames", "release_frame"):
            for value in (-1, 0, 1, 10**1000, True, 15.0, math.nan, math.inf, "15"):
                case = configuration()
                case["families"][0][key] = value
                with self.subTest(field=key, value=str(value)), self.assertRaises(ValueError):
                    pilot.validate(case)
        for value in (0, 99, True, 100.0, math.nan, math.inf, "100"):
            case = configuration()
            case["fps"] = value
            with self.subTest(fps=value), self.assertRaises(ValueError):
                pilot.validate(case)

    def test_unknown_families_clips_and_wrong_durations_are_rejected(self):
        for family in (None, "Sword", "axe"):
            with self.subTest(family=family), self.assertRaises(ValueError):
                pilot.clips_for(family)
        for name, duration in (("Unknown", 60), ("CutDown", 50),
                               ("CutDown", True), ("CutDown", math.nan)):
            with self.subTest(clip=name, duration=duration), self.assertRaises(ValueError):
                pilot.clip_keys("Axe", name, duration)

    def test_validation_rejects_nonfinite_rotations_before_native_export(self):
        original = pilot._authored_keys
        for value in (math.nan, math.inf, -math.inf, "bad", True):
            def invalid(family, name, duration, rotation_value=value):
                keys = copy.deepcopy(original(family, name, duration))
                keys[0][1]["RightHand"] = (rotation_value, 0, 0)
                return keys
            with self.subTest(rotation=value), patch.object(pilot, "_authored_keys", invalid):
                with self.assertRaises(ValueError):
                    pilot.validate(configuration())


if __name__ == "__main__":
    unittest.main()
