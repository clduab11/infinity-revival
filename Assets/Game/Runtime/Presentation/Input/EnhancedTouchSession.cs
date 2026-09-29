using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

namespace Praxen.Game.Presentation.Input
{
    /// <summary>Application-thread lease preserving per-event touch samples.</summary>
    public sealed class EnhancedTouchSession : IDisposable
    {
        private static int leases;
        private static bool applying;
        private static readonly List<SettingsLease> settings = new List<SettingsLease>();
        private bool disposed;

        public EnhancedTouchSession()
        {
            EnhancedTouchSupport.Enable();
            if (leases++ != 0) return;
            InputSystem.onSettingsChange += PreserveSamples;
            PreserveSamples();
        }

        private static void PreserveSamples()
        {
            if (applying || leases == 0) return;
            applying = true;
            try
            {
                var current = InputSystem.settings;
                if (!settings.Exists(entry => ReferenceEquals(entry.Value, current)))
                    settings.Add(new SettingsLease(current));
                current.disableRedundantEventsMerging = true;
            }
            finally { applying = false; }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (--leases == 0)
            {
                InputSystem.onSettingsChange -= PreserveSamples;
                foreach (var entry in settings)
                    if (entry.Value != null && entry.Value.disableRedundantEventsMerging)
                        entry.Value.disableRedundantEventsMerging = entry.PreviousMerging;
                settings.Clear();
            }
            EnhancedTouchSupport.Disable();
        }

        private sealed class SettingsLease
        {
            internal readonly InputSettings Value;
            internal readonly bool PreviousMerging;

            internal SettingsLease(InputSettings value)
            {
                Value = value;
                PreviousMerging = value.disableRedundantEventsMerging;
            }
        }
    }
}
