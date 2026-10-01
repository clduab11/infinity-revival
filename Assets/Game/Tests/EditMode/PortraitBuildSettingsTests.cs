using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Praxen.Game.Tests.EditMode
{
    public sealed class PortraitBuildSettingsTests
    {
        [Test]
        public void MobileDefaultsSelectPortraitForTheOneThumbHud()
        {
            Debug.Log($"Installed portrait orientation enum: {(int)UIOrientation.Portrait}");
            Assert.That(PlayerSettings.defaultInterfaceOrientation, Is.EqualTo(UIOrientation.Portrait));
            Assert.That(PlayerSettings.allowedAutorotateToLandscapeLeft, Is.False);
            Assert.That(PlayerSettings.allowedAutorotateToLandscapeRight, Is.False);
        }
    }
}
