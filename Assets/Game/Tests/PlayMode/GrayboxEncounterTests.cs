using System.Collections;
using NUnit.Framework;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Presentation;
using Praxen.Game.Presentation.Combat;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Praxen.Game.Tests.PlayMode
{
    public sealed class GrayboxEncounterTests
    {
        [UnityTest]
        public IEnumerator SavedGrayboxStartsOwnedPrimitivesAndCorrectTitle()
        {
            yield return SceneManager.LoadSceneAsync("GrayboxEncounter", LoadSceneMode.Single);
            var root = Object.FindFirstObjectByType<GrayboxEncounterRoot>();
            root.SetLifecycle(false, true);
            Assert.That(root.Encounter.Player.Health, Is.EqualTo(100));
            Assert.That(root.View.PlayerAnchor, Is.Not.Null);
            Assert.That(root.View.EnemyAnchor, Is.Not.Null);
            Assert.That(root.View.StatusText, Does.Contain("100"));
            Assert.That(Object.FindObjectsByType<RefugeScreen>(FindObjectsSortMode.None), Is.Empty);
            yield return GrayboxCapture.WarmFrame(Camera.main);
            yield return RefugeCapture.WritePortraitEvidence("forever-we-reign-graybox.png");
            AssertRenderedCombatants();
        }

        [UnityTest]
        public IEnumerator DodgePoseContinuesThroughRecoveryAndReturnsAtActionEnd()
        {
            yield return SceneManager.LoadSceneAsync("GrayboxEncounter", LoadSceneMode.Single);
            var root = Object.FindFirstObjectByType<GrayboxEncounterRoot>();
            root.Timing.EndEncounter();
            var model = new DefenseCombatant();
            var initial = root.View.PlayerAnchor.localPosition;
            model.ApplyControl(new DefenseCommand(1, 0, DefenseCommandKind.DodgeLeft), 0);
            model.AdvanceTo(230001);
            root.View.Show(model, "", "", new CombatClock(0));
            Assert.That(model.State, Is.EqualTo(PlayerCombatState.Recovery));
            Assert.That(root.View.PlayerAnchor.localPosition.x, Is.LessThan(initial.x));
            Assert.That(root.View.PlayerAnchor.localPosition.x, Is.GreaterThanOrEqualTo(initial.x - 0.65f));
            model.AdvanceTo(360000);
            root.View.Show(model, "", "", new CombatClock(0));
            Assert.That(root.View.PlayerAnchor.localPosition, Is.EqualTo(initial));
        }

        private static void AssertRenderedCombatants()
        {
            var folder = System.Environment.GetEnvironmentVariable("PRAXEN_TEST_OUTPUT") ??
                System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath,
                    "..", "output", "task-03"));
            var texture = new Texture2D(2, 2);
            try
            {
                Assert.That(texture.LoadImage(System.IO.File.ReadAllBytes(System.IO.Path.Combine(folder,
                    "forever-we-reign-graybox.png"))), Is.True);
                int playerPixels = 0, enemyPixels = 0;
                foreach (var pixel in texture.GetPixels32())
                {
                    if (pixel.r > 40 && pixel.b > 60 && pixel.b > pixel.r * 1.05f) playerPixels++;
                    if (pixel.r > 60 && pixel.r > pixel.g * 1.1f && pixel.g > pixel.b * 1.02f) enemyPixels++;
                }
                Assert.That(playerPixels, Is.GreaterThan(2000), "Blue player must appear in the rendered evidence.");
                Assert.That(enemyPixels, Is.GreaterThan(2000), "Rust opponent must appear in the rendered evidence.");
            }
            finally { Object.Destroy(texture); }
        }

        [UnityTest]
        public IEnumerator DisablingViewDoesNotAlterAuthoredDamageOrGuard()
        {
            yield return SceneManager.LoadSceneAsync("GrayboxEncounter", LoadSceneMode.Single);
            var root = Object.FindFirstObjectByType<GrayboxEncounterRoot>();
            root.SetLifecycle(false, true);
            root.View.enabled = false;
            var outcome = root.Encounter.Player.ResolveImpact(new EnemyStrike(99,
                DefenseMask.Parry, DodgeSide.None, Domain.Input.SwipeDirection.Right, 0, 25),
                root.Encounter.Player.TimeUs);
            Assert.That(outcome.Outcome, Is.EqualTo(DefenseOutcome.Hit));
            Assert.That(root.Encounter.Player.Health, Is.EqualTo(75));
        }

        [UnityTest]
        public IEnumerator UnloadRestoresRefugeAndReleasesEncounterObjects()
        {
            yield return SceneManager.LoadSceneAsync("GrayboxEncounter", LoadSceneMode.Single);
            var old = Object.FindFirstObjectByType<GrayboxEncounterRoot>();
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
            Assert.That(old == null, Is.True);
            Assert.That(Object.FindObjectsByType<GrayboxEncounterView>(FindObjectsSortMode.None), Is.Empty);
            var refuge = Object.FindFirstObjectByType<BootstrapCompositionRoot>();
            Assert.That(refuge.Timing.Session, Is.Null);
            Assert.That(refuge.CurrentState, Is.EqualTo(Application.ApplicationState.Refuge));
        }
    }
}
