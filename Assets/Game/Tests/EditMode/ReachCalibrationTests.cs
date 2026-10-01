using System;
using NUnit.Framework;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Tests.EditMode
{
    public sealed class ReachCalibrationTests
    {
        [Test]
        public void RequiresThreeAcceptedSamplesAndStopsAtCompletion()
        {
            var calibration = new ReachCalibration();
            Assert.That(calibration.IsComplete, Is.False);
            Assert.Throws<InvalidOperationException>(() => calibration.Result(new HudSettings()));
            Assert.That(calibration.Add(.80, .20), Is.True);
            Assert.That(calibration.Add(.81, .21), Is.True);
            Assert.That(calibration.IsComplete, Is.False);
            Assert.That(calibration.Add(.82, .22), Is.True);
            Assert.That(calibration.IsComplete, Is.True);
            Assert.That(calibration.SampleCount, Is.EqualTo(3));
            Assert.That(calibration.Add(.5, .3), Is.False);
            Assert.That(calibration.SampleCount, Is.EqualTo(3));
            var result = calibration.Result(new HudSettings(scale: 1.1, offsetX: -.05));
            Assert.That(result.OffsetX, Is.EqualTo(.02).Within(1e-10));
            Assert.That(result.OffsetY, Is.EqualTo(.02).Within(1e-10));
            Assert.That(result.Scale, Is.EqualTo(1.1));
        }

        [TestCase(-.001, .2)]
        [TestCase(1.001, .2)]
        [TestCase(.8, -.001)]
        [TestCase(.8, .450001)]
        [TestCase(double.NaN, .2)]
        [TestCase(.8, double.PositiveInfinity)]
        public void InvalidSamplesDoNotAdvance(double x, double y)
        {
            var calibration = new ReachCalibration();
            Assert.That(calibration.Add(x, y), Is.False);
            Assert.That(calibration.SampleCount, Is.Zero);
        }

        [Test]
        public void BoundariesAreAcceptedAndOffsetsClamped()
        {
            var calibration = new ReachCalibration();
            calibration.Add(0, 0);
            calibration.Add(0, 0);
            calibration.Add(0, 0);
            var result = calibration.Result(new HudSettings());
            Assert.That(result.OffsetX, Is.EqualTo(-.08));
            Assert.That(result.OffsetY, Is.EqualTo(-.06));
            calibration.Reset();
            Assert.That(calibration.SampleCount, Is.Zero);
            Assert.That(calibration.IsComplete, Is.False);
            Assert.Throws<InvalidOperationException>(() => calibration.Result(new HudSettings()));
            calibration.Add(1, .45);
            calibration.Add(1, .45);
            calibration.Add(1, .45);
            Assert.That(calibration.Result(new HudSettings()).OffsetX, Is.EqualTo(.08));
            Assert.That(calibration.Result(new HudSettings()).OffsetY, Is.EqualTo(.06));
        }

        [Test]
        public void LeftHandSamplesUseMirroredDefaultGuardAndPreserveHand()
        {
            var calibration = new ReachCalibration();
            calibration.Add(.19, .18);
            calibration.Add(.19, .18);
            calibration.Add(.19, .18);
            var result = calibration.Result(new HudSettings(HudHand.Left));
            Assert.That(result.Hand, Is.EqualTo(HudHand.Left));
            Assert.That(result.OffsetX, Is.EqualTo(.02).Within(1e-10));
            Assert.That(result.OffsetY, Is.EqualTo(-.01).Within(1e-10));
        }
    }
}
