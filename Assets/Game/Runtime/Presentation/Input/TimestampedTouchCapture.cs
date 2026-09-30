using System;
using System.Collections.Generic;
using Praxen.Game.Application.Input;
using Praxen.Game.Domain.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.LowLevel;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using InputPhase = UnityEngine.InputSystem.TouchPhase;

namespace Praxen.Game.Presentation.Input
{
    public sealed class TimestampedTouchCapture : MonoBehaviour
    {
        private IInteractionPhaseSource phases;
        private IPointerOwnershipResolver ownership;
        private CardinalGestureRecognizer recognizer;
        private EnhancedTouchSession session;
        private ScreenMetrics metrics;
        private InputSettings.UpdateMode updateMode;
        private bool inputEnabled = true;
        private readonly Dictionary<long, TouchSample> contacts = new Dictionary<long, TouchSample>();
        private readonly Dictionary<long, PointerOwnership> orderedOwners = new Dictionary<long, PointerOwnership>();

        public event Action<TouchSample> SampleCaptured;
        public event Action<OrderedTouchRecord> OrderedSampleCaptured;
        public event Action<GestureCommand> CommandProduced;
        public event Action ContactsCancelled;
        public bool OrderedRecognition { get; private set; }

        public void SetOrderedRecognition(bool value)
        {
            if (OrderedRecognition == value) return;
            OrderedRecognition = value;
            orderedOwners.Clear();
            recognizer?.ResetContacts();
            ContactsCancelled?.Invoke();
        }

        public void PublishRecognized(GestureCommand command)
        {
            if (!OrderedRecognition) throw new InvalidOperationException("Ordered recognition is disabled.");
            CommandProduced?.Invoke(command);
        }

        public void Configure(IInteractionPhaseSource phaseSource,
            IPointerOwnershipResolver ownershipResolver, GestureTuning tuning)
        {
            if (phaseSource == null) throw new ArgumentNullException(nameof(phaseSource));
            if (ownershipResolver == null) throw new ArgumentNullException(nameof(ownershipResolver));
            StopCapture();
            phases = phaseSource;
            ownership = ownershipResolver;
            recognizer = new CardinalGestureRecognizer(tuning);
            if (isActiveAndEnabled) StartCapture();
        }

        public void SetInputEnabled(bool value)
        {
            bool changed = inputEnabled != value;
            inputEnabled = value;
            if (!value)
            {
                recognizer?.CancelAll();
                orderedOwners.Clear();
                if (changed) ContactsCancelled?.Invoke();
            }
        }

        private void OnEnable()
        {
            if (recognizer != null) StartCapture();
        }

        private void StartCapture()
        {
            if (session != null) return;
            session = new EnhancedTouchSession();
            updateMode = InputSystem.settings.updateMode;
            recognizer.ResetContacts();
            contacts.Clear();
            orderedOwners.Clear();
            metrics = CurrentMetrics();
            SeedHeldContacts();
            EnhancedTouch.onFingerDown += Capture;
            EnhancedTouch.onFingerMove += Capture;
            EnhancedTouch.onFingerUp += Capture;
            InputSystem.onDeviceChange += DeviceChanged;
            InputSystem.onSettingsChange += SettingsChanged;
        }

        private void SeedHeldContacts()
        {
            foreach (var device in InputSystem.devices)
            {
                if (!(device is Touchscreen screen)) continue;
                foreach (var touch in screen.touches)
                {
                    var id = touch.touchId.ReadValue();
                    if (!touch.press.isPressed || id == 0) continue;
                    var position = touch.position.ReadValue();
                    var sample = new TouchSample(PointerId(screen.deviceId, id), SamplePhase.Began,
                        Normalize(position), Microseconds(InputState.currentTime));
                    var excluded = PointerOwnership.Excluded;
                    var inactive = InteractionPhase.Inactive;
                    recognizer.Process(in sample, in excluded, in inactive, in metrics);
                    contacts[sample.PointerId] = sample;
                    orderedOwners[sample.PointerId] = excluded;
                }
            }
        }

        private void Capture(Finger finger)
        {
            // EnhancedTouch reconstructs held records before our settings callback runs.
            // Those records have current time, not a source event's original timestamp.
            if (InputSystem.settings.updateMode != updateMode) return;
            var touch = finger.lastTouch;
            if (!touch.valid || touch.touchId == 0) return;
            var nextMetrics = CurrentMetrics();
            if (nextMetrics.Width != metrics.Width || nextMetrics.Height != metrics.Height)
            {
                recognizer.CancelAll();
                orderedOwners.Clear();
                ContactsCancelled?.Invoke();
            }
            metrics = nextMetrics;
            var phase = ConvertPhase(touch.phase);
            if (!phase.HasValue) return;
            var sample = new TouchSample(PointerId(touch.screen.deviceId, touch.touchId),
                phase.Value, Normalize(touch.screenPosition), Microseconds(touch.time));
            var current = phases.Current;
            var owner = PointerOwnership.Excluded;
            if (sample.Phase == SamplePhase.Began && inputEnabled && touch.displayIndex == 0)
                owner = ownership.Resolve(in sample, in metrics);
            if (!inputEnabled) recognizer.CancelAll();
            var command = recognizer.Process(in sample, in owner, in current, in metrics);
            PublishSample(sample, owner, command);
        }

