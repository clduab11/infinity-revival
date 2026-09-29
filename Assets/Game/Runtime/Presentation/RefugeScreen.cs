using System;
using Praxen.Game.Application;
using UnityEngine;
using UnityEngine.UI;

namespace Praxen.Game.Presentation
{
    public sealed class RefugeScreen : MonoBehaviour
    {
        [SerializeField] private Camera sceneCamera;
        private Canvas canvas;
        private RectTransform safeArea;
        private Text title;

        public bool IsVisible => canvas != null && canvas.gameObject.activeInHierarchy;
        public Canvas UiCanvas => canvas;

        public void SetCamera(Camera camera)
        {
            if (camera == null) throw new ArgumentNullException(nameof(camera));
            sceneCamera = camera;
            if (canvas != null) canvas.worldCamera = camera;
        }

        public void Show(ApplicationState state)
        {
            if (state != ApplicationState.Failed && sceneCamera == null)
                throw new InvalidOperationException("Refuge camera is unbound.");
            EnsureCanvas();
            canvas.renderMode = sceneCamera != null
                ? RenderMode.ScreenSpaceCamera : RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = sceneCamera;
            title.text = state == ApplicationState.Failed ? "Unable to start" : "REFUGE";
            canvas.gameObject.SetActive(true);
            ApplySafeArea();
        }

        public void Hide()
        {
            if (canvas != null) canvas.gameObject.SetActive(false);
        }

        private void EnsureCanvas()
        {
            if (canvas != null) return;
            var canvasObject = new GameObject("RefugeCanvas", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.planeDistance = 1f;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720, 1280);
            scaler.matchWidthOrHeight = 0.5f;
            var background = CreateRect("Background", canvasObject.transform);
            Stretch(background);
            var backgroundImage = background.gameObject.AddComponent<Image>();
            backgroundImage.color = new Color32(19, 27, 33, 255);
            backgroundImage.raycastTarget = false;
            safeArea = CreateRect("SafeArea", canvasObject.transform);
            Stretch(safeArea);
            CreateTitle();
        }

        private void CreateTitle()
        {
            var heading = CreateRect("RefugeTitle", safeArea);
            heading.anchorMin = new Vector2(0, 0.52f);
            heading.anchorMax = new Vector2(1, 0.68f);
            heading.offsetMin = new Vector2(36, 0);
            heading.offsetMax = new Vector2(-36, 0);
            title = heading.gameObject.AddComponent<Text>();
            title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            title.fontSize = 46;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color32(219, 222, 207, 255);
            title.raycastTarget = false;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void LateUpdate() => ApplySafeArea();

        private void ApplySafeArea()
        {
            if (safeArea == null || Screen.width <= 0 || Screen.height <= 0) return;
            var area = Screen.safeArea;
            safeArea.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            safeArea.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            safeArea.offsetMin = Vector2.zero;
            safeArea.offsetMax = Vector2.zero;
        }
    }
}
