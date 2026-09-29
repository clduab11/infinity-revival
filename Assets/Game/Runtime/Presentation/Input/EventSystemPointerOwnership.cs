using System.Collections.Generic;
using Praxen.Game.Application.Input;
using Praxen.Game.Domain.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Praxen.Game.Presentation.Input
{
    public sealed class EventSystemPointerOwnership : IPointerOwnershipResolver
    {
        private readonly List<RaycastResult> results = new List<RaycastResult>();

        public PointerOwnership Resolve(in TouchSample begin, in ScreenMetrics metrics)
        {
            var system = EventSystem.current;
            if (system == null) return PointerOwnership.Gameplay;
            var data = new PointerEventData(system)
            {
                pointerId = (int)(begin.PointerId & uint.MaxValue),
                position = new Vector2((float)(begin.Position.X * metrics.Width),
                    (float)(begin.Position.Y * metrics.Height))
            };
            results.Clear();
            system.RaycastAll(data, results);
            foreach (var result in results)
                if (result.module is GraphicRaycaster && result.gameObject != null)
                    return new PointerOwnership(PointerOwnerKind.Ui,
                        EntityId.ToULong(result.gameObject.GetEntityId()));
            return PointerOwnership.Gameplay;
        }
    }
}
