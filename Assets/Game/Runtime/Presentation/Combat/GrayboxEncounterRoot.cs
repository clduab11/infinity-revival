using System;
using Praxen.Game.Application.Combat;
using Praxen.Game.Application.Input;
using Praxen.Game.Content.Input;
using Praxen.Game.Content.Combat;
using Praxen.Game.Content.Definitions;
using Praxen.Game.Domain.Content;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;
using Praxen.Game.Presentation.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Praxen.Game.Presentation.Combat
{
    public sealed class GrayboxEncounterRoot : MonoBehaviour
    {
        [SerializeField] private Camera encounterCamera;
        [SerializeField] private GestureTuningAsset gestureTuning;
        [SerializeField] private CombatPresentationProfile presentationProfile;
        [SerializeField] private CombatContentCatalog contentCatalog;
        [SerializeField] private MotionProfileDefinition enemyMotionProfile;
        [SerializeField] private string weaponId = "weapon.pilot.sword";
        private CombatPresentationProfile admittedPresentationProfile;
        [SerializeField] private EnemyArchetype enemyArchetype = EnemyArchetype.Sword;
        [SerializeField] private int encounterSeed = 20260929;
        [SerializeField, Range(0, 5)] private int difficultyTier;
        private Func<long> sourceTime;
        private GestureTraceView trace;
        private CombatSession displaySession;
        private bool paused, focused = true;
        private string tell = HudText.Get("combat.prepare"), outcome = HudText.Get("combat.instructions");
        public Camera EncounterCamera => encounterCamera;
        public CombatContentCatalog ContentCatalog => contentCatalog;
        public EncounterContentSnapshot ActiveContent { get; private set; }
        public string SelectedWeaponId => weaponId;
        public CombatPresentation Presentation { get; private set; }
        public CombatTimingDriver Timing { get; private set; }
        public TimestampedTouchCapture InputCapture { get; private set; }
        public GesturePhaseContext InteractionPhase { get; private set; }
        public CombatEncounter Encounter { get; private set; }
        public CombatOpeningController Duel { get; private set; }
        public EnemyPatternDirector EnemyAI { get; private set; }
        public GrayboxEncounterView View { get; private set; }
        public TimestampedDefenseControls Controls { get; private set; }
        public PortraitHudCoordinator HudControls { get; private set; }

        public void Bind(Camera camera, GestureTuningAsset tuning)
        {
            if (Timing != null) throw new InvalidOperationException("Bind before startup.");
            encounterCamera = camera;
            gestureTuning = tuning;
        }

        public void BindPresentation(CombatPresentationProfile profile) => presentationProfile = profile;

        public void BindContent(CombatContentCatalog catalog, MotionProfileDefinition enemyMotion,
            string initialWeaponId = "weapon.pilot.sword")
        {
            if (Timing != null) throw new InvalidOperationException("Bind content before startup.");
            contentCatalog = catalog;
            enemyMotionProfile = enemyMotion;
            weaponId = initialWeaponId;
        }

        private void Awake()
        {
            if (encounterCamera == null) encounterCamera = Camera.main;
            View = Child("Graybox view").AddComponent<GrayboxEncounterView>();
            View.Build(encounterCamera);
            if (presentationProfile != null)
            {
                Presentation = Child("Combat presentation").AddComponent<CombatPresentation>();
                Presentation.Configure(presentationProfile, View, encounterCamera);
            }
            if (EventSystem.current == null)
            {
                var ui = Child("UI EventSystem");
                ui.AddComponent<EventSystem>();
                ui.AddComponent<InputSystemUIInputModule>();
            }
            InteractionPhase = new GesturePhaseContext();
            InputCapture = Child("Timestamped touch capture").AddComponent<TimestampedTouchCapture>();
            InputCapture.SetInputEnabled(false);
            InputCapture.Configure(InteractionPhase, new EventSystemPointerOwnership(),
                gestureTuning != null ? gestureTuning.ToRuntime() : GestureTuning.Default);
            InputCapture.SetOrderedRecognition(true);
            InputCapture.ContactsCancelled += CancelContacts;
            Timing = Child("Combat timing").AddComponent<CombatTimingDriver>();
            Timing.Configure(InputCapture, InteractionPhase);
            Timing.EncounterEnded += EndEncounter;
            Timing.TouchInputRejected += CancelContacts;
            Controls = Child("Timestamped defense controls").AddComponent<TimestampedDefenseControls>();
            Controls.Configure(InputCapture, Timing, View);
            Controls.HeldControlCancelled += ReleaseHeldDefense;
            trace = new GameObject("Gesture trace", typeof(RectTransform)).AddComponent<GestureTraceView>();
            trace.Configure(View.GetComponentInChildren<Canvas>());
            InputCapture.CommandProduced += trace.Show;
            Timing.Suspended += trace.Clear;
            View.ResumeButton.onClick.AddListener(Resume);
            View.RestartButton.onClick.AddListener(RestartFromButton);
            HudControls = Child("Portrait HUD coordinator").AddComponent<PortraitHudCoordinator>();
            HudControls.Configure(this);
            RestartEncounter();
        }

        public void RestartEncounter(Func<long> timestampProvider = null, uint? seed = null,
            EnemyArchetype? archetype = null, int? tier = null, string selectedWeaponId = null)
        {
            // Validate requested encounter inputs before disposing the current session.
            if (tier.HasValue && (tier.Value < 0 || tier.Value > 5))
                throw new ArgumentOutOfRangeException(nameof(tier));
            if (archetype.HasValue && !Enum.IsDefined(typeof(EnemyArchetype), archetype.Value))
                throw new ArgumentOutOfRangeException(nameof(archetype));
            var nextArchetype = archetype ?? enemyArchetype;
            var nextWeaponId = selectedWeaponId ?? weaponId;
            // Validate the whole authoring graph and copy values before changing the active encounter.
            var nextContent = contentCatalog == null ? null : contentCatalog.ToRuntime(nextArchetype, nextWeaponId);
            var nextDeck = nextContent?.EnemyDeck ?? PrototypeEnemyDecks.Create(nextArchetype);
            var nextGesture = gestureTuning != null ? gestureTuning.ToRuntime() : GestureTuning.Default;
            var nextPresentation = Presentation == null ? null : EncounterPresentationCapture.Capture(
                presentationProfile, nextContent == null ? null : contentCatalog.GetWeapon(nextWeaponId),
                enemyMotionProfile, contentCatalog?.Camera);
            PreparedCombatConfiguration prepared;
            try { prepared = nextPresentation == null ? null : Presentation.PrepareConfiguration(nextPresentation); }
            catch
            {
                if (nextPresentation != null) Destroy(nextPresentation);
                throw;
            }
            using (prepared)
            {
                if (seed.HasValue) encounterSeed = unchecked((int)seed.Value);
                if (archetype.HasValue) enemyArchetype = archetype.Value;
                if (tier.HasValue) difficultyTier = tier.Value;
                weaponId = nextWeaponId;
                ActiveContent = nextContent;
                HudControls?.ResetForEncounter();
                DisposeEncounter();
                if (nextPresentation != null)
                {
                    prepared.Commit();
                    if (admittedPresentationProfile != null) Destroy(admittedPresentationProfile);
                    admittedPresentationProfile = nextPresentation;
                }
                StartEncounter(timestampProvider, nextContent, nextDeck, nextGesture);
            }
        }

        private void StartEncounter(Func<long> timestampProvider, EncounterContentSnapshot nextContent,
            EnemyPatternDeck nextDeck, GestureTuning nextGesture)
        {
            Controls.CancelHeld();
            Timing.EndEncounter();
            if (timestampProvider != null) sourceTime = timestampProvider;
            Timing.Configure(InputCapture, InteractionPhase, sourceTime);
            // A deliberate restart creates a fresh session; lifecycle suspension is reapplied below.
            Timing.SetLifecycle(false, true);
            Timing.ClearUserPauseForRestart();
            var phase = new InteractionPhase(checked(InteractionPhase.Current.Id + 1),
                InteractionPhaseKind.EnemySequence);
            Timing.BeginEncounter(phase);
            Encounter = new CombatEncounter(Timing.Session, phase, nextContent?.Defense);
            Duel = new CombatOpeningController(Encounter, InteractionPhase, nextContent?.Offense, nextGesture);
            Duel.GestureRecognized += InputCapture.PublishRecognized;
            Duel.PlayerStrikeResolved += ShowAttackResult;
            Encounter.TelegraphStarted += ShowTell;
            Encounter.StrikeResolved += ShowResult;
            displaySession = Timing.Session;
            displaySession.FrameAdvanced += ShowFrame;
            tell = HudText.Get("combat.prepare");
            outcome = HudText.Get("combat.instructions");
            Timing.SetLifecycle(paused, focused);
            EnemyAI = new EnemyPatternDirector(Duel, nextDeck,
                unchecked((uint)encounterSeed), difficultyTier);
            if (Presentation != null) Presentation.Bind(Encounter, Duel);
            EnemyAI.Start(0);
            ShowFrame(0);
        }

        private void Update()
        {
            if (Encounter == null || Timing.Session == null) return;
            ShowFrame(Timing.Session.Clock.TimeUs);
        }

        private void ShowFrame(long timeUs)
        {
            if (Encounter == null || Duel == null || Timing.Session == null) return;
            var prompt = Duel.CurrentPhase.Kind == InteractionPhaseKind.PlayerOpening
                ? HudText.Get("combat.opening") : tell;
            var result = Duel.Outcome == DuelOutcome.Victory ? HudText.Get("combat.victory") :
                Duel.Outcome == DuelOutcome.Defeat ? HudText.Get("combat.defeat") : outcome;

            View.Show(Encounter.Player, prompt, result, Timing.Session.Clock);
            View.ShowOffense(Duel.Offense, Duel.Momentum, Duel.CurrentPhase, Duel.OpeningRemainingUs);
            if (Presentation != null) Presentation.RenderCurrent();
        }

        public void ShowFeedback(string localizationKey)
        {
            outcome = HudText.Get(localizationKey);
            ShowFrame(Timing.Session?.Clock.TimeUs ?? 0);
        }

        private void ShowTell(EnemyStrike strike) => tell = GrayboxStrikes.Describe(strike);
        private void ShowResult(DefenseResolution result)
        {
            outcome = result.IsDead ? HudText.Get("combat.defeat") : HudText.Get("result." + result.Outcome);
        }
        private void ReleaseHeldDefense() => Encounter?.Player.ReleaseHeldDefense();
        private void CancelContacts() => Duel?.CancelContacts();
        private void ShowAttackResult(PlayerAttackResolution result) => outcome =
            HudText.Format("combat.attack", HudText.Get("direction." + result.Attack.Direction), result.Damage,
                result.Attack.ComboFinisher ? HudText.Get("combat.finisher") : string.Empty);
        private void EndEncounter()
        {
            DisposeEncounter();
            Controls?.CancelHeld();
            trace?.Clear();
        }

        private void DisposeEncounter()
        {
            if (Presentation != null) Presentation.Unbind();
            EnemyAI?.Dispose();
            EnemyAI = null;
            if (displaySession != null) displaySession.FrameAdvanced -= ShowFrame;
            displaySession = null;
            if (Duel != null)
            {
                Duel.GestureRecognized -= InputCapture.PublishRecognized;
                Duel.PlayerStrikeResolved -= ShowAttackResult;
                Duel.Dispose();
                Duel = null;
            }
            Encounter?.Dispose();
            Encounter = null;
        }
        private void Resume() => HudControls.Resume();
        private void RestartFromButton() => RestartEncounter();
        public void SetLifecycle(bool applicationPaused, bool applicationFocused)
        {
            paused = applicationPaused;
            focused = applicationFocused;
            Timing?.SetLifecycle(paused, focused);
            HudControls?.SetLifecycle(paused, focused);
        }
        private void OnApplicationPause(bool value) => SetLifecycle(value, focused);
        private void OnApplicationFocus(bool value) => SetLifecycle(paused, value);

        private GameObject Child(string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(transform, false);
            return child;
        }

        private void OnDestroy()
        {
            if (InputCapture != null && trace != null) InputCapture.CommandProduced -= trace.Show;
            if (InputCapture != null) InputCapture.ContactsCancelled -= CancelContacts;
            if (Timing != null && trace != null) Timing.Suspended -= trace.Clear;
            if (Timing != null) Timing.EncounterEnded -= EndEncounter;
            if (Timing != null) Timing.TouchInputRejected -= CancelContacts;
            if (Controls != null) Controls.HeldControlCancelled -= ReleaseHeldDefense;
            DisposeEncounter();
            if (admittedPresentationProfile != null) Destroy(admittedPresentationProfile);
        }
    }
}
