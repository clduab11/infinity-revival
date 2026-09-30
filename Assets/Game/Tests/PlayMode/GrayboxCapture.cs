using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using NUnit.Framework;

namespace Praxen.Game.Tests.PlayMode
{
    internal static class GrayboxCapture
    {
        public static IEnumerator WarmFrame(Camera camera)
        {
            Assert.That(camera, Is.Not.Null);
            var target = new RenderTexture(720, 1280, 24);
            var previous = camera.targetTexture;
            try
            {
                target.Create();
                camera.targetTexture = target;
                Canvas.ForceUpdateCanvases();
                var request = new RenderPipeline.StandardRequest { destination = target };
                Assert.That(RenderPipeline.SupportsRenderRequest(camera, request), Is.True);
                RenderPipeline.SubmitRenderRequest(camera, request);
            }
            finally
            {
                camera.targetTexture = previous;
                target.Release();
                Object.Destroy(target);
                Canvas.ForceUpdateCanvases();
            }
            // Runtime mesh upload and camera-space graphic depth need a complete graphics frame.
            yield return null;
        }
    }
}
