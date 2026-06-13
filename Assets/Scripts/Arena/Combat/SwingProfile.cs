using UnityEngine;

namespace ParryArena.Arena
{
    /// <summary>A swing direction expressed as the windup and active-end blade poses (pivot Euler, degrees).</summary>
    public readonly struct SwingProfile
    {
        public readonly Vector3 Windup;
        public readonly Vector3 ActiveEnd;

        public SwingProfile(Vector3 windup, Vector3 activeEnd)
        {
            Windup = windup;
            ActiveEnd = activeEnd;
        }
    }
}
