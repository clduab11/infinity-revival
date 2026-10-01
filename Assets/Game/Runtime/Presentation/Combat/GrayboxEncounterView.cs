using System;
using System.Collections.Generic;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;
using UnityEngine;
using UnityEngine.UI;

namespace Praxen.Game.Presentation.Combat
{
    public sealed class GrayboxEncounterView : MonoBehaviour
    {
        private const float DodgeDisplacement = 0.65f;
        private static readonly Vector3 PlayerOrigin = new Vector3(-0.8f, 1f, 0f);
        private readonly List<Material> ownedMaterials = new List<Material>();
        private Transform playerSword;
        private Transform shield;
        private bool prototypeVisuals;
        public PortraitCombatHud Hud { get; private set; }
        public RectTransform GuardZone => Hud.GuardZone;
        public RectTransform DodgeLeftZone => Hud.DodgeLeftZone;
        public RectTransform DodgeRightZone => Hud.DodgeRightZone;
        public Button ResumeButton => Hud.ResumeButton;
        public Button RestartButton => Hud.RestartButton;
        public Transform PlayerAnchor { get; private set; }
        public Transform EnemyAnchor { get; private set; }
        public string StatusText => Hud != null ? Hud.StatusText : string.Empty;
        public string TelegraphText => Hud != null ? Hud.TelegraphText : string.Empty;
        public string OffenseStatusText => Hud != null ? Hud.OffenseStatusText : string.Empty;

        public void SetPrototypeVisuals(bool visible)
        {
            prototypeVisuals = visible;
            foreach (var renderer in PlayerAnchor.GetComponentsInChildren<MeshRenderer>()) renderer.enabled = !visible;
            foreach (var renderer in EnemyAnchor.GetComponentsInChildren<MeshRenderer>()) renderer.enabled = !visible;
            EnemyAnchor.localPosition = visible ? new Vector3(.5f, 1, 1.8f) : new Vector3(.6f, 1, 3);
        }

        public void Build(Camera camera)
        {
            if (camera == null) throw new ArgumentNullException(nameof(camera));
            if (Hud == null)
            {
                BuildStage();
                Hud = gameObject.AddComponent<PortraitCombatHud>();
            }
            Hud.Build(camera);
        }

        public void Show(DefenseCombatant combatant, string tell, string outcome, CombatClock clock)
        {
            if (Hud == null) throw new InvalidOperationException("Build the encounter view first.");
            Hud.Show(combatant, tell, outcome, clock);
            ShowPlayerPose(combatant);
        }

        public void ShowOffense(OffenseCombatant offense, CombatMomentum momentum,
            InteractionPhase phase, long openingRemainingUs)
        {
            if (offense == null) throw new ArgumentNullException(nameof(offense));
            if (momentum == null) throw new ArgumentNullException(nameof(momentum));
            Hud.ShowOffense(offense, momentum, phase, openingRemainingUs);
            float angle = -12f;
            if (offense.ActiveAttack.HasValue)
            {
                var attack = offense.ActiveAttack.Value;
                float progress = Mathf.Clamp01((float)((offense.TimeUs - attack.StartedUs) /
                    (double)(attack.RecoveryEndUs - attack.StartedUs)));
                float direction = attack.Direction == SwipeDirection.Left || attack.Direction == SwipeDirection.Down ? -1 : 1;
                angle += direction * 80f * Mathf.Sin(progress * Mathf.PI);
            }
            playerSword.localRotation = Quaternion.Euler(0, 0, angle);
        }

        private void BuildStage()
        {
            var stone = CreateMaterial("Graybox stone", new Color32(64, 69, 72, 255));
            var steel = CreateMaterial("Graybox steel", new Color32(151, 160, 165, 255));
            var darkSteel = CreateMaterial("Graybox dark steel", new Color32(37, 43, 47, 255));
            var player = CreateMaterial("Graybox player", new Color32(89, 124, 144, 255));
            var enemy = CreateMaterial("Graybox opponent", new Color32(145, 103, 82, 255));
            Primitive("Test floor", PrimitiveType.Cube, transform,
                new Vector3(0, -0.1f, 1.5f), new Vector3(5.5f, 0.2f, 8f), stone);
            BuildArchitecture(stone, darkSteel);
            PlayerAnchor = BuildCombatant("Player anchor", PlayerOrigin, player);
            EnemyAnchor = BuildCombatant("Opponent anchor", new Vector3(0.6f, 1f, 3f), enemy);
            BuildSword(PlayerAnchor, new Vector3(0.5f, 0.05f, 0.08f), steel, darkSteel);
            BuildSword(EnemyAnchor, new Vector3(-0.5f, 0.05f, -0.08f), steel, darkSteel);
            shield = Primitive("Player shield", PrimitiveType.Cube, PlayerAnchor,
                new Vector3(-0.42f, 0, 0.08f), new Vector3(0.12f, 0.75f, 0.6f), darkSteel);
            Primitive("Shield face", PrimitiveType.Cube, shield,
                new Vector3(-0.56f, 0, 0), new Vector3(0.15f, 0.8f, 0.8f), steel);
        }

