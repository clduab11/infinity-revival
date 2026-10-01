using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Praxen.Game.Content.Combat;
using Praxen.Game.Content.Definitions;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Content;
using Praxen.Game.Domain.Input;
using UnityEngine;

namespace Praxen.Game.Tests.EditMode
{
    public sealed partial class Task13ContentDefinitionTests
    {
        [TestCase(null)] [TestCase("")] [TestCase("attack with spaces")]
        [TestCase("Attack.sword.up")] [TestCase("attack")] [TestCase("attack..up")]
        public void InvalidStableIdsAreRejectedRatherThanDerivedFromAssetNames(string id)
        {
            var attack = Attack(SwipeDirection.Up);
            attack.Id = id;
            AssertIssue(attack.Validate(), "Id");
            Assert.Throws<ContentValidationException>(() => attack.ToRuntime());
        }

        [Test]
        public void AttackConversionCapturesIdentityDirectionAndValuesBeforeAssetEdits()
        {
            var attack = Attack(SwipeDirection.Left);
            var snapshot = attack.ToRuntime();
            attack.name = "Renamed asset";
            attack.WindupUs = 200000; attack.RecoveryUs = 700000; attack.Damage = 99;
            attack.MotionKey = "CutRight"; attack.Version = 2;
            Assert.That(snapshot.Id, Is.EqualTo("attack.test.left"));
            Assert.That(snapshot.Direction, Is.EqualTo(SwipeDirection.Left));
            Assert.That(snapshot.Version, Is.EqualTo(1));
            Assert.That(snapshot.MotionKey, Is.EqualTo("CutLeft"));
            Assert.That(snapshot.WindupUs, Is.EqualTo(100000));
            Assert.That(snapshot.RecoveryUs, Is.EqualTo(400000));
            Assert.That(snapshot.Damage, Is.EqualTo(20));
        }

        [TestCase(true)] [TestCase(false)]
        public void OverflowingTimingOrComboDamageCannotProduceAnAttackSnapshot(bool timing)
        {
            var attack = Attack(SwipeDirection.Up);
            if (timing) attack.WindupUs = long.MaxValue;
            else { attack.Damage = int.MaxValue; attack.ComboDamageBonus = 100; }
            Assert.That(attack.Validate().IsValid, Is.False);
            Assert.Throws<ContentValidationException>(() => attack.ToRuntime());
        }

        [TestCase(-1)] [TestCase(100000)]
        public void VisualReleaseMustStayInsideTheLogicalWindup(long release)
        {
            var attack = Attack(SwipeDirection.Up); attack.VisualReleaseUs = release;
            AssertIssue(attack.Validate(), "VisualReleaseUs");
        }

        [TestCase("Version")] [TestCase("LocalizationKey")] [TestCase("Direction")]
        [TestCase("Damage")] [TestCase("ComboDamageBonus")]
        public void MalformedAttackMetadataAndResourcesIdentifyTheirAuthoredField(string field)
        {
            var attack = Attack(SwipeDirection.Up);
            switch (field)
            {
                case "Version": attack.Version = 0; break;
                case "LocalizationKey": attack.LocalizationKey = " "; break;
                case "Direction": attack.Direction = (SwipeDirection)99; break;
                case "Damage": attack.Damage = -1; break;
                default: attack.ComboDamageBonus = -1; break;
            }
            AssertIssue(attack.Validate(), field);
        }

        [Test]
        public void ArbitraryMotionKeyCannotSilentlyOverrideTheCurrentDirectionResolver()
        {
            var attack = Attack(SwipeDirection.Up); attack.MotionKey = "OtherCut";
            AssertIssue(attack.Validate(), "MotionKey");
            Assert.Throws<ContentValidationException>(() => attack.ToRuntime());
        }

        [TestCase(DefenseMask.None, DodgeSide.None, 20)]
        [TestCase((DefenseMask)8, DodgeSide.None, 20)]
        [TestCase(DefenseMask.Guard, DodgeSide.None, 0)]
        [TestCase(DefenseMask.Dodge, DodgeSide.None, 0)]
        [TestCase(DefenseMask.Parry, DodgeSide.Left, 0)]
        public void EnemyAttackReusesAuthoritativeDefenseMaskCostAndSideValidation(
            DefenseMask mask, DodgeSide sides, int guardCost)
        {
            var attack = New<EnemyAttackDefinition>("attack.enemy.test");
            attack.AllowedDefenses = mask; attack.SafeDodgeSides = sides; attack.GuardCost = guardCost;
            Assert.That(attack.Validate().IsValid, Is.False);
            Assert.Throws<ContentValidationException>(() => attack.ToRuntime());
        }

