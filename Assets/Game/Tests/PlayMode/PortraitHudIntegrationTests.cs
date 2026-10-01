using System;
using System.Collections;
using NUnit.Framework;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;
using Praxen.Game.Presentation.Combat;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using InputPhase = UnityEngine.InputSystem.TouchPhase;

namespace Praxen.Game.Tests.PlayMode
{
    public sealed class PortraitHudIntegrationTests
    {
        private GrayboxEncounterRoot root;
        private Touchscreen device;
        private InputSettings.UpdateMode mode;
        private InputSettings.BackgroundBehavior background;
        private InputSettings.EditorInputBehaviorInPlayMode editor;
        private long now;
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync("GrayboxEncounter", LoadSceneMode.Single);
            root = Object.FindAnyObjectByType<GrayboxEncounterRoot>();
            mode = InputSystem.settings.updateMode;
            background = InputSystem.settings.backgroundBehavior;
            editor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            device = InputSystem.AddDevice<Touchscreen>();
            now = (long)Math.Floor((InputState.currentTime - .01) * 1000000);
            root.SetLifecycle(false, true);
            root.RestartEncounter(() => now);
            Canvas.ForceUpdateCanvases();
            yield return GrayboxCapture.WarmFrame(Camera.main);
            Canvas.ForceUpdateCanvases();
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (root != null) root.Timing.EndEncounter();
            if (device != null && device.added) InputSystem.RemoveDevice(device);
            InputSystem.settings.updateMode = mode;
            InputSystem.settings.backgroundBehavior = background;
            InputSystem.settings.editorInputBehaviorInPlayMode = editor;
            yield return null;
        }
        private void Step(long delta = 10000) { now += delta; InputSystem.Update(); }
        private void Touch(int id, InputPhase phase, Vector2 position)
        {
            InputSystem.QueueStateEvent(device, new TouchState {touchId=id,phase=phase,position=position}, (now+1000)/1000000.0);
            Step(2000);
        }
        private static Vector2 Center(RectTransform rect) => RectTransformUtility.WorldToScreenPoint(Camera.main, rect.TransformPoint(rect.rect.center));

        [TestCase(PlayerCombatState.Ready)]
        [TestCase(PlayerCombatState.Guarding)]
        public void DodgeControlsShowAvailabilityForAdmittedStates(PlayerCombatState state)
        {
            var player = HudPlayerInState(state);
            root.View.Hud.Show(player, string.Empty, string.Empty, new CombatClock(0));
            AssertDodgeControlColors(new Color32(23, 27, 30, 230));
            Assert.That(player.ApplyControl(new DefenseCommand(2, 0, DefenseCommandKind.DodgeRight), 0), Is.True);
        }

        [TestCase(PlayerCombatState.Dodging)]
        [TestCase(PlayerCombatState.Recovery)]
        [TestCase(PlayerCombatState.Staggered)]
        [TestCase(PlayerCombatState.Attacking)]
        [TestCase(PlayerCombatState.Parrying)]
        [TestCase(PlayerCombatState.Dead)]
        public void DodgeControlsDimDuringActionLockoutWithChargesRemaining(PlayerCombatState state)
        {
            var player = HudPlayerInState(state);
            Assert.That(player.State, Is.EqualTo(state));
            Assert.That(player.DodgeCharges, Is.GreaterThan(0));
            Assert.That(player.ApplyControl(new DefenseCommand(2, 0, DefenseCommandKind.DodgeRight), 0), Is.False);
            root.View.Hud.Show(player, string.Empty, string.Empty, new CombatClock(0));
            AssertDodgeControlColors(new Color32(45, 45, 45, 230));
        }

        [TestCase(CombatClockState.Suspended)]
        [TestCase(CombatClockState.Countdown)]
        public void DodgeControlsDimWhenClockCannotAcceptInput(CombatClockState state)
        {
            var player = new DefenseCombatant();
            var clock = new CombatClock(0);
            clock.Suspend(0, CombatSuspensionReason.UserPaused);
            if (state == CombatClockState.Countdown) clock.BeginResume(0);
            Assert.That(player.DodgeCharges, Is.EqualTo(3));
            Assert.That(clock.State, Is.EqualTo(state));
            root.View.Hud.Show(player, string.Empty, string.Empty, clock);
            AssertDodgeControlColors(new Color32(45, 45, 45, 230));
        }

