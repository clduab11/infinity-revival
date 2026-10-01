using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Praxen.Game.Content.Combat;
using Praxen.Game.Content.Definitions;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;
using UnityEditor;
using UnityEngine;

namespace Praxen.Game.Tests.EditMode
{
    public sealed partial class Task13ContentDefinitionTests
    {
        private const string PairFolder = "Assets/Game/Content/Characters/PrototypePair/";
        private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();

        [TearDown]
        public void ReleaseTemporaryAuthoringFixtures()
        {
            for (int i = owned.Count - 1; i >= 0; i--)
                if (owned[i] != null) UnityEngine.Object.DestroyImmediate(owned[i]);
            owned.Clear();
        }

        private T New<T>(string id) where T : ContentDefinition
        {
            var asset = ScriptableObject.CreateInstance<T>(); owned.Add(asset);
            asset.Id = id; asset.Version = 1; asset.LocalizationKey = "item.test";
            return asset;
        }

        private AttackDefinition Attack(SwipeDirection direction, string prefix = "attack.test.")
        {
            var attack = New<AttackDefinition>(prefix + direction.ToString().ToLowerInvariant());
            attack.Direction = direction; attack.MotionKey = "Cut" + direction;
            return attack;
        }

        private CombatContentCatalog Catalog()
        {
            var catalog = New<CombatContentCatalog>("catalog.test");
            catalog.Defense = New<DefenseDefinition>("defense.test");
            catalog.Camera = New<CameraProfileDefinition>("camera.test");
            catalog.Weapons = new[] { Weapon(Motion(), "weapon.test.pilot") };
            catalog.EnemyDecks = new[] { Deck(PrototypeEnemyDecks.Create(EnemyArchetype.Sword)) };
            return catalog;
        }

        private WeaponDefinition Weapon(MotionProfileDefinition profile, string id)
        {
            var weapon = New<WeaponDefinition>(id); weapon.MotionProfile = profile;
            weapon.Attacks = new[] { Attack(SwipeDirection.Up, id + ".attack."), Attack(SwipeDirection.Down, id + ".attack."),
                Attack(SwipeDirection.Left, id + ".attack."), Attack(SwipeDirection.Right, id + ".attack.") };
            weapon.Materials = new[] { AssetDatabase.LoadAssetAtPath<Material>(PairFolder + "Ceramic.mat") };
            weapon.ProvenancePath = "SourceArt/Characters/prototype-pair-source.json";
            return weapon;
        }

        private MotionProfileDefinition Motion()
        {
            var profile = New<MotionProfileDefinition>("motion.test.pilot");
            profile.ActorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PairFolder + "Soldier.prefab");
            Assert.That(profile.ActorPrefab, Is.Not.Null, "Accepted soldier prefab is required for real motion validation.");
            profile.RigId = "rig.prototype-pair.v1";
            var animator = profile.ActorPrefab.GetComponentsInChildren<Animator>(true).Single();
            var transforms = animator.GetComponentsInChildren<Transform>(true);
            profile.WeaponTipPath = AnimationUtility.CalculateTransformPath(transforms.Single(t => t.name == "WeaponTip"), profile.ActorPrefab.transform);
            profile.WeaponSocketPath = AnimationUtility.CalculateTransformPath(transforms.Single(t => t.name == "WeaponSocket"), profile.ActorPrefab.transform);
            profile.ShieldSocketPath = AnimationUtility.CalculateTransformPath(transforms.Single(t => t.name == "ShieldSocket"), profile.ActorPrefab.transform);
            profile.WeaponRendererPaths = profile.ActorPrefab.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(r => r.name.EndsWith("_Sword", StringComparison.Ordinal))
                .Select(r => AnimationUtility.CalculateTransformPath(r.transform, profile.ActorPrefab.transform)).ToArray();
            profile.Bindings = AssetDatabase.LoadAllAssetsAtPath(PairFolder + "Soldier_LOD0.fbx").OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal)).Select(clip =>
                {
                    bool attack = clip.name.StartsWith("Cut", StringComparison.Ordinal) || clip.name == "EnemyAttack";
                    return new MotionBinding { Key = clip.name, Clip = clip, DurationSeconds = clip.length,
                        ContactSeconds = attack ? .1 : 0, ReleaseSeconds = attack ? .06 : 0,
                        Loop = clip.name == "Idle" || clip.name == "Guard" };
                }).ToArray();
            return profile;
        }

        private EnemyDeckDefinition Deck(EnemyPatternDeck source)
        {
            var deck = New<EnemyDeckDefinition>(source.Id);
            deck.Archetype = source.Archetype; deck.Lesson = source.Lesson;
            deck.ContentRevision = source.ContentRevision; deck.BalanceRevision = source.BalanceRevision;
            deck.IntroPatternId = source.IntroPatternId;
            var attacks = new Dictionary<string, EnemyAttackDefinition>(StringComparer.Ordinal);
            deck.Patterns = source.Patterns.Select(pattern => Pattern(pattern, attacks)).ToArray();
            return deck;
        }

        private EnemyPatternDefinition Pattern(EnemyPattern source, Dictionary<string, EnemyAttackDefinition> attacks)
        {
            var pattern = New<EnemyPatternDefinition>(source.Id);
            pattern.Weight = source.Weight; pattern.CooldownUs = source.CooldownUs;
            pattern.MinimumGuard = source.MinimumGuard; pattern.MinimumDodgeCharges = source.MinimumDodgeCharges;
            pattern.MinimumFocus = source.MinimumFocus; pattern.MinimumTier = source.MinimumTier; pattern.MaximumTier = source.MaximumTier;
            pattern.HasPreferredDefenseOutcome = source.PreferredDefenseOutcome.HasValue;
            pattern.PreferredDefenseOutcome = source.PreferredDefenseOutcome ?? DefenseOutcome.Hit;
            pattern.Steps = source.Steps.Select(step =>
            {
                if (!attacks.TryGetValue(step.AttackId, out var attack))
                {
                    attack = New<EnemyAttackDefinition>(step.AttackId); attacks.Add(step.AttackId, attack);
                    attack.AllowedDefenses = step.AllowedDefenses; attack.SafeDodgeSides = step.SafeDodgeSides;
                    attack.ParryDirection = step.ParryDirection; attack.GuardCost = step.GuardCost; attack.Damage = step.Damage;
                    attack.TelegraphDurationUs = step.TelegraphDurationUs; attack.RecoveryDurationUs = step.RecoveryDurationUs;
                }
                return new PatternStepBinding { Attack = attack, GapBeforeUs = step.GapBeforeUs };
            }).ToArray();
            return pattern;
        }

        private static void EditBinding(MotionProfileDefinition profile, string key, Func<MotionBinding, MotionBinding> edit)
        {
            int index = Array.FindIndex(profile.Bindings, binding => binding.Key == key);
            Assert.That(index, Is.GreaterThanOrEqualTo(0), key);
            profile.Bindings[index] = edit(profile.Bindings[index]);
        }

        private static void AssertIssue(ContentValidationResult result, string field)
        {
            Assert.That(result.Issues.Any(issue => issue.Field == field && !string.IsNullOrWhiteSpace(issue.AssetId) &&
                !string.IsNullOrWhiteSpace(issue.Message)), Is.True, Diagnostics(result));
        }

        private static string Diagnostics(ContentValidationResult result) => string.Join("\n", result.Issues);
    }
}