        [Test]
        public void EnemyAuthoringRejectsZeroRecoveryEvenThoughLegacyDomainFixturesPermitIt()
        {
            var attack = New<EnemyAttackDefinition>("attack.enemy.test"); attack.RecoveryDurationUs = 0;
            AssertIssue(attack.Validate(), "RecoveryDurationUs");
        }

        [TestCase(-1, 230000)] [TestCase(240000, 230000)] [TestCase(50000, 360000)]
        public void DefenseAuthoringRejectsAvoidanceOutsideTheDodgeAction(long start, long end)
        {
            var defense = New<DefenseDefinition>("defense.test");
            defense.DodgeAvoidanceStartUs = start; defense.DodgeAvoidanceEndUs = end;
            Assert.That(defense.Validate().IsValid, Is.False);
            Assert.Throws<ContentValidationException>(() => defense.ToRuntime());
        }

        [Test]
        public void SharedObjectReferencesRemainValidAcrossTheCatalog()
        {
            var catalog = Catalog();
            var second = Weapon(catalog.Weapons[0].MotionProfile, "weapon.test.second");
            second.Attacks = catalog.Weapons[0].Attacks;
            catalog.Weapons = new[] { catalog.Weapons[0], second, second };
            catalog.EnemyDecks = new[] { catalog.EnemyDecks[0], catalog.EnemyDecks[0] };
            Assert.That(catalog.Validate().IsValid, Is.True, Diagnostics(catalog.Validate()));
            Assert.That(catalog.ToRuntime(EnemyArchetype.Sword, second.Id).Weapon.Id, Is.EqualTo(second.Id));
        }

        [TestCase("attack")] [TestCase("weapon")] [TestCase("camera")]
        public void DistinctObjectsWithOneIdentityRejectTheEntireCatalog(string kind)
        {
            var catalog = Catalog();
            if (kind == "camera") catalog.Camera.Id = catalog.Weapons[0].Id;
            else
            {
                var second = Weapon(catalog.Weapons[0].MotionProfile, "weapon.test.second");
                if (kind == "weapon") second.Id = catalog.Weapons[0].Id;
                else second.Attacks[0].Id = catalog.Weapons[0].Attacks[0].Id;
                catalog.Weapons = new[] { catalog.Weapons[0], second };
            }
            AssertIssue(catalog.Validate(), "Id");
            Assert.Throws<ContentValidationException>(() => catalog.ToRuntime(EnemyArchetype.Sword, catalog.Weapons[0].Id));
        }

        [Test]
        public void InvalidUnselectedWeaponRejectsConversionBeforeReturningAnySnapshot()
        {
            var catalog = Catalog();
            var unsupported = Weapon(catalog.Weapons[0].MotionProfile, "weapon.test.unselected");
            unsupported.Family = (WeaponFamily)99;
            catalog.Weapons = new[] { catalog.Weapons[0], unsupported };
            Assert.Throws<ContentValidationException>(() => catalog.ToRuntime(EnemyArchetype.Sword, catalog.Weapons[0].Id));
        }

        [TestCase("OneHanded")] [TestCase("ShieldCompatible")]
        [TestCase("RequestedCapabilities")] [TestCase("Icon")]
        [TestCase("Materials")] [TestCase("ProvenancePath")]
        public void UnsupportedOrBrokenWeaponReferencesProduceRepairableDiagnosis(string field)
        {
            var weapon = Catalog().Weapons[0];
            switch (field)
            {
                case "OneHanded": weapon.OneHanded = false; break;
                case "ShieldCompatible": weapon.ShieldCompatible = false; break;
                case "RequestedCapabilities": weapon.RequestedCapabilities = new[] { "combat.feint.v1" }; break;
                case "Icon": weapon.ProductionReady = true; break;
                case "Materials": weapon.Materials = new Material[] { null }; break;
                default: weapon.ProvenancePath = "docs/no-such-task13-provenance.md"; break;
            }
            AssertIssue(weapon.Validate(), field);
        }

