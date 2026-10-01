using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace Praxen.Game.Tests.PlayMode
{
    internal static class PortraitHudCapture
    {
        public static IEnumerator Save(Camera camera,int width,int height,string filename)
        {
            var output=Environment.GetEnvironmentVariable("PRAXEN_TEST_OUTPUT");
            if(string.IsNullOrEmpty(output)) yield break;
            var target=new RenderTexture(width,height,24);
            var prior=camera.targetTexture;var active=RenderTexture.active;
            Texture2D pixels=null;
            try
            {
                target.Create();camera.targetTexture=target;Canvas.ForceUpdateCanvases();
                var request=new RenderPipeline.StandardRequest{destination=target};
                Assert.That(RenderPipeline.SupportsRenderRequest(camera,request),Is.True);
                RenderPipeline.SubmitRenderRequest(camera,request);
                RenderTexture.active=target;
                pixels=new Texture2D(width,height,TextureFormat.RGB24,false);
                pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
                Directory.CreateDirectory(output);
                var encoded=pixels.EncodeToPNG();Assert.That(encoded.Length,Is.GreaterThan(10000));
                File.WriteAllBytes(Path.Combine(output,filename),encoded);
            }
            finally
            {
                camera.targetTexture=prior;RenderTexture.active=active;
                if(pixels!=null) UnityEngine.Object.Destroy(pixels);
                target.Release();UnityEngine.Object.Destroy(target);
                Canvas.ForceUpdateCanvases();
            }
            yield return null;
        }
    }
}
