using UnityEngine;

namespace ParryArena.UI
{
    /// <summary>
    /// A simple horizontal fill bar. Drives the fill by adjusting the fill
    /// rect's right anchor (no sprite needed), which is robust regardless of
    /// the render pipeline. Built via <see cref="UIFactory.CreateBar"/>.
    /// </summary>
    public class StatBar : MonoBehaviour
    {
        RectTransform _fill;

        public void Init(RectTransform fill) => _fill = fill;

        public void SetFraction(float fraction)
        {
            if (_fill == null)
                return;

            fraction = Mathf.Clamp01(fraction);
            var max = _fill.anchorMax;
            max.x = fraction;
            _fill.anchorMax = max;
        }
    }
}