        private void BuildArchitecture(Material stone, Material steel)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Primitive("Foundry pillar", PrimitiveType.Cube, transform,
                    new Vector3(side * 2.25f, 1.7f, 4.8f), new Vector3(0.4f, 3.4f, 0.5f), stone);
                Primitive("Floor rail", PrimitiveType.Cube, transform,
                    new Vector3(side * 2.5f, 0.15f, 1.5f), new Vector3(0.14f, 0.3f, 8f), steel);
            }
            Primitive("Foundry lintel", PrimitiveType.Cube, transform,
                new Vector3(0, 3.35f, 4.8f), new Vector3(4.9f, 0.25f, 0.5f), steel);
        }

        private Transform BuildCombatant(string name, Vector3 position, Material material)
        {
            var anchor = new GameObject(name).transform;
            anchor.SetParent(transform, false);
            anchor.localPosition = position;
            Primitive("Combatant capsule", PrimitiveType.Capsule, anchor,
                Vector3.zero, new Vector3(0.65f, 1f, 0.65f), material);
            return anchor;
        }

        private void BuildSword(Transform anchor, Vector3 position, Material blade, Material grip)
        {
            var sword = new GameObject("Test sword").transform;
            sword.SetParent(anchor, false);
            sword.localPosition = position;
            sword.localRotation = Quaternion.Euler(0, 0, anchor == PlayerAnchor ? -12f : 12f);
            if (anchor == PlayerAnchor) playerSword = sword;
            Primitive("Sword blade", PrimitiveType.Cube, sword,
                new Vector3(0, 0.25f, 0), new Vector3(0.09f, 1f, 0.045f), blade);
            Primitive("Sword guard", PrimitiveType.Cube, sword,
                new Vector3(0, -0.28f, 0), new Vector3(0.32f, 0.08f, 0.08f), blade);
            Primitive("Sword grip", PrimitiveType.Cube, sword,
                new Vector3(0, -0.43f, 0), new Vector3(0.09f, 0.25f, 0.08f), grip);
        }

        private void ShowPlayerPose(DefenseCombatant combatant)
        {
            var position = prototypeVisuals ? new Vector3(-.5f, 1, .2f) : PlayerOrigin;
            if (combatant.DodgeSide != DodgeSide.None)
            {
                float progress = Mathf.Clamp01((float)(
                    (combatant.TimeUs - combatant.DodgeStartedUs) / (double)combatant.Tuning.DodgeDurationUs));
                float direction = combatant.DodgeSide == DodgeSide.Left ? -1f :
                    combatant.DodgeSide == DodgeSide.Right ? 1f : 0f;
                if (progress < 1f)
                    position.x += direction * DodgeDisplacement * Mathf.Sin(Mathf.PI * progress);
            }
            PlayerAnchor.localPosition = position;
            shield.localPosition = new Vector3(-0.42f, combatant.GuardHeld ? 0.2f : 0f, 0.08f);
            shield.localRotation = Quaternion.Euler(0, combatant.GuardHeld ? -25f : 0f, 0);
        }

        private Material CreateMaterial(string name, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ??
                Shader.Find("Unlit/Color");
            if (shader == null) throw new InvalidOperationException("A graybox material shader is required.");
            var material = new Material(shader) { name = name };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.15f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.25f);
            ownedMaterials.Add(material);
            return material;
        }

        private static Transform Primitive(string name, PrimitiveType type, Transform parent,
            Vector3 position, Vector3 scale, Material material)
        {
            var primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = position;
            primitive.transform.localScale = scale;
            primitive.GetComponent<Renderer>().sharedMaterial = material;
            var collider = primitive.GetComponent<Collider>();
            collider.enabled = false;
            if (UnityEngine.Application.isPlaying) Destroy(collider);
            else DestroyImmediate(collider);
            return primitive.transform;
        }

        private void OnDestroy()
        {
            foreach (var material in ownedMaterials)
            {
                if (material == null) continue;
                if (UnityEngine.Application.isPlaying) Destroy(material);
                else DestroyImmediate(material);
            }
            ownedMaterials.Clear();
        }
    }
}