        [Test]
        public void DodgeControlsBecomeAvailableAtExactActionEnd()
        {
            var player = new DefenseCombatant();
            var clock = new CombatClock(0);
            Assert.That(player.ApplyControl(new DefenseCommand(1, 0, DefenseCommandKind.DodgeLeft), 0), Is.True);
            Assert.That(player.ActionEndsUs, Is.EqualTo(360000));
            player.AdvanceTo(359999);
            Assert.That(player.State, Is.EqualTo(PlayerCombatState.Recovery));
            root.View.Hud.Show(player, string.Empty, string.Empty, clock);
            AssertDodgeControlColors(new Color32(45, 45, 45, 230));
            player.AdvanceTo(360000);
            Assert.That(player.State, Is.EqualTo(PlayerCombatState.Ready));
            Assert.That(player.DodgeCharges, Is.EqualTo(2));
            root.View.Hud.Show(player, string.Empty, string.Empty, clock);
            AssertDodgeControlColors(new Color32(23, 27, 30, 230));
        }

        [Test]
        public void DodgeControlsRemainDimWithNoChargesAfterActionEnd()
        {
            var player = new DefenseCombatant();
            for (int i = 0; i < 3; i++)
                Assert.That(player.ApplyControl(new DefenseCommand(i + 1, i * 360000, DefenseCommandKind.DodgeLeft),
                    i * 360000), Is.True);
            player.AdvanceTo(1080000);
            Assert.That(player.State, Is.EqualTo(PlayerCombatState.Ready));
            Assert.That(player.DodgeCharges, Is.Zero);
            root.View.Hud.Show(player, string.Empty, string.Empty, new CombatClock(0));
            AssertDodgeControlColors(new Color32(45, 45, 45, 230));
        }

        [Test]
        public void DimDodgeControlsRetainNativeUiPointerOwnership()
        {
            root.View.Hud.Show(HudPlayerInState(PlayerCombatState.Dodging), string.Empty, string.Empty, new CombatClock(0));
            Canvas.ForceUpdateCanvases();
            var metrics = new ScreenMetrics(Screen.width, Screen.height);
            var resolver = new Praxen.Game.Presentation.Input.EventSystemPointerOwnership();
            foreach (var zone in new[] { root.View.Hud.DodgeLeftZone, root.View.Hud.DodgeRightZone })
            {
                var position = Center(zone);
                var sample = new TouchSample(99, SamplePhase.Began,
                    new NormalizedPoint(position.x / Screen.width, position.y / Screen.height), now);
                var owner = resolver.Resolve(in sample, in metrics);
                Assert.That(owner.Kind, Is.EqualTo(PointerOwnerKind.Ui), zone.name);
                Assert.That(owner.ControlId, Is.EqualTo(EntityId.ToULong(zone.gameObject.GetEntityId())), zone.name);
            }
            AssertDodgeControlColors(new Color32(45, 45, 45, 230));
        }

        private void AssertDodgeControlColors(Color expected)
        {
            foreach (var zone in new[] { root.View.Hud.DodgeLeftZone, root.View.Hud.DodgeRightZone })
            {
                var image = zone.GetComponent<UnityEngine.UI.Image>();
                Assert.That(zone.gameObject.activeInHierarchy, Is.True, zone.name);
                Assert.That(image.raycastTarget, Is.True, zone.name);
                Assert.That(image.color, Is.EqualTo(expected), zone.name);
            }
        }

        private static DefenseCombatant HudPlayerInState(PlayerCombatState state)
        {
            var player = new DefenseCombatant();
            var strike = new EnemyStrike(1, DefenseMask.Guard | DefenseMask.Parry, DodgeSide.None,
                SwipeDirection.Right, player.Tuning.MaximumGuard, 10);
            switch (state)
            {
                case PlayerCombatState.Guarding:
                    player.ApplyControl(new DefenseCommand(1, 0, DefenseCommandKind.GuardPress), 0);
                    break;
                case PlayerCombatState.Dodging:
                    player.ApplyControl(new DefenseCommand(1, 0, DefenseCommandKind.DodgeLeft), 0);
                    break;
                case PlayerCombatState.Recovery:
                    player.ResolveImpact(strike, 0);
                    break;
                case PlayerCombatState.Staggered:
                    player.ApplyControl(new DefenseCommand(1, 0, DefenseCommandKind.GuardPress), 0);
                    player.ResolveImpact(strike, 0);
                    break;
                case PlayerCombatState.Attacking:
                    player.TryBeginOffense(0, 100000, 300000);
                    break;
                case PlayerCombatState.Parrying:
                    player.TryParry(strike, 100000, SwipeDirection.Right, 0);
                    break;
                case PlayerCombatState.Dead:
                    player.ResolveImpact(new EnemyStrike(1, DefenseMask.Guard, DodgeSide.None,
                        SwipeDirection.Right, 1, player.Tuning.MaximumHealth), 0);
                    break;
            }
            return player;
        }

