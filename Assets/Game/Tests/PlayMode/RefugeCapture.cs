using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace Praxen.Game.Tests.PlayMode
{
    internal static class RefugeCapture
    {
        public static IEnumerator WritePortraitEvidence(string filename = "refuge-portrait.png")
        {
            yield return null;
            var camera = Camera.main;
            Assert.That(camera, Is.Not.Null);
            var target = new RenderTexture(720, 1280, 24);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            Texture2D pixels = null;
            try
            {
                target.Create();
                camera.targetTexture = target;
                Canvas.ForceUpdateCanvases();
                var request = new RenderPipeline.StandardRequest { destination = target };
                Assert.That(RenderPipeline.SupportsRenderRequest(camera, request), Is.True);
                RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = target;
                pixels = new Texture2D(720, 1280, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, 720, 1280), 0, 0);
                pixels.Apply();
                var directory = System.Environment.GetEnvironmentVariable("PRAXEN_TEST_OUTPUT")
                    ?? Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,
                        "..", "output", "task-03"));
                var output = Path.Combine(directory, filename);
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                File.WriteAllBytes(output, pixels.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                target.Release();
                Object.Destroy(target);
                if (pixels != null) Object.Destroy(pixels);
                Canvas.ForceUpdateCanvases();
            }
        }
    }
}
