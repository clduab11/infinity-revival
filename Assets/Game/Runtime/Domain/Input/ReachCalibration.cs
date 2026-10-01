using System;

namespace Praxen.Game.Domain.Input
{
    public sealed class ReachCalibration
    {
        private double sumX;
        private double sumY;

        public int SampleCount { get; private set; }
        public bool IsComplete => SampleCount == 3;

        public bool Add(double normalizedSafeX, double normalizedSafeY)
        {
            if (IsComplete || double.IsNaN(normalizedSafeX) || double.IsInfinity(normalizedSafeX) ||
                double.IsNaN(normalizedSafeY) || double.IsInfinity(normalizedSafeY) ||
                normalizedSafeX < 0 || normalizedSafeX > 1 ||
                normalizedSafeY < 0 || normalizedSafeY > .45)
                return false;
            sumX += normalizedSafeX;
            sumY += normalizedSafeY;
            SampleCount++;
            return true;
        }

        public HudSettings Result(HudSettings baseline)
        {
            if (!IsComplete) throw new InvalidOperationException("Three accepted reach samples are required.");
            if (baseline == null) throw new ArgumentNullException(nameof(baseline));
            // Offsets belong to the right-hand coordinate model; layout applies mirroring later.
            var x = baseline.Hand == HudHand.Left ? .21 - sumX / 3 : sumX / 3 - .79;
            return new HudSettings(baseline.Hand, baseline.Scale, x, sumY / 3 - .19);
        }

        public void Reset()
        {
            SampleCount = 0;
            sumX = 0;
            sumY = 0;
        }
    }
}
