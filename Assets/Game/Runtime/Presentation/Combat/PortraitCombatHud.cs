using System;
using System.Collections.Generic;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;
using UnityEngine;
using UnityEngine.UI;

namespace Praxen.Game.Presentation.Combat
{
    public sealed class PortraitCombatHud : MonoBehaviour
    {
        internal static readonly Color Ink = new Color32(23, 27, 30, 230);
        private static readonly Color Bone = new Color32(235, 231, 216, 255);
        private static readonly Color Brass = new Color32(174, 147, 91, 255);
        private static readonly Color Teal = new Color32(78, 147, 143, 255);
        private Canvas dynamicCanvas;
        private Canvas menuCanvas;
        private RectTransform safe;
        private RectTransform menuSafe;
        private Text status, telegraph, outcome, clockStatus, offenseStatus, explorationStatus;
        private Image hpMeter, guardMeter, balanceMeter, focusMeter;
        private int viewportWidth, viewportHeight;
        private Rect viewportSafe;
        private bool preview;
        private Sprite controlSprite;
        private Texture2D controlTexture;
        private readonly List<Sprite> emblemSprites = new List<Sprite>();
        private readonly List<Texture2D> emblemTextures = new List<Texture2D>();
        public HudSettings Settings { get; private set; } = new HudSettings();
        public Rect SafeArea { get; private set; }
        public RectTransform GuardZone { get; private set; }
        public RectTransform DodgeLeftZone { get; private set; }
        public RectTransform DodgeRightZone { get; private set; }
        public Button AbilityButton { get; private set; }
        public Button PauseButton { get; private set; }
        public Button ResumeButton => Menu.ResumeButton;
        public Button RestartButton => Menu.RestartButton;
        public PortraitHudMenu Menu { get; private set; }
        public event Action LayoutChanged;
        public string StatusText => status != null ? status.text : string.Empty;
        public string TelegraphText => telegraph != null ? telegraph.text : string.Empty;
        public string OffenseStatusText => offenseStatus != null ? offenseStatus.text : string.Empty;

        public void Build(Camera camera)
        {
            if (camera == null) throw new ArgumentNullException(nameof(camera));
            if (dynamicCanvas == null)
            {
                dynamicCanvas = Canvas("Portrait combat HUD", 10);
                menuCanvas = Canvas("Portrait static menu", 20);
                safe = Rect("Combat safe area", dynamicCanvas.transform, Vector2.zero, Vector2.one);
                menuSafe = Rect("Menu safe area", menuCanvas.transform, Vector2.zero, Vector2.one);
                BuildReadout();
                BuildControls();
                Menu = new PortraitHudMenu(menuCanvas.transform, menuSafe);
            }
            foreach (var canvas in new[] { dynamicCanvas, menuCanvas })
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
            }
            RefreshLayout();
        }

