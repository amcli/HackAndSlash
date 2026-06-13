using ParryArena.Core;
using UnityEngine;

namespace ParryArena.Arena
{
    /// <summary>
    /// Assembles the two fighters from the menu selection: builds each greybox
    /// avatar, attaches and configures the shared combat components (Health,
    /// StaggerMeter, the actor brain, the ActorCombat FSM, and the Hitbox/Hurtbox),
    /// then cross-wires the actors and camera. Pure construction; the match-flow
    /// wiring (death → result) stays in <see cref="ArenaController"/>.
    /// </summary>
    public static class CombatantFactory
    {
        public static Combatants Build(GameSession session, ThirdPersonCamera cameraRig)
        {
            var loadout = session.SelectedLoadout;
            var enemyDef = session.SelectedEnemy;

            // ---- Enemy ----
            var enemyParts = ActorVisualFactory.CreateAvatar(enemyDef.DisplayName, enemyDef.Tint, enemyDef.BodyScale, 1.3f);
            enemyParts.Root.transform.position = new Vector3(0f, 0f, 4f);
            var enemyHealth = enemyParts.Root.AddComponent<Health>();
            var enemyStagger = enemyParts.Root.AddComponent<StaggerMeter>();
            var enemy = enemyParts.Root.AddComponent<EnemyController>();
            var enemyCombat = enemyParts.Root.AddComponent<ActorCombat>();
            SetupHurtbox(enemyParts.HurtboxAnchor, CombatTeam.Enemy, enemyHealth, enemyCombat);
            var enemyHit = SetupHitbox(enemyParts.Weapon, CombatTeam.Enemy, parryable: true, blockable: true, enemyCombat);

            enemyCombat.Configure(enemyParts.Weapon, enemyHit, enemyHealth);
            enemyCombat.ConfigureOffense(12f, 0f);
            enemyCombat.SetSwingTimings(0.7f, 0.22f, 0.6f); // slow, readable telegraph
            enemyCombat.TelegraphWindup = true;
            enemyCombat.SetDodgeTrail(enemyParts.DodgeTrail);
            enemyCombat.SetStaggerMeter(enemyStagger);

            // ---- Player ----
            var playerParts = ActorVisualFactory.CreateAvatar("Player", loadout.Tint, 1f, loadout.WeaponLength);
            playerParts.Root.transform.position = new Vector3(0f, 0.1f, -4f);
            var controller = playerParts.Root.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 1f, 0f);
            controller.height = 2f;
            controller.radius = 0.4f;
            var playerHealth = playerParts.Root.AddComponent<Health>();
            var player = playerParts.Root.AddComponent<PlayerController>();
            var playerCombat = playerParts.Root.AddComponent<ActorCombat>();
            SetupHurtbox(playerParts.HurtboxAnchor, CombatTeam.Player, playerHealth, playerCombat);
            var playerHit = SetupHitbox(playerParts.Weapon, CombatTeam.Player, parryable: false, blockable: true, playerCombat);

            playerCombat.Configure(playerParts.Weapon, playerHit, playerHealth);
            playerCombat.ConfigureStamina(usesStamina: true, loadout.MaxStamina, loadout.AttackStaminaCost);
            playerCombat.ConfigureOffense(loadout.AttackDamage, 10f);
            playerCombat.SetDodgeTrail(playerParts.DodgeTrail);

            // ---- Cross-wire actors + camera ----
            player.Configure(loadout, cameraRig, playerCombat);
            enemy.Configure(enemyDef, playerParts.Root.transform, enemyCombat);
            cameraRig.Configure(playerParts.Root.transform, GameApp.Instance.Settings);
            cameraRig.SetLockTarget(enemyParts.Root.transform);

            return new Combatants(player, enemy, playerCombat, enemyCombat, enemyStagger);
        }

        static Hurtbox SetupHurtbox(Transform anchor, CombatTeam team, Health health, ActorCombat combat)
        {
            var hurtbox = anchor.gameObject.AddComponent<Hurtbox>();
            hurtbox.Configure(team, health, combat, new Vector3(0.9f, 2.0f, 0.9f));
            return hurtbox;
        }

        static Hitbox SetupHitbox(WeaponRig rig, CombatTeam team, bool parryable, bool blockable, ActorCombat owner)
        {
            var hitbox = rig.HitboxAnchor.gameObject.AddComponent<Hitbox>();
            hitbox.Configure(team, parryable, blockable, new Vector3(0.14f, 0.14f, rig.BladeLength * 0.5f), owner);
            return hitbox;
        }
    }
}
