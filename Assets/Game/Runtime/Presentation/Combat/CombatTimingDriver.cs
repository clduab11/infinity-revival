using System;
using System.Collections.Generic;
using Praxen.Game.Application.Combat;
using Praxen.Game.Application.Input;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;
using Praxen.Game.Presentation.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Praxen.Game.Presentation.Combat
{
    public sealed class CombatTimingDriver : MonoBehaviour
    {
        private const int InputCapacity = 1024;
        private readonly List<CombatInput> pending = new List<CombatInput>();
        private TimestampedTouchCapture capture;
        private GesturePhaseContext phases;
        private Func<long> sourceTime;
        private bool hooked, paused, focused = true;
        private bool touchInboxDropped;

        public CombatSession Session { get; private set; }
        public bool IsUserPaused { get; private set; }
        public long DroppedCommandCount { get; private set; }
        public bool CanAcceptInput => isActiveAndEnabled && !paused && focused && !IsUserPaused &&
            (Session == null || Session.Clock.State == CombatClockState.Running);
        public event Action Suspended;
        public event Action EncounterEnded;
        public event Action TouchInputRejected;

        public void Configure(TimestampedTouchCapture input, GesturePhaseContext phaseContext,
            Func<long> timestampProvider = null)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (phaseContext == null) throw new ArgumentNullException(nameof(phaseContext));
            if (Session != null) throw new InvalidOperationException("End the active encounter before rebinding.");
            Unhook();
            capture = input;
            phases = phaseContext;
            sourceTime = timestampProvider ?? SourceTimeUs;
            if (isActiveAndEnabled) Hook();
            RefreshInput();
        }

        public void BeginEncounter(InteractionPhase phase)
        {
            if (capture == null) throw new InvalidOperationException("Configure timing before an encounter.");
            if (Session != null) throw new InvalidOperationException("An encounter is already active.");
            if (!CanAcceptInput) throw new InvalidOperationException("Lifecycle state prohibits an encounter.");
            if (phase.Kind == InteractionPhaseKind.Inactive)
                throw new ArgumentOutOfRangeException(nameof(phase));
            var next = new CombatSession(sourceTime());
            phases.SetPhase(phase);
            capture.SetInputEnabled(false);
            pending.Clear();
            touchInboxDropped = false;
            Session = next;
            RefreshInput();
        }

        public void EndEncounter()
        {
            if (Session == null) return;
            var inactive = new InteractionPhase(checked(phases.Current.Id + 1), InteractionPhaseKind.Inactive);
            capture.SetInputEnabled(false);
            phases.SetPhase(inactive);
            pending.Clear();
            touchInboxDropped = false;
            Session = null;
            EncounterEnded?.Invoke();
            RefreshInput();
        }

        public void SetLifecycle(bool applicationPaused, bool applicationFocused)
        {
            bool wasPermitted = !paused && focused;
            paused = applicationPaused;
            focused = applicationFocused;
            if (Session != null && (paused || !focused))
            {
                Session.Suspend(sourceTime(), paused ? CombatSuspensionReason.ApplicationPaused :
                    CombatSuspensionReason.FocusLost);
                pending.Clear();
                Suspended?.Invoke();
            }
            else if (Session != null && !IsUserPaused && !wasPermitted && Session.Clock.State == CombatClockState.Suspended)
                RequestResume();
            RefreshInput();
        }

        public bool RequestResume()
        {
            if (Session == null || IsUserPaused || paused || !focused || !isActiveAndEnabled ||
                Session.Clock.State != CombatClockState.Suspended) return false;
            Session.BeginResume(sourceTime());
            pending.Clear();
            RefreshInput();
            return true;
        }

        public void PauseByUser()
        {
            IsUserPaused = true;
            if (Session != null) Session.Suspend(sourceTime(), CombatSuspensionReason.UserPaused);
            pending.Clear();
            touchInboxDropped = false;
            Suspended?.Invoke();
            RefreshInput();
        }

        public bool ResumeByUser()
        {
            if (paused || !focused || !isActiveAndEnabled) return false;
            if (!IsUserPaused) return Session == null || RequestResume();
            IsUserPaused = false;
            if (Session != null && Session.Clock.State == CombatClockState.Suspended)
                Session.BeginResume(sourceTime());
            pending.Clear();
            RefreshInput();
            return true;
        }

        public void ClearUserPauseForRestart()
        {
            IsUserPaused = false;
            pending.Clear();
            RefreshInput();
        }

        private void ReceiveCommand(GestureCommand command)
        {
            if (capture.OrderedRecognition || Session == null || !CanAcceptInput || !IsGameplayUpdate()) return;
            if (pending.Count < InputCapacity) pending.Add(new CombatInput(command));
            else DroppedCommandCount = checked(DroppedCommandCount + 1);
        }

        private void ReceiveTouch(OrderedTouchRecord touch)
        {
            if (Session == null || !CanAcceptInput || !IsGameplayUpdate()) return;
            if (pending.Count < InputCapacity) pending.Add(new CombatInput(touch));
            else
            {
                DroppedCommandCount = checked(DroppedCommandCount + 1);
                touchInboxDropped = true;
            }
        }

        public bool SubmitDefense(DefenseCommand command)
        {
            _ = new CombatInput(command);
            if (Session == null || !CanAcceptInput) return false;
            if (pending.Count >= InputCapacity)
            {
                DroppedCommandCount = checked(DroppedCommandCount + 1);
                // A terminal release must not strand held guard when the bounded inbox is full.
                if (command.Kind == DefenseCommandKind.GuardRelease)
                {
                    if (pending[InputCapacity - 1].Touch.HasValue) touchInboxDropped = true;
                    pending[InputCapacity - 1] = new CombatInput(command);
                    return true;
                }
                return false;
            }
            pending.Add(new CombatInput(command));
            return true;
        }

        private void BeforeInputUpdate()
        {
            if (!IsGameplayUpdate() || Session == null) return;
            if (Session.CheckForStall(sourceTime()))
            {
                pending.Clear();
                Suspended?.Invoke();
            }
            RefreshInput();
        }

        private void AfterInputUpdate()
        {
            if (!IsGameplayUpdate() || Session == null) return;
            var previous = Session.Clock.State;
            Session.AdvanceInputFrame(sourceTime(), pending);
            pending.Clear();
            bool resetContacts = touchInboxDropped;
            touchInboxDropped = false;
            if (resetContacts) TouchInputRejected?.Invoke();
            if (previous != CombatClockState.Suspended && Session.Clock.State == CombatClockState.Suspended)
                Suspended?.Invoke();
            RefreshInput();
        }

        private static bool IsGameplayUpdate()
        {
            var type = InputState.currentUpdateType;
            return type == InputUpdateType.Dynamic || type == InputUpdateType.Fixed ||
                type == InputUpdateType.Manual;
        }

        // Touch event timestamps use realtime; InputState's cached offset can lag behind Play entry.
        private static long SourceTimeUs() =>
            checked((long)Math.Round(Time.realtimeSinceStartupAsDouble * 1000000,
                MidpointRounding.AwayFromZero));

        private void RefreshInput() => capture?.SetInputEnabled(CanAcceptInput);
        private void OnEnable()
        {
            if (capture == null) return;
            Hook();
            RefreshInput();
        }

        private void Hook()
        {
            if (hooked) return;
            capture.CommandProduced += ReceiveCommand;
            capture.OrderedSampleCaptured += ReceiveTouch;
            InputSystem.onBeforeUpdate += BeforeInputUpdate;
            InputSystem.onAfterUpdate += AfterInputUpdate;
            hooked = true;
        }

        private void Unhook()
        {
            if (!hooked) return;
            if (capture != null)
            {
                capture.CommandProduced -= ReceiveCommand;
                capture.OrderedSampleCaptured -= ReceiveTouch;
            }
            InputSystem.onBeforeUpdate -= BeforeInputUpdate;
            InputSystem.onAfterUpdate -= AfterInputUpdate;
            hooked = false;
        }

        private void OnDisable()
        {
            Unhook();
            // Teardown can outlive the source epoch. Freeze accepted time without sampling it again.
            if (Session != null) Session.Suspend(Session.Clock.LastDeviceTimeUs,
                CombatSuspensionReason.ApplicationPaused);
            pending.Clear();
            if (capture != null) capture.SetInputEnabled(false);
        }
    }
}
