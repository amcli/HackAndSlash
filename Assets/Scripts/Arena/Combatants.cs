namespace ParryArena.Arena
{
    /// <summary>
    /// The two fully-assembled fighters and the enemy's stagger meter, produced by
    /// <see cref="CombatantFactory"/> and consumed by <see cref="ArenaController"/>
    /// (match flow) and the HUD (bars). Each fighter's <see cref="Health"/> is
    /// reached via <c>Player.Health</c> / <c>Enemy.Health</c>.
    /// </summary>
    public readonly struct Combatants
    {
        public readonly PlayerController Player;
        public readonly EnemyController Enemy;
        public readonly ActorCombat PlayerCombat;
        public readonly ActorCombat EnemyCombat;
        public readonly StaggerMeter EnemyStagger;

        public Combatants(PlayerController player, EnemyController enemy,
            ActorCombat playerCombat, ActorCombat enemyCombat, StaggerMeter enemyStagger)
        {
            Player = player;
            Enemy = enemy;
            PlayerCombat = playerCombat;
            EnemyCombat = enemyCombat;
            EnemyStagger = enemyStagger;
        }
    }
}
