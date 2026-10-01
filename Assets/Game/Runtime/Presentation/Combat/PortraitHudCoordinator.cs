using System;
using Praxen.Game.Application.Input;
using Praxen.Game.Application.Combat;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;
using UnityEngine;

namespace Praxen.Game.Presentation.Combat
{
    public sealed class PortraitHudCoordinator : MonoBehaviour
    {
        private GrayboxEncounterRoot root;
        private PortraitCombatHud hud;
        private ReachCalibrationCapture calibration;
        private ExplorationInputController exploration;
        private ExplorationPreview preview;
        private bool focused=true,paused;
        public bool IsExploring => preview != null;
        public bool IsCalibrating { get; private set; }
        public string SelectedDestination => preview?.SelectedDestination;
        public float CameraYaw => preview?.Yaw ?? 0;
        public float CameraPitch => preview?.Pitch ?? 0;
        public event Action AbilityRequested;
        public void Configure(GrayboxEncounterRoot encounterRoot)
        {
            root=encounterRoot;hud=root.View.Hud;calibration=new ReachCalibrationCapture(hud);
            hud.PauseButton.onClick.AddListener(Pause);
            hud.AbilityButton.onClick.AddListener(AbilityFromButton);
            hud.Menu.HandButton.onClick.AddListener(ToggleHand);
            hud.Menu.ScaleDownButton.onClick.AddListener(ScaleDown);
            hud.Menu.ScaleUpButton.onClick.AddListener(ScaleUp);
            hud.Menu.CalibrateButton.onClick.AddListener(BeginCalibration);
            hud.Menu.CancelButton.onClick.AddListener(CancelCalibration);
            hud.Menu.ApplyButton.onClick.AddListener(ApplyFromButton);
            hud.Menu.ExploreButton.onClick.AddListener(StartExploration);
            hud.LayoutChanged+=LayoutChanged;
            root.InputCapture.SampleCaptured+=CaptureCalibration;
            root.InputCapture.OrderedSampleCaptured+=CaptureExploration;
            root.InputCapture.ContactsCancelled+=CancelContacts;
            root.Timing.Suspended+=OnSuspended;
        }
        public void Pause() { root.Timing.PauseByUser(); hud.Menu.Show(true); }
        public bool Resume()
        {
            if(IsCalibrating) return false;
            bool resumed=root.Timing.ResumeByUser();
            if(resumed) hud.Menu.Show(false);
            return resumed;
        }
        public void SetHand(HudHand hand)
        {
            Pause(); var s=hud.Settings;hud.ApplySettings(new HudSettings(hand,s.Scale,s.OffsetX,s.OffsetY));
        }
        public void SetScale(double scale)
        {
            Pause();var s=hud.Settings;hud.ApplySettings(new HudSettings(s.Hand,scale,s.OffsetX,s.OffsetY));
        }
        private void ToggleHand()=>SetHand(hud.Settings.Hand==HudHand.Right?HudHand.Left:HudHand.Right);
        private void ScaleDown()=>SetScale(hud.Settings.Scale-.05);
        private void ScaleUp()=>SetScale(hud.Settings.Scale+.05);
        private void ApplyFromButton()=>ApplyCalibration();
        private void AbilityFromButton()=>RequestAbility();
        public bool RequestAbility()
        {
            if(IsExploring || IsCalibrating || !root.Timing.CanAcceptInput ||
                root.Duel==null || root.Duel.Outcome!=DuelOutcome.Running) return false;
            AbilityRequested?.Invoke();
            root.ShowFeedback("ability.none");
            return true;
        }
        public void BeginCalibration()
        {
            Pause();calibration.Reset();IsCalibrating=true;hud.Menu.ShowCalibration(0,false);
        }
        public bool ApplyCalibration()
        {
            if(!IsCalibrating || !calibration.IsComplete) return false;
            var settings=calibration.Result();IsCalibrating=false;
            hud.ApplySettings(settings);calibration.Reset();hud.Menu.Show(true);return true;
        }
        public void CancelCalibration()
        {
            IsCalibrating=false;calibration.Reset();hud.Menu.Show(true);
        }
        private void CaptureCalibration(TouchSample sample)
        {
            if(!IsCalibrating || paused || !focused) return;
            calibration.Process(sample);hud.Menu.ShowCalibration(calibration.SampleCount,calibration.IsComplete);
        }
        public void StartExploration()
        {
            ResetForEncounter();
            root.Timing.EndEncounter();
            root.Timing.ClearUserPauseForRestart();
            root.InteractionPhase.SetPhase(new InteractionPhase(checked(root.InteractionPhase.Current.Id+1),InteractionPhaseKind.Exploration));
            exploration=new ExplorationInputController(root.InteractionPhase.Current.Id);
            preview=new ExplorationPreview(hud,root.EncounterCamera);
            exploration.DestinationTapped+=preview.Tap;exploration.InspectionDragged+=preview.Drag;
            hud.SetExploration(true);hud.Menu.Show(false);
            root.InputCapture.SetInputEnabled(root.Timing.CanAcceptInput);
        }
        private void CaptureExploration(OrderedTouchRecord sample)
        {
            if(IsExploring && root.Timing.CanAcceptInput) exploration.Process(sample,root.InteractionPhase.Current);
        }
        public void ResetForEncounter()
        {
            IsCalibrating=false;calibration?.Reset();preview?.Dispose();preview=null;exploration=null;
            if(hud!=null) { hud.SetExploration(false);hud.Menu.Show(false); }
        }
        public void SetLifecycle(bool appPaused,bool appFocused)
        {
            paused=appPaused;focused=appFocused;
            if(paused || !focused) CancelContacts();
        }
        private void OnSuspended()
        {
            CancelContacts();
            if(!IsCalibrating) hud.Menu.Show(true);
        }
        private void LayoutChanged()
        {
            root.Controls.CancelHeld();root.Duel?.CancelContacts();CancelContacts();
            root.InputCapture.SetInputEnabled(false);
            root.InputCapture.SetInputEnabled(root.Timing.CanAcceptInput);
        }
        private void CancelContacts()
        {
            hud?.CancelUiContacts();
            calibration?.CancelContact();exploration?.Reset(root.InteractionPhase.Current.Id);
        }
        private void LateUpdate()
        {
            if(root==null || IsCalibrating || IsExploring) return;
            if(root.Timing.Session?.Clock.State==CombatClockState.Countdown && !root.Timing.IsUserPaused)
                hud.Menu.Show(false);
        }
        private void OnDestroy()
        {
            preview?.Dispose();
            if(hud!=null) hud.LayoutChanged-=LayoutChanged;
            if(root==null) return;
            if(root.InputCapture!=null)
            {
                root.InputCapture.SampleCaptured-=CaptureCalibration;
                root.InputCapture.OrderedSampleCaptured-=CaptureExploration;
                root.InputCapture.ContactsCancelled-=CancelContacts;
            }
            if(root.Timing!=null) root.Timing.Suspended-=OnSuspended;
        }
    }
}
