using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Praxen.Game.Application.Combat;
using Praxen.Game.Application.Input;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;
using UnityEditor;
using UnityEngine;

namespace Praxen.Game.Tests.EditMode
{
    public sealed class CombatPresentationAdmissionTests
    {
        private static readonly InteractionPhase Phase = new InteractionPhase(1, InteractionPhaseKind.EnemySequence);

        [Test]
        public void OnlyAcceptedDefenseControlsNotifyAtTheirResolvedTimestamp()
        {
            var session = new CombatSession(0);
            using (var encounter = new CombatEncounter(session, Phase))
            {
                var seen = new List<string>();
                Subscribe(encounter, "DefenseControlAccepted", (Action<DefenseCommand, long>)
                    ((command, time) => seen.Add(command.Kind + ":" + time)));
                Control(session, 1, 10000, DefenseCommandKind.GuardPress);
                Control(session, 2, 20000, DefenseCommandKind.DodgeLeft);
                Control(session, 3, 30000, DefenseCommandKind.DodgeRight);
                Control(session, 4, 40000, DefenseCommandKind.GuardRelease);
                Assert.That(seen, Is.EqualTo(new[] { "GuardPress:10000", "DodgeLeft:20000", "GuardRelease:40000" }));
                Assert.That(encounter.Player.DodgeCharges, Is.EqualTo(2));
            }
        }

        [Test]
        public void RejectedAndRepeatedParriesCannotStartPresentation()
        {
            var session = new CombatSession(0);
            using (var encounter = new CombatEncounter(session, Phase))
            {
                var seen = new List<long>();
                Subscribe(encounter, "ParryAccepted", (Action<GestureCommand, long>)((command, time) => seen.Add(time)));
                Assert.That(encounter.CommitStrike(Strike(), 0), Is.True);
                StepTo(session, 500000);
                Gesture(session, 1, 550000, GestureIntent.Parry, SwipeDirection.Up, Phase);
                Gesture(session, 2, 600000, GestureIntent.Parry, SwipeDirection.Right, Phase);
                Gesture(session, 3, 610000, GestureIntent.Parry, SwipeDirection.Right, Phase);
                StepTo(session, 650000);
                Assert.That(seen, Is.EqualTo(new[] { 600000L }));
                Assert.That(encounter.Player.Health, Is.EqualTo(100));
            }
        }

        [Test]
        public void BufferedAttackNotifiesAtActualAdmissionAndCannotLeakWrongPhaseIntent()
        {
            var session = new CombatSession(0);
            var phases = new GesturePhaseContext();
            phases.SetPhase(Phase);
            using (var encounter = new CombatEncounter(session, Phase))
            using (var duel = new CombatOpeningController(encounter, phases))
            {
                var seen = new List<PlayerAttack>();
                Subscribe(duel, "PlayerAttackStarted", (Action<PlayerAttack>)seen.Add);
                Assert.That(duel.CommitStrike(Strike(), 0), Is.True);
                StepTo(session, 1150000);
                var opening = duel.CurrentPhase;
                Gesture(session, 1, 1160000, GestureIntent.Attack, SwipeDirection.Left, opening);
                Gesture(session, 2, 1200000, GestureIntent.Attack, SwipeDirection.Down,
                    new InteractionPhase(opening.Id + 100, InteractionPhaseKind.PlayerOpening));
                StepTo(session, 1500000);
                Gesture(session, 3, 1600000, GestureIntent.Attack, SwipeDirection.Right, opening);
                Assert.That(seen.Count, Is.EqualTo(1), "Buffered intent is not an admitted action.");
                Assert.That(duel.Offense.HasBuffer, Is.True);
                StepTo(session, 1660000);
                Assert.That(seen.Select(a => a.StartedUs), Is.EqualTo(new[] { 1160000L, 1660000L }));
                Assert.That(seen.Select(a => a.Direction), Is.EqualTo(new[] { SwipeDirection.Left, SwipeDirection.Right }));
                Assert.That(duel.Offense.EnemyHealth, Is.EqualTo(80));
            }
        }

        [Test]
        public void SavedPresentationProfileReferencesBothHumanoidsAndAllThirteenMotionClips()
        {
            var profile = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Game/Content/CombatPresentationProfile.asset");
            Assert.That(profile, Is.Not.Null, "Task12 presentation profile must be a saved asset.");
            var serialized = new SerializedObject(profile);
            foreach (string actor in new[] { "Soldier", "Captain" })
            {
                var prefab = serialized.FindProperty(actor).objectReferenceValue as GameObject;
                Assert.That(prefab, Is.Not.Null, actor);
                Assert.That(prefab.GetComponentInChildren<Animator>().avatar.isHuman, Is.True, actor);
                var bindings = serialized.FindProperty(actor + "Motions");
                Assert.That(bindings.arraySize, Is.EqualTo(13), actor);
                var names = new HashSet<string>();
                for (int i = 0; i < bindings.arraySize; i++)
                {
                    var binding = bindings.GetArrayElementAtIndex(i);
                    string key = binding.FindPropertyRelative("Key").stringValue;
                    var clip = binding.FindPropertyRelative("Clip").objectReferenceValue as AnimationClip;
                    Assert.That(clip, Is.Not.Null, key);
                    Assert.That(clip.isHumanMotion, Is.True, key);
                    Assert.That(names.Add(key), Is.True, key + " duplicated");
                    Assert.That(AnimationUtility.GetAnimationEvents(clip), Is.Empty, key);
                }
                Assert.That(names, Is.EquivalentTo(new[] { "Idle", "Guard", "Parry", "DodgeLeft", "DodgeRight",
                    "CutUp", "CutDown", "CutLeft", "CutRight", "Hit", "Death", "EnemyTell", "EnemyAttack" }));
            }
        }

        private static void Subscribe(object owner, string name, Delegate handler)
        {
            var notification = owner.GetType().GetEvent(name);
            Assert.That(notification, Is.Not.Null, "Missing accepted-action observation: " + name);
            notification.AddEventHandler(owner, handler);
        }

        private static EnemyStrike Strike() => new EnemyStrike(1, DefenseMask.Parry, DodgeSide.None,
            SwipeDirection.Right, 0, 25);

        private static void Control(CombatSession session, long id, long time, DefenseCommandKind kind) =>
            session.AdvanceInputFrame(time, new[] { new CombatInput(new DefenseCommand(id, time, kind)) });

        private static void Gesture(CombatSession session, long id, long time, GestureIntent intent,
            SwipeDirection direction, InteractionPhase phase) => session.AdvanceFrame(time,
                new[] { new GestureCommand(id, time, 1, direction, intent, phase,
                    new NormalizedPoint(.5, .5), new NormalizedPoint(.7, .5)) });

        private static void StepTo(CombatSession session, long time)
        {
            while (session.Clock.TimeUs < time)
                session.AdvanceInputFrame(Math.Min(time, session.Clock.TimeUs + 100000), Array.Empty<CombatInput>());
        }
    }
}
