using System;
using System.IO;
using System.Linq;
using Praxen.Game.Presentation.Combat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Praxen.Game.Editor
{
    public static class CombatPrototypeAuthoring
    {
        public const string ProfilePath = "Assets/Game/Content/CombatPresentationProfile.asset";
        public const string ScenePath = "Assets/Game/Scenes/CombatPrototype.unity";
        private const string SourceScenePath = "Assets/Game/Scenes/GrayboxEncounter.unity";
        private const string PairFolder = "Assets/Game/Content/Characters/PrototypePair/";
        private const string AudioFolder = "Assets/Game/Content/Audio/";
        private static readonly string[] MotionKeys = { "Idle", "Guard", "Parry", "DodgeLeft", "DodgeRight",
            "CutUp", "CutDown", "CutLeft", "CutRight", "Hit", "Death", "EnemyTell", "EnemyAttack" };

        [MenuItem("Praxen/Assets/Build Combat Prototype")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before authoring the combat prototype.");
            bool batch = UnityEngine.Application.isBatchMode;
            if (!CanPreserveLoadedScenes(batch)) return;
            var soldier = LoadPrefab("Soldier");
            var captain = LoadPrefab("Captain");
            var soldierMotions = LoadMotions("Soldier");
            var captainMotions = LoadMotions("Captain");
            var sounds = LoadSounds();
            var profile = LoadOrCreateProfile();
            profile.Soldier = soldier;
            profile.Captain = captain;
            profile.SoldierMotions = soldierMotions;
            profile.CaptainMotions = captainMotions;
            profile.Sounds = sounds;
            profile.EnableCameraOrbit = true;
            profile.CameraOrbitDegrees = 3;
            profile.CameraFocusDistance = 8;
            EditorUtility.SetDirty(profile);
            WritePrototypeScene(profile, batch);
            if (!EditorBuildSettings.scenes.Any(scene => scene.path == ScenePath))
                EditorBuildSettings.scenes = EditorBuildSettings.scenes
                    .Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("Task 12 presentation profile and separate combat prototype scene authored.");
        }

        private static bool CanPreserveLoadedScenes(bool batch)
        {
            if (batch) return true;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
            foreach (int index in Enumerable.Range(0, SceneManager.sceneCount))
            {
                var scene = SceneManager.GetSceneAt(index);
                if (scene.isDirty || string.IsNullOrEmpty(scene.path))
                    throw new InvalidOperationException("Save every loaded scene before authoring the combat prototype.");
                if (scene.path == SourceScenePath || scene.path == ScenePath)
                    throw new InvalidOperationException("Close the Graybox and CombatPrototype scenes first, or use an isolated batch Editor.");
            }
            return true;
        }

        private static GameObject LoadPrefab(string actor)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PairFolder + actor + ".prefab");
            var animator = prefab != null ? prefab.GetComponentInChildren<Animator>() : null;
            if (animator == null || animator.avatar == null || !animator.avatar.isValid || !animator.avatar.isHuman)
                throw new InvalidOperationException(actor + " requires the Task 11 Humanoid prefab.");
            return prefab;
        }

        private static MotionClipBinding[] LoadMotions(string actor)
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(PairFolder + actor + "_LOD0.fbx")
                .OfType<AnimationClip>().Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal)).ToArray();
            if (clips.Length != MotionKeys.Length)
                throw new InvalidOperationException(actor + " requires exactly 13 embedded Task 11 motions.");
            return MotionKeys.Select(key =>
            {
                var clip = clips.SingleOrDefault(candidate => candidate.name == key);
                if (clip == null || !clip.isHumanMotion)
                    throw new InvalidOperationException("Missing Humanoid motion: " + actor + " " + key);
                return new MotionClipBinding { Key = key, Clip = clip };
            }).ToArray();
        }

        private static CombatSoundBinding[] LoadSounds()
        {
            var tell = LoadSound("Tell");
            var swing = LoadSound("Swing");
            var clash = LoadSound("Clash");
            var impact = LoadSound("Impact");
            return new[] { Sound(CombatCueKind.Telegraph, tell), Sound(CombatCueKind.Attack, swing),
                Sound(CombatCueKind.Parry, clash), Sound(CombatCueKind.Block, clash), Sound(CombatCueKind.Dodge, swing),
                Sound(CombatCueKind.Hit, impact), Sound(CombatCueKind.Death, impact),
                Sound(CombatCueKind.Opening, tell), Sound(CombatCueKind.Guard, clash) };
        }

        private static CombatSoundBinding Sound(CombatCueKind kind, AudioClip clip) =>
            new CombatSoundBinding { Kind = kind, Clip = clip };

        private static AudioClip LoadSound(string name)
        {
            string path = AudioFolder + "Task12" + name + ".wav";
            if (!(AssetImporter.GetAtPath(path) is AudioImporter importer))
                throw new InvalidOperationException("Generate and import the original feedback tone first: " + path);
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
            importer.loadInBackground = false;
            importer.SaveAndReimport();
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null || clip.channels != 1 || clip.length <= 0 || clip.length >= .2f)
                throw new InvalidOperationException("Feedback must be mono and shorter than 200 ms: " + path);
            return clip;
        }

        private static CombatPresentationProfile LoadOrCreateProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<CombatPresentationProfile>(ProfilePath);
            if (profile != null) return profile;
            if (File.Exists(ProfilePath))
                throw new InvalidOperationException("Profile path is occupied by another asset type: " + ProfilePath);
            profile = ScriptableObject.CreateInstance<CombatPresentationProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
            return profile;
        }

        private static void WritePrototypeScene(CombatPresentationProfile profile, bool batch)
        {
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(SourceScenePath, batch ? OpenSceneMode.Single : OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects().SelectMany(owner =>
                    owner.GetComponentsInChildren<GrayboxEncounterRoot>(true)).ToArray();
                if (roots.Length != 1)
                    throw new InvalidOperationException("Graybox source requires exactly one encounter root.");
                roots[0].BindPresentation(profile);
                EditorUtility.SetDirty(roots[0]);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, ScenePath, !batch))
                    throw new InvalidOperationException("Could not save the separate combat prototype scene.");
            }
            finally
            {
                if (!batch)
                {
                    if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }
    }
}
