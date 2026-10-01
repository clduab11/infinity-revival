using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Praxen.Game.Presentation.Combat
{
    /// <summary>Layout and lifecycle cancellation invalidate a press until a fresh pointer down.</summary>
    public sealed class CancellableHudButton : Button
    {
        private int? pendingPointerId;

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !IsActive() || !IsInteractable()) return;
            pendingPointerId = eventData.pointerId;
            base.OnPointerDown(eventData);
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            bool paired = eventData.button == PointerEventData.InputButton.Left &&
                pendingPointerId.HasValue && pendingPointerId.Value == eventData.pointerId;
            if (!paired) return;
            pendingPointerId = null;
            base.OnPointerClick(eventData);
        }

        public void CancelPendingPress() => pendingPointerId = null;

        protected override void OnDisable()
        {
            CancelPendingPress();
            base.OnDisable();
        }
    }
}
