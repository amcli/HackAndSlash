using UnityEngine;

namespace ParryArena.Arena
{
    /// <summary>
    /// The single hit-resolution pipeline: validate → resolve → apply. Every
    /// landed hit funnels through here so the defensive priority lives in one
    /// place, in the order the plan specifies:
    ///   1. Perfect parry  → negate + stagger the attacker + feedback
    ///   2. Dodge i-frames → negate
    ///   3. Block          → chip damage + stamina drain (guard break at 0)
    ///   4. Clean hit      → damage + hitstun + knockback
    /// </summary>
    public static class CombatResolver
    {
        public static void Resolve(Hitbox attacker, Hurtbox target)
        {
            if (target.Team == attacker.Team || target.Health == null || target.Health.IsDead)
                return;

            var defender = target.Combat;

            // 1. Perfect parry.
            if (defender != null && defender.IsParrying && attacker.Parryable)
            {
                defender.OnParrySuccess();
                if (attacker.Owner != null)
                    attacker.Owner.OnGotParried();
                Hitstop.Freeze(0.09f);
                ScreenShake.Shake(0.6f);
                return;
            }

            // 2. Dodge i-frames.
            if (defender != null && defender.IsInvulnerable)
                return;

            // 3. Block.
            if (defender != null && defender.IsBlocking && attacker.Blockable)
            {
                defender.OnBlocked(attacker.Damage);
                return;
            }

            // 4. Clean hit.
            target.Health.TakeDamage(attacker.Damage);
            if (defender != null)
            {
                Vector3 dir = attacker.Owner != null
                    ? defender.transform.position - attacker.Owner.transform.position
                    : Vector3.zero;
                defender.OnHit(dir);
            }
        }
    }
}
