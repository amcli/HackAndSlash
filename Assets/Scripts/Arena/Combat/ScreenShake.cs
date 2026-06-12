using UnityEngine;

namespace ParryArena.Arena
{
    /// <summary>
    /// A tiny trauma-based screen-shake service. Effects call <see cref="Shake"/>;
    /// the camera samples <see cref="Evaluate"/> every frame and adds the offset.
    /// Trauma decays quadratically so the shake eases out instead of cutting off.
    /// Uses unscaled time so it still plays during a hitstop freeze.
    /// </summary>
    public static class ScreenShake
    {
        const float Decay = 3f;
        const float MaxOffset = 0.3f;

        static float _trauma;

        public static void Shake(float trauma) => _trauma = Mathf.Clamp01(Mathf.Max(_trauma, trauma));

        /// <summary>Returns a screen-space (x = right, y = up) offset and advances the decay.</summary>
        public static Vector3 Evaluate(float deltaTime)
        {
            if (_trauma <= 0f)
                return Vector3.zero;

            _trauma = Mathf.Max(0f, _trauma - Decay * deltaTime);
            float magnitude = _trauma * _trauma * MaxOffset;
            return new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * magnitude;
        }
    }
}
