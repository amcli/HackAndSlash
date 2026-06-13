using ParryArena.Data;

namespace ParryArena.Core
{
    /// <summary>
    /// The choices and outcome that need to survive scene transitions
    /// (menu -> arena -> result). Lives on the persistent <see cref="GameApp"/>.
    /// </summary>
    public class GameSession
    {
        public LoadoutDefinition SelectedLoadout;
        public EnemyDefinition SelectedEnemy;
        public MatchResult LastResult = MatchResult.None;

        /// <summary>
        /// Guarantees a valid selection so the arena can be launched directly
        /// (e.g. pressing Play on the Arena scene in the editor) without crashing.
        /// </summary>
        public void EnsureDefaults()
        {
            if (SelectedLoadout == null && GameContent.Loadouts.Count > 0)
                SelectedLoadout = GameContent.Loadouts[0];
            if (SelectedEnemy == null && GameContent.Enemies.Count > 0)
                SelectedEnemy = GameContent.Enemies[0];
        }
    }
}
