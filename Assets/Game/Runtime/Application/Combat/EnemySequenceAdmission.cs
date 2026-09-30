using System;
using System.Collections.Generic;
using Praxen.Game.Domain.Combat;

namespace Praxen.Game.Application.Combat
{
    internal static class EnemySequenceAdmission
    {
        internal static CombatMilestone[] CreateBatch(IReadOnlyList<ScheduledEnemyStrike> schedule,
            CombatMilestone? continuation = null)
        {
            if (schedule == null) throw new ArgumentNullException(nameof(schedule));
            if (schedule.Count < 1 || schedule.Count > 3)
                throw new ArgumentException("A sequence requires one to three steps.", nameof(schedule));
            var ids = new HashSet<long>();
            var batch = new CombatMilestone[schedule.Count * 3 + (continuation.HasValue ? 1 : 0)];
            long previousRecovery = 0;
            for (int i = 0; i < schedule.Count; i++)
            {
                var step = schedule[i];
                if (step.Strike == null || !ids.Add(step.Strike.Id) ||
                    (i > 0 && step.TelegraphTimeUs < previousRecovery))
                    throw new ArgumentException("Steps must have unique strikes and cannot overlap.", nameof(schedule));
                long id = checked(step.Strike.Id * 3);
                batch[i * 3] = new CombatMilestone(id - 2, step.TelegraphTimeUs, CombatMilestoneKind.Telegraph);
                batch[i * 3 + 1] = new CombatMilestone(id - 1, step.ImpactTimeUs, CombatMilestoneKind.Impact);
                batch[i * 3 + 2] = new CombatMilestone(id, step.RecoveryTimeUs, CombatMilestoneKind.RecoveryComplete);
                previousRecovery = step.RecoveryTimeUs;
            }
            if (continuation.HasValue) batch[batch.Length - 1] = continuation.Value;
            return batch;
        }

        internal static long AllocateBoundary(CombatTimeline timeline, CombatMilestone[] enemyRecords)
        {
            var enemyIds = new HashSet<long>();
            foreach (var record in enemyRecords) enemyIds.Add(record.Id);
            long id;
            do { id = timeline.AllocateMilestoneId(); } while (enemyIds.Contains(id));
            return id;
        }
    }
}
