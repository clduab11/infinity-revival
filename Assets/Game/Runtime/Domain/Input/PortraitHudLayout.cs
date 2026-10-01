using System;

namespace Praxen.Game.Domain.Input
{
    public enum HudHand { Right, Left }

    public sealed class HudSettings
    {
        public HudSettings(HudHand hand = HudHand.Right, double scale = 1,
            double offsetX = 0, double offsetY = 0)
        {
            if (hand != HudHand.Right && hand != HudHand.Left)
                throw new ArgumentOutOfRangeException(nameof(hand));
            RequireFinite(scale, nameof(scale));
            RequireFinite(offsetX, nameof(offsetX));
            RequireFinite(offsetY, nameof(offsetY));
            Hand = hand;
            Scale = Clamp(scale, .85, 1.20);
            OffsetX = Clamp(offsetX, -.08, .08);
            OffsetY = Clamp(offsetY, -.06, .06);
        }

        public HudHand Hand { get; }
        public double Scale { get; }
        public double OffsetX { get; }
        public double OffsetY { get; }

        internal static double Clamp(double value, double min, double max) =>
            Math.Max(min, Math.Min(max, value));

        internal static void RequireFinite(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(name);
        }
    }

    public readonly struct HudRect
    {
        public HudRect(double x, double y, double width, double height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public double X { get; }
        public double Y { get; }
        public double Width { get; }
        public double Height { get; }
        public double CenterX => X + Width / 2;
        public double CenterY => Y + Height / 2;
    }

    public sealed class PortraitHudLayout
    {
        private PortraitHudLayout(HudRect guard, HudRect dodgeLeft, HudRect dodgeRight,
            HudRect ability, HudRect pause, HudRect gestureArea, HudRect tellArea)
        {
            Guard = guard;
            DodgeLeft = dodgeLeft;
            DodgeRight = dodgeRight;
            Ability = ability;
            Pause = pause;
            GestureArea = gestureArea;
            TellArea = tellArea;
        }

        public HudRect Guard { get; }
        public HudRect DodgeLeft { get; }
        public HudRect DodgeRight { get; }
        public HudRect Ability { get; }
        public HudRect Pause { get; }
        public HudRect GestureArea { get; }
        public HudRect TellArea { get; }

        public static PortraitHudLayout Create(double safeWidth, double safeHeight,
            HudSettings settings)
        {
            HudSettings.RequireFinite(safeWidth, nameof(safeWidth));
            HudSettings.RequireFinite(safeHeight, nameof(safeHeight));
            if (safeWidth <= 0) throw new ArgumentOutOfRangeException(nameof(safeWidth));
            if (safeHeight <= 0) throw new ArgumentOutOfRangeException(nameof(safeHeight));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            var ratio = safeWidth / safeHeight;
            var scale = settings.Scale;
            var rects = new[] {
                Square(.79, .19, .18 * scale, ratio),
                Square(.56, .19, .14 * scale, ratio),
                Square(.79, .34, .14 * scale, ratio),
                Square(.56, .34, .14 * scale, ratio),
                Square(.90, .055, .10 * scale, ratio)
            };
            FitCluster(rects, settings.OffsetX, settings.OffsetY);
            var clearRight = 1.0;
            foreach (var rect in rects) clearRight = Math.Min(clearRight, rect.X);
            var gesture = new HudRect(0, 0, Math.Max(0, clearRight - .025), .45);
            var tell = new HudRect(.10, .70, .80, .15);
            if (settings.Hand == HudHand.Left)
            {
                for (var i = 0; i < rects.Length; i++) rects[i] = Mirror(rects[i]);
                gesture = Mirror(gesture);
                tell = Mirror(tell);
            }
            return new PortraitHudLayout(rects[0], rects[1], rects[2],
                rects[3], rects[4], gesture, tell);
        }

        private static HudRect Square(double x, double y, double diameter, double ratio) =>
            new HudRect(x - diameter / 2, y - diameter * ratio / 2,
                diameter, diameter * ratio);

        private static HudRect Mirror(HudRect rect) =>
            new HudRect(1 - rect.X - rect.Width, rect.Y, rect.Width, rect.Height);

        private static void FitCluster(HudRect[] rects, double offsetX, double offsetY)
        {
            var minX = 1.0;
            var minY = 1.0;
            var maxX = 0.0;
            var maxY = 0.0;
            foreach (var rect in rects)
            {
                minX = Math.Min(minX, rect.X);
                minY = Math.Min(minY, rect.Y);
                maxX = Math.Max(maxX, rect.X + rect.Width);
                maxY = Math.Max(maxY, rect.Y + rect.Height);
            }
            // Translate the complete cluster together, preserving spacing and square pixels.
            var x = HudSettings.Clamp(offsetX, -minX, 1 - maxX);
            var y = HudSettings.Clamp(offsetY, -minY, .45 - maxY);
            for (var i = 0; i < rects.Length; i++)
            {
                var rect = rects[i];
                rects[i] = new HudRect(rect.X + x, rect.Y + y, rect.Width, rect.Height);
            }
        }
    }
}
