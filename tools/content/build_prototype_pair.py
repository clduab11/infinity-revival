"""Original FWR prototype actors. Blender 5.x: main(Unity_project_root).

Run with Blender --background --python THIS_FILE -- --root PROJECT_ROOT.
Existing scenes/objects survive. Existing deliverables require --overwrite or
main(root, allow_overwrite=True). Geometry is original procedural prototype art.
"""
import argparse
import hashlib
import importlib.util
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Vector

VERSION = "prototype-pair-1.0.3"
# Facing -Y with Z up, anatomical right is -X.
SIDES = (("Left", 1), ("Right", -1))


def config_path(root):
    script = globals().get("__file__")
    candidates = [Path(script).with_name("prototype-export.json")] if script else []
    candidates += [p / "tools/content/prototype-export.json" for p in (root, *root.parents)]
    return next((p for p in candidates if p.is_file()), None)


def bone_spec():
    specs = [
        ("Hips", None, (0, 0, .91), (0, 0, 1.01)),
        ("Spine", "Hips", (0, 0, 1.01), (0, 0, 1.19)),
        ("Chest", "Spine", (0, 0, 1.19), (0, 0, 1.40)),
        ("Neck", "Chest", (0, 0, 1.40), (0, 0, 1.53)),
        ("Head", "Neck", (0, 0, 1.53), (0, 0, 1.78)),
    ]
    for side, sign in SIDES:
        specs += [
            (side + "Shoulder", "Chest", (sign * .04, 0, 1.40), (sign * .18, 0, 1.40)),
            (side + "UpperArm", side + "Shoulder", (sign * .18, 0, 1.40), (sign * .45, 0, 1.40)),
            (side + "LowerArm", side + "UpperArm", (sign * .45, 0, 1.40), (sign * .70, 0, 1.40)),
            (side + "Hand", side + "LowerArm", (sign * .70, 0, 1.40), (sign * .83, 0, 1.40)),
            (side + "UpperLeg", "Hips", (sign * .10, 0, .91), (sign * .10, 0, .50)),
            (side + "LowerLeg", side + "UpperLeg", (sign * .10, 0, .50), (sign * .10, 0, .12)),
            (side + "Foot", side + "LowerLeg", (sign * .10, 0, .12), (sign * .10, -.14, .05)),
            (side + "Toes", side + "Foot", (sign * .10, -.14, .05), (sign * .10, -.24, .05)),
        ]
    specs += [("WeaponSocket", "RightHand", (-.79, 0, 1.40), (-.79, 0, 1.46)),
              ("ShieldSocket", "LeftHand", (.79, 0, 1.40), (.79, -.06, 1.40))]
    return specs


def make_rig(scene, name):
    rig = bpy.data.objects.new(name + "_Rig", bpy.data.armatures.new(name + "_Skeleton"))
    scene.collection.objects.link(rig)
    scene.view_layers[0].objects.active = rig
    rig.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    for name, parent, head, tail in bone_spec():
        bone = rig.data.edit_bones.new(name)
        bone.head, bone.tail = head, tail
        if parent:
            bone.parent = rig.data.edit_bones[parent]
            bone.use_connect = (bone.head - bone.parent.tail).length < .00001
        bone.align_roll(Vector((0, -1, 0)) if name != "ShieldSocket" else Vector((0, 0, 1)))
        bone.use_deform = not name.endswith("Socket")
    bpy.ops.object.mode_set(mode="OBJECT")
    rig.select_set(False)
    rig.show_in_front = True
    rig.data.display_type = "STICK"
    for bone in rig.pose.bones:
        bone.rotation_mode = "XYZ"
    return rig


def materials():
    result = []
    for name, color, metal, rough in [
        ("Ceramic", (.72, .69, .58, 1), .12, .40),
        ("Brass", (.39, .23, .065, 1), .78, .32),
        ("Cloth", (.028, .065, .10, 1), 0, .82),
    ]:
        mat = bpy.data.materials.new("FWR_" + name)
        mat.diffuse_color = color
        mat.use_nodes = True
        shader = mat.node_tree.nodes.get("Principled BSDF")
        for key, value in (("Base Color", color), ("Metallic", metal), ("Roughness", rough)):
            shader.inputs[key].default_value = value
        result.append(mat)
    return result


def blend(a, b, value):
    value = min(1.0, max(0.0, value))
    return {a: 1 - value, b: value}


