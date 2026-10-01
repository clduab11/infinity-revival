using UnityEngine;

namespace Praxen.Game.Presentation.Combat
{
    public readonly struct CombatCameraPose
    {
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public CombatCameraPose(Vector3 position, Quaternion rotation)
        { Position = position; Rotation = rotation; }
    }

    /// <summary>Pure combat-time orbit in the camera parent's coordinate space.</summary>
    public static class CombatCameraMotion
    {
        public static CombatCameraPose Sample(Vector3 baselinePosition, Quaternion baselineRotation,
            ActorMotion motion, long timeUs, bool enabled, float degrees, float focusDistance)
        {
            var baseline = new CombatCameraPose(baselinePosition, baselineRotation);
            if (!enabled || motion.Loop || motion.EndUs == long.MaxValue ||
                timeUs <= motion.StartedUs || timeUs >= motion.EndUs ||
                motion.EndUs <= motion.StartedUs || !Finite(degrees) || !Finite(focusDistance)) return baseline;
            float sign = Direction(motion.Key);
            float yaw = sign * Mathf.Clamp(degrees, 0, 4) * Envelope(motion, timeUs);
            if (yaw == 0) return baseline;
            var turn = Quaternion.AngleAxis(yaw, Vector3.up);
            var rotation = turn * baselineRotation;
            var focus = baselinePosition + baselineRotation * Vector3.forward * Mathf.Clamp(focusDistance, 1, 12);
            var position = focus + turn * (baselinePosition - focus);
            return new CombatCameraPose(position, rotation);
        }

        private static float Direction(string key)
        {
            switch (key)
            {
                case "CutLeft": case "DodgeLeft": return 1;
                case "CutRight": case "DodgeRight": return -1;
                default: return 0;
            }
        }

        private static float Envelope(ActorMotion motion, long timeUs)
        {
            long peak = motion.ImpactUs > motion.StartedUs && motion.ImpactUs < motion.EndUs
                ? motion.ImpactUs : motion.StartedUs + (motion.EndUs - motion.StartedUs) / 2;
            float fraction = timeUs <= peak
                ? (float)((timeUs - motion.StartedUs) / (double)(peak - motion.StartedUs))
                : (float)((motion.EndUs - timeUs) / (double)(motion.EndUs - peak));
            return fraction * fraction * (3 - 2 * fraction);
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
