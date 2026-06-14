using UnityEngine;

namespace ParryArena.Arena
{
    /// <summary>
    /// A region that can receive hits. Carries a trigger collider (so weapon
    /// hitboxes can find it via overlap queries without affecting the
    /// CharacterController) plus the refs the resolver needs: which team it
    /// belongs to, its <see cref="Health"/>, and its <see cref="ActorCombat"/>
    /// (to check for an active parry).
    ///
    /// A <see cref="IsForesightSensor"/> hurtbox is a special, detached phantom
    /// parked at the spot a foresight slash was triggered: it ONLY ever absorbs
    /// a hit during the counter window and never takes normal damage, so the read
    /// is judged against the original position while the body dashes clear.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class Hurtbox : MonoBehaviour
    {
        public CombatTeam Team { get; private set; }
        public Health Health { get; private set; }
        public ActorCombat Combat { get; private set; }
        public bool IsForesightSensor { get; private set; }

        public void Configure(CombatTeam team, Health health, ActorCombat combat, Vector3 size,
            bool foresightSensor = false)
        {
            Team = team;
            Health = health;
            Combat = combat;
            IsForesightSensor = foresightSensor;

            var box = GetComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;

            DebugVolume.Attach(transform, size, CombatDebug.HurtboxColor);
        }
    }
}
