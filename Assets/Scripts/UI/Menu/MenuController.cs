using System.Collections.Generic;
using ParryArena.Core;
using UnityEngine;

namespace ParryArena.UI
{
    /// <summary>
    /// Owns the main-menu scene: builds the canvas and every menu screen as a
    /// panel, then wires the navigation between them. Screens are shown one at a
    /// time (the menu flow is panel swaps, not scene loads, so transitions are
    /// instant). The arena is the only place a scene load happens.
    /// </summary>
    public class MenuController : MonoBehaviour
    {
        Canvas _canvas;
        readonly List<UIScreen> _screens = new();

        MainMenuScreen _main;
        SettingsScreen _settings;
        LoadoutSelectScreen _loadout;
        EnemySelectScreen _enemy;

        void Start()
        {
            UICursor.ShowForMenus();

            CreateMenuCamera();
            _canvas = UIFactory.CreateCanvas("MenuCanvas");

            _main = CreateScreen<MainMenuScreen>("MainMenuScreen");
            _settings = CreateScreen<SettingsScreen>("SettingsScreen");
            _loadout = CreateScreen<LoadoutSelectScreen>("LoadoutSelectScreen");
            _enemy = CreateScreen<EnemySelectScreen>("EnemySelectScreen");

            _main.OnPlay = () => ShowSelection(_loadout);
            _main.OnSettings = () => Show(_settings);
            _main.OnQuit = AppQuit.Quit;

            _settings.OnBack = () => Show(_main);

            _loadout.OnBack = () => Show(_main);
            _loadout.OnConfirm = () => ShowSelection(_enemy);

            _enemy.OnBack = () => ShowSelection(_loadout);
            _enemy.OnConfirm = StartFight;

            Show(_main);
        }

        /// <summary>
        /// A do-nothing camera so the menu scene has a defined background clear
        /// and an AudioListener. The screen-space UI draws on top of it.
        /// </summary>
        static void CreateMenuCamera()
        {
            var camGo = new GameObject("MenuCamera", typeof(Camera), typeof(AudioListener))
            {
                tag = "MainCamera"
            };
            var camera = camGo.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = UITheme.Background;
            camera.cullingMask = 0; // nothing 3D to render; UI is screen-space overlay
        }

        T CreateScreen<T>(string name) where T : UIScreen
        {
            var screen = UIFactory.CreateScreen<T>(_canvas.transform, name);
            _screens.Add(screen);
            return screen;
        }

        void Show(UIScreen screen)
        {
            foreach (var s in _screens)
                s.SetVisible(s == screen);
        }

        /// <summary>Selection screens need their highlight re-synced each time they open.</summary>
        void ShowSelection<T>(SelectionScreen<T> screen) where T : class
        {
            Show(screen);
            screen.Refresh();
        }

        void StartFight()
        {
            GameApp.Instance.Session.EnsureDefaults();
            SceneFlow.GoToArena();
        }
    }
}
