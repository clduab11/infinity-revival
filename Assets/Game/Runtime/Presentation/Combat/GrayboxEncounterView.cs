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
        private static readonly Color Ink = new Color32(23, 27, 30, 255);
        private static readonly Color Bone = new Color32(226, 229, 225, 255);
        private readonly List<Material> ownedMaterials = new List<Material>();
        private Canvas canvas;
        private Font font;
        private Text status;
        private Text telegraph;
        private Text outcome;
        private Text clockStatus;
        private Text offenseStatus;
        private Transform playerSword;
        private Transform shield;
        private Image guardImage;
        private Image dodgeLeftImage;
        private Image dodgeRightImage;

        public RectTransform GuardZone { get; private set; }
        public RectTransform DodgeLeftZone { get; private set; }
        public RectTransform DodgeRightZone { get; private set; }
        public Button ResumeButton { get; private set; }
        public Button RestartButton { get; private set; }
        public Transform PlayerAnchor { get; private set; }
        public Transform EnemyAnchor { get; private set; }
        public string StatusText => status != null ? status.text : string.Empty;
        public string TelegraphText => telegraph != null ? telegraph.text : string.Empty;
        public string OffenseStatusText => offenseStatus != null ? offenseStatus.text : string.Empty;

        public void Build(Camera camera)
        {
            if (camera == null) throw new ArgumentNullException(nameof(camera));
            if (canvas == null)
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                BuildStage();
                BuildCanvas();
            }
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
        }

        public void Show(DefenseCombatant combatant, string tell, string outcome, CombatClock clock)
        {
            if (combatant == null) throw new ArgumentNullException(nameof(combatant));
            if (clock == null) throw new ArgumentNullException(nameof(clock));
            if (canvas == null) throw new InvalidOperationException("Build the encounter view first.");
            status.text = $"HP {combatant.Health}   GUARD {combatant.Guard}   " +
                $"DODGE {combatant.DodgeCharges}/{combatant.Tuning.MaximumDodgeCharges}\n" +
                $"{combatant.State.ToString().ToUpperInvariant()}   {combatant.TimeUs / 1000000d:0.00}s";
            telegraph.text = tell ?? string.Empty;
            this.outcome.text = outcome ?? string.Empty;
            ShowClock(clock);
            ShowPlayerPose(combatant);
            ShowControlState(combatant);
        }

        public void ShowOffense(OffenseCombatant offense, CombatMomentum momentum,
            InteractionPhase phase, long openingRemainingUs)
        {
            if (offense == null) throw new ArgumentNullException(nameof(offense));
            if (momentum == null) throw new ArgumentNullException(nameof(momentum));
            offenseStatus.text = $"ENEMY HP {offense.EnemyHealth}   BALANCE {momentum.Balance}\n" +
                $"FOCUS {momentum.Focus}/{offense.Tuning.FocusMaximum}   " +
                (phase.Kind == InteractionPhaseKind.PlayerOpening ? $"OPEN {openingRemainingUs / 1000000d:0.00}s" :
                    phase.Kind == InteractionPhaseKind.Inactive ? "ENCOUNTER ENDED" : "DEFEND") +
                (offense.HasBuffer ? "\nATTACK BUFFERED" : string.Empty);
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

        private void BuildCanvas()
        {
            var ui = new GameObject("Graybox encounter canvas", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            ui.transform.SetParent(transform, false);
            canvas = ui.GetComponent<Canvas>();
            var scaler = ui.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720, 1280);
            scaler.matchWidthOrHeight = 0.5f;
            Label("Game title", ui.transform, "FOREVER WE REIGN", 34,
                new Vector2(0.025f, 0.90f), new Vector2(0.975f, 0.96f));
            status = Label("Resources and state", ui.transform, string.Empty, 22,
                new Vector2(0.025f, 0.825f), new Vector2(0.975f, 0.90f));
            clockStatus = Label("Combat clock", ui.transform, string.Empty, 18,
                new Vector2(0.025f, 0.795f), new Vector2(0.975f, 0.825f));
            telegraph = Label("Attack direction and allowed defenses", ui.transform, string.Empty, 24,
                new Vector2(0.025f, 0.705f), new Vector2(0.975f, 0.795f));
            outcome = Label("Defense result", ui.transform, string.Empty, 20,
                new Vector2(0.025f, 0.665f), new Vector2(0.975f, 0.705f));
            offenseStatus = Label("Enemy balance and Focus", ui.transform, string.Empty, 22,
                new Vector2(0.025f, 0.585f), new Vector2(0.975f, 0.665f));
            BuildControls(ui.transform);
        }

        private void BuildControls(Transform parent)
        {
            Label("Test control caption", parent, "Temporary test controls", 18,
                new Vector2(0.025f, 0.245f), new Vector2(0.975f, 0.28f));
            DodgeLeftZone = Control("Dodge left", parent, "DODGE\nLEFT",
                new Vector2(0.025f, 0.065f), new Vector2(0.315f, 0.235f), out dodgeLeftImage);
            GuardZone = Control("Hold guard", parent, "HOLD\nGUARD",
                new Vector2(0.355f, 0.065f), new Vector2(0.645f, 0.235f), out guardImage);
            DodgeRightZone = Control("Dodge right", parent, "DODGE\nRIGHT",
                new Vector2(0.685f, 0.065f), new Vector2(0.975f, 0.235f), out dodgeRightImage);
            ResumeButton = ActionButton("Resume", parent,
                new Vector2(0.025f, 0.012f), new Vector2(0.315f, 0.05f));
            RestartButton = ActionButton("Restart", parent,
                new Vector2(0.685f, 0.012f), new Vector2(0.975f, 0.05f));
            ResumeButton.gameObject.SetActive(false);
        }

        private void ShowClock(CombatClock clock)
        {
            bool suspended = clock.State == CombatClockState.Suspended;
            ResumeButton.gameObject.SetActive(suspended);
            clockStatus.text = clock.State == CombatClockState.Countdown
                ? $"RESUMING IN {Mathf.CeilToInt(clock.CountdownRemainingUs / 1000000f)}"
                : suspended ? $"SUSPENDED: {clock.SuspensionReason}" : string.Empty;
        }

        private void ShowPlayerPose(DefenseCombatant combatant)
        {
            var position = PlayerOrigin;
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

        private void ShowControlState(DefenseCombatant combatant)
        {
            guardImage.color = combatant.GuardHeld ? new Color32(73, 91, 102, 255) : Ink;
            Color dodgeColor = combatant.DodgeCharges > 0 ? Ink : (Color)new Color32(54, 54, 54, 255);
            dodgeLeftImage.color = dodgeColor;
            dodgeRightImage.color = dodgeColor;
        }

        private Text Label(string name, Transform parent, string value, int size,
            Vector2 minimum, Vector2 maximum)
        {
            var rect = Rect(name, parent, minimum, maximum);
            var label = rect.gameObject.AddComponent<Text>();
            label.font = font;
            label.fontSize = size;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Bone;
            label.raycastTarget = false;
            label.text = value;
            return label;
        }

        private RectTransform Control(string name, Transform parent, string caption,
            Vector2 minimum, Vector2 maximum, out Image image)
        {
            var rect = Rect(name, parent, minimum, maximum);
            image = rect.gameObject.AddComponent<Image>();
            image.color = Ink;
            image.raycastTarget = true;
            Label(name + " caption", rect, caption, 24, Vector2.zero, Vector2.one);
            return rect;
        }

        private Button ActionButton(string caption, Transform parent, Vector2 minimum, Vector2 maximum)
        {
            var rect = Control(caption, parent, caption.ToUpperInvariant(), minimum, maximum,
                out var image);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 minimum, Vector2 maximum)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = minimum;
            rect.anchorMax = maximum;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
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
