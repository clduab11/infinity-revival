using System;
using Praxen.Game.Application.Combat;
using Praxen.Game.Application.Input;
using Praxen.Game.Content.Input;
using Praxen.Game.Content.Combat;
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
        [SerializeField] private EnemyArchetype enemyArchetype = EnemyArchetype.Sword;
        [SerializeField] private int encounterSeed = 20260929;
        [SerializeField, Range(0, 5)] private int difficultyTier;
        private Func<long> sourceTime;
        private GestureTraceView trace;
        private CombatSession displaySession;
        private bool paused, focused = true;
        private string tell = "Prepare to defend", outcome = "Swipe in the authored direction to parry";
        public CombatTimingDriver Timing { get; private set; }
        public TimestampedTouchCapture InputCapture { get; private set; }
        public GesturePhaseContext InteractionPhase { get; private set; }
        public CombatEncounter Encounter { get; private set; }
        public CombatOpeningController Duel { get; private set; }
        public EnemyPatternDirector EnemyAI { get; private set; }
        public GrayboxEncounterView View { get; private set; }
        public TimestampedDefenseControls Controls { get; private set; }

        public void Bind(Camera camera, GestureTuningAsset tuning)
        {
            if (Timing != null) throw new InvalidOperationException("Bind before startup.");
            encounterCamera = camera;
            gestureTuning = tuning;
        }

        private void Awake()
        {
            if (encounterCamera == null) encounterCamera = Camera.main;
            View = Child("Graybox view").AddComponent<GrayboxEncounterView>();
            View.Build(encounterCamera);
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
            RestartEncounter();
        }

        public void RestartEncounter(Func<long> timestampProvider = null, uint? seed = null,
            EnemyArchetype? archetype = null, int? tier = null)
        {
            // Validate requested encounter inputs before disposing the current session.
            if (tier.HasValue && (tier.Value < 0 || tier.Value > 5))
                throw new ArgumentOutOfRangeException(nameof(tier));
            if (archetype.HasValue && !Enum.IsDefined(typeof(EnemyArchetype), archetype.Value))
                throw new ArgumentOutOfRangeException(nameof(archetype));
            if (seed.HasValue) encounterSeed = unchecked((int)seed.Value);
            if (archetype.HasValue) enemyArchetype = archetype.Value;
            if (tier.HasValue) difficultyTier = tier.Value;
            DisposeEncounter();
            Controls.CancelHeld();
            Timing.EndEncounter();
            if (timestampProvider != null) sourceTime = timestampProvider;
            Timing.Configure(InputCapture, InteractionPhase, sourceTime);
            // A deliberate restart creates a fresh session; lifecycle suspension is reapplied below.
            Timing.SetLifecycle(false, true);
            var phase = new InteractionPhase(checked(InteractionPhase.Current.Id + 1),
                InteractionPhaseKind.EnemySequence);
            Timing.BeginEncounter(phase);
            Encounter = new CombatEncounter(Timing.Session, phase);
            Duel = new CombatOpeningController(Encounter, InteractionPhase, null,
                gestureTuning != null ? gestureTuning.ToRuntime() : GestureTuning.Default);
            Duel.GestureRecognized += InputCapture.PublishRecognized;
            Duel.PlayerStrikeResolved += ShowAttackResult;
            Encounter.TelegraphStarted += ShowTell;
            Encounter.StrikeResolved += ShowResult;
            displaySession = Timing.Session;
            displaySession.FrameAdvanced += ShowFrame;
            tell = "Prepare to defend";
            outcome = "Swipe in the authored direction to parry";
            Timing.SetLifecycle(paused, focused);
            EnemyAI = new EnemyPatternDirector(Duel, PrototypeEnemyDecks.Create(enemyArchetype),
                unchecked((uint)encounterSeed), difficultyTier);
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
                ? "OPENING: swipe through the arena to attack" : tell;
            var result = Duel.Outcome == DuelOutcome.Victory ? "VICTORY: restart the encounter" :
                Duel.Outcome == DuelOutcome.Defeat ? "DEFEATED: restart the encounter" : outcome;
            if (EnemyAI?.CurrentDecision != null)
                prompt += $"\n{EnemyAI.CurrentDecision.Pattern.Id} ({EnemyAI.State})";
            View.Show(Encounter.Player, prompt, result, Timing.Session.Clock);
            View.ShowOffense(Duel.Offense, Duel.Momentum, Duel.CurrentPhase, Duel.OpeningRemainingUs);
        }

        private void ShowTell(EnemyStrike strike) => tell = GrayboxStrikes.Describe(strike);
        private void ShowResult(DefenseResolution result)
        {
            outcome = result.IsDead ? "DEFEATED: restart the encounter" : result.Outcome.ToString();
        }
        private void ReleaseHeldDefense() => Encounter?.Player.ReleaseHeldDefense();
        private void CancelContacts() => Duel?.CancelContacts();
        private void ShowAttackResult(PlayerAttackResolution result) => outcome =
            $"{result.Attack.Direction}: {result.Damage} damage" + (result.Attack.ComboFinisher ? " (FINISHER)" : string.Empty);
        private void EndEncounter()
        {
            DisposeEncounter();
            Controls?.CancelHeld();
            trace?.Clear();
        }

        private void DisposeEncounter()
        {
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
        private void Resume() => Timing.RequestResume();
        private void RestartFromButton() => RestartEncounter();
        public void SetLifecycle(bool applicationPaused, bool applicationFocused)
        {
            paused = applicationPaused;
            focused = applicationFocused;
            Timing?.SetLifecycle(paused, focused);
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
        }
    }
}
