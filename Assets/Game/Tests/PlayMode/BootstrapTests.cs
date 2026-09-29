using System.Collections;
using System.Linq;
using NUnit.Framework;
using Praxen.Game.Application;
using Praxen.Game.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Praxen.Game.Tests.PlayMode
{
    public sealed class BootstrapTests
    {
        private BootstrapCompositionRoot bootstrap;

        [UnitySetUp]
        public IEnumerator LoadSavedBootstrap()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
            bootstrap = Object.FindFirstObjectByType<BootstrapCompositionRoot>();
            Assert.That(bootstrap, Is.Not.Null);
            bootstrap.SetFocused(true);
            bootstrap.SetPaused(false);
        }

        [UnityTest]
        public IEnumerator SavedSceneReachesOneEmptyRefugeScreen()
        {
            yield return null;
            Assert.That(bootstrap.CurrentState, Is.EqualTo(ApplicationState.Refuge));
            var screens = Object.FindObjectsByType<RefugeScreen>(FindObjectsSortMode.None);
            Assert.That(screens, Has.Length.EqualTo(1));
            Assert.That(screens[0].IsVisible, Is.True);
            Assert.That(screens[0].GetComponentsInChildren<Text>().Single().text,
                Is.EqualTo("REFUGE"));
            Assert.That(screens[0].GetComponentsInChildren<Button>(), Is.Empty);
            Assert.That(bootstrap.Diagnostics.Snapshot().Any(e =>
                e.Code == DiagnosticCode.StateChanged &&
                e.CurrentState == ApplicationState.Refuge), Is.True);
            yield return RefugeCapture.WritePortraitEvidence();
        }

        [UnityTest]
        public IEnumerator OverlappingFocusAndPauseResumeOnlyWhenBothClear()
        {
            bootstrap.SetFocused(false);
            bootstrap.SetPaused(true);
            bootstrap.SetFocused(true);
            Assert.That(bootstrap.CurrentState, Is.EqualTo(ApplicationState.Suspended));
            bootstrap.SetPaused(false);
            Assert.That(bootstrap.CurrentState, Is.EqualTo(ApplicationState.Refuge));
            Assert.That(bootstrap.Diagnostics.Snapshot().Count(e =>
                e.CurrentState == ApplicationState.Suspended), Is.EqualTo(1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator RepeatedInitializeKeepsOneViewAndStateHistory()
        {
            var count = bootstrap.Diagnostics.TotalRecorded;
            bootstrap.Initialize();
            bootstrap.Initialize();
            Assert.That(bootstrap.Diagnostics.TotalRecorded, Is.EqualTo(count));
            Assert.That(Object.FindObjectsByType<RefugeScreen>(FindObjectsSortMode.None),
                Has.Length.EqualTo(1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator DestroyingCompositionRootShutsDownOwnedServices()
        {
            var diagnostics = bootstrap.Diagnostics;
            Object.Destroy(bootstrap.gameObject);
            yield return null;
            Assert.That(diagnostics.Snapshot().Last().CurrentState,
                Is.EqualTo(ApplicationState.Shutdown));
            Assert.That(Object.FindObjectsByType<RefugeScreen>(FindObjectsSortMode.None),
                Is.Empty);
        }

        [UnityTest]
        public IEnumerator MissingViewFailsAndRetryRecoversAfterBinding()
        {
            var malformed = new GameObject("MissingViewBootstrap");
            malformed.SetActive(false);
            var root = malformed.AddComponent<BootstrapCompositionRoot>();
            malformed.SetActive(true);
            Assert.That(root.CurrentState, Is.EqualTo(ApplicationState.Failed));
            var failureView = malformed.GetComponentsInChildren<RefugeScreen>().SingleOrDefault();
            Assert.That(failureView, Is.Not.Null);
            Assert.That(failureView.IsVisible, Is.True);
            Assert.That(failureView.GetComponentsInChildren<Text>().Single().text,
                Is.EqualTo("Unable to start"));
            Assert.That(root.Retry(), Is.False);
            var child = new GameObject("ReplacementRefuge", typeof(RectTransform));
            child.transform.SetParent(malformed.transform, false);
            var screen = child.AddComponent<RefugeScreen>();
            screen.SetCamera(Camera.main);
            root.BindView(screen);
            Assert.That(root.Retry(), Is.True);
            Assert.That(root.CurrentState, Is.EqualTo(ApplicationState.Refuge));
            Assert.That(screen.IsVisible, Is.True);
            Assert.That(failureView.IsVisible, Is.False);
            Object.Destroy(malformed);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FocusReturnWhilePausedDoesNotResume()
        {
            bootstrap.SetPaused(true);
            bootstrap.SetFocused(false);
            bootstrap.SetPaused(false);
            Assert.That(bootstrap.CurrentState, Is.EqualTo(ApplicationState.Suspended));
            bootstrap.SetFocused(true);
            Assert.That(bootstrap.CurrentState, Is.EqualTo(ApplicationState.Refuge));
            yield return null;
        }

        [UnityTest]
        public IEnumerator MissingCameraShowsGenericFailureWithoutClaimingRefuge()
        {
            var malformed = new GameObject("MissingCameraBootstrap");
            malformed.SetActive(false);
            var root = malformed.AddComponent<BootstrapCompositionRoot>();
            var child = new GameObject("UnboundCameraView", typeof(RectTransform));
            child.transform.SetParent(malformed.transform, false);
            var screen = child.AddComponent<RefugeScreen>();
            root.BindView(screen);
            malformed.SetActive(true);
            Assert.That(root.CurrentState, Is.EqualTo(ApplicationState.Failed));
            Assert.That(screen.IsVisible, Is.True);
            Assert.That(screen.GetComponentInChildren<Canvas>().renderMode,
                Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(screen.GetComponentsInChildren<Text>().Single().text,
                Is.EqualTo("Unable to start"));
            Assert.That(root.Retry(), Is.False);
            Object.Destroy(malformed);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RemovingOnlyCompositionComponentHidesItsOwnedView()
        {
            var screen = bootstrap.GetComponentInChildren<RefugeScreen>();
            var diagnostics = bootstrap.Diagnostics;
            Object.Destroy(bootstrap);
            yield return null;
            Assert.That(diagnostics.Snapshot().Last().CurrentState,
                Is.EqualTo(ApplicationState.Shutdown));
            Assert.That(screen.IsVisible, Is.False);
        }
    }
}
