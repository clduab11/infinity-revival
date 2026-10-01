using System;
using Praxen.Game.Domain.Input;
using UnityEngine;
using UnityEngine.UI;

namespace Praxen.Game.Presentation.Combat
{
    internal sealed class ExplorationPreview : IDisposable
    {
        private readonly Camera camera;
        private readonly Quaternion original;
        private readonly GameObject surface;
        private readonly Text description;
        public float Yaw { get; private set; }
        public float Pitch { get; private set; }
        public string SelectedDestination { get; private set; }
        public ExplorationPreview(PortraitCombatHud hud,Camera camera)
        {
            this.camera=camera; original=camera.transform.rotation;
            surface=new GameObject("Exploration preview labels",typeof(RectTransform));
            surface.transform.SetParent(hud.GetComponentInChildren<Canvas>().transform,false);
            var rect=surface.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;
            rect.offsetMin=rect.offsetMax=Vector2.zero;
            Label("exploration.gate",new Vector2(.20f,.51f),new Vector2(.50f,.58f));
            Label("exploration.refuge",new Vector2(.50f,.51f),new Vector2(.80f,.58f));
            description=Label("exploration.instructions",new Vector2(.07f,.63f),new Vector2(.93f,.71f));
        }
        private Text Label(string key,Vector2 min,Vector2 max)
        {
            var go=new GameObject(key,typeof(RectTransform),typeof(Text));go.transform.SetParent(surface.transform,false);
            var rect=go.GetComponent<RectTransform>();rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var text=go.GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text=HudText.Get(key);text.fontSize=23;text.alignment=TextAnchor.MiddleCenter;
            text.color=new Color(.91f,.84f,.65f);text.raycastTarget=false;return text;
        }
        public void Tap(NormalizedPoint point)
        {
            if (Math.Abs(point.Y-.545) > .06) return;
            if (Math.Abs(point.X-.35)<.13) SelectedDestination="exploration.gate";
            else if (Math.Abs(point.X-.65)<.13) SelectedDestination="exploration.refuge";
            else return;
            description.text=HudText.Format("exploration.selected",HudText.Get(SelectedDestination));
        }
        public void Drag(double dx,double dy)
        {
            Yaw=Mathf.Clamp(Yaw+(float)dx*60,-12,12);
            Pitch=Mathf.Clamp(Pitch-(float)dy*60,-8,8);
            camera.transform.rotation=original*Quaternion.Euler(Pitch,Yaw,0);
        }
        public void Dispose()
        {
            if(camera!=null) camera.transform.rotation=original;
            if(surface!=null) UnityEngine.Object.Destroy(surface);
        }
    }
}
