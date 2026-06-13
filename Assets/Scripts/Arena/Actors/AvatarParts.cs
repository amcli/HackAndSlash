using UnityEngine;

namespace ParryArena.Arena
{
    /// <summary>The pieces of a built avatar that gameplay code needs to wire up.</summary>
    public class AvatarParts
    {
        public GameObject Root;
        public WeaponRig Weapon;        // swingable weapon (pivot/blade/trail/hitbox anchor)
        public Transform HurtboxAnchor; // empty at body centre; Hurtbox component attaches here
        public TrailRenderer DodgeTrail; // body trail emitted during a dodge
    }
}
