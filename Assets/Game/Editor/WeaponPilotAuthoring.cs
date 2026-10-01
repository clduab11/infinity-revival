using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Praxen.Game.Editor
{
    public static class WeaponPilotAuthoring
    {
        private const string Folder = WeaponPilotImportPolicy.Folder;
        private static readonly string[] Actors = { "AxePilot", "MacePilot" };

        [MenuItem("Praxen/Assets/Build Weapon Family Pilots")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before authoring the family pilots.");
            Directory.CreateDirectory(Folder);
            foreach (string actor in Actors) Assemble(actor, Materials(actor));
            AssetDatabase.SaveAssets();
            Debug.Log("Task 13 original axe and mace pilot prefabs authored in their separate folder.");
        }

        private static Material[] Materials(string actor)
        {
            bool axe = actor == "AxePilot";
            return new[] {
                Material(actor + "Ceramic", axe ? new Color(.66f, .59f, .43f) : new Color(.52f, .56f, .6f), .35f, .55f),
                Material(actor + "Brass", new Color(.39f, .23f, .065f), .78f, .68f),
                Material(actor + "Cloth", axe ? new Color(.11f, .055f, .025f) : new Color(.045f, .055f, .09f), 0, .18f) };
        }

        private static Material Material(string name, Color color, float metal, float smoothness)
        {
            string path = Folder + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("Pinned URP Lit shader unavailable.");
            var material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metal);
            material.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void Assemble(string actor, Material[] materials)
        {
            var owner = new GameObject(actor);
            try
            {
                var models = Enumerable.Range(0, 3).Select(lod => InstantiateModel(actor, lod, owner)).ToArray();
                var animator = models[0].GetComponent<Animator>();
                if (animator == null || animator.avatar == null || !animator.avatar.isValid || !animator.avatar.isHuman)
                    throw new InvalidOperationException(actor + " requires a valid imported Humanoid avatar.");
                animator.applyRootMotion = false;
                var bones = models[0].GetComponentsInChildren<Transform>().ToDictionary(t => t.name);
                var lods = Enumerable.Range(0, 3).Select(lod => ConfigureLod(models[lod], bones, materials,
                    lod == 0 ? .55f : lod == 1 ? .25f : .05f)).ToArray();
                var group = owner.AddComponent<LODGroup>();
                group.fadeMode = LODFadeMode.None;
                group.SetLODs(lods);
                group.RecalculateBounds();
                FindRigReferences(owner);
                PrefabUtility.SaveAsPrefabAsset(owner, Folder + actor + ".prefab");
            }
            finally { Object.DestroyImmediate(owner); }
        }

        private static GameObject InstantiateModel(string actor, int lod, GameObject owner)
        {
            string path = Folder + actor + "_LOD" + lod + ".fbx";
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model == null) throw new InvalidOperationException("Missing original family pilot: " + path);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            instance.name = "LOD" + lod;
            instance.transform.SetParent(owner.transform, false);
            return instance;
        }

        private static LOD ConfigureLod(GameObject model,
            System.Collections.Generic.Dictionary<string, Transform> bones, Material[] materials, float transition)
        {
            foreach (var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                renderer.sharedMaterials = materials.Take(renderer.sharedMesh.subMeshCount).ToArray();
                renderer.bones = renderer.bones.Select(b => bones[b.name]).ToArray();
                if (renderer.rootBone != null) renderer.rootBone = bones[renderer.rootBone.name];
                renderer.quality = SkinQuality.Bone4;
            }
            if (model.name != "LOD0")
            {
                var animator = model.GetComponent<Animator>();
                if (animator != null) Object.DestroyImmediate(animator);
                foreach (var marker in model.GetComponentsInChildren<Transform>().Where(t => t.name == "WeaponTip" ||
                    t.name == "WeaponSocketMarker" || t.name == "ShieldSocketMarker").ToArray())
                    Object.DestroyImmediate(marker.gameObject);
            }
            return new LOD(transition, model.GetComponentsInChildren<Renderer>());
        }

        public static RigReferences FindRigReferences(GameObject prefab)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            var animator = prefab.GetComponentsInChildren<Animator>().Single();
            if (animator.transform.name != "LOD0") throw new InvalidOperationException("Pilot Animator must own LOD0.");
            var nodes = animator.GetComponentsInChildren<Transform>();
            var tip = nodes.Single(t => t.name == "WeaponTip");
            var weapon = nodes.Single(t => t.name == "WeaponSocket");
            var shield = nodes.Single(t => t.name == "ShieldSocket");
            var right = nodes.Single(t => t.name == "RightHand");
            var left = nodes.Single(t => t.name == "LeftHand");
            if (tip.parent != weapon || !weapon.IsChildOf(right) || !shield.IsChildOf(left))
                throw new InvalidOperationException("Pilot equipment roles must match the anatomical hands.");
            var renderers = Enumerable.Range(0, 3).Select(lod =>
            {
                var child = prefab.transform.Find("LOD" + lod + "/" + prefab.name + "_LOD" + lod + "_Weapon");
                if (child == null || child.GetComponent<SkinnedMeshRenderer>() == null)
                    throw new InvalidOperationException("Pilot weapon renderer path is missing at LOD" + lod);
                return AnimationUtility.CalculateTransformPath(child, prefab.transform);
            }).ToArray();
            return new RigReferences {
                WeaponTipPath = AnimationUtility.CalculateTransformPath(tip, prefab.transform),
                WeaponSocketPath = AnimationUtility.CalculateTransformPath(weapon, prefab.transform),
                ShieldSocketPath = AnimationUtility.CalculateTransformPath(shield, prefab.transform),
                WeaponRendererPaths = renderers };
        }

        public sealed class RigReferences
        {
            public string WeaponTipPath;
            public string WeaponSocketPath;
            public string ShieldSocketPath;
            public string[] WeaponRendererPaths;
        }
    }
}
