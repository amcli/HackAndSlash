using UnityEngine;

namespace ParryArena.Arena
{
    /// <summary>
    /// References to the swingable weapon parts, cached by
    /// <see cref="ActorVisualFactory"/> so <see cref="ActorCombat"/> can animate
    /// the swing and toggle the trail without re-finding children every frame.
    /// </summary>
    public class WeaponRig : MonoBehaviour
    {
        public Transform Pivot;          // rotated to swing the blade
        public Renderer BladeRenderer;   // tinted for parry/telegraph flashes
        public Transform HitboxAnchor;   // where the Hitbox component lives (blade centre)
        public TrailRenderer Trail;      // emits during the active swing
        public Quaternion RestLocalRotation;
        public float BladeLength;
    }
}
