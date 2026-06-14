using UnityEngine;

namespace ParryArena.Data
{
    /// <summary>
    /// Designer-tunable description of an opponent. The single data-driven AI
    /// brain (<see cref="ParryArena.Arena.EnemyController"/>) reads the Behaviour
    /// values below, so opponents differ by data rather than by code: a passive
    /// training dummy and an aggressive brawler are the same brain, tuned apart.
    /// </summary>
    [CreateAssetMenu(fileName = "Enemy", menuName = "Parry Arena/Enemy Definition")]
    public class EnemyDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string Id = "enemy";
        public string DisplayName = "Enemy";
        [TextArea] public string Description = "";

        [Header("Stats")]
        public float MaxHealth = 500f;

        [Header("Moveset")]
        [Tooltip("Attacks this enemy can throw. The AI loops the first one.")]
        public AttackDefinition[] Attacks;

        [Header("Behaviour (data-driven AI)")]
        [Tooltip("Locomotion speed. 0 = stationary (a training dummy).")]
        public float MoveSpeed = 0f;
        [Tooltip("Distance the AI tries to hold from the player.")]
        public float PreferredRange = 2f;
        [Tooltip("Commits an attack within this distance. Very large = swings on a loop regardless (dummy).")]
        public float AttackRange = 2.5f;
        [Range(0f, 1f)]
        [Tooltip("How eagerly it attacks: higher = shorter gaps between swings.")]
        public float Aggression = 0.4f;
        [Range(0f, 1f)]
        [Tooltip("Random ± swing on the gap between attacks. 0 = metronome, higher = unpredictable.")]
        public float AttackIntervalVariance = 0f;
        [Tooltip("Delay before it reacts — to an opening (punishing your whiff) or to your incoming attack. Lower = sharper.")]
        public float ReactionTime = 0.3f;
        [Range(0f, 1f)]
        [Tooltip("Chance it defends a telegraphed attack instead of eating it. 0 = never guards.")]
        public float Defensiveness = 0f;
        [Range(0f, 1f)]
        [Tooltip("When it defends, the chance it commits to a (risky, high-reward) parry rather than a safe block.")]
        public float ParrySkill = 0f;

        [Header("Greybox visuals")]
        public Color Tint = new Color(0.80f, 0.30f, 0.27f, 1f);
        [Tooltip("Uniform scale of the enemy avatar.")]
        public float BodyScale = 1f;
    }
}
