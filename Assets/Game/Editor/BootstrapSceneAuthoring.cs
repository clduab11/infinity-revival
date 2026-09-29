using System.Linq;
using Praxen.Game.Presentation;
using Praxen.Game.Content.Input;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Praxen.Game.Editor
{
    public static class BootstrapSceneAuthoring
    {
        public const string ScenePath = "Assets/Game/Scenes/Bootstrap.unity";

        [MenuItem("Praxen/Create Bootstrap Scene")]
        public static void CreateScene()
        {
            if (!UnityEngine.Application.isBatchMode &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var owner = new GameObject("Bootstrap");
            var root = owner.AddComponent<BootstrapCompositionRoot>();
            var camera = CreateCamera(owner.transform);
            var viewObject = new GameObject("RefugeScreen", typeof(RectTransform));
            viewObject.transform.SetParent(owner.transform, false);
            var view = viewObject.AddComponent<RefugeScreen>();
            view.SetCamera(camera);
            root.BindView(view);
            root.BindInputTuning(LoadTuning());
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new System.InvalidOperationException("Bootstrap scene could not be saved.");
            var preserved = EditorBuildSettings.scenes.Where(s => s.path != ScenePath)
                .Select(s => new EditorBuildSettingsScene(s.path, false));
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }
                .Concat(preserved).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("Task 03 Bootstrap scene saved and enabled first.");
        }

        public static void ConfigureInput()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = Object.FindFirstObjectByType<BootstrapCompositionRoot>();
            if (root == null) throw new System.InvalidOperationException("Bootstrap root is missing.");
            root.BindInputTuning(LoadTuning());
            EditorUtility.SetDirty(root);
            if (!EditorSceneManager.SaveScene(scene))
                throw new System.InvalidOperationException("Bootstrap input binding could not be saved.");
            AssetDatabase.SaveAssets();
            Debug.Log("Task 04 input tuning bound; existing Bootstrap scene preserved.");
        }

        private static GestureTuningAsset LoadTuning()
        {
            const string path = "Assets/Game/Content/GestureTuning.asset";
            var tuning = AssetDatabase.LoadAssetAtPath<GestureTuningAsset>(path);
            if (tuning != null) return tuning;
            tuning = ScriptableObject.CreateInstance<GestureTuningAsset>();
            AssetDatabase.CreateAsset(tuning, path);
            return tuning;
        }

        private static Camera CreateCamera(Transform parent)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.transform.SetParent(parent, false);
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(19, 27, 33, 255);
            camera.orthographic = true;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 10f;
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }
    }
}
