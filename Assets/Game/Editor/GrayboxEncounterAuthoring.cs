using System.Linq;
using Praxen.Game.Content.Input;
using Praxen.Game.Presentation.Combat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Praxen.Game.Editor
{
    public static class GrayboxEncounterAuthoring
    {
        public const string ScenePath = "Assets/Game/Scenes/GrayboxEncounter.unity";

        [MenuItem("Praxen/Create Graybox Encounter")]
        public static void CreateScene()
        {
            if (!UnityEngine.Application.isBatchMode &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var owner = new GameObject("Graybox encounter");
            var root = owner.AddComponent<GrayboxEncounterRoot>();
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.transform.SetParent(owner.transform, false);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0, 3, -7);
            cameraObject.transform.LookAt(new Vector3(0, 1.2f, 1.5f));
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(19, 27, 33, 255);
            camera.fieldOfView = 45;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100;
            var light = new GameObject("Foundry light", typeof(Light));
            light.transform.SetParent(owner.transform, false);
            light.transform.rotation = Quaternion.Euler(45, -30, 0);
            light.GetComponent<Light>().type = LightType.Directional;
            light.GetComponent<Light>().intensity = 1.4f;
            root.Bind(camera, AssetDatabase.LoadAssetAtPath<GestureTuningAsset>(
                "Assets/Game/Content/GestureTuning.asset"));
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new System.InvalidOperationException("Graybox scene could not be saved.");
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath)
                .Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
            PlayerSettings.productName = "Forever We Reign";
            AssetDatabase.SaveAssets();
            Debug.Log("Task 06 graybox scene saved; Forever We Reign product name applied.");
        }
    }
}
