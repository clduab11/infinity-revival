using System;
using Praxen.Game.Application.Combat;
using Praxen.Game.Domain.Combat;
using UnityEngine;

namespace Praxen.Game.Presentation.Combat
{
    /// <summary>Owns visual/audio resources; gameplay supplies time and immutable cues.</summary>
    public sealed class CombatPresentation : MonoBehaviour
    {
        private CombatPresentationProfile profile;
        private CombatPresentationSnapshot snapshot;
        private GrayboxEncounterView view;
        private Camera camera;
        private Vector3 cameraPosition;
        private Quaternion cameraRotation;
        private GameObject player, enemy;
        private Transform playerTip, enemyTip;
        private readonly AudioSource[] voices = new AudioSource[4];
        private int voice;
        private CombatEncounter encounter;
        private CombatPresentationObserver observer;
        private long impulseUs = -1000000;
        private bool configured;
        private bool cameraOrbitSuppressed;
        private ActorMotion suppressedCameraMotion;
        private bool enableCameraOrbit, reducedMotion;
        private float cameraOrbitDegrees, cameraFocusDistance;
        public CharacterMotionPlayer PlayerAnimation { get; private set; }
        public CharacterMotionPlayer EnemyAnimation { get; private set; }
        public CombatImpactEffects Effects { get; private set; }
        public int SoundCueCount { get; private set; }

        public void Configure(CombatPresentationProfile configuration, GrayboxEncounterView target, Camera encounterCamera)
        {
            if (configured) throw new InvalidOperationException("Presentation is configured once.");
            if (configuration == null || target == null || encounterCamera == null)
                throw new ArgumentNullException(nameof(configuration));
            view = target; camera = encounterCamera;
            cameraPosition = camera.transform.localPosition;
            cameraRotation = camera.transform.localRotation;
            configured = true;
            try { RefreshConfiguration(configuration); }
            catch { configured = false; throw; }
            Effects = new CombatImpactEffects(transform);
            for (int i = 0; i < voices.Length; i++)
            {
                var child = new GameObject("Feedback voice " + i, typeof(AudioSource));
                child.transform.SetParent(transform, false);
                voices[i] = child.GetComponent<AudioSource>();
                voices[i].playOnAwake = false;
                voices[i].spatialBlend = 0;
                voices[i].volume = .35f;
            }
            view.SetPrototypeVisuals(true);
        }

        /// <summary>Only a deliberate unbound restart can replace the captured loadout.</summary>
        public void RefreshConfiguration(CombatPresentationProfile configuration)
        {
            if (encounter != null || observer != null)
                throw new InvalidOperationException("Unbind the current encounter before refreshing presentation.");
            using (var prepared = PrepareConfiguration(configuration)) prepared.Commit();
        }

        /// <summary>Validates and owns replacements before the running encounter is changed.</summary>
        public PreparedCombatConfiguration PrepareConfiguration(CombatPresentationProfile configuration)
        {
            if (!configured) throw new InvalidOperationException("Configure presentation before preparing it.");
            var next = new CombatPresentationSnapshot(configuration);
            BuildReplacement(next, out var nextPlayer, out var nextEnemy,
                out var nextPlayerMotion, out var nextEnemyMotion);
            nextPlayer.SetActive(false); nextEnemy.SetActive(false);
            return new PreparedCombatConfiguration(() =>
            {
                if (encounter != null || observer != null)
                    throw new InvalidOperationException("Unbind the current encounter before committing presentation.");
                if (this == null) throw new InvalidOperationException("The presentation owner was destroyed.");
                var nextPlayerTip = Tip(nextPlayer, next.SoldierWeaponTipPath);
                var nextEnemyTip = Tip(nextEnemy, next.CaptainWeaponTipPath);
                DisposePlayers();
                RemoveActor(player); RemoveActor(enemy);
                profile = configuration; snapshot = next;
                player = nextPlayer; enemy = nextEnemy;
                playerTip = nextPlayerTip; enemyTip = nextEnemyTip;
                PlayerAnimation = nextPlayerMotion; EnemyAnimation = nextEnemyMotion;
                player.SetActive(isActiveAndEnabled); enemy.SetActive(isActiveAndEnabled);
                ApplyCameraPreferences(next.EnableCameraOrbit, next.CameraOrbitDegrees,
                    next.CameraFocusDistance, next.ReducedMotion);
                RestoreCamera();
            }, () =>
            {
                nextPlayerMotion?.Dispose(); nextEnemyMotion?.Dispose();
                RemoveActor(nextPlayer); RemoveActor(nextEnemy);
            });
        }

