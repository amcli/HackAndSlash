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
                    tint: new Color(0.85f, 0.32f, 0.42f), weaponLength: 1.4f,
                    combo: new[]
                    {
                        // A 3-hit string: two slashes into a heavy overhead finisher.
                        // Each swing steps in (lunge) so it closes the gap; the
                        // finisher commits hardest. Active/recovery are deliberately
                        // weighty so the string reads instead of mashing out.
                        MakeAttack("katana_1", new Vector3(-90f, 55f, 0f), new Vector3(40f, -55f, 0f),
                            windup: 0.13f, active: 0.16f, recovery: 0.24f, damage: 22f, stagger: 8f, staminaCost: 12f,
                            lungeSpeed: 4f),
                        MakeAttack("katana_2", new Vector3(-10f, 80f, 0f), new Vector3(-10f, -80f, 0f),
                            windup: 0.13f, active: 0.16f, recovery: 0.22f, damage: 24f, stagger: 9f, staminaCost: 12f,
                            lungeSpeed: 4f),
                        MakeAttack("katana_finisher", new Vector3(-135f, 0f, 0f), new Vector3(55f, 0f, 0f),
                            windup: 0.20f, active: 0.18f, recovery: 0.40f, damage: 40f, stagger: 20f, staminaCost: 18f,
                            lungeSpeed: 6f),
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
                // Passive: stands still (MoveSpeed 0), swings on a loop regardless of
                // range, and never reacts to openings (huge ReactionTime).
                MakeEnemy("dummy", "Training Dummy",
                    "Throws one slow, telegraphed overhead on a loop. Read the windup and parry it — or just whittle it down.",
                    health: 500f, tint: new Color(0.55f, 0.57f, 0.60f), scale: 1f,
                    attacks: new[]
                    {
                        // One slow, very readable overhead. Stays planted (no lunge)
                        // so the training target doesn't drift toward the player.
                        MakeAttack("dummy_overhead", new Vector3(-135f, 0f, 0f), new Vector3(55f, 0f, 0f),
                            windup: 0.7f, active: 0.22f, recovery: 0.6f, damage: 12f, stagger: 0f, staminaCost: 0f,
                            lungeSpeed: 0f),
                    },
                    moveSpeed: 0f, preferredRange: 2f, attackRange: 100f, aggression: 0.35f,
                    reactionTime: 99f, attackIntervalVariance: 0f,    // metronome for clean practice
                    defensiveness: 0f, parrySkill: 0f),              // pure target — never guards

                // Aggressive: closes in, strafes, and mixes three attack speeds at
                // unpredictable intervals; punishes whiffs with its quickest jab.
                MakeEnemy("brawler", "Brawler",
                    "Closes the distance and mixes fast jabs, slashes, and heavy overheads at unpredictable timing. Punishes your whiffs — keep your guard honest.",
                    health: 250f, tint: new Color(0.85f, 0.42f, 0.25f), scale: 1f,
                    attacks: new[]
                    {
                        // Fast jab — low damage, hard to react to (used to punish).
                        MakeAttack("brawler_jab", new Vector3(-15f, 70f, 0f), new Vector3(-15f, -40f, 0f),
                            windup: 0.25f, active: 0.10f, recovery: 0.30f, damage: 9f, stagger: 0f, staminaCost: 0f,
                            lungeSpeed: 3f),
                        // Medium slash — steps in.
                        MakeAttack("brawler_slash", new Vector3(-90f, 55f, 0f), new Vector3(40f, -55f, 0f),
                            windup: 0.4f, active: 0.15f, recovery: 0.45f, damage: 14f, stagger: 0f, staminaCost: 0f,
                            lungeSpeed: 4.5f),
                        // Slow heavy overhead — big telegraph, big damage, big commit forward.
                        MakeAttack("brawler_heavy", new Vector3(-135f, 0f, 0f), new Vector3(55f, 0f, 0f),
                            windup: 0.65f, active: 0.18f, recovery: 0.55f, damage: 22f, stagger: 0f, staminaCost: 0f,
                            lungeSpeed: 5.5f),
                    },
                    moveSpeed: 3.2f, preferredRange: 1.8f, attackRange: 1.8f, aggression: 0.7f,
                    reactionTime: 0.15f, attackIntervalVariance: 0.45f,
                    defensiveness: 0.9f, parrySkill: 0.4f),         // guards telegraphed attacks; sometimes parries back
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
            float health, Color tint, float scale, AttackDefinition[] attacks,
            float moveSpeed, float preferredRange, float attackRange, float aggression,
            float reactionTime, float attackIntervalVariance, float defensiveness, float parrySkill)
        {
            var e = ScriptableObject.CreateInstance<EnemyDefinition>();
            e.name = name;
            e.Id = id;
            e.DisplayName = name;
            e.Description = desc;
            e.MaxHealth = health;
            e.Tint = tint;
            e.BodyScale = scale;
            e.Attacks = attacks;
            e.MoveSpeed = moveSpeed;
            e.PreferredRange = preferredRange;
            e.AttackRange = attackRange;
            e.Aggression = aggression;
            e.ReactionTime = reactionTime;
            e.AttackIntervalVariance = attackIntervalVariance;
            e.Defensiveness = defensiveness;
            e.ParrySkill = parrySkill;
            return e;
        }

        static AttackDefinition MakeAttack(string id, Vector3 windupPose, Vector3 activeEndPose,
            float windup, float active, float recovery, float damage, float stagger, float staminaCost,
            float lungeSpeed = 4f, bool parryable = true, bool blockable = true)
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
            a.LungeSpeed = lungeSpeed;
            a.Parryable = parryable;
            a.Blockable = blockable;
            a.StaminaCost = staminaCost;
            return a;
        }
    }
}
