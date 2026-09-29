using Praxen.Game.Domain.Input;
using UnityEngine;
using UnityEngine.UI;

namespace Praxen.Game.Presentation.Input
{
    public sealed class GestureTraceView : MonoBehaviour
    {
        private RectTransform surface;
        private RectTransform shaft;
        private RectTransform firstHead;
        private RectTransform secondHead;
        private double expiresAt;
        private GestureCommand lastCommand;

        public bool IsVisible => shaft != null && shaft.gameObject.activeSelf;

        public void Configure(Canvas canvas)
        {
            transform.SetParent(canvas.transform, false);
            surface = (RectTransform)transform;
            surface.anchorMin = Vector2.zero;
            surface.anchorMax = Vector2.one;
            surface.offsetMin = surface.offsetMax = Vector2.zero;
            if (shaft == null)
            {
                shaft = CreateLine("SwipeTrace");
                firstHead = CreateLine("ArrowHeadA");
                secondHead = CreateLine("ArrowHeadB");
            }
            Clear();
        }

        public void Show(GestureCommand command)
        {
            lastCommand = command;
            Canvas.ForceUpdateCanvases();
            RefreshGeometry();
            expiresAt = Time.unscaledTimeAsDouble + 0.18;
        }

        private void OnRectTransformDimensionsChange()
        {
            if (surface != null && IsVisible) RefreshGeometry();
        }

        private void RefreshGeometry()
        {
            var start = Position(lastCommand.Start);
            var end = Position(lastCommand.End);
            var direction = Cardinal(lastCommand.Direction);
            DrawLine(shaft, start, end);
            DrawLine(firstHead, end, end - direction * 12 + new Vector2(-direction.y, direction.x) * 9);
            DrawLine(secondHead, end, end - direction * 12 - new Vector2(-direction.y, direction.x) * 9);
        }

        public void Clear()
        {
            if (shaft == null) return;
            shaft.gameObject.SetActive(false);
            firstHead.gameObject.SetActive(false);
            secondHead.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (IsVisible && Time.unscaledTimeAsDouble >= expiresAt) Clear();
        }

        private Vector2 Position(NormalizedPoint point) => new Vector2(
            (float)((point.X - 0.5) * surface.rect.width),
            (float)((point.Y - 0.5) * surface.rect.height));

        private RectTransform CreateLine(string name)
        {
            var line = new GameObject(name, typeof(RectTransform), typeof(Image));
            line.transform.SetParent(transform, false);
            var image = line.GetComponent<Image>();
            image.color = new Color32(81, 216, 178, 255);
            image.raycastTarget = false;
            return line.GetComponent<RectTransform>();
        }

        private static void DrawLine(RectTransform line, Vector2 start, Vector2 end)
        {
            line.anchorMin = line.anchorMax = new Vector2(0.5f, 0.5f);
            line.anchoredPosition = (start + end) * 0.5f;
            line.sizeDelta = new Vector2(Vector2.Distance(start, end), 4);
            line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg);
            line.gameObject.SetActive(true);
        }

        private static Vector2 Cardinal(SwipeDirection direction)
        {
            switch (direction)
            {
                case SwipeDirection.Left: return Vector2.left;
                case SwipeDirection.Right: return Vector2.right;
                case SwipeDirection.Up: return Vector2.up;
                default: return Vector2.down;
            }
        }
    }
}
