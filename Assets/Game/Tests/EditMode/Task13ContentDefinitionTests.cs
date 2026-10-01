using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Praxen.Game.Content.Input;
using Praxen.Game.Domain;
using Praxen.Game.Domain.Combat;
using UnityEngine;

namespace Praxen.Game.Tests.EditMode
{
    public sealed partial class Task13ContentDefinitionTests
    {
        private const string Definitions = "Praxen.Game.Content.Definitions.";
        private const string RuntimeValues = "Praxen.Game.Domain.Content.";

        [TestCase("AttackDefinition")]
        [TestCase("DefenseDefinition")]
        [TestCase("EnemyAttackDefinition")]
        [TestCase("EnemyPatternDefinition")]
        [TestCase("EnemyDeckDefinition")]
        [TestCase("MotionProfileDefinition")]
        [TestCase("CameraProfileDefinition")]
        [TestCase("WeaponDefinition")]
        [TestCase("CombatContentCatalog")]
        public void AuthoringDefinitionsCanBeCreatedAsAssetsWithExplicitStableIdentity(string name)
        {
            var type = ContentType(name);
            Assert.That(typeof(ScriptableObject).IsAssignableFrom(type), Is.True);
            Assert.That(type.GetCustomAttribute<CreateAssetMenuAttribute>(), Is.Not.Null);
            AssertFields(type, "Id", "Version", "LocalizationKey");
            var asset = ScriptableObject.CreateInstance(type);
            try
            {
                Set(asset, "Id", "test." + name.ToLowerInvariant());
                asset.name = "Renamed authoring asset";
                Assert.That(Get(asset, "Id"), Is.EqualTo("test." + name.ToLowerInvariant()));
            }
            finally { UnityEngine.Object.DestroyImmediate(asset); }
        }

        [TestCase("AttackDefinition", "Direction,WindupUs,RecoveryUs,VisualReleaseUs,Damage,ComboDamageBonus,MotionKey")]
        [TestCase("DefenseDefinition", "MaximumHealth,MaximumGuard,MaximumDodgeCharges,GuardStaggerUs,DodgeDurationUs,DodgeAvoidanceStartUs,DodgeAvoidanceEndUs,ParryWindowUs,ParryRecoveryUs,HitRecoveryUs,DodgeRechargeUs")]
        [TestCase("EnemyAttackDefinition", "AllowedDefenses,SafeDodgeSides,ParryDirection,GuardCost,Damage,TelegraphDurationUs,RecoveryDurationUs")]
        [TestCase("EnemyPatternDefinition", "Steps,Weight,CooldownUs,MinimumGuard,MinimumDodgeCharges,MinimumFocus,MinimumTier,MaximumTier")]
        [TestCase("EnemyDeckDefinition", "Archetype,Lesson,ContentRevision,BalanceRevision,Patterns,IntroPatternId")]
        [TestCase("MotionProfileDefinition", "ActorPrefab,RigId,RightHanded,WeaponTipPath,WeaponSocketPath,ShieldSocketPath,WeaponRendererPaths,Bindings")]
        [TestCase("CameraProfileDefinition", "EnableOrbit,OrbitDegrees,FocusDistance,ReducedMotion")]
        [TestCase("WeaponDefinition", "Family,OneHanded,ShieldCompatible,Attacks,MotionProfile,Icon,Materials,ProvenancePath,ProductionReady,RequestedCapabilities")]
        [TestCase("CombatContentCatalog", "Weapons,EnemyDecks,Defense,Camera")]
        public void AuthoringSchemaCapturesEveryRequiredAdmissionValue(string name, string fieldNames)
        {
            AssertFields(ContentType(name), fieldNames.Split(','));
        }

        [TestCase("PatternStepBinding", "Attack,GapBeforeUs")]
        [TestCase("MotionBinding", "Key,Clip,ContactSeconds,DurationSeconds,ReleaseSeconds,Loop")]
        public void NestedBindingsAreSerializableExplicitRecords(string name, string fieldNames)
        {
            var type = ContentType(name);
            Assert.That(type.IsSerializable, Is.True);
            AssertFields(type, fieldNames.Split(','));
        }

        [TestCase("AttackSnapshot")]
        [TestCase("WeaponSnapshot")]
        [TestCase("EncounterContentSnapshot")]
        public void RuntimeSnapshotsHaveNoMutationPortOrUnityReferences(string name)
        {
            var type = DomainType(name);
            Assert.That(type.GetProperties(BindingFlags.Instance | BindingFlags.Public), Is.Not.Empty);
            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                Assert.That(property.GetSetMethod(true), Is.Null, property.Name);
                Assert.That(property.PropertyType.IsArray, Is.False, property.Name);
                Assert.That(typeof(UnityEngine.Object).IsAssignableFrom(property.PropertyType), Is.False, property.Name);
            }
            Assert.That(type.GetFields(BindingFlags.Instance | BindingFlags.Public)
                .All(field => field.IsInitOnly), Is.True);
        }