class Geometry:
    def __init__(self, segments):
        self.n = segments
        self.vertices, self.faces, self.weights, self.mats, self.uv = [], [], [], [], []

    def vertex(self, point, weights, uv):
        weights = {b: max(0, w) for b, w in weights.items() if w > .000001}
        total = sum(weights.values())
        assert total > 0 and len(weights) <= 4
        self.vertices.append(tuple(point))
        self.weights.append({b: w / total for b, w in weights.items()})
        self.uv.append(uv)
        return len(self.vertices) - 1

    def face(self, indices, material):
        self.faces.append(tuple(indices))
        self.mats.append(material)

    def loft(self, center, axis, rings, material, weight_fn):
        start = len(self.vertices)
        reverse = (rings[-1][0] < rings[0][0]) != (axis == 1)
        for row, (distance, ra, rb) in enumerate(rings):
            for column in range(self.n):
                angle = math.tau * column / self.n
                radial = (ra * math.cos(angle), rb * math.sin(angle))
                point = list(center)
                point[axis] += distance
                indices = [i for i in range(3) if i != axis]
                point[indices[0]] += radial[0]
                point[indices[1]] += radial[1]
                self.vertex(point, weight_fn(point), (column / self.n, row / (len(rings) - 1)))
        for row in range(len(rings) - 1):
            for column in range(self.n):
                a = start + row * self.n + column
                b = start + row * self.n + (column + 1) % self.n
                face = (a, b, b + self.n, a + self.n)
                self.face(tuple(reversed(face)) if reverse else face, material)
        bottom = tuple(start + i for i in reversed(range(self.n)))
        top = tuple(start + (len(rings) - 1) * self.n + i for i in range(self.n))
        self.face(tuple(reversed(bottom)) if reverse else bottom, material)
        self.face(tuple(reversed(top)) if reverse else top, material)

    def box(self, center, size, material, weight_fn):
        start = len(self.vertices)
        for x, y, z in ((-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),
                         (-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)):
            point = tuple(c + s * sign / 2 for c, s, sign in zip(center, size, (x,y,z)))
            self.vertex(point, weight_fn(point), ((x + 1) / 2, (z + 1) / 2))
        for face in ((0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)):
            self.face(tuple(start + i for i in face), material)

    def object(self, scene, name, rig, mats):
        mesh = bpy.data.meshes.new(name + "_Mesh")
        mesh.from_pydata(self.vertices, [], self.faces)
        mesh.update()
        obj = bpy.data.objects.new(name, mesh)
        scene.collection.objects.link(obj)
        for mat in mats:
            mesh.materials.append(mat)
        uv_layer = mesh.uv_layers.new(name="UVMap")
        for poly, material in zip(mesh.polygons, self.mats):
            poly.material_index = material
            for loop in poly.loop_indices:
                uv_layer.data[loop].uv = self.uv[mesh.loops[loop].vertex_index]
        groups = {b: obj.vertex_groups.new(name=b) for b in {b for w in self.weights for b in w}}
        for index, weights in enumerate(self.weights):
            for bone, value in weights.items():
                groups[bone].add([index], value, "REPLACE")
        modifier = obj.modifiers.new("SharedSkeleton", "ARMATURE")
        modifier.object = rig
        obj.parent = rig
        mesh.calc_loop_triangles()
        return obj


def torso_weights(point):
    z = point[2]
    return blend("Hips", "Spine", (z - .92) / .16) if z < 1.08 else blend("Spine", "Chest", (z - 1.08) / .22)