        private void BuildReplacement(CombatPresentationSnapshot next, out GameObject nextPlayer,
            out GameObject nextEnemy, out CharacterMotionPlayer nextPlayerMotion,
            out CharacterMotionPlayer nextEnemyMotion)
        {
            nextPlayer = null; nextEnemy = null;
            nextPlayerMotion = null; nextEnemyMotion = null;
            try
            {
                nextPlayer = CreateActor(next.Soldier, view.PlayerAnchor, "Player soldier", 0);
                nextEnemy = CreateActor(next.Captain, view.EnemyAnchor, "Enemy captain", 180);
                Tip(nextPlayer, next.SoldierWeaponTipPath); Tip(nextEnemy, next.CaptainWeaponTipPath);
                // Even a disabled presentation validates its replacement avatar and contact records.
                nextPlayerMotion = new CharacterMotionPlayer(nextPlayer, next.SoldierMotions);
                nextEnemyMotion = new CharacterMotionPlayer(nextEnemy, next.CaptainMotions);
                if (!isActiveAndEnabled)
                {
                    nextPlayerMotion.Dispose(); nextEnemyMotion.Dispose();
                    nextPlayerMotion = null; nextEnemyMotion = null;
                    nextPlayer.SetActive(false); nextEnemy.SetActive(false);
                }
            }
            catch
            {
                nextPlayerMotion?.Dispose(); nextEnemyMotion?.Dispose();
                RemoveActor(nextPlayer); RemoveActor(nextEnemy);
                throw;
            }
        }

        /// <summary>Live accessibility preferences are separate from frozen authoring data.</summary>
        public void ApplyCameraPreferences(bool enableOrbit, float degrees, float focusDistance,
            bool reducedMotion = false)
        {
            if (float.IsNaN(degrees) || float.IsInfinity(degrees))
                throw new ArgumentOutOfRangeException(nameof(degrees));
            if (float.IsNaN(focusDistance) || float.IsInfinity(focusDistance))
                throw new ArgumentOutOfRangeException(nameof(focusDistance));
            enableCameraOrbit = enableOrbit;
            cameraOrbitDegrees = Mathf.Clamp(degrees, 0, 4);
            cameraFocusDistance = Mathf.Clamp(focusDistance, 1, 12);
            this.reducedMotion = reducedMotion;
        }

        private static GameObject CreateActor(GameObject prefab, Transform anchor, string name, float yaw)
        {
            if (prefab == null) throw new InvalidOperationException("Prototype actor prefab missing.");
            var actor = Instantiate(prefab, anchor, false);
            actor.name = name;
            actor.transform.localPosition = new Vector3(0, -1, 0);
            actor.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            return actor;
        }

        private static Transform Tip(GameObject actor, string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                var marker = actor.transform.Find(path);
                var animator = actor.GetComponentInChildren<Animator>();
                if (marker == null || animator == null || !marker.IsChildOf(animator.transform))
                    throw new InvalidOperationException("The weapon tip path must resolve under the actor animator.");
                return marker;
            }
            foreach (var node in actor.GetComponentsInChildren<Transform>())
                if (node.name == "WeaponTip") return node;
            return null;
        }

        private void CreatePlayers()
        {
            if (!configured || !isActiveAndEnabled || PlayerAnimation != null) return;
            player.SetActive(true); enemy.SetActive(true);
            PlayerAnimation = new CharacterMotionPlayer(player, snapshot.SoldierMotions);
            EnemyAnimation = new CharacterMotionPlayer(enemy, snapshot.CaptainMotions);
        }

        public void Bind(CombatEncounter combatEncounter, CombatOpeningController duel)
        {
            Unbind();
            encounter = combatEncounter;
            observer = new CombatPresentationObserver(combatEncounter, duel);
            observer.Cue += PresentCue;
            encounter.Session.Suspended += StopAudio;
            SoundCueCount = 0;
            RenderCurrent();
        }

