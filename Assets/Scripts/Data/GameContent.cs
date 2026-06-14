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
                    health: 90f, stamina: 130f,
                    tint: new Color(0.85f, 0.32f, 0.42f), weaponLength: 1.5f,
                    combo: new[]
                    {
                        // A 3-hit string: two quick slashes into a heavy overhead finisher.
                        MakeAttack("katana_1", new Vector3(-90f, 55f, 0f), new Vector3(40f, -55f, 0f),
                            windup: 0.13f, active: 0.10f, recovery: 0.20f, damage: 22f, stagger: 8f, staminaCost: 12f),
                        MakeAttack("katana_2", new Vector3(-10f, 80f, 0f), new Vector3(-10f, -80f, 0f),
                            windup: 0.11f, active: 0.10f, recovery: 0.18f, damage: 24f, stagger: 9f, staminaCost: 12f),
                        MakeAttack("katana_finisher", new Vector3(-135f, 0f, 0f), new Vector3(55f, 0f, 0f),
                            windup: 0.18f, active: 0.12f, recovery: 0.34f, damage: 40f, stagger: 20f, staminaCost: 18f),
                    }),
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
                    health: 500f, contactDps: 0f, contactRadius: 1.8f,
                    tint: new Color(0.55f, 0.57f, 0.60f), scale: 1f,
                    attacks: new[]
                    {
                        // One slow, very readable overhead.
                        MakeAttack("dummy_overhead", new Vector3(-135f, 0f, 0f), new Vector3(55f, 0f, 0f),
                            windup: 0.7f, active: 0.22f, recovery: 0.6f, damage: 12f, stagger: 0f, staminaCost: 0f),
                    }),
            };
        }

        static LoadoutDefinition MakeLoadout(string id, string name, string desc,
            float health, float stamina, Color tint, float weaponLength, AttackDefinition[] combo)
        {
            var l = ScriptableObject.CreateInstance<LoadoutDefinition>();
            l.name = name;
            l.Id = id;
            l.DisplayName = name;
            l.Description = desc;
            l.MaxHealth = health;
            l.MaxStamina = stamina;
            l.Tint = tint;
            l.WeaponLength = weaponLength;
            l.Combo = combo;
            return l;
        }

        static EnemyDefinition MakeEnemy(string id, string name, string desc,
            float health, float contactDps, float contactRadius, Color tint, float scale, AttackDefinition[] attacks)
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
            e.Attacks = attacks;
            return e;
        }

        static AttackDefinition MakeAttack(string id, Vector3 windupPose, Vector3 activeEndPose,
            float windup, float active, float recovery, float damage, float stagger, float staminaCost,
            bool parryable = true, bool blockable = true)
        {
            var a = ScriptableObject.CreateInstance<AttackDefinition>();
            a.name = id;
            a.Id = id;
            a.Windup = windup;
            a.Active = active;
            a.Recovery = recovery;
            a.WindupPose = windupPose;
            a.ActiveEndPose = activeEndPose;
            a.Damage = damage;
            a.StaggerDamage = stagger;
            a.Parryable = parryable;
            a.Blockable = blockable;
            a.StaminaCost = staminaCost;
            return a;
        }
    }
}