def body_geometry(n, captain):
    g = Geometry(n)
    bulk = 1.18 if captain else 1.0
    fixed = lambda name: lambda p: {name: 1}
    g.loft((0,0,0), 2, [(.89,.13,.095),(1.00,.14,.10),(1.12,.15,.105),
           (1.25,.18,.115),(1.39,.19,.10)], 2, torso_weights)
    g.loft((0,0,0), 2, [(1.15,.15*bulk,.115),(1.24,.185*bulk,.125),
           (1.36,.195*bulk,.115),(1.40,.16*bulk,.10)], 0, torso_weights)
    g.loft((0,0,0), 2, [(1.13,.153*bulk,.118),(1.17,.158*bulk,.12)], 1, torso_weights)
    g.loft((0,0,0), 2, [(1.40,.05,.048),(1.53,.052,.05)], 2, fixed("Neck"))
    g.loft((0,0,0), 2, [(1.51,.07,.075),(1.57,.10,.10),(1.72,.105,.10),
           (1.80,.075,.07)], 0, fixed("Head"))
    g.box((0,-.097,1.666), (.16,.016,.046), 2, fixed("Head"))
    g.box((0,-.109,1.666), (.015,.018,.064), 1, fixed("Head"))
    for side, sign in SIDES:
        arm_weight = lambda p, s=side: blend(s+"UpperArm", s+"LowerArm", (abs(p[0])-.40)/.10)
        g.loft((0,0,1.40), 0, [(sign*x, ry, rz) for x,ry,rz in
               ((.18,.065,.07),(.30,.064,.068),(.43,.052,.054),(.48,.05,.052),
                (.59,.044,.047),(.70,.037,.039))], 2, arm_weight)
        g.loft((0,0,1.40), 0, [(sign*.17,.081*bulk,.10*bulk),
               (sign*.23,.097*bulk,.11*bulk),(sign*.31,.07*bulk,.085*bulk)], 0, fixed(side+"UpperArm"))
        g.loft((0,0,1.40), 0, [(sign*.51,.056,.06),(sign*.65,.046,.052)], 0, fixed(side+"LowerArm"))
        g.loft((0,0,1.40), 0, [(sign*.70,.04,.038),(sign*.80,.043,.04),
               (sign*.84,.031,.032)], 2, fixed(side+"Hand"))
        leg_weight = lambda p, s=side: blend(s+"LowerLeg", s+"UpperLeg", (p[2]-.44)/.12)
        g.loft((sign*.10,0,0), 2, [(.13,.047,.05),(.32,.055,.063),(.48,.055,.06),
               (.54,.064,.07),(.72,.074,.09),(.91,.078,.09)], 2, leg_weight)
        g.loft((sign*.10,0,0), 2, [(.19,.060,.069),(.40,.064,.074)], 0, fixed(side+"LowerLeg"))
        g.loft((sign*.10,0,0), 2, [(.56,.075*bulk,.084),(.83,.088*bulk,.102)], 0, fixed(side+"UpperLeg"))
        foot_weight = lambda p, s=side: blend(s+"Foot", s+"Toes", (-p[1]-.12)/.08)
        g.box((sign*.10,-.072,.065), (.14,.29,.13), 2, foot_weight)
    if captain:
        g.box((0,.005,1.785), (.045,.19,.028), 1, fixed("Head"))
        cape(g)
    return g


def cape(g):
    start = len(g.vertices)
    rows = [(1.37,.17,.13),(1.19,.20,.17),(1.03,.235,.20),(.87,.26,.23)]
    for row, (z, width, y) in enumerate(rows):
        for column in range(5):
            x = width * (column / 2 - 1)
            g.vertex((x,y+.016*math.cos(column*math.pi),z), torso_weights((x,y,z)), (column/4,row/3))
    for row in range(3):
        for column in range(4):
            a = start + row*5 + column
            g.face((a,a+1,a+6,a+5), 2)
            g.face((a+5,a+6,a+1,a), 2)


def equipment(n):
    sword, shield = Geometry(n), Geometry(n)
    hand = lambda p: {"RightHand": 1}
    left = lambda p: {"LeftHand": 1}
    sword.loft((-.79,0,0), 2, [(1.32,.017,.017),(1.45,.017,.017)], 2, hand)
    sword.box((-.79,0,1.46), (.17,.04,.035), 1, hand)
    sword.loft((-.79,0,0), 2, [(1.48,.036,.009),(1.94,.032,.008),(2.055,.001,.001)], 0, hand)
    sword.loft((-.79,0,0), 2, [(1.30,.026,.026),(1.33,.026,.026)], 1, hand)
    shield.loft((.79,0,1.40), 1, [(-.065,.20,.235),(-.03,.20,.235)], 0, left)
    shield.loft((.79,0,1.40), 1, [(-.072,.205,.24),(-.062,.205,.24)], 1, left)
    shield.loft((.79,0,1.40), 1, [(-.095,.055,.06),(-.075,.068,.074)], 1, left)
    return sword, shield


def marker(scene, rig, name, bone, point):
    obj = bpy.data.objects.new(name, None)
    scene.collection.objects.link(obj)
    obj.empty_display_type, obj.empty_display_size = "ARROWS", .035
    obj.parent, obj.parent_type, obj.parent_bone = rig, "BONE", bone
    parent = rig.matrix_world @ rig.data.bones[bone].matrix_local @ Matrix.Translation((0,rig.data.bones[bone].length,0))
    obj.matrix_parent_inverse = parent.inverted()
    obj.matrix_basis = Matrix.Translation(point)
    return obj


def load_motion(cfg_file):
    path = cfg_file.with_name("prototype_motion.py")
    spec = importlib.util.spec_from_file_location("fwr_prototype_motion", path)
    motion = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(motion)
    return motion, path


