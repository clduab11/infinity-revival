using System;

namespace Praxen.Game.Presentation.Combat
{
    /// <summary>Owns validated replacement resources until their encounter-boundary commit.</summary>
    public sealed class PreparedCombatConfiguration : IDisposable
    {
        private Action commit;
        private Action release;
        private bool committed, disposed;

        internal PreparedCombatConfiguration(Action commit, Action release)
        { this.commit = commit; this.release = release; }

        public void Commit()
        {
            if (disposed) throw new ObjectDisposedException(nameof(PreparedCombatConfiguration));
            if (committed) throw new InvalidOperationException("The prepared configuration was already committed.");
            commit();
            committed = true;
            commit = null;
            release = null;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            release?.Invoke();
            release = null;
            commit = null;
        }
    }
}
