using UnityEngine;

namespace ParryArena.Data
{
    /// <summary>
    /// Designer-tunable description of an opponent. For the slice the behaviour
    /// is a placeholder (face the player, optional contact damage); the data
    /// layout already anticipates the one-brain, data-driven AI from the plan,
    /// where personalities differ by values rather than by code.
    /// </summary>
    [CreateAssetMenu(fileName = "Enemy", menuName = "Parry Arena/Enemy Definition")]
    public class EnemyDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string Id = "enemy";
        public string DisplayName = "Enemy";
        [TextArea] public string Description = "";

        [Header("Stats")]
        public float MaxHealth = 150f;

        [Header("Placeholder hostility")]
        [Tooltip("Health drained from the player per second while inside ContactRadius. 0 = passive target dummy.")]
        public float ContactDamagePerSecond = 0f;
        public float ContactRadius = 1.8f;

        [Header("Greybox visuals")]
        public Color Tint = new Color(0.80f, 0.30f, 0.27f, 1f);
        [Tooltip("Uniform scale of the enemy avatar.")]
        public float BodyScale = 1f;
    }
}
