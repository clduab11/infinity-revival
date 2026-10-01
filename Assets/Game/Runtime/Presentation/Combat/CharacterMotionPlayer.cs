using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Praxen.Game.Presentation.Combat
{
    /// <summary>Samples presentation clips exclusively from the combat clock.</summary>
    public sealed class CharacterMotionPlayer : IDisposable
    {
        private const long MaximumBlendUs = 60000;
        private readonly MotionSlot[] slots;
        private PlayableGraph graph;
        private AnimationMixerPlayable mixer;
        private ActorMotion currentMotion;
        private ActorMotion previousMotion;
        private string requestedKey;
        private long requestedStartedUs;
        private long blendStartedUs;
        private long blendDurationUs;
        private int currentSlot = -1;
        private int previousSlot = -1;
        private int idleSlot = -1;
        private bool hasMotion;

        public Animator Animator { get; }
        public string MotionKey => hasMotion ? currentMotion.Key : string.Empty;
        public double SampleTimeSeconds { get; private set; }
        public bool GraphIsValid => graph.IsValid();

        public CharacterMotionPlayer(GameObject actor, CombatPresentationProfile profile, bool enemy)
            : this(actor, profile == null ? null : enemy ? profile.CaptainMotions : profile.SoldierMotions)
        { }

        public CharacterMotionPlayer(GameObject actor, MotionClipBinding[] bindings)
        {
            Animator = actor == null ? null : actor.GetComponentInChildren<Animator>();
            ValidateAvatar();
            Animator.applyRootMotion = false;
            Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            slots = new MotionSlot[bindings == null ? 0 : bindings.Length];
            graph = PlayableGraph.Create("Combat character motion");
            try
            {
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                mixer = AnimationMixerPlayable.Create(graph, slots.Length);
                CreateClips(bindings);
                AnimationPlayableOutput.Create(graph, "Character", Animator).SetSourcePlayable(mixer);
                Animator.Rebind();
                graph.Play();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Render(ActorMotion motion, long combatTimeUs)
        {
            if (!graph.IsValid()) return;
            if (!hasMotion || motion.StartedUs != requestedStartedUs ||
                !string.Equals(motion.Key, requestedKey, StringComparison.Ordinal))
                BeginMotion(motion);
            else currentMotion = ResolveMotion(motion, currentSlot);
            if (currentSlot < 0)
            {
                SampleTimeSeconds = 0;
                return;
            }
            SampleTimeSeconds = Sample(currentSlot, currentMotion, combatTimeUs);
            if (previousSlot >= 0)
            {
                Sample(previousSlot, previousMotion, combatTimeUs);
                float weight = BlendWeight(combatTimeUs);
                mixer.SetInputWeight(currentSlot, weight);
                mixer.SetInputWeight(previousSlot, 1f - weight);
                if (weight >= 1f) previousSlot = -1;
            }
            graph.Evaluate(0f);
        }

        public void Dispose()
        {
            if (graph.IsValid()) graph.Destroy();
        }

        private void ValidateAvatar()
        {
            if (Animator == null || Animator.avatar == null ||
                !Animator.avatar.isValid || !Animator.avatar.isHuman)
                throw new InvalidOperationException("Combat presentation requires a valid humanoid avatar.");
        }

        private void CreateClips(MotionClipBinding[] bindings)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                var binding = bindings[i];
                if (binding.Clip == null) continue;
                var playable = AnimationClipPlayable.Create(graph, binding.Clip);
                playable.SetApplyFootIK(false);
                playable.SetApplyPlayableIK(false);
                playable.SetSpeed(0d);
                graph.Connect(playable, 0, mixer, i);
                mixer.SetInputWeight(i, 0f);
                double contact = binding.ContactSeconds == 0 ? .1 : binding.ContactSeconds;
                if (double.IsNaN(contact) || double.IsInfinity(contact) || contact < 0)
                    throw new InvalidOperationException("A motion contact time must be finite and nonnegative.");
                slots[i] = new MotionSlot(binding.Key, binding.Clip.length, contact, playable);
                if (idleSlot < 0 && string.Equals(binding.Key, "Idle", StringComparison.Ordinal)) idleSlot = i;
            }
        }

        private void BeginMotion(ActorMotion motion)
        {
            previousSlot = currentSlot;
            previousMotion = currentMotion;
            currentSlot = FindSlot(motion.Key);
            currentMotion = ResolveMotion(motion, currentSlot);
            requestedKey = motion.Key;
            requestedStartedUs = motion.StartedUs;
            blendStartedUs = motion.StartedUs;
            blendDurationUs = BlendDuration(currentMotion);
            hasMotion = true;
            if (previousSlot == currentSlot || currentSlot < 0 || blendDurationUs <= 0) previousSlot = -1;
            for (int i = 0; i < slots.Length; i++) mixer.SetInputWeight(i, 0f);
            if (currentSlot >= 0) mixer.SetInputWeight(currentSlot, previousSlot < 0 ? 1f : 0f);
            if (previousSlot >= 0) mixer.SetInputWeight(previousSlot, 1f);
        }

        private int FindSlot(string key)
        {
            for (int i = 0; i < slots.Length; i++)
                if (slots[i].Playable.IsValid() && string.Equals(slots[i].Key, key, StringComparison.Ordinal))
                    return i;
            return idleSlot;
        }

        private ActorMotion ResolveMotion(ActorMotion motion, int slot)
        {
            return slot >= 0 && !string.Equals(slots[slot].Key, motion.Key, StringComparison.Ordinal)
                ? new ActorMotion(slots[slot].Key, motion.StartedUs, loop: true) : motion;
        }

        private double Sample(int slot, ActorMotion motion, long combatTimeUs)
        {
            double time = motion.SampleSeconds(combatTimeUs, slots[slot].Length, slots[slot].ContactSeconds);
            slots[slot].Playable.SetTime(time);
            return time;
        }

        private static long BlendDuration(ActorMotion motion)
        {
            return motion.ImpactUs > motion.StartedUs
                ? Math.Min(MaximumBlendUs, (motion.ImpactUs - motion.StartedUs) / 2)
                : MaximumBlendUs;
        }

        private float BlendWeight(long combatTimeUs)
        {
            double progress = Math.Max(0d, Math.Min(1d,
                (combatTimeUs - blendStartedUs) / (double)blendDurationUs));
            return (float)(progress * progress * (3d - 2d * progress));
        }

        private readonly struct MotionSlot
        {
            public readonly string Key;
            public readonly double Length;
            public readonly double ContactSeconds;
            public readonly AnimationClipPlayable Playable;

            public MotionSlot(string key, double length, double contactSeconds, AnimationClipPlayable playable)
            { Key = key; Length = length; ContactSeconds = contactSeconds; Playable = playable; }
        }
    }
}
