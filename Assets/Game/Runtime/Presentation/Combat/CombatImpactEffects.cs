using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Praxen.Game.Presentation.Combat
{
    /// <summary>Bounded, combat-time-only impact rings and weapon trails.</summary>
    public sealed class CombatImpactEffects : IDisposable
    {
        private const int Capacity = 8;
        private readonly LineRenderer[] impacts = new LineRenderer[Capacity];
        private readonly long[] impactStarts = new long[Capacity];
        private readonly bool[] active = new bool[Capacity];
        private readonly LineRenderer[] trails = new LineRenderer[2];
        private readonly Vector3[,] points = new Vector3[2, 12];
        private readonly int[] pointCounts = new int[2];
        private readonly long[] trailTimes = { -1, -1 };
        private readonly long[] trailActions = { -1, -1 };
        private readonly Material material;
        private readonly GameObject owner;
        private int cursor;
        public int ImpactCount { get; private set; }
        public int ActiveImpactCount { get; private set; }
        public long RenderedTimeUs { get; private set; }

        public CombatImpactEffects(Transform parent)
        {
            owner = new GameObject("Combat feedback effects");
            owner.transform.SetParent(parent, false);
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            material = new Material(shader) { name = "Owned prototype feedback" };
            material.SetColor(material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color", new Color(1, .8f, .32f));
            for (int i = 0; i < Capacity; i++) impacts[i] = Line("Impact " + i, .025f, true);
            for (int i = 0; i < 2; i++) trails[i] = Line("Weapon trail " + i, .018f, false);
        }

        private LineRenderer Line(string name, float width, bool loop)
        {
            var child = new GameObject(name, typeof(LineRenderer));
            child.transform.SetParent(owner.transform, false);
            var line = child.GetComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.widthMultiplier = width;
            line.loop = loop;
            line.enabled = false;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        public void Emit(CombatPresentationCue cue, Vector3 position)
        {
            if (cue.Kind != CombatCueKind.Hit && cue.Kind != CombatCueKind.Block &&
                cue.Kind != CombatCueKind.Parry && cue.Kind != CombatCueKind.Death) return;
            int slot = cursor++ % Capacity;
            active[slot] = true;
            impactStarts[slot] = cue.TimeUs;
            impacts[slot].transform.position = position;
            ImpactCount++;
        }

        public void Render(long timeUs, Transform playerTip, Transform enemyTip, ActorMotion player, ActorMotion enemy)
        {
            RenderedTimeUs = timeUs;
            ActiveImpactCount = 0;
            for (int i = 0; i < Capacity; i++)
            {
                long elapsed = timeUs - impactStarts[i];
                if (active[i] && elapsed >= 180000) active[i] = false;
                impacts[i].enabled = active[i];
                if (!active[i]) continue;
                ActiveImpactCount++;
                float radius = .035f + .2f * Mathf.Clamp01(elapsed / 180000f);
                impacts[i].positionCount = 8;
                for (int point = 0; point < 8; point++)
                {
                    float angle = point * Mathf.PI / 4;
                    impacts[i].SetPosition(point, impacts[i].transform.position +
                        new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius);
                }
            }
            Trail(0, playerTip, player, timeUs);
            Trail(1, enemyTip, enemy, timeUs);
        }

        private void Trail(int slot, Transform tip, ActorMotion motion, long time)
        {
            bool moving = tip != null && (motion.Key.StartsWith("Cut", StringComparison.Ordinal) || motion.Key == "EnemyAttack");
            if (!moving) { trails[slot].enabled = false; pointCounts[slot] = 0; return; }
            if (trailActions[slot] != motion.StartedUs) { pointCounts[slot] = 0; trailActions[slot] = motion.StartedUs; }
            if (trailTimes[slot] != time)
            {
                int count = Math.Min(11, pointCounts[slot]);
                for (int i = count; i > 0; i--) points[slot, i] = points[slot, i - 1];
                points[slot, 0] = tip.position;
                pointCounts[slot] = count + 1;
                trailTimes[slot] = time;
            }
            trails[slot].positionCount = pointCounts[slot];
            trails[slot].enabled = pointCounts[slot] > 1;
            for (int i = 0; i < pointCounts[slot]; i++) trails[slot].SetPosition(i, points[slot, i]);
        }

        public void Clear()
        {
            for (int i = 0; i < Capacity; i++) { active[i] = false; if (impacts[i] != null) impacts[i].enabled = false; }
            for (int i = 0; i < 2; i++) { pointCounts[i] = 0; if (trails[i] != null) trails[i].enabled = false; trailTimes[i] = -1; }
            ActiveImpactCount = 0;
        }

        public void Dispose()
        {
            if (UnityEngine.Application.isPlaying) { Object.Destroy(owner); Object.Destroy(material); }
            else { Object.DestroyImmediate(owner); Object.DestroyImmediate(material); }
        }
    }
}
