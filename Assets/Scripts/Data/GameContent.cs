using System.Collections.Generic;
using UnityEngine;

namespace ParryArena.Data
{
    /// <summary>
    /// Single access point for the selectable loadouts and enemies.
    ///
    /// Resolution order:
    ///   1. Any authored assets found in a <c>Resources</c> folder (designer path).
    ///   2. Built-in defaults created in memory (so the game is fully playable
    ///      before a single asset is authored).
    ///
    /// The lists are cached for the lifetime of the run.
    /// </summary>
    public static class GameContent
    {
        static List<LoadoutDefinition> _loadouts;
        static List<EnemyDefinition> _enemies;

        public static IReadOnlyList<LoadoutDefinition> Loadouts => _loadouts ??= BuildLoadouts();
        public static IReadOnlyList<EnemyDefinition> Enemies => _enemies ??= BuildEnemies();

        static List<LoadoutDefinition> BuildLoadouts()
        {
            var authored = Resources.LoadAll<LoadoutDefinition>(string.Empty);
            if (authored != null && authored.Length > 0)
                return new List<LoadoutDefinition>(authored);

            return new List<LoadoutDefinition>
            {
                MakeLoadout("katana", "Katana",
                    "Fast and fragile. Lower health but big damage and a deep stamina pool that rewards aggression.",
                    health: 90f, stamina: 130f, damage: 30f, staminaCost: 16f,
                    tint: new Color(0.85f, 0.32f, 0.42f), weaponLength: 1.5f),
            };
        }

        static List<EnemyDefinition> BuildEnemies()
        {
            var authored = Resources.LoadAll<EnemyDefinition>(string.Empty);
            if (authored != null && authored.Length > 0)
                return new List<EnemyDefinition>(authored);

            return new List<EnemyDefinition>
            {
                MakeEnemy("dummy", "Training Dummy",
                    "Throws one slow, telegraphed overhead on a loop. Read the windup and parry it — or just whittle it down.",
                    health: 160f, contactDps: 0f, contactRadius: 1.8f,
                    tint: new Color(0.55f, 0.57f, 0.60f), scale: 1f),
            };
        }

        static LoadoutDefinition MakeLoadout(string id, string name, string desc,
            float health, float stamina, float damage, float staminaCost, Color tint, float weaponLength)
        {
            var l = ScriptableObject.CreateInstance<LoadoutDefinition>();
            l.name = name;
            l.Id = id;
            l.DisplayName = name;
            l.Description = desc;
            l.MaxHealth = health;
            l.MaxStamina = stamina;
            l.AttackDamage = damage;
            l.AttackStaminaCost = staminaCost;
            l.Tint = tint;
            l.WeaponLength = weaponLength;
            return l;
        }

        static EnemyDefinition MakeEnemy(string id, string name, string desc,
            float health, float contactDps, float contactRadius, Color tint, float scale)
        {
            var e = ScriptableObject.CreateInstance<EnemyDefinition>();
            e.name = name;
            e.Id = id;
            e.DisplayName = name;
            e.Description = desc;
            e.MaxHealth = health;
            e.ContactDamagePerSecond = contactDps;
            e.ContactRadius = contactRadius;
            e.Tint = tint;
            e.BodyScale = scale;
            return e;
        }
    }
}
