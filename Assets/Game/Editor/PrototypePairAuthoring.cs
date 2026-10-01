using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Praxen.Game.Editor
{
    public static class PrototypePairAuthoring
    {
        private const string Folder = PrototypePairImportPolicy.Folder;
        public const string ScenePath = "Assets/Game/Scenes/PrototypePairReview.unity";

        // Author assets in an isolated native Editor copy; interactive rebuild is explicit.
        [MenuItem("Praxen/Assets/Build Prototype Pair Review")]
        public static void Build()
        {
            if (!UnityEngine.Application.isBatchMode &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!UnityEngine.Application.isBatchMode && Enumerable.Range(0, SceneManager.sceneCount)
                .Any(i => string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path)))
                throw new InvalidOperationException("Save all loaded scenes before authoring an additive asset review.");
            Directory.CreateDirectory(Folder);
            var materials = new[] { Material("Ceramic", new Color(.72f, .69f, .58f), .12f, .6f),
                Material("Brass", new Color(.39f, .23f, .065f), .78f, .68f),
                Material("Cloth", new Color(.028f, .065f, .1f), 0, .18f) };
            var soldier = Assemble("Soldier", materials);
            var captain = Assemble("Captain", materials);
            WriteReviewScene(soldier, captain);
            AssetDatabase.SaveAssets();
            Debug.Log("Task 11 pair prefabs and separate review scene authored; gameplay scenes unchanged.");
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

        private static GameObject Assemble(string actor, Material[] materials)
        {
            var owner = new GameObject(actor);
            try
            {
                var models = Enumerable.Range(0, 3).Select(lod =>
                {
                    string path = Folder + actor + "_LOD" + lod + ".fbx";
                    var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (source == null) throw new InvalidOperationException("Missing source: " + path);
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
                    instance.transform.SetParent(owner.transform, false);
                    instance.name = "LOD" + lod;
                    return instance;
                }).ToArray();
                var animator = models[0].GetComponent<Animator>();
                if (animator == null || animator.avatar == null || !animator.avatar.isValid || !animator.avatar.isHuman)
                    throw new InvalidOperationException(actor + " must have a valid Humanoid avatar.");
                animator.applyRootMotion = false;
                var bones = models[0].GetComponentsInChildren<Transform>().ToDictionary(t => t.name);
                var lods = Enumerable.Range(0, 3).Select(lod => ConfigureLod(models[lod], bones, materials,
                    lod == 0 ? .55f : lod == 1 ? .25f : .05f)).ToArray();
                var group = owner.AddComponent<LODGroup>();
                group.fadeMode = LODFadeMode.None;
                group.SetLODs(lods);
                group.RecalculateBounds();
                string prefabPath = Folder + actor + ".prefab";
                return PrefabUtility.SaveAsPrefabAsset(owner, prefabPath);
            }
            finally { Object.DestroyImmediate(owner); }
        }

        private static LOD ConfigureLod(GameObject model, System.Collections.Generic.Dictionary<string, Transform> bones,
            Material[] materials, float transition)
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
            }
            return new LOD(transition, model.GetComponentsInChildren<Renderer>());
        }

        private static void WriteReviewScene(GameObject soldier, GameObject captain)
        {
            bool batch = UnityEngine.Application.isBatchMode;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                batch ? NewSceneMode.Single : NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                Place(soldier, new Vector3(-1, 0, 0));
                Place(captain, new Vector3(1, 0, 0));
                var cameraObject = new GameObject("Review Camera", typeof(Camera));
                var camera = cameraObject.GetComponent<Camera>();
                cameraObject.tag = "MainCamera";
                camera.transform.position = new Vector3(0, 1.2f, -11);
                camera.transform.LookAt(new Vector3(0, .95f, 0));
                camera.fieldOfView = 38;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.05f, .075f, .09f);
                var light = new GameObject("Review Key", typeof(Light));
                light.transform.rotation = Quaternion.Euler(35, -35, 0);
                light.GetComponent<Light>().type = LightType.Directional;
                light.GetComponent<Light>().intensity = 2;
                var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "Review Ground";
                floor.transform.localScale = Vector3.one * .6f;
                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                    throw new InvalidOperationException("Could not save separate prototype review scene.");
            }
            finally { if (!batch) EditorSceneManager.CloseScene(scene, true); }
        }

        private static void Place(GameObject source, Vector3 position)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.transform.position = position;
        }
    }
}
