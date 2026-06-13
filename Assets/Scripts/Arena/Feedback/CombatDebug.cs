using UnityEngine;

namespace ParryArena.Arena
{
    /// <summary>Global toggle + palette for the runtime hitbox/hurtbox visualisers.</summary>
    public static class CombatDebug
    {
        public static bool Enabled = true;

        public static readonly Color HurtboxColor = new Color(0.25f, 0.90f, 0.35f, 0.16f);
        public static readonly Color HitboxIdleColor = new Color(0.95f, 0.30f, 0.22f, 0.10f);
        public static readonly Color HitboxActiveColor = new Color(1.00f, 0.35f, 0.25f, 0.50f);
    }
}
