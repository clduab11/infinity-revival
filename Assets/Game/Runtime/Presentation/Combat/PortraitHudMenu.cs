using Praxen.Game.Domain.Input;
using UnityEngine;
using UnityEngine.UI;

namespace Praxen.Game.Presentation.Combat
{
    public sealed class PortraitHudMenu
    {
        private readonly RectTransform overlay;
        private readonly GameObject actions;
        private readonly Text instruction;
        private readonly Text detail;
        private readonly Text hand;
        private readonly Text scale;
        public Button HandButton { get; }
        public Button ScaleDownButton { get; }
        public Button ScaleUpButton { get; }
        public Button CalibrateButton { get; }
        public Button ExploreButton { get; }
        public Button CancelButton { get; }
        public Button ApplyButton { get; }
        public Button ResumeButton { get; }
        public Button RestartButton { get; }
        public RectTransform CalibrationSurface { get; }
        public bool CalibrationVisible { get; private set; }
        public bool IsVisible => overlay.gameObject.activeSelf;

        internal PortraitHudMenu(Transform canvas, RectTransform safe)
        {
            overlay = PortraitCombatHud.Rect("Pause menu overlay", canvas, Vector2.zero, Vector2.one);
            var backdrop = overlay.gameObject.AddComponent<Image>();
            backdrop.color = new Color32(15, 20, 23, 245);
            backdrop.raycastTarget = true;
            CalibrationSurface = PortraitCombatHud.Rect("Reach calibration surface", overlay, Vector2.zero, Vector2.one);
            var surface = CalibrationSurface.gameObject.AddComponent<Image>();
            surface.color = Color.clear;
            surface.raycastTarget = true;
            // The safe content follows the menu canvas, above the full-screen contact surfaces.
            safe.SetParent(overlay, false);
            instruction = PortraitCombatHud.Label("Menu instruction", safe, HudText.Get("menu"), 32, .08f, .82f, .92f, .9f);
            detail = PortraitCombatHud.Label("Menu detail", safe, string.Empty, 21, .08f, .73f, .92f, .81f);
            actions = PortraitCombatHud.Rect("Menu actions", safe, Vector2.zero, Vector2.one).gameObject;
            ResumeButton = GridAction("Resume", "resume", 1, .345f);
            RestartButton = GridAction("Restart", "restart", 0, .345f);
            HandButton = GridAction("Handedness", "hand.right", 1, .26f);
            hand = HandButton.GetComponentInChildren<Text>();
            CalibrateButton = GridAction("Calibrate reach", "calibrate", 0, .26f);
            ScaleDownButton = GridAction("Scale down", "scale.down", 0, .175f);
            ScaleUpButton = GridAction("Scale up", "scale.up", 1, .175f);
            ExploreButton = GridAction("Explore foundry", "explore", 1, .09f);
            scale = PortraitCombatHud.Label("Control scale", actions.transform, string.Empty, 18, .1f, .425f, .94f, .46f);
            CancelButton = PortraitCombatHud.Button("Cancel calibration", safe, "cancel", .08f, .05f, .47f, .12f);
            ApplyButton = PortraitCombatHud.Button("Apply calibration", safe, "apply", .53f, .05f, .92f, .12f);
            Show(false);
        }

        private Button GridAction(string name, string key, int column, float y)
        {
            float x = column == 0 ? .10f : .54f;
            return PortraitCombatHud.Button(name, actions.transform, key, x, y, x + .4f, y + .075f);
        }
        public void Show(bool visible)
        {
            CalibrationVisible = false;
            CalibrationSurface.gameObject.SetActive(false);
            actions.SetActive(true);
            CancelButton.gameObject.SetActive(false);
            ApplyButton.gameObject.SetActive(false);
            instruction.text = HudText.Get("menu");
            detail.text = string.Empty;
            overlay.gameObject.SetActive(visible);
        }
        public void ShowCalibration(int count, bool complete)
        {
            overlay.gameObject.SetActive(true);
            CalibrationVisible = true;
            actions.SetActive(false);
            CalibrationSurface.gameObject.SetActive(true);
            CancelButton.gameObject.SetActive(true);
            ApplyButton.gameObject.SetActive(true);
            ApplyButton.interactable = complete;
            instruction.text = complete ? HudText.Get("calibration.complete") : HudText.Format("calibration", count);
            detail.text = string.Empty;
        }
        public void ShowStatus(string key, params object[] args) => detail.text = HudText.Format(key, args);
        internal void ShowSettings(HudSettings settings)
        {
            hand.text = HudText.Get(settings.Hand == HudHand.Right ? "hand.right" : "hand.left");
            scale.text = HudText.Format("scale", settings.Scale * 100);
            SetColumn(ResumeButton, 1, settings.Hand); SetColumn(RestartButton, 0, settings.Hand);
            SetColumn(HandButton, 1, settings.Hand); SetColumn(CalibrateButton, 0, settings.Hand);
            SetColumn(ScaleDownButton, 0, settings.Hand); SetColumn(ScaleUpButton, 1, settings.Hand);
            SetColumn(ExploreButton, 1, settings.Hand);
            SetCalibrationColumn(CancelButton, .08f, .47f, settings.Hand);
            SetCalibrationColumn(ApplyButton, .53f, .92f, settings.Hand);
        }
        private static void SetCalibrationColumn(Button button, float min, float max, HudHand hand)
        {
            var rect = (RectTransform)button.transform;
            var a = rect.anchorMin; var b = rect.anchorMax;
            a.x = hand == HudHand.Right ? min : 1 - max;
            b.x = hand == HudHand.Right ? max : 1 - min;
            rect.anchorMin = a; rect.anchorMax = b;
        }
        private static void SetColumn(Button button, int column, HudHand hand)
        {
            float min = column == 0 ? .10f : .54f;
            float max = min + .4f;
            var rect = (RectTransform)button.transform;
            var a = rect.anchorMin; var b = rect.anchorMax;
            a.x = hand == HudHand.Right ? min : 1 - max;
            b.x = hand == HudHand.Right ? max : 1 - min;
            rect.anchorMin = a; rect.anchorMax = b;
        }
    }
}
