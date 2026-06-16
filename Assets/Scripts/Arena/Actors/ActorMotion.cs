using UnityEngine;

namespace ParryArena.Arena
{
    /// <summary>
    /// Shared actor-movement helpers used by both the player and enemy controllers,
    /// so the same motion maths isn't re-implemented per controller.
    /// </summary>
    public static class ActorMotion
    {
        /// <summary>
        /// Smoothly rotate a transform to face a flat (XZ) direction at the given
        /// turn rate. No-ops on a near-zero direction. The one place "turn toward a
        /// direction" lives — both controllers use it to track their movement/target.
        /// </summary>
        public static void FaceDirection(this Transform transform, Vector3 flatDirection, float turnRate)
        {
            flatDirection.y = 0f;
            if (flatDirection.sqrMagnitude < 0.0001f)
                return;

            var look = Quaternion.LookRotation(flatDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, turnRate * Time.deltaTime);
        }
    }
}
