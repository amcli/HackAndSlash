namespace ParryArena.Arena
{
    /// <summary>
    /// The per-actor combat FSM states. Kept flat and explicit (plan: "a clean
    /// flat FSM with guarded transitions is plenty"). Driven by
    /// <see cref="ActorCombat"/>.
    /// </summary>
    public enum CombatState
    {
        Idle,
        Windup,
        Active,
        Recovery,
        Parry,
        Block,
        Dodge,
        Hitstun,
        Charge,
        Staggered,
        Foresight
    }
}