        [Test]
        public void DefaultRightHandMirrorPreservesSemanticControlObjects()
        {
            var hud = root.View.Hud;
            Assert.That(hud.Settings.Hand, Is.EqualTo(HudHand.Right));
            var guard = root.View.GuardZone;
            root.HudControls.Pause();
            root.HudControls.SetHand(HudHand.Left);
            Canvas.ForceUpdateCanvases();
            Assert.That(root.View.GuardZone, Is.SameAs(guard));
            Assert.That(hud.Settings.Hand, Is.EqualTo(HudHand.Left));
            Assert.That(root.Timing.IsUserPaused, Is.True);
        }
        [Test]
        public void UserPauseSurvivesFocusReturnAndExplicitResumeKeepsCountdown()
        {
            root.HudControls.Pause();
            var time = root.Timing.Session.Clock.TimeUs;
            var health = root.Encounter.Player.Health;
            root.SetLifecycle(false, false);
            root.SetLifecycle(false, true);
            Step();
            Assert.That(root.Timing.Session.Clock.State, Is.EqualTo(CombatClockState.Suspended));
            Assert.That(root.Timing.CanAcceptInput, Is.False);
            Assert.That(root.View.Hud.Menu.IsVisible, Is.True);
            Assert.That(root.HudControls.Resume(), Is.True);
            Assert.That(root.Timing.Session.Clock.State, Is.EqualTo(CombatClockState.Countdown));
            for (int i=0;i<29;i++) Step(100000);
            Assert.That(root.Timing.CanAcceptInput, Is.False);
            Step(100000);
            Assert.That(root.Timing.Session.Clock.State, Is.EqualTo(CombatClockState.Running));
            Assert.That(root.Timing.Session.Clock.TimeUs, Is.EqualTo(time));
            Assert.That(root.Encounter.Player.Health, Is.EqualTo(health));
        }
        [Test]
        public void PauseCancelsHeldGuardAndLayoutChangesKeepClockFrozen()
        {
            Touch(7,InputPhase.Began,Center(root.View.GuardZone));
            Assert.That(root.Encounter.Player.GuardHeld, Is.True);
            root.HudControls.Pause();
            Assert.That(root.Encounter.Player.GuardHeld, Is.False);
            root.HudControls.SetScale(1.2);
            Step();
            Assert.That(root.Timing.Session.Clock.TimeUs, Is.EqualTo(2000));
            Assert.That(root.Timing.CanAcceptInput, Is.False);
        }
        [Test]
        public void AbilityIntentIsGatedAndDoesNotSpendFutureAbilityResources()
        {
            int intents=0;
            root.HudControls.AbilityRequested += () => intents++;
            int focus=root.Duel.Momentum.Focus;
            Assert.That(root.HudControls.RequestAbility(), Is.True);
            root.HudControls.Pause();
            Assert.That(root.HudControls.RequestAbility(), Is.False);
            Assert.That(intents, Is.EqualTo(1));
            Assert.That(root.Duel.Momentum.Focus, Is.EqualTo(focus));
        }
        [Test]
        public void CalibrationNeedsThreeFreshTapsAndCancelPreservesSettings()
        {
            var original = root.View.Hud.Settings;
            root.HudControls.BeginCalibration();
            Assert.That(root.HudControls.IsCalibrating, Is.True);
            Assert.That(root.HudControls.ApplyCalibration(), Is.False);
            root.HudControls.CancelCalibration();
            Assert.That(root.View.Hud.Settings.OffsetX, Is.EqualTo(original.OffsetX));
            Assert.That(root.Timing.IsUserPaused, Is.True);
        }
        [Test]
        public void ExplorationSwitchEndsCombatAndCancelsOldContact()
        {
            Touch(7,InputPhase.Began,new Vector2(Screen.width*.5f,Screen.height*.5f));
            root.HudControls.StartExploration();
            Assert.That(root.HudControls.IsExploring, Is.True);
            Assert.That(root.Timing.Session, Is.Null);
            Assert.That(root.InteractionPhase.Current.Kind, Is.EqualTo(InteractionPhaseKind.Exploration));
            Touch(7,InputPhase.Ended,new Vector2(Screen.width*.5f,Screen.height*.5f));
            Assert.That(root.HudControls.SelectedDestination, Is.Null);
            root.RestartEncounter(() => now);
            Assert.That(root.HudControls.IsExploring, Is.False);
            Assert.That(root.Timing.Session, Is.Not.Null);
        }
        [UnityTest]
        public IEnumerator ThreeFreshCalibrationTapsApplyAndDragDoesNotCount()
        {
            root.HudControls.BeginCalibration();
            Canvas.ForceUpdateCanvases();
            yield return GrayboxCapture.WarmFrame(Camera.main);
            var safe=root.View.Hud.SafeArea;
            var comfortable=new Vector2(safe.x+safe.width*.83f,safe.y+safe.height*.24f);
            Touch(10,InputPhase.Began,comfortable);
            Touch(10,InputPhase.Moved,comfortable+Vector2.right*Screen.width*.08f);
            Touch(10,InputPhase.Ended,comfortable);
            Assert.That(root.HudControls.ApplyCalibration(),Is.False);
            for(int i=0;i<3;i++)
            {
                Touch(11+i,InputPhase.Began,comfortable);
                Touch(11+i,InputPhase.Ended,comfortable);
            }
            Assert.That(root.HudControls.ApplyCalibration(),Is.True);
            Assert.That(root.View.Hud.Settings.OffsetX,Is.EqualTo(.04).Within(.001));
            Assert.That(root.View.Hud.Settings.OffsetY,Is.EqualTo(.05).Within(.001));
            Assert.That(root.View.Hud.Menu.IsVisible,Is.True);
            Assert.That(root.Timing.CanAcceptInput,Is.False);
        }
        [Test]
        public void ExplorationTapSelectsButDragOnlyInspectsAndUiCannotSelect()
        {
            root.HudControls.StartExploration();
            Touch(20,InputPhase.Began,new Vector2(Screen.width*.35f,Screen.height*.545f));
            Touch(20,InputPhase.Ended,new Vector2(Screen.width*.35f,Screen.height*.545f));
            Assert.That(root.HudControls.SelectedDestination,Is.EqualTo("exploration.gate"));
            Touch(21,InputPhase.Began,new Vector2(Screen.width*.65f,Screen.height*.545f));
            Touch(21,InputPhase.Ended,new Vector2(Screen.width*.9f,Screen.height*.7f));
            Assert.That(root.HudControls.SelectedDestination,Is.EqualTo("exploration.gate"));
            Assert.That(Math.Abs(root.HudControls.CameraYaw),Is.InRange(1,12));
            Assert.That(Math.Abs(root.HudControls.CameraPitch),Is.LessThanOrEqualTo(8));
            root.HudControls.Pause();
            Touch(22,InputPhase.Began,new Vector2(Screen.width*.65f,Screen.height*.545f));
            Touch(22,InputPhase.Ended,new Vector2(Screen.width*.65f,Screen.height*.545f));
            Assert.That(root.HudControls.SelectedDestination,Is.EqualTo("exploration.gate"));
        }
        [UnityTest]
        public IEnumerator AbilityButtonEmitsOneIntentFromNativeUiTouch()
        {
            int intents=0;root.HudControls.AbilityRequested+=()=>intents++;
            var p=Center((RectTransform)root.View.Hud.AbilityButton.transform);
            Touch(30,InputPhase.Began,p);
            UnityEngine.EventSystems.EventSystem.current.currentInputModule.Process();
            yield return null;
            Touch(30,InputPhase.Ended,p);
            UnityEngine.EventSystems.EventSystem.current.currentInputModule.Process();
            yield return null;
            Assert.That(intents,Is.EqualTo(1));
            Assert.That(root.Encounter.Player.DodgeCharges,Is.EqualTo(3));
        }
        [UnityTest]
        public IEnumerator LayoutChangeCancelsHeldNativeAbilityPress()
        {
            int intents=0;root.HudControls.AbilityRequested+=()=>intents++;
            var p=Center((RectTransform)root.View.Hud.AbilityButton.transform);
            Touch(50,InputPhase.Began,p);
            UnityEngine.EventSystems.EventSystem.current.currentInputModule.Process();
            yield return null;
            root.View.Hud.ApplyViewport(Screen.width,Screen.height,new Rect(1,0,Screen.width-2,Screen.height));
            Canvas.ForceUpdateCanvases();
            Touch(50,InputPhase.Ended,p);
            UnityEngine.EventSystems.EventSystem.current.currentInputModule.Process();
            yield return null;
            Assert.That(intents,Is.Zero);
        }
        [UnityTest]
        public IEnumerator FocusCancellationRequiresFreshNativeResumePress()
        {
            root.HudControls.Pause();
            Canvas.ForceUpdateCanvases();
            yield return GrayboxCapture.WarmFrame(Camera.main);
            var p=Center((RectTransform)root.View.ResumeButton.transform);
            Touch(51,InputPhase.Began,p);
            UnityEngine.EventSystems.EventSystem.current.currentInputModule.Process();
            yield return null;
            root.SetLifecycle(false,false);root.SetLifecycle(false,true);
            Touch(51,InputPhase.Ended,p);
            UnityEngine.EventSystems.EventSystem.current.currentInputModule.Process();
            yield return null;
            Assert.That(root.Timing.IsUserPaused,Is.True);
            Assert.That(root.Timing.Session.Clock.State,Is.EqualTo(CombatClockState.Suspended));
        }
        [UnityTest]
        public IEnumerator NativeCameraCapturesBothReferenceProfilesAndMenus()
        {
            var hud=root.View.Hud;
            foreach(var hand in new[]{HudHand.Right,HudHand.Left})
            {
                hud.ApplySettings(new HudSettings(hand));
                hud.ApplyViewport(1320,2868,new Rect(0,100,1320,2600));
                yield return PortraitHudCapture.Save(Camera.main,1320,2868,"iphone17-"+hand+".png");
                hud.ApplyViewport(1440,3120,new Rect(0,80,1440,2960));
                yield return PortraitHudCapture.Save(Camera.main,1440,3120,"android-"+hand+".png");
            }
            hud.ApplySettings(new HudSettings());
            hud.ApplyViewport(720,1280,new Rect(0,24,720,1200));
            root.HudControls.Pause();
            yield return PortraitHudCapture.Save(Camera.main,720,1280,"pause-menu.png");
            root.HudControls.BeginCalibration();
            yield return PortraitHudCapture.Save(Camera.main,720,1280,"reach-calibration.png");
            root.HudControls.CancelCalibration();
            root.HudControls.StartExploration();
            yield return PortraitHudCapture.Save(Camera.main,720,1280,"exploration-preview.png");
            hud.RefreshLayout();
        }
        [Test]
        public void CalibrationActionsMirrorWithoutChangingApplyAndCancelMeaning()
        {
            var menu=root.View.Hud.Menu;
            var right=((RectTransform)menu.ApplyButton.transform).anchorMin.x;
            var width=((RectTransform)menu.ApplyButton.transform).anchorMax.x-right;
            root.HudControls.SetHand(HudHand.Left);
            root.HudControls.BeginCalibration();
            var left=((RectTransform)menu.ApplyButton.transform).anchorMin.x;
            Assert.That(left,Is.EqualTo(1-right-width).Within(.0001));
        }
        [UnityTest]
        public IEnumerator ExplorationUsesBoundCameraWithAnotherMainCameraPresent()
        {
            var holder=new GameObject("Bound camera test root");holder.SetActive(false);
            var cameraObject=new GameObject("Explicitly bound camera");
            var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
            var original=Quaternion.Euler(10,20,0);camera.transform.rotation=original;
            var mainOriginal=Camera.main.transform.rotation;
            try
            {
                var other=holder.AddComponent<GrayboxEncounterRoot>();other.Bind(camera,null);
                holder.SetActive(true);other.SetLifecycle(false,true);other.RestartEncounter(()=>now);
                other.HudControls.StartExploration();
                Touch(40,InputPhase.Began,new Vector2(Screen.width*.5f,Screen.height*.5f));
                Touch(40,InputPhase.Ended,new Vector2(Screen.width*.7f,Screen.height*.65f));
                Assert.That(Quaternion.Angle(camera.transform.rotation,original),Is.GreaterThan(1));
                Assert.That(Quaternion.Angle(Camera.main.transform.rotation,mainOriginal),Is.LessThan(.01));
            }
            finally {Object.Destroy(holder);Object.Destroy(cameraObject);}
            yield return null;
        }
        [UnityTest]
        public IEnumerator BothCanvasesUseCameraRaycastersWithStaticMenuOnTop()
        {
            var canvases=root.View.GetComponentsInChildren<Canvas>(true);
            Assert.That(canvases.Length, Is.GreaterThanOrEqualTo(2));
            foreach(var canvas in canvases)
            {
                Assert.That(canvas.worldCamera, Is.EqualTo(Camera.main));
                Assert.That(canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>(), Is.Not.Null);
            }
            root.HudControls.Pause();
            Canvas.ForceUpdateCanvases();
            yield return GrayboxCapture.WarmFrame(Camera.main);
            var sample=new TouchSample(99,SamplePhase.Began,new NormalizedPoint(.5,.5),now);
            var metrics=new ScreenMetrics(Screen.width,Screen.height);
            var owner=new Praxen.Game.Presentation.Input.EventSystemPointerOwnership().Resolve(in sample,in metrics);
            Assert.That(owner.Kind, Is.EqualTo(PointerOwnerKind.Ui));
        }
    }
}