def animate(rig, cfg, motion):
    animation = rig.animation_data_create()
    cursor, timings = 1, {}
    for clip, duration in cfg["clips"].items():
        action = bpy.data.actions.new(rig.name.replace("_Rig", "") + "|" + clip)
        animation.action = action
        for frame, pose in motion.clip_keys(clip, duration):
            for bone in rig.pose.bones:
                bone.rotation_euler = tuple(math.radians(v) for v in pose.get(bone.name, (0,0,0)))
                bone.keyframe_insert(data_path="rotation_euler", frame=frame, group=bone.name)
        if action.slots:
            animation.action_slot = action.slots[0]
        track = animation.nla_tracks.new()
        track.name = clip
        strip = track.strips.new(clip, cursor, action)
        if action.slots:
            strip.action_slot = action.slots[0]
        strip.action_frame_start, strip.action_frame_end = 0, duration
        strip.frame_start, strip.frame_end = cursor, cursor + duration
        strip.extrapolation, strip.blend_type = "NOTHING", "REPLACE"
        strip.use_auto_blend = False
        timings[clip] = {"duration_frames": duration, "duration_seconds": duration/cfg["fps"],
                         "action_frames": [0,duration], "nla_frames": [cursor,cursor+duration]}
        if clip.startswith("Cut") or clip == "EnemyAttack":
            timings[clip]["impact_frame"] = cfg["cut_impact_frame"]
        cursor += duration + 10
    animation.action = None
    return timings, cursor


def preview(scene):
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y, scene.render.resolution_percentage = 1080, 1440, 100
    world = bpy.data.worlds.new("FWR_PrototypeWorld")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (.025,.03,.04,1)
    world.node_tree.nodes["Background"].inputs[1].default_value = .45
    scene.world = world
    camera = bpy.data.objects.new("FWR_PortraitCamera", bpy.data.cameras.new("FWR_PortraitCamera"))
    scene.collection.objects.link(camera)
    camera.location = (3,-7,3)
    camera.rotation_euler = (Vector((0,0,1.03))-camera.location).to_track_quat("-Z","Y").to_euler()
    camera.data.type, camera.data.ortho_scale = "ORTHO", 4.5
    scene.camera = camera
    for name, position, power, size in (("Key",(-3,-4,5),650,4),("Fill",(4,-1,3),450,3),("Rim",(0,3,4),850,3)):
        light = bpy.data.objects.new("FWR_"+name, bpy.data.lights.new("FWR_"+name,"AREA"))
        scene.collection.objects.link(light)
        light.location = position
        light.rotation_euler = (Vector((0,0,1))-light.location).to_track_quat("-Z","Y").to_euler()
        light.data.energy, light.data.shape, light.data.size = power, "DISK", size


def export_actor(scene, rig, lods, markers, root, name, cfg):
    original = rig.matrix_world.copy()
    pose_position = rig.data.pose_position
    frame = scene.frame_current
    # No NLA strip owns frame zero. Export the same rest channels at every LOD,
    # then let the FBX baker sample the named strips at their authored frames.
    scene.frame_set(0)
    for bone in rig.pose.bones:
        bone.rotation_euler = (0, 0, 0)
    rig.matrix_world = Matrix.Identity(4)
    for level, meshes in enumerate(lods):
        for obj in scene.objects:
            obj.select_set(False)
        selection = [rig, *meshes, *markers]
        for obj in selection:
            obj.hide_set(False)
            obj.select_set(True)
        scene.view_layers[0].objects.active = rig
        rig.data.pose_position = "POSE" if level == 0 else "REST"
        scene.view_layers[0].update()
        bpy.ops.export_scene.fbx(filepath=str(root / cfg["export_directory"] / f"{name}_LOD{level}.fbx"),
            use_selection=True, object_types={"MESH","ARMATURE","EMPTY"}, global_scale=1.0,
            apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z", axis_up="Y",
            use_space_transform=True, bake_space_transform=False, add_leaf_bones=False,
            armature_nodetype="NULL", use_armature_deform_only=False, mesh_smooth_type="FACE",
            use_mesh_modifiers=True, use_triangles=True, path_mode="AUTO", embed_textures=False,
            bake_anim=(level==0), bake_anim_use_all_bones=True, bake_anim_use_all_actions=False,
            bake_anim_use_nla_strips=True, bake_anim_force_startend_keying=True,
            bake_anim_step=1.0, bake_anim_simplify_factor=0.0)
        for obj in meshes:
            obj.hide_set(level != 0)
    rig.matrix_world, rig.data.pose_position = original, pose_position
    scene.frame_set(frame)
    for obj in scene.objects:
        obj.select_set(False)


