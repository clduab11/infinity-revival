using System;
using System.Collections.Generic;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;
using Praxen.Game.Presentation.Input;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace Praxen.Game.Presentation.Combat
{
    public sealed class TimestampedDefenseControls : MonoBehaviour
    {
        private readonly HashSet<long> contacts = new HashSet<long>();
        private readonly EventSystemPointerOwnership ownership = new EventSystemPointerOwnership();
        private TimestampedTouchCapture capture;
        private CombatTimingDriver timing;
        private GrayboxEncounterView view;
        private long? activePointer;
        private bool guarding, hooked;
        private long sequence;
        private long guardActionId;
        public event Action HeldControlCancelled;

        public void Configure(TimestampedTouchCapture input, CombatTimingDriver driver,
            GrayboxEncounterView encounterView)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (driver == null) throw new ArgumentNullException(nameof(driver));
            if (encounterView == null) throw new ArgumentNullException(nameof(encounterView));
            Unhook();
            CancelHeld();
            capture = input;
            timing = driver;
            view = encounterView;
            if (isActiveAndEnabled) Hook();
        }

        private void Capture(TouchSample sample)
        {
            var update = InputState.currentUpdateType;
            if (update != InputUpdateType.Dynamic && update != InputUpdateType.Fixed &&
                update != InputUpdateType.Manual) return;
            if (sample.Phase == SamplePhase.Began)
                Begin(sample);
            else if (sample.Phase == SamplePhase.Ended || sample.Phase == SamplePhase.Cancelled)
                End(sample);
        }

        private void Begin(TouchSample sample)
        {
            if (!contacts.Add(sample.PointerId) || activePointer.HasValue ||
                !timing.CanAcceptInput || timing.Session == null) return;
            var metrics = new ScreenMetrics(Math.Max(1, Screen.width), Math.Max(1, Screen.height));
            var owner = ownership.Resolve(in sample, in metrics);
            if (owner.Kind != PointerOwnerKind.Ui) return;
            DefenseCommandKind kind;
            if (owner.ControlId == Id(view.GuardZone)) kind = DefenseCommandKind.GuardPress;
            else if (owner.ControlId == Id(view.DodgeLeftZone)) kind = DefenseCommandKind.DodgeLeft;
            else if (owner.ControlId == Id(view.DodgeRightZone)) kind = DefenseCommandKind.DodgeRight;
            else return;
            if (!Submit(kind, sample.TimestampUs)) return;
            activePointer = sample.PointerId;
            guarding = kind == DefenseCommandKind.GuardPress;
        }

        private void End(TouchSample sample)
        {
            contacts.Remove(sample.PointerId);
            if (activePointer != sample.PointerId) return;
            if (guarding) Submit(DefenseCommandKind.GuardRelease, sample.TimestampUs, guardActionId);
            activePointer = null;
            guarding = false;
        }

        private bool Submit(DefenseCommandKind kind, long timestampUs, long actionId = 0)
        {
            var nextSequence = checked(++sequence);
            if (kind == DefenseCommandKind.GuardPress) actionId = nextSequence;
            var accepted = timing.SubmitDefense(new DefenseCommand(nextSequence, timestampUs, kind, actionId));
            if (accepted && kind == DefenseCommandKind.GuardPress) guardActionId = actionId;
            return accepted;
        }

        private static ulong Id(RectTransform zone) => EntityId.ToULong(zone.gameObject.GetEntityId());

        public void CancelHeld()
        {
            contacts.Clear();
            activePointer = null;
            guarding = false;
            guardActionId = 0;
            HeldControlCancelled?.Invoke();
        }

        private void OnEnable() { if (capture != null) Hook(); }
        private void OnDisable() { Unhook(); CancelHeld(); }
        private void Hook()
        {
            if (hooked) return;
            capture.SampleCaptured += Capture;
            capture.ContactsCancelled += CancelHeld;
            timing.Suspended += CancelHeld;
            hooked = true;
        }
        private void Unhook()
        {
            if (!hooked) return;
            if (capture != null) capture.SampleCaptured -= Capture;
            if (capture != null) capture.ContactsCancelled -= CancelHeld;
            if (timing != null) timing.Suspended -= CancelHeld;
            hooked = false;
        }
    }
}
