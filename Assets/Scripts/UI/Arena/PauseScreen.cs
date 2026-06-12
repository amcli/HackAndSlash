using TMPro;
using UnityEngine;

namespace ParryArena.UI
{
    /// <summary>
    /// In-arena pause overlay. Resume / Settings / Quit-to-Menu. The Settings
    /// page is the very same <see cref="SettingsScreen"/> used in the main menu —
    /// no duplicated options UI. Attached to a stretched root by ArenaController.
    /// </summary>
    public class PauseScreen : MonoBehaviour
    {
        GameObject _menu;
        SettingsScreen _settings;

        System.Action _onResume;
        System.Action _onQuit;

        public bool IsOpen { get; private set; }

        public void Build(System.Action onResume, System.Action onQuit)
        {
            _onResume = onResume;
            _onQuit = onQuit;

            _menu = UIFactory.CreatePanel(transform, "PauseMenu", UITheme.Overlay);
            UIFactory.AddVerticalLayout(_menu, 20f, TextAnchor.MiddleCenter);
            UIFactory.CreateLabel(_menu.transform, "PAUSED", UITheme.TitleSize,
                TextAlignmentOptions.Center, UITheme.Accent);
            UIFactory.Spacer(_menu.transform, 12f);
            UIFactory.CreateButton(_menu.transform, "Resume", () => _onResume?.Invoke());
            UIFactory.CreateButton(_menu.transform, "Settings", ShowSettings);
            UIFactory.CreateButton(_menu.transform, "Quit to Menu", () => _onQuit?.Invoke());

            var settingsGo = new GameObject("PauseSettings", typeof(RectTransform));
            settingsGo.transform.SetParent(transform, false);
            UIFactory.Stretch((RectTransform)settingsGo.transform);
            _settings = settingsGo.AddComponent<SettingsScreen>();
            _settings.BuildUI();
            _settings.OnBack = ShowMenu;
            settingsGo.SetActive(false);

            gameObject.SetActive(false);
        }

        public void Open()
        {
            IsOpen = true;
            gameObject.SetActive(true);
            ShowMenu();
        }

        public void Close()
        {
            IsOpen = false;
            gameObject.SetActive(false);
        }

        void ShowSettings()
        {
            _menu.SetActive(false);
            _settings.gameObject.SetActive(true);
        }

        void ShowMenu()
        {
            _settings.gameObject.SetActive(false);
            _menu.SetActive(true);
        }
    }
}
