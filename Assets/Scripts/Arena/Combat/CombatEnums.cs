namespace ParryArena.Arena
{
    /// <summary>Which side an attack belongs to, so hits only land on the opposing team.</summary>
    public enum CombatTeam
    {
        Player,
        Enemy
    }

    /// <summary>
    /// The per-actor combat FSM states. Kept flat and explicit (plan: "a clean
    /// flat FSM with guarded transitions is plenty"). Hitstun/Staggered/Riposte
    /// join later milestones.
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
        Hitstun
    }
}
