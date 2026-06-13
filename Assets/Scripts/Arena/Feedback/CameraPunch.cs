using UnityEngine;

namespace ParryArena.Arena
{
    /// <summary>
    /// A brief FOV "punch-in" for cinematic beats (the riposte). The camera
    /// samples <see cref="EvaluateFovDelta"/> each frame and adds the offset to
    /// its base FOV. Unscaled time so it plays through a hitstop freeze.
    /// </summary>
    public static class CameraPunch
    {
        const float Decay = 4.5f;
        const float Strength = 9f; // max degrees of FOV reduction (zoom-in)

        static float _amount;

        public static void Punch(float amount) => _amount = Mathf.Clamp01(Mathf.Max(_amount, amount));

        public static float EvaluateFovDelta(float deltaTime)
        {
            if (_amount <= 0f)
                return 0f;
            _amount = Mathf.Max(0f, _amount - Decay * deltaTime);
            return -_amount * Strength;
        }
    }
}
