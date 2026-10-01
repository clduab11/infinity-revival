using System;
using NUnit.Framework;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Tests.EditMode
{
    public sealed class PortraitHudLayoutTests
    {
        [Test]
        public void DefaultsAndBoundsAreExplicit()
        {
            var settings = new HudSettings();
            Assert.That(settings.Hand, Is.EqualTo(HudHand.Right));
            Assert.That(settings.Scale, Is.EqualTo(1));
            var bounded = new HudSettings(HudHand.Left, 2, -1, 1);
            Assert.That(bounded.Scale, Is.EqualTo(1.20));
            Assert.That(bounded.OffsetX, Is.EqualTo(-0.08));
            Assert.That(bounded.OffsetY, Is.EqualTo(0.06));
            Assert.That(new HudSettings(scale: 0).Scale, Is.EqualTo(0.85));
        }

        [Test]
        public void BaseGeometryUsesSafeAreaPixelsForSquares()
        {
            var layout = PortraitHudLayout.Create(1000, 2000, new HudSettings());
            AssertRect(layout.Guard, .79, .19, .18, .09);
            AssertRect(layout.DodgeLeft, .56, .19, .14, .07);
            AssertRect(layout.DodgeRight, .79, .34, .14, .07);
            AssertRect(layout.Ability, .56, .34, .14, .07);
            AssertRect(layout.Pause, .90, .055, .10, .05);
            Assert.That(layout.TellArea.Y, Is.EqualTo(.70).Within(1e-10));
            Assert.That(layout.TellArea.Height, Is.EqualTo(.15).Within(1e-10));
        }

        [TestCase(1.4)]
        [TestCase(2.4)]
        [TestCase(2868.0 / 1320)]
        [TestCase(3120.0 / 1440)]
        public void AllScaleOffsetExtremesFitWithoutOverlap(double aspect)
        {
            foreach (var scale in new[] { .85, 1.0, 1.20 })
            foreach (var x in new[] { -.08, 0, .08 })
            foreach (var y in new[] { -.06, 0, .06 })
            foreach (var hand in new[] { HudHand.Right, HudHand.Left })
            {
                var layout = PortraitHudLayout.Create(1000, 1000 * aspect,
                    new HudSettings(hand, scale, x, y));
                var controls = new[] { layout.Guard, layout.DodgeLeft,
                    layout.DodgeRight, layout.Ability, layout.Pause };
                for (var i = 0; i < controls.Length; i++)
                {
                    var rect = controls[i];
                    Assert.That(rect.X, Is.GreaterThanOrEqualTo(-1e-10));
                    Assert.That(rect.X + rect.Width, Is.LessThanOrEqualTo(1 + 1e-10));
                    Assert.That(rect.Y, Is.GreaterThanOrEqualTo(-1e-10));
                    Assert.That(rect.Y + rect.Height, Is.LessThanOrEqualTo(.45 + 1e-10));
                    Assert.That(rect.Width, Is.EqualTo(rect.Height * aspect).Within(1e-10));
                    Assert.That(Overlaps(rect, layout.GestureArea), Is.False);
                    Assert.That(Overlaps(rect, layout.TellArea), Is.False);
                    for (var j = i + 1; j < controls.Length; j++)
                        Assert.That(Overlaps(rect, controls[j]), Is.False);
                }
                Assert.That(layout.GestureArea.Width, Is.GreaterThan(0));
                Assert.That(layout.GestureArea.Height, Is.GreaterThan(0));
            }
        }

        [Test]
        public void LeftHandMirrorsEveryRectWithoutSwappingDodgeMeaning()
        {
            var right = PortraitHudLayout.Create(1000, 1800, new HudSettings(offsetX: .03));
            var left = PortraitHudLayout.Create(1000, 1800, new HudSettings(HudHand.Left, offsetX: .03));
            var a = new[] { right.Guard, right.DodgeLeft, right.DodgeRight, right.Ability,
                right.Pause, right.GestureArea, right.TellArea };
            var b = new[] { left.Guard, left.DodgeLeft, left.DodgeRight, left.Ability,
                left.Pause, left.GestureArea, left.TellArea };
            for (var i = 0; i < a.Length; i++)
            {
                Assert.That(b[i].X, Is.EqualTo(1 - a[i].X - a[i].Width).Within(1e-10));
                Assert.That(b[i].Y, Is.EqualTo(a[i].Y).Within(1e-10));
                Assert.That(b[i].Width, Is.EqualTo(a[i].Width).Within(1e-10));
                Assert.That(b[i].Height, Is.EqualTo(a[i].Height).Within(1e-10));
            }
        }

        [Test]
        public void ScaleChangesDiameterAndOffsetsTranslateWithoutChangingSpacing()
        {
            var layout = PortraitHudLayout.Create(1000, 2000,
                new HudSettings(scale: 1.2, offsetX: -.03, offsetY: .01));
            AssertRect(layout.Guard, .76, .20, .216, .108);
            Assert.That(layout.DodgeRight.CenterY - layout.Guard.CenterY,
                Is.EqualTo(.15).Within(1e-10));
        }

        [Test]
        public void ProportionalSafeAreaSizesKeepNormalizedGeometry()
        {
            var a = PortraitHudLayout.Create(1200, 2400, new HudSettings());
            var b = PortraitHudLayout.Create(1000, 2000, new HudSettings());
            Assert.That(a.Guard.X, Is.EqualTo(b.Guard.X));
            Assert.That(a.Guard.Height, Is.EqualTo(b.Guard.Height));
        }

        [Test]
        public void InvalidSettingsAndDimensionsFailFast()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new HudSettings((HudHand)99));
            foreach (var invalid in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => new HudSettings(scale: invalid));
                Assert.Throws<ArgumentOutOfRangeException>(() => new HudSettings(offsetX: invalid));
                Assert.Throws<ArgumentOutOfRangeException>(() => new HudSettings(offsetY: invalid));
                Assert.Throws<ArgumentOutOfRangeException>(() => PortraitHudLayout.Create(invalid, 2000, new HudSettings()));
                Assert.Throws<ArgumentOutOfRangeException>(() => PortraitHudLayout.Create(1000, invalid, new HudSettings()));
            }
            Assert.Throws<ArgumentOutOfRangeException>(() => PortraitHudLayout.Create(0, 2000, new HudSettings()));
            Assert.Throws<ArgumentOutOfRangeException>(() => PortraitHudLayout.Create(1000, -1, new HudSettings()));
        }

        private static void AssertRect(HudRect rect, double x, double y, double width, double height)
        {
            Assert.That(rect.CenterX, Is.EqualTo(x).Within(1e-10));
            Assert.That(rect.CenterY, Is.EqualTo(y).Within(1e-10));
            Assert.That(rect.Width, Is.EqualTo(width).Within(1e-10));
            Assert.That(rect.Height, Is.EqualTo(height).Within(1e-10));
        }

        private static bool Overlaps(HudRect a, HudRect b) =>
            a.X < b.X + b.Width - 1e-10 && b.X < a.X + a.Width - 1e-10 &&
            a.Y < b.Y + b.Height - 1e-10 && b.Y < a.Y + a.Height - 1e-10;
    }
}
