using System;
using UnityEngine;

namespace Praxen.Game.Presentation.Combat
{
    public enum CombatCueKind { Telegraph, Attack, Parry, Block, Dodge, Hit, Death, Opening, Guard }

    public readonly struct CombatPresentationCue
    {
        public CombatCueKind Kind { get; }
        public long TimeUs { get; }
        public bool TargetEnemy { get; }
        public CombatPresentationCue(CombatCueKind kind, long timeUs, bool targetEnemy = false)
        { Kind = kind; TimeUs = timeUs; TargetEnemy = targetEnemy; }
    }

    /// <summary>Immutable visual timing, with no command or damage port.</summary>
    public readonly struct ActorMotion
    {
        public string Key { get; }
        public long StartedUs { get; }
        public long ImpactUs { get; }
        public long EndUs { get; }
        public bool Loop { get; }
        public ActorMotion(string key, long startedUs, long endUs = long.MaxValue,
            long impactUs = -1, bool loop = false)
        { Key = key; StartedUs = startedUs; EndUs = endUs; ImpactUs = impactUs; Loop = loop; }

        public double SampleSeconds(long timeUs, double length) => SampleSeconds(timeUs, length, .1);

        public double SampleSeconds(long timeUs, double length, double contactSeconds)
        {
            if (double.IsNaN(length) || double.IsInfinity(length) || length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            if (double.IsNaN(contactSeconds) || double.IsInfinity(contactSeconds) || contactSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(contactSeconds));
            double elapsed = Math.Max(0, timeUs - StartedUs) / 1000000d;
            if (Loop) return length > 0 ? elapsed % length : 0;
            if (ImpactUs > StartedUs && EndUs > ImpactUs)
            {
                double contact = Math.Min(contactSeconds, length);
                return timeUs <= ImpactUs ? contact * Fraction(timeUs, StartedUs, ImpactUs) :
                    contact + (length - contact) * Fraction(timeUs, ImpactUs, EndUs);
            }
            return EndUs > StartedUs && EndUs != long.MaxValue
                ? length * Fraction(timeUs, StartedUs, EndUs) : Math.Min(elapsed, length);
        }

        private static double Fraction(long time, long start, long end) =>
            Math.Max(0, Math.Min(1, (time - start) / (double)(end - start)));
    }
}