        public void Unbind()
        {
            if (encounter != null) encounter.Session.Suspended -= StopAudio;
            if (observer != null) { observer.Cue -= PresentCue; observer.Dispose(); }
            observer = null; encounter = null;
            StopAudio();
            Effects?.Clear();
            RestoreCamera();
            impulseUs = -1000000;
            cameraOrbitSuppressed = false;
            suppressedCameraMotion = default;
        }

        public void RenderCurrent()
        {
            if (!configured || observer == null || encounter == null) return;
            long time = encounter.Session.Clock.TimeUs;
            observer.Sample(time);
            if (!isActiveAndEnabled) return;
            CreatePlayers();
            PlayerAnimation.Render(observer.PlayerMotion, time);
            EnemyAnimation.Render(observer.EnemyMotion, time);
            Effects.Render(time, playerTip, enemyTip, observer.PlayerMotion, observer.EnemyMotion);
            RenderCamera(observer.PlayerMotion, time);
        }

        private void RenderCamera(ActorMotion motion, long time)
        {
            if (cameraOrbitSuppressed && (motion.Key != suppressedCameraMotion.Key ||
                motion.StartedUs != suppressedCameraMotion.StartedUs)) cameraOrbitSuppressed = false;
            var pose = CombatCameraMotion.Sample(cameraPosition, cameraRotation, motion, time,
                enableCameraOrbit && !reducedMotion && !cameraOrbitSuppressed,
                cameraOrbitDegrees, cameraFocusDistance);
            float elapsed = (time - impulseUs) / 1000000f;
            float kick = !reducedMotion && elapsed >= 0 && elapsed < .18f
                ? .045f * Mathf.Sin(elapsed / .18f * Mathf.PI) : 0;
            camera.transform.localPosition = pose.Position + new Vector3(0, kick, -kick * .4f);
            camera.transform.localRotation = pose.Rotation;
        }

        private void PresentCue(CombatPresentationCue cue)
        {
            if (!configured || !isActiveAndEnabled) return;
            var clip = snapshot.FindSound(cue.Kind);
            if (clip != null)
            {
                var source = voices[voice++ % voices.Length];
                source.Stop(); source.clip = clip; source.Play();
                SoundCueCount++;
            }
            var anchor = cue.TargetEnemy ? view.EnemyAnchor : view.PlayerAnchor;
            Effects.Emit(cue, anchor.position + Vector3.up * .2f);
            if (cue.Kind == CombatCueKind.Hit || cue.Kind == CombatCueKind.Parry ||
                cue.Kind == CombatCueKind.Block || cue.Kind == CombatCueKind.Death) impulseUs = cue.TimeUs;
        }

        private void StopAudio()
        { foreach (var source in voices) if (source != null) source.Stop(); }
        private void RestoreCamera()
        {
            if (camera == null) return;
            camera.transform.localPosition = cameraPosition;
            camera.transform.localRotation = cameraRotation;
        }
        private void DisposePlayers()
        {
            PlayerAnimation?.Dispose(); EnemyAnimation?.Dispose();
            PlayerAnimation = null; EnemyAnimation = null;
        }
        private static void RemoveActor(GameObject actor)
        {
            if (actor == null) return;
            actor.SetActive(false);
            if (UnityEngine.Application.isPlaying) Destroy(actor);
            else DestroyImmediate(actor);
        }
        private void OnEnable()
        {
            CreatePlayers();
            if (configured && observer != null && encounter != null)
            {
                observer.Sample(encounter.Session.Clock.TimeUs);
                suppressedCameraMotion = observer.PlayerMotion;
                cameraOrbitSuppressed = true;
            }
            RenderCurrent();
        }
        private void OnDisable()
        {
            StopAudio(); DisposePlayers(); Effects?.Clear(); RestoreCamera();
            impulseUs = -1000000;
            if (player != null) player.SetActive(false);
            if (enemy != null) enemy.SetActive(false);
        }
        private void OnDestroy()
        {
            Unbind(); DisposePlayers(); Effects?.Dispose();
            foreach (var source in voices) if (source != null) Destroy(source.gameObject);
            RemoveActor(player); RemoveActor(enemy);
        }
    }
}
