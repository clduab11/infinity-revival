using System;
using System.IO;
using System.Linq;
using Praxen.Game.Content.Definitions;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Content;
using Praxen.Game.Domain.Input;
using Praxen.Game.Presentation.Combat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Praxen.Game.Editor
{
    /// <summary>Reproducible pilot catalog authoring in an isolated native Editor.</summary>
    public static class Task13ContentAuthoring
    {
        public const string Folder = "Assets/Game/Content/Definitions/";
        public const string CatalogPath = Folder + "catalog.prototype.asset";
        private const string Pair = "Assets/Game/Content/Characters/PrototypePair/";
        private const string Pilot = "Assets/Game/Content/Characters/WeaponFamilyPilot/";
        private static readonly string[] Keys = { "Idle", "Guard", "Parry", "DodgeLeft", "DodgeRight",
            "CutUp", "CutDown", "CutLeft", "CutRight", "Hit", "Death", "EnemyTell", "EnemyAttack" };

        public static void Build()
        {
            if (!UnityEngine.Application.isBatchMode)
                throw new InvalidOperationException("Run pilot authoring in an isolated batch Editor to preserve loaded scenes.");
            WeaponPilotAuthoring.Build();
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            var sword = Weapon(WeaponFamily.Sword, "Soldier", Pair, 100000, 400000,
                "SourceArt/Characters/prototype-pair-source.json");
            var axe = Weapon(WeaponFamily.Axe, "AxePilot", Pilot, 150000, 450000,
                "SourceArt/Equipment/WeaponFamilyPilot/weapon-family-pilot-source.json");
            var mace = Weapon(WeaponFamily.Mace, "MacePilot", Pilot, 180000, 520000,
                "SourceArt/Equipment/WeaponFamilyPilot/weapon-family-pilot-source.json");
            var enemy = Motion("motion.pilot.captain", "Captain", Pair, .1);
            var defense = Asset<DefenseDefinition>("defense.prototype");
            var camera = Asset<CameraProfileDefinition>("camera.prototype");
            camera.EnableOrbit = true;
            camera.OrbitDegrees = 3;
            camera.FocusDistance = 8;
            camera.ReducedMotion = false;
            Save(camera);
            Save(defense);
            var catalog = Asset<CombatContentCatalog>("catalog.prototype");
            catalog.Weapons = new[] { sword, axe, mace };
            catalog.EnemyDecks = AuthoredEnemyDeckBuilder.Build();
            catalog.Defense = defense;
            catalog.Camera = camera;
            catalog.Validate().ThrowIfInvalid();
            enemy.Validate().ThrowIfInvalid();
            Save(catalog);
            AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.OpenScene(CombatPrototypeAuthoring.ScenePath, OpenSceneMode.Single);
            var root = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<GrayboxEncounterRoot>(true)).Single();
            root.BindContent(catalog, enemy, sword.Id);
            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save Task 13 catalog admission.");
            Task13ContentValidation.ValidateAll();
            Debug.Log("Task 13 authored catalog: three weapon pilots, four enemy decks, explicit motion/contact and camera definitions.");
        }

        private static WeaponDefinition Weapon(WeaponFamily family, string actor, string folder,
            long windup, long recovery, string provenance)
        {
            string name = family.ToString().ToLowerInvariant();
            var weapon = Asset<WeaponDefinition>("weapon.pilot." + name);
            weapon.Family = family;
            weapon.OneHanded = weapon.ShieldCompatible = true;
            weapon.ProductionReady = false;
            weapon.Icon = null;
            weapon.RequestedCapabilities = Array.Empty<string>();
            weapon.ProvenancePath = provenance;
            weapon.MotionProfile = Motion("motion.pilot." + name, actor, folder, windup / 1000000d);
            weapon.Materials = weapon.MotionProfile.ActorPrefab.GetComponentsInChildren<Renderer>(true)
                .SelectMany(renderer => renderer.sharedMaterials).Distinct().ToArray();
            weapon.Attacks = new[] { SwipeDirection.Up, SwipeDirection.Down, SwipeDirection.Left, SwipeDirection.Right }
                .Select(direction => Attack(name, direction, windup, recovery)).ToArray();
            Save(weapon);
            return weapon;
        }

        private static AttackDefinition Attack(string family, SwipeDirection direction, long windup, long recovery)
        {
            var attack = Asset<AttackDefinition>("attack.pilot." + family + "." + direction.ToString().ToLowerInvariant());
            attack.Direction = direction;
            attack.WindupUs = windup;
            attack.RecoveryUs = recovery;
            attack.VisualReleaseUs = family == "axe" ? 90000 : family == "mace" ? 100000 : 40000;
            attack.Damage = 20;
            attack.ComboDamageBonus = 10;
            attack.MotionKey = "Cut" + direction;
            Save(attack);
            return attack;
        }

        private static MotionProfileDefinition Motion(string id, string actor, string folder, double contact)
        {
            var motion = Asset<MotionProfileDefinition>(id);
            motion.ActorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(folder + actor + ".prefab");
            if (motion.ActorPrefab == null) throw new InvalidOperationException("Missing pilot prefab: " + actor);
            motion.RigId = "rig.prototype-pair.23-bone.v1";
            motion.RightHanded = true;
            var animator = motion.ActorPrefab.GetComponentsInChildren<Animator>(true).Single();
            var tip = animator.GetComponentsInChildren<Transform>(true).Single(t => t.name == "WeaponTip");
            var weaponSocket = animator.GetComponentsInChildren<Transform>(true).Single(t => t.name == "WeaponSocket");
            var shieldSocket = animator.GetComponentsInChildren<Transform>(true).Single(t => t.name == "ShieldSocket");
            motion.WeaponTipPath = Path(tip, motion.ActorPrefab);
            motion.WeaponSocketPath = Path(weaponSocket, motion.ActorPrefab);
            motion.ShieldSocketPath = Path(shieldSocket, motion.ActorPrefab);
            motion.WeaponRendererPaths = motion.ActorPrefab.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(r => r.name.EndsWith("_Sword", StringComparison.Ordinal) || r.name.EndsWith("_Weapon", StringComparison.Ordinal))
                .Select(r => Path(r.transform, motion.ActorPrefab)).OrderBy(path => path, StringComparer.Ordinal).ToArray();
            var clips = AssetDatabase.LoadAllAssetsAtPath(folder + actor + "_LOD0.fbx")
                .OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal)).ToArray();
            motion.Bindings = Keys.Select(key =>
            {
                var clip = clips.Single(c => c.name == key);
                bool attack = key.StartsWith("Cut", StringComparison.Ordinal) || key == "EnemyAttack";
                return new MotionBinding { Key = key, Clip = clip, DurationSeconds = clip.length,
                    ContactSeconds = attack ? contact : 0, ReleaseSeconds = attack ? actor == "AxePilot" ? .09 : actor == "MacePilot" ? .1 : .04 : 0,
                    Loop = key == "Idle" || key == "Guard" };
            }).ToArray();
            Save(motion);
            return motion;
        }

        private static string Path(Transform transform, GameObject root) =>
            AnimationUtility.CalculateTransformPath(transform, root.transform);

        internal static T Asset<T>(string id) where T : ContentDefinition
        {
            string path = Folder + id + ".asset";
            var result = AssetDatabase.LoadAssetAtPath<T>(path);
            if (result == null)
            {
                if (File.Exists(path)) throw new InvalidOperationException("Definition path occupied by another type: " + path);
                result = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(result, path);
            }
            result.Id = id;
            result.Version = 1;
            result.LocalizationKey = "content." + id;
            return result;
        }

        internal static void Save(UnityEngine.Object asset) => EditorUtility.SetDirty(asset);
    }
}
