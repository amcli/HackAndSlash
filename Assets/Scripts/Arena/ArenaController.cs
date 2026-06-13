using ParryArena.Core;
using ParryArena.UI;
using UnityEngine;

namespace ParryArena.Arena
{
    /// <summary>
    /// Runs an arena match. Construction is delegated to <see cref="ArenaStage"/>
    /// (greybox world + camera) and <see cref="CombatantFactory"/> (the two
    /// fighters); this class owns only the runtime match state machine
    /// (playing / paused / over) and the HUD / pause / result overlays.
    /// Everything is built from code so the scene file stays empty.
    /// </summary>
    public class ArenaController : MonoBehaviour
    {
        ThirdPersonCamera _cameraRig;
        Combatants _combatants;
        PauseScreen _pause;
        ResultScreen _result;
        bool _matchOver;

        void Start()
        {
            Time.timeScale = 1f;

            var session = GameApp.Instance.Session;
            session.EnsureDefaults();
            session.LastResult = MatchResult.None;

            _cameraRig = ArenaStage.Build();
            _combatants = CombatantFactory.Build(session, _cameraRig);
            BuildUI();

            _combatants.Player.Health.Died += _ => EndMatch(MatchResult.Defeat);
            _combatants.Enemy.Health.Died += _ => EndMatch(MatchResult.Victory);

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

        // ---- UI construction ---------------------------------------------------

        void BuildUI()
        {
            var canvas = UIFactory.CreateCanvas("ArenaCanvas");

            var hud = UIFactory.CreateStretchedChild(canvas.transform, "ArenaHUD").AddComponent<ArenaHUD>();
            hud.Build(hud.transform, _combatants.Player, _combatants.Enemy, _combatants.EnemyStagger);

            _pause = UIFactory.CreateScreen<PauseScreen>(canvas.transform, "PauseScreen");
            _pause.OnResume = Resume;
            _pause.OnQuit = SceneFlow.GoToMainMenu;

            _result = UIFactory.CreateScreen<ResultScreen>(canvas.transform, "ResultScreen");
            _result.OnRetry = SceneFlow.RestartCurrent;
            _result.OnQuit = SceneFlow.GoToMainMenu;
        }

        // ---- Match state -------------------------------------------------------

        void TogglePause()
        {
            if (_pause.IsVisible)
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
            _pause.SetVisible(true);
        }

        void Resume()
        {
            _pause.SetVisible(false);
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
            _pause.SetVisible(false);

            Time.timeScale = 1f;
            SetGameplayActive(false);
            UICursor.ShowForMenus();
            _result.Show(result);
        }

        void SetGameplayActive(bool active)
        {
            if (_combatants.Player != null)
                _combatants.Player.enabled = active;
            if (_cameraRig != null)
                _cameraRig.enabled = active;
            if (_combatants.Enemy != null)
                _combatants.Enemy.enabled = active;
            if (_combatants.PlayerCombat != null)
                _combatants.PlayerCombat.enabled = active;
            if (_combatants.EnemyCombat != null)
                _combatants.EnemyCombat.enabled = active;
        }
    }
}
