using System;
using Praxen.Game.Application;
using Praxen.Game.Infrastructure;
using Praxen.Game.Application.Input;
using Praxen.Game.Content.Input;
using Praxen.Game.Domain.Input;
using Praxen.Game.Presentation.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Praxen.Game.Presentation
{
    public sealed class BootstrapCompositionRoot : MonoBehaviour
    {
        [SerializeField] private RefugeScreen view;
        [SerializeField] private GestureTuningAsset gestureTuning;
        private ApplicationStateMachine states;
        private ApplicationState resumeState;
        private bool paused;
        private bool focused = true;

        public ApplicationState CurrentState => states?.Current ?? ApplicationState.Bootstrap;
        public BoundedDiagnosticsSink Diagnostics { get; private set; }
        public GesturePhaseContext InteractionPhase { get; private set; }
        public TimestampedTouchCapture InputCapture { get; private set; }
        public GestureTraceView GestureTrace { get; private set; }

        private void Awake() => Initialize();

        public void BindInputTuning(GestureTuningAsset tuning)
        {
            if (tuning == null) throw new ArgumentNullException(nameof(tuning));
            if (states != null && CurrentState != ApplicationState.Failed)
                throw new InvalidOperationException("Bind input tuning before startup or while failed.");
            gestureTuning = tuning;
        }

        public void BindView(RefugeScreen screen)
        {
            if (screen == null) throw new ArgumentNullException(nameof(screen));
            if (!screen.transform.IsChildOf(transform))
                throw new ArgumentException("The composition root must own its view.", nameof(screen));
            if (states != null && CurrentState != ApplicationState.Failed)
                throw new InvalidOperationException("Bind the view before startup or while failed.");
            if (view != null && view != screen) view.Hide();
            view = screen;
        }

        public void Initialize()
        {
            if (states != null) return;
            Diagnostics = new BoundedDiagnosticsSink();
            states = new ApplicationStateMachine(Diagnostics);
            LoadRefuge();
        }

        public bool Retry()
        {
            if (states == null || CurrentState != ApplicationState.Failed) return false;
            return LoadRefuge();
        }

        private bool LoadRefuge()
        {
            if (!states.TryTransition(ApplicationState.Loading)) return false;
            try
            {
                if (view == null) throw new InvalidOperationException("Refuge view is unbound.");
                view.Show(ApplicationState.Refuge);
                EnsureInputComposed();
                states.TryTransition(ApplicationState.Refuge);
                RefreshSuspension();
                return true;
            }
            catch (Exception)
            {
                states.TryTransition(ApplicationState.Failed);
                ShowFailure();
                return false;
            }
        }

        private void EnsureInputComposed()
        {
            if (InputCapture == null)
            {
                var tuning = gestureTuning != null ? gestureTuning.ToRuntime() : GestureTuning.Default;
                if (EventSystem.current == null)
                {
                    var ui = new GameObject("UI EventSystem", typeof(EventSystem),
                        typeof(InputSystemUIInputModule));
                    ui.transform.SetParent(transform, false);
                }
                InteractionPhase = new GesturePhaseContext();
                var owner = new GameObject("Timestamped Touch Capture");
                owner.transform.SetParent(transform, false);
                InputCapture = owner.AddComponent<TimestampedTouchCapture>();
                InputCapture.SetInputEnabled(false);
                InputCapture.Configure(InteractionPhase, new EventSystemPointerOwnership(), tuning);
                var trace = new GameObject("Gesture Trace", typeof(RectTransform));
                GestureTrace = trace.AddComponent<GestureTraceView>();
                InputCapture.CommandProduced += GestureTrace.Show;
            }
            GestureTrace.Configure(view.UiCanvas);
        }

        private void ShowFailure()
        {
            if (view == null)
            {
                var fallback = new GameObject("StartupFailureView", typeof(RectTransform));
                fallback.transform.SetParent(transform, false);
                view = fallback.AddComponent<RefugeScreen>();
            }
            try { view.Show(ApplicationState.Failed); }
            catch (Exception) { view.Hide(); }
        }

        public void SetPaused(bool value)
        {
            paused = value;
            RefreshSuspension();
        }

        public void SetFocused(bool value)
        {
            focused = value;
            RefreshSuspension();
        }

        private void RefreshSuspension()
        {
            if (states == null) return;
            if (paused || !focused)
            {
                if (CurrentState != ApplicationState.Loading && CurrentState != ApplicationState.Refuge)
                    return;
                resumeState = CurrentState;
                states.TryTransition(ApplicationState.Suspended);
            }
            else if (CurrentState == ApplicationState.Suspended)
                states.TryTransition(resumeState);
            InputCapture?.SetInputEnabled(!paused && focused && CurrentState == ApplicationState.Refuge);
            if ((paused || !focused) && GestureTrace != null) GestureTrace.Clear();
        }

        private void OnApplicationPause(bool value) => SetPaused(value);
        private void OnApplicationFocus(bool value) => SetFocused(value);

        private void OnDestroy()
        {
            if (InputCapture != null) InputCapture.enabled = false;
            if (GestureTrace != null) GestureTrace.Clear();
            if (view != null) view.Hide();
            if (states == null || CurrentState == ApplicationState.Shutdown) return;
            states.TryTransition(ApplicationState.Shutdown);
        }
    }
}
