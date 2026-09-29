using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Praxen.Game.Application;
using Praxen.Game.Content.Input;
using Praxen.Game.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Praxen.Game.Tests.PlayMode
{
    public sealed class InputCompositionTests
    {
        [UnityTest]
        public IEnumerator InvalidTuningFailsCleanlyAndRetryUsesReplacement()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
            var malformed = new GameObject("InvalidInputBootstrap");
            malformed.SetActive(false);
            var root = malformed.AddComponent<BootstrapCompositionRoot>();
            var child = new GameObject("InputRetryRefuge", typeof(RectTransform));
            child.transform.SetParent(malformed.transform, false);
            var view = child.AddComponent<RefugeScreen>();
            view.SetCamera(Camera.main);
            root.BindView(view);
            var invalid = ScriptableObject.CreateInstance<GestureTuningAsset>();
            var valid = ScriptableObject.CreateInstance<GestureTuningAsset>();
            typeof(GestureTuningAsset).GetField("travelThreshold",
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(invalid, 0.0);
            try
            {
                root.BindInputTuning(invalid);
                malformed.SetActive(true);
                Assert.That(root.CurrentState, Is.EqualTo(ApplicationState.Failed));
                Assert.That(root.InputCapture, Is.Null, "Failed tuning must not publish a partial capture.");
                root.BindInputTuning(valid);
                Assert.That(root.Retry(), Is.True);
                Assert.That(root.CurrentState, Is.EqualTo(ApplicationState.Refuge));
                Assert.That(root.InputCapture, Is.Not.Null);
                Assert.That(root.GestureTrace, Is.Not.Null);
            }
            finally
            {
                Object.Destroy(malformed);
                Object.Destroy(invalid);
                Object.Destroy(valid);
            }
            yield return null;
        }
    }
}
