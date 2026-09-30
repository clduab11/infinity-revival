using System;

namespace Praxen.Game.Domain.Combat
{
    /// <summary>Immutable authored timing. Resume may retime the live timeline uniformly.</summary>
    public readonly struct ScheduledEnemyStrike
    {
        public EnemyStrike Strike { get; }
        public long TelegraphTimeUs { get; }
        public long ImpactTimeUs { get; }
        public long RecoveryTimeUs { get; }

        public ScheduledEnemyStrike(EnemyStrike strike, long telegraphTimeUs)
        {
            Strike = strike ?? throw new ArgumentNullException(nameof(strike));
            if (telegraphTimeUs < 0) throw new ArgumentOutOfRangeException(nameof(telegraphTimeUs));
            TelegraphTimeUs = telegraphTimeUs;
            ImpactTimeUs = checked(telegraphTimeUs + strike.TelegraphDurationUs);
            RecoveryTimeUs = checked(ImpactTimeUs + strike.RecoveryDurationUs);
        }
    }
}
