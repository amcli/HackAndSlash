using ParryArena.Core;
using ParryArena.UI;
using UnityEngine;

namespace ParryArena.Arena
{
    /// <summary>
    /// Builds and runs the arena: greybox environment, camera, the player and
    /// enemy chosen in the menu, and the HUD / pause / result overlays. Also
    /// owns the match state machine at this stage (playing / paused / over).
    /// Everything is constructed from code so the scene file stays empty.
    /// </summary>
    public class ArenaController : MonoBehaviour
    {
        PlayerController _player;
        EnemyActor _enemy;
        ActorCombat _playerCombat;
        ActorCombat _enemyCombat;
        ThirdPersonCamera _cameraRig;
        PauseScreen _pause;
        ResultScreen _result;
        bool _matchOver;

        void Start()
        {
            Time.timeScale = 1f;

            var session = GameApp.Instance.Session;
            session.EnsureDefaults();
            session.LastResult = MatchResult.None;

            BuildEnvironment();
            BuildCamera();
            BuildActors(session);
            BuildUI();

            UICursor.LockForGameplay();
        }

        void Update()
        {
            if (_matchOver)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
                TogglePause();

            // Debug shortcuts: F1/F2 force win/lose; F3 toggles the hitbox/hurtbox
            // visualisers.
            if (Input.GetKeyDown(KeyCode.F1))
                EndMatch(MatchResult.Victory);
            if (Input.GetKeyDown(KeyCode.F2))
                EndMatch(MatchResult.Defeat);
            if (Input.GetKeyDown(KeyCode.F3))
                CombatDebug.Enabled = !CombatDebug.Enabled;
        }

        // ---- World construction ------------------------------------------------

        void BuildEnvironment()
        {
            var lightGo = new GameObject("Directional Light");
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.9f);
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(4f, 1f, 4f); // plane is 10x10 -> 40x40
            Tint(ground, new Color(0.18f, 0.19f, 0.21f));

            const float half = 20f;
            CreateWall(new Vector3(0f, 1.5f, half), new Vector3(42f, 3f, 1f));
            CreateWall(new Vector3(0f, 1.5f, -half), new Vector3(42f, 3f, 1f));
            CreateWall(new Vector3(half, 1.5f, 0f), new Vector3(1f, 3f, 42f));
            CreateWall(new Vector3(-half, 1.5f, 0f), new Vector3(1f, 3f, 42f));
        }

        void CreateWall(Vector3 position, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.transform.position = position;
            wall.transform.localScale = scale;
            Tint(wall, new Color(0.12f, 0.13f, 0.15f));
        }