        private void PublishSample(TouchSample sample, PointerOwnership owner, GestureCommand? command)
        {
            if (sample.Phase == SamplePhase.Began && !orderedOwners.ContainsKey(sample.PointerId))
                orderedOwners.Add(sample.PointerId, owner);
            // UI contacts already produce typed controls. Queue only contacts owned by gameplay at Begin.
            if (OrderedRecognition && inputEnabled && orderedOwners.TryGetValue(sample.PointerId, out var capturedOwner) &&
                capturedOwner.Kind == PointerOwnerKind.Gameplay)
                OrderedSampleCaptured?.Invoke(new OrderedTouchRecord(sample, capturedOwner, metrics));
            if (sample.Phase == SamplePhase.Ended || sample.Phase == SamplePhase.Cancelled)
            {
                contacts.Remove(sample.PointerId);
                orderedOwners.Remove(sample.PointerId);
            }
            else
                contacts[sample.PointerId] = sample;
            SampleCaptured?.Invoke(sample);
            if (!OrderedRecognition && command.HasValue) CommandProduced?.Invoke(command.Value);
        }

        private void SettingsChanged()
        {
            var next = InputSystem.settings.updateMode;
            if (next == updateMode) return;
            recognizer.CancelAll();
            orderedOwners.Clear();
            ContactsCancelled?.Invoke();
            updateMode = next;
        }

        private void DeviceChanged(InputDevice device, InputDeviceChange change)
        {
            if (change != InputDeviceChange.Removed || !(device is Touchscreen)) return;
            var removed = new List<long>();
            foreach (var id in contacts.Keys)
                if ((id >> 32) == device.deviceId) removed.Add(id);
            var current = phases.Current;
            var excluded = PointerOwnership.Excluded;
            foreach (var id in removed)
            {
                var last = contacts[id];
                var cancelled = new TouchSample(id, SamplePhase.Cancelled, last.Position,
                    Math.Max(last.TimestampUs, Microseconds(InputState.currentTime)));
                recognizer.Process(in cancelled, in excluded, in current, in metrics);
                if (OrderedRecognition && orderedOwners.TryGetValue(id, out var owner) &&
                    owner.Kind == PointerOwnerKind.Gameplay)
                    OrderedSampleCaptured?.Invoke(new OrderedTouchRecord(cancelled, owner, metrics));
                orderedOwners.Remove(id);
                contacts.Remove(id);
                SampleCaptured?.Invoke(cancelled);
            }
            if (removed.Count > 0) ContactsCancelled?.Invoke();
        }

        private NormalizedPoint Normalize(Vector2 point) =>
            new NormalizedPoint(point.x / (double)metrics.Width, point.y / (double)metrics.Height);

        private static long PointerId(int device, int touch) => ((long)device << 32) | (uint)touch;
        private static long Microseconds(double time) =>
            checked((long)Math.Round(time * 1000000, MidpointRounding.AwayFromZero));
        private static ScreenMetrics CurrentMetrics() =>
            new ScreenMetrics(Math.Max(1, Screen.width), Math.Max(1, Screen.height));

        private static SamplePhase? ConvertPhase(InputPhase phase)
        {
            switch (phase)
            {
                case InputPhase.Began: return SamplePhase.Began;
                case InputPhase.Moved: return SamplePhase.Moved;
                case InputPhase.Ended: return SamplePhase.Ended;
                case InputPhase.Canceled: return SamplePhase.Cancelled;
                default: return null;
            }
        }

        private void OnDisable() => StopCapture();
        private void OnDestroy() => StopCapture();

        private void StopCapture()
        {
            if (session == null) return;
            EnhancedTouch.onFingerDown -= Capture;
            EnhancedTouch.onFingerMove -= Capture;
            EnhancedTouch.onFingerUp -= Capture;
            InputSystem.onDeviceChange -= DeviceChanged;
            InputSystem.onSettingsChange -= SettingsChanged;
            recognizer.CancelAll();
            contacts.Clear();
            orderedOwners.Clear();
            ContactsCancelled?.Invoke();
            session.Dispose();
            session = null;
        }
    }
}
