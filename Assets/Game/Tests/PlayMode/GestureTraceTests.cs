using System.Collections;
using NUnit.Framework;
using Praxen.Game.Domain.Input;
using Praxen.Game.Presentation.Input;
using UnityEngine;
using UnityEngine.TestTools;

namespace Praxen.Game.Tests.PlayMode
{
    public sealed class GestureTraceTests
    {
        private GameObject owner;
        private GestureTraceView trace;

        [SetUp]
        public void SetUp()
        {
            owner = new GameObject("TraceTestCanvas", typeof(RectTransform), typeof(Canvas));
            var canvas = owner.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            owner.GetComponent<RectTransform>().sizeDelta = new Vector2(640, 480);
            var child = new GameObject("TraceTest", typeof(RectTransform));
            trace = child.AddComponent<GestureTraceView>();
            trace.Configure(canvas);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(owner);

        private static GestureCommand Command() => new GestureCommand(1, 0, 1,
            SwipeDirection.Right, GestureIntent.Parry,
            new InteractionPhase(1, InteractionPhaseKind.EnemySequence),
            new NormalizedPoint(0.15, 0.25), new NormalizedPoint(0.25, 0.25));

        [Test]
        public void TraceRemapsNormalizedEndpointsWhenItsSurfaceResizes()
        {
            trace.Show(Command());
            var rect = owner.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1200, 800);
            rect.ForceUpdateRectTransforms();
            Canvas.ForceUpdateCanvases();
            var shaft = trace.transform.Find("SwipeTrace").GetComponent<RectTransform>();
            Assert.That(shaft.anchoredPosition.x, Is.EqualTo(-360).Within(0.01));
            Assert.That(shaft.anchoredPosition.y, Is.EqualTo(-200).Within(0.01));
            Assert.That(shaft.sizeDelta.x, Is.EqualTo(120).Within(0.01));
        }

        [UnityTest]
        public IEnumerator TraceExpiresWithoutFurtherInput()
        {
            trace.Show(Command());
            Assert.That(trace.IsVisible, Is.True);
            yield return new WaitForSecondsRealtime(0.25f);
            Assert.That(trace.IsVisible, Is.False);
        }
    }
}