        Transform BuildCamera()
        {
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener))
            {
                tag = "MainCamera"
            };
            _cameraRig = camGo.AddComponent<ThirdPersonCamera>();
            return camGo.transform;
        }

        void BuildActors(GameSession session)
        {
            var loadout = session.SelectedLoadout;
            var enemyDef = session.SelectedEnemy;

            // ---- Enemy ----
            var enemyParts = ActorVisualFactory.CreateAvatar(enemyDef.DisplayName, enemyDef.Tint, enemyDef.BodyScale, 1.3f);
            enemyParts.Root.transform.position = new Vector3(0f, 0f, 4f);
            var enemyHealth = enemyParts.Root.AddComponent<Health>();
            _enemy = enemyParts.Root.AddComponent<EnemyActor>();
            _enemyCombat = enemyParts.Root.AddComponent<ActorCombat>();
            SetupHurtbox(enemyParts.HurtboxAnchor, CombatTeam.Enemy, enemyHealth, _enemyCombat);
            var enemyHit = SetupHitbox(enemyParts.Weapon, CombatTeam.Enemy, 12f, parryable: true, blockable: true, _enemyCombat);

            _enemyCombat.Configure(enemyParts.Weapon, enemyHit, enemyHealth);
            _enemyCombat.SetSwingTimings(0.7f, 0.22f, 0.6f); // slow, readable telegraph
            _enemyCombat.TelegraphWindup = true;

            // ---- Player ----
            var playerParts = ActorVisualFactory.CreateAvatar("Player", loadout.Tint, 1f, loadout.WeaponLength);
            playerParts.Root.transform.position = new Vector3(0f, 0.1f, -4f);
            var controller = playerParts.Root.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 1f, 0f);
            controller.height = 2f;
            controller.radius = 0.4f;
            var playerHealth = playerParts.Root.AddComponent<Health>();
            _player = playerParts.Root.AddComponent<PlayerController>();
            _playerCombat = playerParts.Root.AddComponent<ActorCombat>();
            SetupHurtbox(playerParts.HurtboxAnchor, CombatTeam.Player, playerHealth, _playerCombat);
            var playerHit = SetupHitbox(playerParts.Weapon, CombatTeam.Player, loadout.AttackDamage, parryable: false, blockable: true, _playerCombat);

            _playerCombat.Configure(playerParts.Weapon, playerHit, playerHealth);
            _playerCombat.ConfigureStamina(usesStamina: true, loadout.MaxStamina, loadout.AttackStaminaCost);

            // ---- Hook everything up ----
            _player.Configure(loadout, _cameraRig, _playerCombat);
            _enemy.Configure(enemyDef, playerParts.Root.transform, _enemyCombat);
            _cameraRig.Configure(playerParts.Root.transform, GameApp.Instance.Settings);
            _cameraRig.SetLockTarget(enemyParts.Root.transform);

            playerHealth.Died += _ => EndMatch(MatchResult.Defeat);
            enemyHealth.Died += _ => EndMatch(MatchResult.Victory);
        }

        static Hurtbox SetupHurtbox(Transform anchor, CombatTeam team, Health health, ActorCombat combat)
        {
            var hurtbox = anchor.gameObject.AddComponent<Hurtbox>();
            hurtbox.Configure(team, health, combat, new Vector3(0.9f, 2.0f, 0.9f));
            return hurtbox;
        }

        static Hitbox SetupHitbox(WeaponRig rig, CombatTeam team, float damage, bool parryable, bool blockable, ActorCombat owner)
        {
            var hitbox = rig.HitboxAnchor.gameObject.AddComponent<Hitbox>();
            hitbox.Configure(team, damage, parryable, blockable, new Vector3(0.14f, 0.14f, rig.BladeLength * 0.5f), owner);
            return hitbox;
        }

        void BuildUI()
        {
            var canvas = UIFactory.CreateCanvas("ArenaCanvas");

            var hud = CreateOverlay<ArenaHUD>(canvas.transform, "ArenaHUD");
            hud.Build(hud.transform, _player, _enemy);

            _pause = CreateOverlay<PauseScreen>(canvas.transform, "PauseScreen");
            _pause.Build(onResume: Resume, onQuit: SceneFlow.GoToMainMenu);

            _result = CreateOverlay<ResultScreen>(canvas.transform, "ResultScreen");
            _result.Build(onRetry: SceneFlow.RestartCurrent, onQuit: SceneFlow.GoToMainMenu);
        }

        static T CreateOverlay<T>(Transform canvas, string name) where T : MonoBehaviour
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            UIFactory.Stretch((RectTransform)go.transform);
            return go.AddComponent<T>();
        }

        // ---- Match state -------------------------------------------------------

        void TogglePause()
        {
            if (_pause.IsOpen)
                Resume();
            else
                Pause();
        }

        void Pause()
        {
            Hitstop.CancelAll(); // reclaim timeScale from any in-flight freeze
            Time.timeScale = 0f;
            SetGameplayActive(false);
            UICursor.ShowForMenus();
            _pause.Open();
        }

        void Resume()
        {
            _pause.Close();
            Time.timeScale = 1f;
            SetGameplayActive(true);
            UICursor.LockForGameplay();
        }

        void EndMatch(MatchResult result)
        {
            if (_matchOver)
                return;
            _matchOver = true;

            GameApp.Instance.Session.LastResult = result;
            if (_pause.IsOpen)
                _pause.Close();

            Time.timeScale = 1f;
            SetGameplayActive(false);
            UICursor.ShowForMenus();
            _result.Show(result);
        }

        void SetGameplayActive(bool active)
        {
            if (_player != null)
                _player.enabled = active;
            if (_cameraRig != null)
                _cameraRig.enabled = active;
            if (_enemy != null)
                _enemy.enabled = active;
            if (_playerCombat != null)
                _playerCombat.enabled = active;
            if (_enemyCombat != null)
                _enemyCombat.enabled = active;
        }

        static void Tint(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = color;
        }
    }
}