        private Canvas Canvas(string name, int order)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            obj.transform.SetParent(transform, false);
            var canvas = obj.GetComponent<Canvas>();
            canvas.sortingOrder = order;
            var scaler = obj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720, 1280);
            scaler.matchWidthOrHeight = .5f;
            return canvas;
        }
        private void BuildReadout()
        {
            Label("Game title", safe, HudText.Get("title"), 29, .04f, .95f, .96f, .99f);
            status = Label("Resources and state", safe, string.Empty, 22, .04f, .87f, .96f, .94f);
            hpMeter = Meter("HP meter", .04f, .852f, .47f);
            guardMeter = Meter("Guard meter", .53f, .852f, .96f);
            telegraph = Label("Attack direction and allowed defenses", safe, string.Empty, 27, .04f, .70f, .96f, .85f);
            outcome = Label("Defense result", safe, string.Empty, 21, .04f, .655f, .96f, .70f);
            offenseStatus = Label("Enemy balance and Focus", safe, string.Empty, 21, .04f, .585f, .96f, .655f);
            balanceMeter = Meter("Balance meter", .04f, .575f, .47f);
            focusMeter = Meter("Focus meter", .53f, .575f, .96f);
            clockStatus = Label("Combat clock", safe, string.Empty, 34, .06f, .48f, .94f, .56f);
            explorationStatus = Label("Exploration status", safe, string.Empty, 24, .06f, .70f, .94f, .84f);
            explorationStatus.gameObject.SetActive(false);
        }
        private Image Meter(string name, float x, float y, float right)
        {
            var track = Rect(name, safe, new Vector2(x, y), new Vector2(right, y + .006f));
            var background = track.gameObject.AddComponent<Image>();
            background.color = Ink;
            background.raycastTarget = false;
            var fill = Rect(name + " fill", track, Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
            fill.color = name == "Focus meter" ? Teal : Brass;
            fill.raycastTarget = false;
            return fill;
        }
        private void BuildControls()
        {
            GuardZone = Control("Hold guard", safe, "guard");
            DodgeLeftZone = Control("Dodge left", safe, "left");
            DodgeRightZone = Control("Dodge right", safe, "right");
            AbilityButton = Button("Ability", safe, "ability", 0, 0, 1, 1);
            PauseButton = Button("Pause", safe, "pause", 0, 0, 1, 1);
            FitCaption(PauseButton.GetComponentInChildren<Text>());
            FitCaption(AbilityButton.GetComponentInChildren<Text>());
            BuildRoundControls();
            Glyph(GuardZone, "shield");
            Glyph(DodgeLeftZone, "left");
            Glyph(DodgeRightZone, "right");
            Glyph((RectTransform)AbilityButton.transform, "diamond");
            AbilityButton.GetComponentInChildren<Text>().rectTransform.anchorMax = new Vector2(.96f, .40f);
        }
        private void Glyph(RectTransform parent, string kind)
        {
            if (kind == "left" || kind == "right")
            {
                for (int i = 0; i < 2; i++)
                {
                    var stroke = Rect("Arrow stroke", parent, new Vector2(.35f, .57f), new Vector2(.65f, .61f));
                    stroke.localRotation = Quaternion.Euler(0, 0, (i == 0 ? 45 : -45) * (kind == "left" ? 1 : -1));
                    stroke.anchoredPosition = new Vector2(0, i == 0 ? 7 : -7);
                    var image = stroke.gameObject.AddComponent<Image>(); image.color = Brass; image.raycastTarget = false;
                }
                return;
            }
            var icon = Rect("Control emblem", parent, new Vector2(.38f, .51f), new Vector2(.62f, .77f));
            var graphic = icon.gameObject.AddComponent<Image>();
            graphic.sprite = EmblemSprite(kind == "shield");
            graphic.color = Brass;
            graphic.raycastTarget = false;
        }
        private Sprite EmblemSprite(bool shield)
        {
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            texture.name = shield ? "Procedural shield emblem" : "Procedural ability diamond";
            var pixels = new Color[64 * 64];
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float px = (x + .5f) / 64, py = (y + .5f) / 64;
                    float halfWidth = py < .36f ? py - .04f : .32f + (py - .36f) * (.1f / .56f);
                    bool inside = shield ? py >= .04f && py <= .92f && Mathf.Abs(px - .5f) <= halfWidth
                        : Mathf.Abs(px - .5f) + Mathf.Abs(py - .5f) <= .48f;
                    pixels[y * 64 + x] = inside ? Color.white : Color.clear;
                }
            texture.SetPixels(pixels); texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(.5f, .5f));
            emblemTextures.Add(texture); emblemSprites.Add(sprite);
            return sprite;
        }
        private void BuildRoundControls()
        {
            controlTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            controlTexture.name = "Procedural thumb control";
            var pixels = new Color[64 * 64];
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(32, 32));
                    pixels[y * 64 + x] = new Color(1, 1, 1, Mathf.Clamp01(32 - distance));
                }
            controlTexture.SetPixels(pixels); controlTexture.Apply();
            controlSprite = Sprite.Create(controlTexture, new Rect(0, 0, 64, 64), new Vector2(.5f, .5f));
            foreach (var zone in new[] { GuardZone, DodgeLeftZone, DodgeRightZone, (RectTransform)AbilityButton.transform, (RectTransform)PauseButton.transform })
                zone.GetComponent<Image>().sprite = controlSprite;
        }
        private void OnDestroy()
        {
            foreach (var sprite in emblemSprites) Release(sprite);
            foreach (var texture in emblemTextures) Release(texture);
            if (controlSprite != null)
            {
                if (UnityEngine.Application.isPlaying) Destroy(controlSprite); else DestroyImmediate(controlSprite);
            }
            if (controlTexture != null)
            {
                if (UnityEngine.Application.isPlaying) Destroy(controlTexture); else DestroyImmediate(controlTexture);
            }
        }
        private static void Release(UnityEngine.Object resource)
        {
            if (resource == null) return;
            if (UnityEngine.Application.isPlaying) Destroy(resource); else DestroyImmediate(resource);
        }
        public void CancelUiContacts()
        {
            foreach (var button in GetComponentsInChildren<CancellableHudButton>(true)) button.CancelPendingPress();
        }
        public void ApplySettings(HudSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            Settings = settings;
            Layout(viewportWidth, viewportHeight, viewportSafe);
        }
        public void ApplyViewport(int width, int height, Rect safeArea)
        {
            if (width <= 0 || height <= 0 || safeArea.width <= 0 || safeArea.height <= 0)
                throw new ArgumentOutOfRangeException(nameof(safeArea));
            preview = true;
            Layout(width, height, safeArea);
        }
        public void RefreshLayout()
        {
            preview = false;
            Layout(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height), Screen.safeArea);
        }
        private void Update()
        {
            if (!preview && (viewportWidth != Screen.width || viewportHeight != Screen.height || viewportSafe != Screen.safeArea)) RefreshLayout();
        }
        private void Layout(int width, int height, Rect safeArea)
        {
            if (safe == null) return;
            viewportWidth = Mathf.Max(1, width); viewportHeight = Mathf.Max(1, height); viewportSafe = safeArea;
            if (safeArea.width <= 0 || safeArea.height <= 0) safeArea = new Rect(0, 0, viewportWidth, viewportHeight);
            float portraitWidth = Mathf.Min(safeArea.width, safeArea.height / 1.4f);
            SafeArea = new Rect(safeArea.center.x - portraitWidth / 2, safeArea.y, portraitWidth, safeArea.height);
            foreach (var target in new[] { safe, menuSafe })
            {
                target.anchorMin = new Vector2(SafeArea.xMin / viewportWidth, SafeArea.yMin / viewportHeight);
                target.anchorMax = new Vector2(SafeArea.xMax / viewportWidth, SafeArea.yMax / viewportHeight);
                target.offsetMin = target.offsetMax = Vector2.zero;
            }
            var layout = PortraitHudLayout.Create(SafeArea.width, SafeArea.height, Settings);
            Place(GuardZone, layout.Guard); Place(DodgeLeftZone, layout.DodgeLeft); Place(DodgeRightZone, layout.DodgeRight);
            Place((RectTransform)AbilityButton.transform, layout.Ability); Place((RectTransform)PauseButton.transform, layout.Pause);
            Place(telegraph.rectTransform, layout.TellArea);
            Menu.ShowSettings(Settings);
            LayoutChanged?.Invoke();
        }
        private static void Place(RectTransform target, HudRect rect)
        {
            target.anchorMin = new Vector2((float)rect.X, (float)rect.Y);
            target.anchorMax = new Vector2((float)(rect.X + rect.Width), (float)(rect.Y + rect.Height));
            target.offsetMin = target.offsetMax = Vector2.zero;
        }
        public void Show(DefenseCombatant combatant, string tell, string result, CombatClock clock)
        {
            if (combatant == null) throw new ArgumentNullException(nameof(combatant));
            if (clock == null) throw new ArgumentNullException(nameof(clock));
            status.text = HudText.Format("resources", combatant.Health, combatant.Guard, combatant.DodgeCharges, combatant.Tuning.MaximumDodgeCharges)
                + "\n" + HudText.Format("state", HudText.Get("state." + combatant.State), combatant.TimeUs / 1000000d);
            telegraph.text = tell ?? string.Empty; outcome.text = result ?? string.Empty;
            Fill(hpMeter, combatant.Health, combatant.Tuning.MaximumHealth);
            Fill(guardMeter, combatant.Guard, combatant.Tuning.MaximumGuard);
            GuardZone.GetComponent<Image>().color = combatant.GuardHeld ? Teal : Ink;
            bool dodgeAvailable = combatant.DodgeCharges > 0 && clock.State == CombatClockState.Running &&
                (combatant.State == PlayerCombatState.Ready || combatant.State == PlayerCombatState.Guarding);
            var dodgeColor = dodgeAvailable ? Ink : (Color)new Color32(45, 45, 45, 230);
            DodgeLeftZone.GetComponent<Image>().color = dodgeColor; DodgeRightZone.GetComponent<Image>().color = dodgeColor;
            clockStatus.text = clock.State == CombatClockState.Countdown
                ? HudText.Format("countdown", Mathf.CeilToInt(clock.CountdownRemainingUs / 1000000f))
                : clock.State == CombatClockState.Suspended ? HudText.Format("suspended", HudText.Get("reason." + clock.SuspensionReason)) : string.Empty;
        }
        public void ShowFeedback(string key, params object[] args) => outcome.text = HudText.Format(key, args);
        public void ShowOffense(OffenseCombatant offense, CombatMomentum momentum, InteractionPhase phase, long openingRemainingUs)
        {
            if (offense == null) throw new ArgumentNullException(nameof(offense));
            if (momentum == null) throw new ArgumentNullException(nameof(momentum));
            string phaseText = phase.Kind == InteractionPhaseKind.PlayerOpening ? HudText.Format("open", openingRemainingUs / 1000000d)
                : HudText.Get(phase.Kind == InteractionPhaseKind.Inactive ? "ended" : "defend");
            offenseStatus.text = HudText.Format("offense", offense.EnemyHealth, momentum.Balance, momentum.Focus,
                offense.Tuning.FocusMaximum, phaseText, offense.HasBuffer ? HudText.Get("buffered") : string.Empty);
            Fill(balanceMeter, momentum.Balance, offense.Tuning.BalanceMaximum); Fill(focusMeter, momentum.Focus, offense.Tuning.FocusMaximum);
        }
        public void SetExploration(bool visible)
        {
            foreach (var control in new[] { GuardZone, DodgeLeftZone, DodgeRightZone, (RectTransform)AbilityButton.transform })
                control.gameObject.SetActive(!visible);
            foreach (var label in new[] { status, telegraph, outcome, clockStatus, offenseStatus }) label.gameObject.SetActive(!visible);
            foreach (var meter in new[] { hpMeter, guardMeter, balanceMeter, focusMeter }) meter.transform.parent.gameObject.SetActive(!visible);
            explorationStatus.gameObject.SetActive(visible);
            if (visible) explorationStatus.text = HudText.Get("exploration.detail");
        }
        public void ShowExplorationStatus(string key, params object[] args) => explorationStatus.text = HudText.Format(key, args);
        private static void Fill(Image image, int value, int maximum)
        {
            image.rectTransform.anchorMax = new Vector2(Mathf.Clamp01((float)value / maximum), 1);
        }
        internal static Text Label(string name, Transform parent, string text, int size, float x, float y, float right, float top)
        {
            var label = Rect(name, parent, new Vector2(x, y), new Vector2(right, top)).gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize = size;
            label.alignment = TextAnchor.MiddleCenter; label.color = Bone; label.raycastTarget = false; label.text = text;
            return label;
        }
        private static RectTransform Control(string name, Transform parent, string key)
        {
            var rect = Rect(name, parent, Vector2.zero, Vector2.one);
            var image = rect.gameObject.AddComponent<Image>(); image.color = Ink; image.raycastTarget = true;
            FitCaption(Label(name + " caption", rect, HudText.Get(key), 20, .04f, .06f, .96f, .40f));
            return rect;
        }
        private static void FitCaption(Text caption)
        {
            caption.horizontalOverflow = HorizontalWrapMode.Overflow;
            caption.verticalOverflow = VerticalWrapMode.Truncate;
            caption.resizeTextForBestFit = true;
            caption.resizeTextMinSize = 12;
            caption.resizeTextMaxSize = caption.fontSize;
        }
        internal static Button Button(string name, Transform parent, string key, float x, float y, float right, float top)
        {
            var rect = Rect(name, parent, new Vector2(x, y), new Vector2(right, top));
            var image = rect.gameObject.AddComponent<Image>(); image.color = Ink;
            Label(name + " caption", rect, HudText.Get(key), 22, .03f, .08f, .97f, .92f);
            var button = rect.gameObject.AddComponent<CancellableHudButton>(); button.targetGraphic = image;
            return button;
        }
        internal static RectTransform Rect(string name, Transform parent, Vector2 minimum, Vector2 maximum)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = minimum; rect.anchorMax = maximum;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }
    }
}
