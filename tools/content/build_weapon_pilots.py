"""Original axe/mace pilots, exported in a separate factory Blender process.

Blender --background --factory-startup --python THIS_FILE -- --root UNITY_ROOT.
Accepted sword source, scene, FBXs, metadata and prefabs are read-only upstream.
"""
import argparse
import importlib.util
import json
import math
import sys
from pathlib import Path

import bpy

VERSION = "weapon-family-pilot-1.0.0"
FOLDER = "Assets/Game/Content/Characters/WeaponFamilyPilot"
SOURCE_FOLDER = "SourceArt/Equipment/WeaponFamilyPilot"


def load_module(path, name):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def upstream_snapshot(root, recipe):
    accepted = root / "Assets/Game/Content/Characters/PrototypePair"
    source = root / "SourceArt/Characters"
    paths = [p for folder in (accepted, source) for p in folder.rglob("*") if p.is_file()]
    snapshot = {p.relative_to(root).as_posix(): digest(p) for p in paths}
    for name in ("build_prototype_pair.py", "prototype-export.json", "prototype_motion.py"):
        snapshot["tools/content/" + name] = digest(recipe.with_name(name))
    return snapshot


def digest(path):
    import hashlib
    h = hashlib.sha256()
    with path.open("rb") as source:
        for block in iter(lambda: source.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def validate_upstream(root, cfg, recipe):
    if digest(recipe) != cfg["accepted_recipe_sha256"]:
        raise ValueError("Accepted prototype geometry recipe hash changed")
    if digest(recipe.with_name("prototype_motion.py")) != cfg["accepted_motion_sha256"]:
        raise ValueError("Accepted prototype motion recipe hash changed")
    manifest = json.loads((root / "SourceArt/Characters/prototype-pair-source.json").read_text())
    if manifest["source_version"] != cfg["accepted_source_version"] or not manifest["original_art"]:
        raise ValueError("Accepted original-art source version differs")
    for name, expected in manifest["sha256"].items():
        if digest(root / name.replace("\\", "/")) != expected:
            raise ValueError("Accepted source artifact hash changed: " + name)


def extruded_head(geometry, outline, thickness, material):
    """Original asymmetric axe outline in the XZ plane, rigid to RightHand."""
    start = len(geometry.vertices)
    count = len(outline)
    for y in (-thickness / 2, thickness / 2):
        for x, z in outline:
            geometry.vertex((x, y, z), {"RightHand": 1}, ((x + 1.1) / .4, (z - 1.65) / .4))
    geometry.face(tuple(start + i for i in reversed(range(count))), material)
    geometry.face(tuple(start + count + i for i in range(count)), material)
    for i in range(count):
        next_index = (i + 1) % count
        geometry.face((start + i, start + next_index, start + count + next_index, start + count + i), material)


def equipment(original, segments, family):
    weapon = original.Geometry(segments)
    hand = lambda point: {"RightHand": 1}
    weapon.loft((-.79, 0, 0), 2, [(1.30, .026, .026), (1.33, .026, .026)], 1, hand)
    weapon.loft((-.79, 0, 0), 2, [(1.32, .018, .018), (1.76, .018, .018)], 2, hand)
    if family == "Axe":
        weapon.loft((-.79, 0, 0), 2, [(1.73, .025, .023), (1.88, .025, .023)], 1, hand)
        outline = [(-.84, 1.73), (-1.015, 1.70), (-1.06, 1.76), (-1.06, 1.92),
                   (-.985, 1.99), (-.82, 1.95), (-.735, 1.88), (-.75, 1.77)]
        extruded_head(weapon, outline, .047, 0)
        weapon.box((-.79, 0, 1.835), (.065, .058, .11), 1, hand)
        contact = (-1.055, 0, 1.88)
    elif family == "Mace":
        weapon.loft((-.79, 0, 0), 2, [(1.71, .025, .025), (1.78, .025, .025)], 1, hand)
        weapon.loft((-.79, 0, 0), 2, [(1.76, .045, .042), (1.79, .092, .079),
                    (1.87, .105, .09), (1.92, .065, .054), (1.94, .025, .025)], 0, hand)
        weapon.loft((-.79, 0, 0), 2, [(1.83, .107, .092), (1.845, .107, .092)], 1, hand)
        contact = (-.79, 0, 1.94)
    else:
        raise ValueError("Unsupported original pilot family: " + family)
    _, shield = original.equipment(segments)
    return weapon, shield, contact


class FamilyMotion:
    def __init__(self, motion, family):
        self.motion, self.family = motion, family

    def clip_keys(self, name, duration):
        return self.motion.clip_keys(self.family, name, duration)


def construct_actor(scene, original, motion, cfg, family, mats, root):
    actor, name = family["actor"], family["family"]
    rig = original.make_rig(scene, actor)
    lods, contacts = [], []
    for level, segments in enumerate(cfg["lod_segments"]):
        weapon, shield, contact = equipment(original, segments, name)
        parts = [original.body_geometry(segments, False), weapon, shield]
        meshes = [geometry.object(scene, f"{actor}_LOD{level}_{part}", rig, mats)
                  for geometry, part in zip(parts, ("Body", "Weapon", "Shield"))]
        for mesh in meshes:
            mesh.hide_render = level != 0
            mesh.hide_set(level != 0)
        lods.append(meshes)
        contacts.append(contact)
    if len(set(contacts)) != 1:
        raise ValueError("Pilot LOD contact marker positions differ")
    markers = [original.marker(scene, rig, actor + "_WeaponTip", "WeaponSocket", contacts[0]),
               original.marker(scene, rig, actor + "_WeaponSocket", "WeaponSocket", (-.79, 0, 1.40)),
               original.marker(scene, rig, actor + "_ShieldSocket", "ShieldSocket", (.79, 0, 1.40))]
    actor_cfg = dict(cfg, clips=motion.clips_for(name), cut_impact_frame=family["contact_frame"])
    timing, end = original.animate(rig, actor_cfg, FamilyMotion(motion, name))
    scene.frame_end = max(scene.frame_end, end)
    original.export_actor(scene, rig, lods, markers, root, actor, actor_cfg)
    return rig, {"actor": actor, "rig": rig.name, "clips": timing, "contact_point_blender": contacts[0],
                 "release_frame": family["release_frame"], "markers": [marker.name for marker in markers],
                 "lods": [lod_record(actor, level, meshes) for level, meshes in enumerate(lods)]}


def lod_record(actor, level, meshes):
    return {"file": f"{actor}_LOD{level}.fbx", "vertices": sum(len(mesh.data.vertices) for mesh in meshes),
            "triangles": sum(len(mesh.data.loop_triangles) for mesh in meshes),
            "max_influences": max(len(vertex.groups) for mesh in meshes for vertex in mesh.data.vertices),
            "parts": [mesh.name for mesh in meshes], "weapon_rigid_bone": "RightHand", "shield_rigid_bone": "LeftHand"}


def targets_for(root, cfg):
    expected = {"export_directory": FOLDER, "source_file": SOURCE_FOLDER + "/WeaponFamilyPilot.blend",
                "source_manifest": SOURCE_FOLDER + "/weapon-family-pilot-source.json",
                "source_config": SOURCE_FOLDER + "/weapon-pilot-export.json"}
    if any(cfg[key] != value for key, value in expected.items()):
        raise ValueError("Pilot outputs must remain in their separate exact controlled paths")
    targets = [root / cfg[key] for key in ("source_file", "source_manifest", "source_config")]
    targets += [root / FOLDER / f"{entry['actor']}_LOD{level}.fbx" for entry in cfg["families"] for level in range(3)]
    return targets


def write_manifest(root, cfg, cfg_file, motion_file, original, targets, families, upstream):
    manifest = {"schema_version": 1, "source_version": VERSION, "blender_version": bpy.app.version_string,
                "original_art": True, "external_textures": False, "rig_id": cfg["rig_id"], "units": "meters",
                "forward": "-Y", "up": "Z", "bones": original.bone_spec(), "families": families,
                "configuration": cfg, "upstream_sha256": upstream,
                "sha256": {path.relative_to(root).as_posix(): digest(path) for path in targets
                           if path.is_file() and path != root / cfg["source_manifest"]},
                "recipe_sha256": digest(Path(__file__)), "motion_recipe_sha256": digest(motion_file),
                "config_sha256": digest(cfg_file)}
    (root / cfg["source_manifest"]).write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    return manifest


def main(root, allow_overwrite=False):
    root = Path(root).expanduser().resolve()
    if not (root / "Assets").is_dir():
        raise ValueError("Provide the Unity root containing Assets")
    cfg_file = Path(__file__).with_name("weapon-pilot-export.json")
    cfg = json.loads(cfg_file.read_text(encoding="utf-8"))
    if cfg["source_version"] != VERSION or cfg["lod_segments"] != [12, 8, 5]:
        raise ValueError("Pilot source version or controlled LOD contract differs")
    motion_file = cfg_file.with_name("weapon_pilot_motion.py")
    motion = load_module(motion_file, "fwr_weapon_pilot_motion")
    motion.validate(cfg)
    recipe = cfg_file.with_name("build_prototype_pair.py")
    validate_upstream(root, cfg, recipe)
    upstream = upstream_snapshot(root, recipe)
    targets = targets_for(root, cfg)
    existing = [str(path) for path in targets if path.exists()]
    if existing and not allow_overwrite:
        raise FileExistsError("Pilot deliverables already exist; explicit --overwrite required: " + ", ".join(existing))
    for path in targets:
        path.parent.mkdir(parents=True, exist_ok=True)
    original = load_module(recipe, "fwr_accepted_prototype_geometry")
    scene = bpy.data.scenes.new(cfg["scene"])
    if bpy.context.window:
        bpy.context.window.scene = scene
    scene.unit_settings.system, scene.unit_settings.scale_length = "METRIC", 1.0
    scene.render.fps, scene.render.fps_base = cfg["fps"], 1.0
    families = {}
    with bpy.context.temp_override(scene=scene, view_layer=scene.view_layers[0]):
        mats = original.materials()
        for index, family in enumerate(cfg["families"]):
            rig, record = construct_actor(scene, original, motion, cfg, family, mats, root)
            families[family["family"]] = record
            rig.location.x = -.62 if index == 0 else .62
        original.preview(scene)
        scene.frame_set(1)
        bpy.ops.wm.save_as_mainfile(filepath=str(root / cfg["source_file"]))
    (root / cfg["source_config"]).write_bytes(cfg_file.read_bytes())
    if upstream_snapshot(root, recipe) != upstream:
        raise RuntimeError("Accepted upstream bundle changed during isolated export")
    manifest = write_manifest(root, cfg, cfg_file, motion_file, original, targets, families, upstream)
    print(json.dumps({"ready": True, "source_version": VERSION, "manifest": cfg["source_manifest"],
                      "actors": [entry["actor"] for entry in cfg["families"]]}))
    return manifest


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", required=True)
    parser.add_argument("--overwrite", action="store_true")
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
    main(args.root, args.overwrite)
