using UnityEngine;

namespace ParryArena.Data
{
    /// <summary>
    /// Designer-tunable description of a player kit. Authored as a
    /// ScriptableObject asset (Create > Parry Arena > Loadout), or supplied as a
    /// built-in default by <see cref="GameContent"/>. Per-hit values live in the
    /// <see cref="Combo"/> moveset (<see cref="AttackDefinition"/>s); this kit
    /// carries the actor-level stats and visuals.
    /// </summary>
    [CreateAssetMenu(fileName = "Loadout", menuName = "Parry Arena/Loadout Definition")]
    public class LoadoutDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string Id = "loadout";
        public string DisplayName = "Loadout";
        [TextArea] public string Description = "";

        [Header("Stats")]
        public float MaxHealth = 100f;
        public float MaxStamina = 100f;

        [Header("Moveset")]
        [Tooltip("Light-attack combo string, swung in order; the last entry is the finisher.")]
        public AttackDefinition[] Combo;

        [Header("Greybox visuals")]
        public Color Tint = new Color(0.30f, 0.55f, 0.85f, 1f);
        [Tooltip("Length of the weapon primitive, in metres.")]
        public float WeaponLength = 0.1f;
    }
}