        [TestCase("../source.md")] [TestCase("C:/source.md")]
        [TestCase("docs\\source.md")] [TestCase("docs//source.md")]
        public void ProvenanceAlwaysUsesACanonicalRelativeProjectPath(string path)
        {
            var weapon = Catalog().Weapons[0]; weapon.ProvenancePath = path;
            AssertIssue(weapon.Validate(), "ProvenancePath");
        }

        [Test]
        public void WeaponRejectsDuplicateDirectionsAndUnsupportedDirectionalTuning()
        {
            var weapon = Catalog().Weapons[0];
            weapon.Attacks[1].Direction = weapon.Attacks[0].Direction;
            weapon.Attacks[1].MotionKey = weapon.Attacks[0].MotionKey;
            AssertIssue(weapon.Validate(), "Attacks");
            weapon.Attacks[1].Direction = SwipeDirection.Down; weapon.Attacks[1].MotionKey = "CutDown";
            weapon.Attacks[1].WindupUs += 20000;
            AssertIssue(weapon.Validate(), "Attacks");
        }

        [Test]
        public void LogicalTimingAndVisualContactLandmarksCanBeCapturedIndependently()
        {
            var weapon = Catalog().Weapons[0];
            foreach (var attack in weapon.Attacks) attack.WindupUs += 20000;
            var runtime = weapon.ToRuntime();
            Assert.That(runtime.ToOffenseTuning().WindupUs, Is.EqualTo(120000));
            Assert.That(weapon.MotionProfile.ToSnapshot().Bindings.Single(b => b.Key == "CutUp").ContactSeconds,
                Is.EqualTo(.1).Within(.000001));
        }

        [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)] [TestCase(-.1)]
        [TestCase(0)] [TestCase(.5)]
        public void AttackClipContactMustBeFinitePositiveAndInsideItsActualDuration(double contact)
        {
            var profile = Motion();
            EditBinding(profile, "CutUp", binding => { binding.ContactSeconds = contact; return binding; });
            Assert.That(profile.Validate().Issues.Any(issue => issue.Field.EndsWith(".ContactSeconds", StringComparison.Ordinal)), Is.True);
            Assert.Throws<ContentValidationException>(() => profile.ToSnapshot());
        }

        [Test]
        public void MotionBindingsRejectDuplicateKeysMissingClipsAndFalseDurations()
        {
            var profile = Motion();
            EditBinding(profile, "CutUp", binding => { binding.Key = "CutDown"; return binding; });
            Assert.That(profile.Validate().IsValid, Is.False);
            profile = Motion();
            EditBinding(profile, "CutUp", binding => { binding.Clip = null; return binding; });
            Assert.That(profile.Validate().IsValid, Is.False);
            profile = Motion();
            EditBinding(profile, "CutUp", binding => { binding.DurationSeconds = .6; return binding; });
            Assert.That(profile.Validate().IsValid, Is.False);
        }

