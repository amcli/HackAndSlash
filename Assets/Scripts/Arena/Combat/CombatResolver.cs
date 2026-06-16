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
    ///   4. Riposte        → huge damage if the defender is staggered + cinematic punch
    ///   5. Clean hit      → damage + small stagger build + hitstun + knockback
    ///
    /// Stagger only ever accrues on the enemy (the player has no meter), so the
    /// AddStagger calls are no-ops on the player's combat.
    /// </summary>
    public static class CombatResolver
    {
        const float ParryStaggerGain = 40f;
        const float RiposteMultiplier = 3.5f;

        // Spark colours for each impact (built in code, no VFX assets).
        static readonly Color ParrySpark = new Color(0.55f, 0.80f, 1.00f);     // parry blue
        static readonly Color ForesightSpark = new Color(0.70f, 0.90f, 1.00f); // brighter cyan flash
        static readonly Color BlockSpark = new Color(0.82f, 0.86f, 0.95f);     // steel
        static readonly Color HitSpark = new Color(1.00f, 0.45f, 0.30f);       // warm red
        static readonly Color RiposteSpark = new Color(1.00f, 0.82f, 0.35f);   // gold

        public static void Resolve(Hitbox attacker, Hurtbox target)
        {
            if (target.Team == attacker.Team || target.Health == null || target.Health.IsDead)
                return;

            var defender = target.Combat;

            // Contact point: the blade hitbox sits where the swing connects.
            Vector3 contact = attacker.transform.position;

            // An absorb-only hurtbox (the foresight read sensor) is a phantom parked
            // at the trigger spot: it ONLY ever absorbs (during the window), never
            // takes a normal hit.
            if (target.AbsorbOnly)
            {
                TryForesightAbsorb(attacker, defender, contact);
                return;
            }

            // Is this landing blow an empowered foresight counter? It gets its own
            // signature VFX/SFX wherever it resolves.
            bool foresightHit = attacker.Owner != null && attacker.Owner.IsForesightCounter;

            // 0. Foresight slash — the read pays off (also covers the real hurtbox
            //    when it hasn't dashed clear yet).
            if (TryForesightAbsorb(attacker, defender, contact))
                return;

            // 1. Perfect parry.
            if (defender != null && defender.IsParrying && attacker.Parryable)
            {
                defender.OnParrySuccess();
                if (attacker.Owner != null)
                {
                    attacker.Owner.OnGotParried();
                    attacker.Owner.AddStagger(ParryStaggerGain);
                }
                Hitstop.Freeze(0.09f);
                ScreenShake.Shake(0.6f);
                CombatAudio.Play(CombatSound.Parry);
                ImpactVfx.Play(contact, ParrySpark, scale: 1.2f, count: 22);
                return;
            }

            // 2. Dodge i-frames.
            if (defender != null && defender.IsInvulnerable)
                return;

            // 3. Block.
            if (defender != null && defender.IsBlocking && attacker.Blockable)
            {
                defender.OnBlocked(attacker.Damage);
                ScreenShake.Shake(0.2f);
                CombatAudio.Play(CombatSound.Block);
                ImpactVfx.Play(contact, BlockSpark, scale: 0.8f, count: 10);
                return;
            }

            // 4. Riposte — the defender is staggered and wide open.
            if (defender != null && defender.IsStaggered)
            {
                target.Health.TakeDamage(attacker.Damage * RiposteMultiplier);
                defender.OnRiposted();
                Hitstop.Freeze(0.18f);
                ScreenShake.Shake(0.9f);
                CameraPunch.Punch(1f);
                CombatAudio.Play(CombatSound.Riposte);
                if (foresightHit)
                    ImpactVfx.PlayForesightHit(contact);
                else
                    ImpactVfx.Play(contact, RiposteSpark, scale: 1.6f, count: 32);
                return;
            }

            // 5. Clean hit — the hit VFX/SFX fire here for both player and enemy.
            target.Health.TakeDamage(attacker.Damage);
            if (defender != null)
            {
                defender.AddStagger(attacker.StaggerDamage);
                Vector3 dir = attacker.Owner != null
                    ? defender.transform.position - attacker.Owner.transform.position
                    : Vector3.zero;
                defender.OnHit(dir);
                ScreenShake.Shake(foresightHit ? 0.4f : 0.12f);
            }

            if (foresightHit)
            {
                // A landed foresight counter: signature flash + a beat of freeze
                // and a punch so it lands hard even on a non-staggered enemy.
                CombatAudio.Play(CombatSound.Riposte);
                ImpactVfx.PlayForesightHit(contact);
                Hitstop.Freeze(0.10f);
                CameraPunch.Punch(0.5f);
            }
            else
            {
                CombatAudio.Play(CombatSound.Hit);
                ImpactVfx.Play(contact, HitSpark, scale: 1f, count: 16);
            }
        }

        /// <summary>
        /// The foresight absorb: if the defender is in its counter window and the
        /// hit is parryable, negate it, launch the empowered counter, stagger the
        /// attacker, and play the feedback. Returns true if it fired. Shared by the
        /// phantom sensor and the real hurtbox.
        /// </summary>
        static bool TryForesightAbsorb(Hitbox attacker, ActorCombat defender, Vector3 contact)
        {
            if (defender == null || !defender.IsInForesightWindow || !attacker.Parryable)
                return false;

            defender.OnForesightSuccess();
            if (attacker.Owner != null)
            {
                attacker.Owner.OnGotParried();
                attacker.Owner.AddStagger(ParryStaggerGain);
            }
            Hitstop.Freeze(0.12f);
            ScreenShake.Shake(0.7f);
            CameraPunch.Punch(0.7f);
            CombatAudio.Play(CombatSound.Parry);
            ImpactVfx.Play(contact, ForesightSpark, scale: 1.4f, count: 26);
            return true;
        }
    }
}
