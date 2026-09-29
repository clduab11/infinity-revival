using Praxen.Game.Domain.Input;
using UnityEngine;

namespace Praxen.Game.Content.Input
{
    [CreateAssetMenu(menuName = "Praxen/Input/Gesture Tuning")]
    public sealed class GestureTuningAsset : ScriptableObject
    {
        [SerializeField] private double travelThreshold = 0.06;
        [SerializeField] private int maximumDurationMs = 350;

        public GestureTuning ToRuntime() =>
            new GestureTuning(travelThreshold, maximumDurationMs * 1000L);
    }
}