        [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)] [TestCase(-.01)] [TestCase(.1)]
        public void VisualClipReleaseMustRemainFiniteWithinThePreContactInterval(double release)
        {
            var profile = Motion();
            EditBinding(profile, "CutUp", binding => { binding.ReleaseSeconds = release; return binding; });
            Assert.That(profile.Validate().Issues.Any(issue => issue.Field.EndsWith(".ReleaseSeconds", StringComparison.Ordinal)), Is.True);
        }

        [TestCase("Missing")] [TestCase("Avatar")] [TestCase("RootMotion")]
        [TestCase("Scale")] [TestCase("RendererPath")]
        public void InvalidActorsAndRendererPathsCannotCreatePresentationSnapshots(string mutation)
        {
            var profile = Motion();
            if (mutation == "Missing") profile.ActorPrefab = null;
            else if (mutation == "RendererPath") profile.WeaponRendererPaths[0] = "LOD0/missing";
            else
            {
                profile.ActorPrefab = UnityEngine.Object.Instantiate(profile.ActorPrefab); owned.Add(profile.ActorPrefab);
                var animator = profile.ActorPrefab.GetComponentInChildren<Animator>();
                if (mutation == "Avatar") animator.avatar = null;
                else if (mutation == "RootMotion") animator.applyRootMotion = true;
                else profile.ActorPrefab.transform.localScale = Vector3.zero;
            }
            Assert.That(profile.Validate().IsValid, Is.False);
            Assert.Throws<ContentValidationException>(() => profile.ToSnapshot());
        }

        [TestCase("Idle", false)] [TestCase("CutUp", true)]
        public void ImportedLoopPolicyMustMatchTheRequiredMotionRole(string key, bool loop)
        {
            var profile = Motion(); EditBinding(profile, key, binding => { binding.Loop = loop; return binding; });
            Assert.That(profile.Validate().Issues.Any(issue => issue.Field.EndsWith(".Loop", StringComparison.Ordinal)), Is.True);
        }

        [TestCase("LOD0/missing")] [TestCase("../WeaponTip")] [TestCase("LOD1/WeaponTip")]
        public void TipReferencesMustResolveAnExactRoleUnderTheSoleAnimator(string path)
        {
            var profile = Motion(); profile.WeaponTipPath = path;
            AssertIssue(profile.Validate(), "WeaponTipPath");
        }

        [Test]
        public void AmbiguousMarkerNamesCannotResolveToAnArbitrarySibling()
        {
            var profile = Motion();
            profile.ActorPrefab = UnityEngine.Object.Instantiate(profile.ActorPrefab); owned.Add(profile.ActorPrefab);
            var tip = profile.ActorPrefab.transform.Find(profile.WeaponTipPath);
            var duplicate = new GameObject("WeaponTip"); duplicate.transform.SetParent(tip.parent, false);
            AssertIssue(profile.Validate(), "WeaponTipPath");
        }

        [Test]
        public void MotionSnapshotCopiesBindingsPathsAndSettingsBeforeAssetMutation()
        {
            var profile = Motion(); var snapshot = profile.ToSnapshot();
            string path = profile.WeaponRendererPaths[0];
            EditBinding(profile, "CutUp", binding => { binding.ContactSeconds = .2; return binding; });
            profile.WeaponRendererPaths[0] = "Broken"; profile.RigId = "changed";
            Assert.That(snapshot.Bindings.Single(b => b.Key == "CutUp").ContactSeconds, Is.EqualTo(.1));
            Assert.That(snapshot.WeaponRendererPaths[0], Is.EqualTo(path));
            Assert.That(snapshot.RigId, Is.EqualTo("rig.prototype-pair.v1"));
            Assert.Throws<NotSupportedException>(() => ((IList<MotionBinding>)snapshot.Bindings)[0] = default);
            Assert.Throws<NotSupportedException>(() => ((IList<string>)snapshot.WeaponRendererPaths)[0] = "Changed");
        }

        [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)] [TestCase(-1)] [TestCase(4.01f)]
        public void CameraRejectsNonfiniteOrUnboundedOrbit(float orbit)
        {
            var camera = New<CameraProfileDefinition>("camera.test"); camera.OrbitDegrees = orbit;
            AssertIssue(camera.Validate(), "OrbitDegrees");
            Assert.Throws<ContentValidationException>(() => camera.ToSnapshot());
        }

        [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)] [TestCase(.5f)] [TestCase(12.01f)]
        public void CameraRejectsNonfiniteOrUnboundedFocusDistance(float distance)
        {
            var camera = New<CameraProfileDefinition>("camera.test"); camera.FocusDistance = distance;
            AssertIssue(camera.Validate(), "FocusDistance");
        }

        [Test]
        public void NullNestedReferencesRejectConversionWithValidationRatherThanNullDereference()
        {
            var catalog = Catalog(); catalog.Weapons[0].Attacks[0] = null;
            Assert.Throws<ContentValidationException>(() => catalog.ToRuntime(EnemyArchetype.Sword, catalog.Weapons[0].Id));
            catalog = Catalog(); catalog.EnemyDecks[0].Patterns[0] = null;
            Assert.Throws<ContentValidationException>(() => catalog.ToRuntime(EnemyArchetype.Sword, catalog.Weapons[0].Id));
        }

        [Test]
        public void CameraAndEncounterSnapshotsSurviveLaterAuthoringEdits()
        {
            var catalog = Catalog(); var runtime = catalog.ToRuntime(EnemyArchetype.Sword, catalog.Weapons[0].Id);
            var camera = catalog.Camera.ToSnapshot();
            catalog.Camera.OrbitDegrees = 0; catalog.Camera.FocusDistance = 2;
            catalog.Defense.MaximumHealth = 999; catalog.Weapons[0].Attacks[0].Damage = 99;
            catalog.EnemyDecks[0].Patterns[0].Steps[0].Attack.Damage = 99;
            catalog.Weapons = Array.Empty<WeaponDefinition>();
            Assert.That(camera.OrbitDegrees, Is.EqualTo(3)); Assert.That(camera.FocusDistance, Is.EqualTo(8));
            Assert.That(runtime.Defense.MaximumHealth, Is.EqualTo(100));
            Assert.That(runtime.Offense.AttackDamage, Is.EqualTo(20));
            Assert.That(runtime.EnemyDeck.Patterns[0].Steps[0].Damage, Is.EqualTo(25));
            Assert.Throws<NotSupportedException>(() => ((IList<AttackSnapshot>)runtime.Weapon.Attacks).Clear());
        }

        [TestCase(EnemyArchetype.Sword)] [TestCase(EnemyArchetype.Shield)]
        [TestCase(EnemyArchetype.Polearm)] [TestCase(EnemyArchetype.Hammer)]
        public void AuthoredDeckConversionPreservesTheExistingCodeContentAndRevisions(EnemyArchetype archetype)
        {
            var source = PrototypeEnemyDecks.Create(archetype); var snapshot = Deck(source).ToRuntime();
            Assert.That(snapshot.Id, Is.EqualTo(source.Id));
            Assert.That(snapshot.IntroPatternId, Is.EqualTo(source.IntroPatternId));
            Assert.That(snapshot.ContentRevision, Is.EqualTo(source.ContentRevision));
            Assert.That(snapshot.BalanceRevision, Is.EqualTo(source.BalanceRevision));
            Assert.That(snapshot.Patterns.Count, Is.EqualTo(3));
            for (int i = 0; i < source.Patterns.Count; i++)
            {
                var want = source.Patterns[i]; var actual = snapshot.Patterns[i];
                Assert.That(actual.Id, Is.EqualTo(want.Id)); Assert.That(actual.Weight, Is.EqualTo(want.Weight));
                Assert.That(actual.CooldownUs, Is.EqualTo(want.CooldownUs));
                Assert.That(actual.PreferredDefenseOutcome, Is.EqualTo(want.PreferredDefenseOutcome));
                Assert.That(actual.Steps.Select(s => s.AttackId), Is.EqualTo(want.Steps.Select(s => s.AttackId)));
                Assert.That(actual.Steps.Select(s => s.Damage), Is.EqualTo(want.Steps.Select(s => s.Damage)));
                Assert.That(actual.Steps.Select(s => s.TelegraphDurationUs), Is.EqualTo(want.Steps.Select(s => s.TelegraphDurationUs)));
                Assert.That(actual.Steps.Select(s => s.RecoveryDurationUs), Is.EqualTo(want.Steps.Select(s => s.RecoveryDurationUs)));
                Assert.That(actual.Steps.Select(s => s.GapBeforeUs), Is.EqualTo(want.Steps.Select(s => s.GapBeforeUs)));
            }
        }

        [Test]
        public void DeckRejectsBrokenIntroReferencesDuplicatePatternsAndOverflowingSequenceTiming()
        {
            var deck = Deck(PrototypeEnemyDecks.Create(EnemyArchetype.Sword)); deck.IntroPatternId = "pattern.absent";
            AssertIssue(deck.Validate(), "IntroPatternId");
            deck.IntroPatternId = deck.Patterns[0].Id; deck.Patterns[1] = deck.Patterns[0];
            Assert.That(deck.Validate().IsValid, Is.False);
            deck = Deck(PrototypeEnemyDecks.Create(EnemyArchetype.Sword));
            deck.Patterns[0].Steps[0].Attack.TelegraphDurationUs = long.MaxValue;
            Assert.Throws<ContentValidationException>(() => deck.ToRuntime());
        }

        [Test]
        public void UnknownRequestedWeaponOrArchetypeCannotSelectAFallback()
        {
            var catalog = Catalog();
            Assert.Throws<ArgumentException>(() => catalog.GetWeapon("weapon.absent"));
            Assert.Throws<ArgumentException>(() => catalog.ToRuntime(EnemyArchetype.Hammer, catalog.Weapons[0].Id));
        }
    }
}
