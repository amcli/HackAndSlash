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
        public Transform Pivot;          // shoulder — rotated by the pose system for the swing arc
        public Transform Elbow;          // flexes procedurally so the forearm cocks and extends
        public Transform Wrist;          // whips the blade through the swing (its changing trajectory)
        public Renderer BladeRenderer;   // tinted for parry/telegraph flashes
        public Transform HitboxAnchor;   // where the Hitbox component lives (blade centre)
        public TrailRenderer Trail;      // emits during the active swing
        public Quaternion RestLocalRotation;
        public Quaternion RestElbowRotation;
        public Quaternion RestWristRotation;
        public float BladeLength;        // the cutting blade only (handle excluded)

        // Model rig: the sword is parented to an animated hand bone and driven by the
        // animation clips (locomotion holds it, the slash clip swings it), so
        // ActorCombat does NOT pose it — it only drives the gameplay/hitbox timing.
        public bool ClipDriven;
    }
}
