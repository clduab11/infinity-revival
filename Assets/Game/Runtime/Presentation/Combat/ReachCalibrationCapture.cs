using System;
using Praxen.Game.Domain.Input;
using Praxen.Game.Presentation.Input;
using UnityEngine;

namespace Praxen.Game.Presentation.Combat
{
    internal sealed class ReachCalibrationCapture
    {
        private readonly PortraitCombatHud hud;
        private readonly EventSystemPointerOwnership ownership = new EventSystemPointerOwnership();
        private readonly ReachCalibration calibration = new ReachCalibration();
        private TouchSample? start;
        private long lastTimestamp;
        private bool dragged;
        public int SampleCount => calibration.SampleCount;
        public bool IsComplete => calibration.IsComplete;
        public ReachCalibrationCapture(PortraitCombatHud hud) { this.hud = hud; }
        public void Reset() { calibration.Reset(); CancelContact(); }
        public void CancelContact() { start = null; dragged = false; }
        public HudSettings Result() => calibration.Result(hud.Settings);

        public void Process(TouchSample sample)
        {
            if (IsComplete) return;
            var metrics = new ScreenMetrics(Math.Max(1,Screen.width),Math.Max(1,Screen.height));
            if (sample.Phase == SamplePhase.Began)
            {
                if (start.HasValue) return;
                var owner = ownership.Resolve(in sample,in metrics);
                var surface = hud.Menu.CalibrationSurface;
                if (owner.Kind != PointerOwnerKind.Ui || surface == null ||
                    owner.ControlId != EntityId.ToULong(surface.gameObject.GetEntityId())) return;
                start=sample; lastTimestamp=sample.TimestampUs; dragged=false;
                return;
            }
            if (!start.HasValue || start.Value.PointerId != sample.PointerId) return;
            if (sample.TimestampUs < lastTimestamp) dragged=true;
            lastTimestamp=Math.Max(lastTimestamp,sample.TimestampUs);
            var delta=sample.Position;
            double dx=(delta.X-start.Value.Position.X)*metrics.Width;
            double dy=(delta.Y-start.Value.Position.Y)*metrics.Height;
            if (dx*dx+dy*dy > Math.Pow(.025*Math.Min(metrics.Width,metrics.Height),2)) dragged=true;
            if (sample.Phase != SamplePhase.Ended && sample.Phase != SamplePhase.Cancelled) return;
            if (!dragged && sample.Phase == SamplePhase.Ended)
            {
                var safe=hud.SafeArea;
                calibration.Add((sample.Position.X*metrics.Width-safe.x)/safe.width,
                    (sample.Position.Y*metrics.Height-safe.y)/safe.height);
            }
            CancelContact();
        }
    }
}