def digest(path):
    h = hashlib.sha256()
    with path.open("rb") as file:
        for block in iter(lambda: file.read(1024*1024), b""):
            h.update(block)
    return h.hexdigest()


def main(root, allow_overwrite=False):
    root = Path(root).expanduser().resolve()
    cfg_file = config_path(root)
    if cfg_file is None or not (root / "Assets").is_dir():
        raise ValueError("Provide the Unity project directory containing Assets and locate prototype-export.json beside this script.")
    cfg = json.loads(cfg_file.read_text(encoding="utf-8"))
    assert cfg["source_version"] == VERSION and cfg["fps"] == 100
    motion, motion_file = load_motion(cfg_file)
    motion.validate(cfg)
    targets = [root/cfg["source_file"], root/cfg["source_manifest"]]
    targets += [root/cfg["export_directory"]/f"{name}_LOD{i}.fbx" for name in cfg["characters"] for i in range(3)]
    existing = [str(p) for p in targets if p.exists()]
    if existing and not allow_overwrite:
        raise FileExistsError("Explicit allow_overwrite=True required for existing deliverables: " + ", ".join(existing))
    for target in targets:
        target.parent.mkdir(parents=True, exist_ok=True)
    scene = bpy.data.scenes.new(cfg["scene"])
    if bpy.context.window:
        bpy.context.window.scene = scene
    scene.unit_settings.system, scene.unit_settings.scale_length = "METRIC", 1.0
    scene.render.fps, scene.render.fps_base = cfg["fps"], 1.0
    report = {"schema_version":1,"source_version":VERSION,"blender_version":bpy.app.version_string,
              "units":"meters","height_m":1.8,"forward":"-Y","up":"Z","bones":bone_spec(),
              "characters":{},"original_art":True,"external_textures":False}
    with bpy.context.temp_override(scene=scene, view_layer=scene.view_layers[0]):
        mats = materials()
        for index, name in enumerate(cfg["characters"]):
            rig = make_rig(scene,name)
            lods = []
            for level,n in enumerate(cfg["lod_segments"]):
                parts = [body_geometry(n,name=="Captain"),*equipment(n)]
                meshes = [g.object(scene,f"{name}_LOD{level}_{part}",rig,mats) for g,part in zip(parts,("Body","Sword","Shield"))]
                for obj in meshes:
                    obj.hide_render = level != 0
                    obj.hide_set(level != 0)
                lods.append(meshes)
            markers = [marker(scene,rig,name+"_WeaponTip","WeaponSocket",(-.79,0,2.055)),
                       marker(scene,rig,name+"_WeaponSocket","WeaponSocket",(-.79,0,1.40)),
                       marker(scene,rig,name+"_ShieldSocket","ShieldSocket",(.79,0,1.40))]
            timing,end = animate(rig,cfg,motion)
            rig.location.x = -.62 if index == 0 else .62
            scene.frame_start, scene.frame_end = 1, max(scene.frame_end,end)
            scene.frame_set(1)
            export_actor(scene,rig,lods,markers,root,name,cfg)
            report["characters"][name] = {"rig":rig.name,"clips":timing,"markers":[o.name for o in markers],
                "lods":[{"file":f"{name}_LOD{i}.fbx","vertices":sum(len(o.data.vertices) for o in objects),
                         "triangles":sum(len(o.data.loop_triangles) for o in objects),"max_influences":max(len(v.groups) for o in objects for v in o.data.vertices)} for i,objects in enumerate(lods)]}
        preview(scene)
        scene.frame_set(1)
        bpy.ops.wm.save_as_mainfile(filepath=str(root/cfg["source_file"]))
    report["configuration"] = cfg
    report["sha256"] = {str(p.relative_to(root)):digest(p) for p in targets if p.is_file() and p != root/cfg["source_manifest"]}
    report["recipe_sha256"] = digest(Path(__file__)) if globals().get("__file__") and Path(__file__).is_file() else None
    report["config_sha256"] = digest(cfg_file)
    report["motion_recipe_sha256"] = digest(motion_file)
    (root/cfg["source_manifest"]).write_text(json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print(json.dumps({"ready":True,"scene":scene.name,"project_root":str(root),"manifest":cfg["source_manifest"]}))
    return report


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--root",required=True)
    parser.add_argument("--overwrite",action="store_true")
    args = parser.parse_args(sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else [])
    main(args.root,args.overwrite)