        [Test]
        public void OnlyTheThreeApprovedOneHandedWeaponFamiliesAreAvailable()
        {
            var type = DomainType("WeaponFamily");
            Assert.That(type.IsEnum, Is.True);
            Assert.That(Enum.GetNames(type), Is.EquivalentTo(new[] { "Sword", "Axe", "Mace" }));
        }

        [Test]
        public void CatalogExposesAtomicValidationConversionAndPresentationLookup()
        {
            var type = ContentType("CombatContentCatalog");
            var validate = type.GetMethod("Validate", Type.EmptyTypes);
            Assert.That(validate, Is.Not.Null, "Catalog must diagnose before encounter teardown.");
            Assert.That(validate.ReturnType.GetProperty("Issues"), Is.Not.Null);
            Assert.That(validate.ReturnType.GetMethod("ThrowIfInvalid", Type.EmptyTypes), Is.Not.Null);
            var convert = type.GetMethod("ToRuntime", new[] { typeof(EnemyArchetype), typeof(string) });
            Assert.That(convert, Is.Not.Null);
            Assert.That(convert.ReturnType, Is.EqualTo(DomainType("EncounterContentSnapshot")));
            Assert.That(type.GetMethod("GetWeapon", new[] { typeof(string) }), Is.Not.Null);
            Assert.That(ContentType("MotionProfileSnapshot"), Is.Not.Null);
        }

        [TestCase("WindupUs", 0L)]
        [TestCase("WindupUs", -1L)]
        [TestCase("RecoveryUs", 0L)]
        [TestCase("RecoveryUs", -1L)]
        public void InvalidPlayerAttackTimingReportsItsAssetAndField(string field, long invalidValue)
        {
            var asset = ScriptableObject.CreateInstance(ContentType("AttackDefinition"));
            try
            {
                Set(asset, "Id", "attack.sword.pilot.up");
                Set(asset, "Version", 1);
                Set(asset, "LocalizationKey", "weapon.sword.pilot");
                Set(asset, "WindupUs", 100000L);
                Set(asset, "RecoveryUs", 400000L);
                Set(asset, "VisualReleaseUs", 60000L);
                Set(asset, "MotionKey", "CutUp");
                Set(asset, field, invalidValue);
                var validate = asset.GetType().GetMethod("Validate", Type.EmptyTypes);
                Assert.That(validate, Is.Not.Null);
                var result = validate.Invoke(asset, null);
                var issues = (IEnumerable)result.GetType().GetProperty("Issues").GetValue(result);
                bool diagnosed = false;
                foreach (var issue in issues)
                {
                    if ((string)Get(issue, "AssetId") != "attack.sword.pilot.up" ||
                        (string)Get(issue, "Field") != field) continue;
                    Assert.That((string)Get(issue, "Message"), Is.Not.Null.And.Not.Empty);
                    diagnosed = true;
                }
                Assert.That(diagnosed, Is.True, "Invalid timing must identify its repairable field.");
            }
            finally { UnityEngine.Object.DestroyImmediate(asset); }
        }

        private static Type ContentType(string name)
        {
            var type = typeof(GestureTuningAsset).Assembly.GetType(Definitions + name);
            Assert.That(type, Is.Not.Null, "Missing Task13 content type: " + Definitions + name);
            return type;
        }

        private static Type DomainType(string name)
        {
            var type = typeof(DomainAssemblyMarker).Assembly.GetType(RuntimeValues + name);
            Assert.That(type, Is.Not.Null, "Missing Task13 immutable domain type: " + RuntimeValues + name);
            return type;
        }

        private static void AssertFields(Type type, params string[] names)
        {
            foreach (string name in names)
                Assert.That(type.GetField(name, BindingFlags.Instance | BindingFlags.Public), Is.Not.Null,
                    type.Name + "." + name + " must be an authored value.");
        }

        private static void Set(object target, string field, object value)
            => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public).SetValue(target, value);

        private static object Get(object target, string member)
        {
            var property = target.GetType().GetProperty(member, BindingFlags.Instance | BindingFlags.Public);
            return property != null ? property.GetValue(target) :
                target.GetType().GetField(member, BindingFlags.Instance | BindingFlags.Public).GetValue(target);
        }
    }
}
