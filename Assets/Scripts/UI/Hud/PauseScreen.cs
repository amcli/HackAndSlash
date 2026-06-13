using TMPro;
using UnityEngine;

namespace ParryArena.UI
{
    /// <summary>
    /// In-arena pause overlay. Resume / Settings / Quit-to-Menu. The Settings page
    /// is the very same <see cref="SettingsScreen"/> used in the main menu — no
    /// duplicated options UI. A <see cref="UIScreen"/> like the menu pages: the
    /// arena controller sets the callbacks and toggles it with
    /// <see cref="UIScreen.SetVisible"/>.
    /// </summary>
    public class PauseScreen : UIScreen
    {
        public System.Action OnResume;
        public System.Action OnQuit;

        GameObject _menu;
        SettingsScreen _settings;

        public override void BuildUI()
        {
            _menu = UIFactory.CreatePanel(transform, "PauseMenu", UITheme.Overlay);
            UIFactory.AddVerticalLayout(_menu, 20f, TextAnchor.MiddleCenter);
            UIFactory.CreateLabel(_menu.transform, "PAUSED", UITheme.TitleSize,
                TextAlignmentOptions.Center, UITheme.Accent);
            UIFactory.Spacer(_menu.transform, 12f);
            UIFactory.CreateButton(_menu.transform, "Resume", () => OnResume?.Invoke());
            UIFactory.CreateButton(_menu.transform, "Settings", ShowSettings);
            UIFactory.CreateButton(_menu.transform, "Quit to Menu", () => OnQuit?.Invoke());

            _settings = UIFactory.CreateScreen<SettingsScreen>(transform, "PauseSettings");
            _settings.OnBack = ShowMenu;
            _settings.SetVisible(false);

            gameObject.SetActive(false);
        }

        /// <summary>Always reopen on the menu page, never mid-Settings.</summary>
        public override void SetVisible(bool visible)
        {
            base.SetVisible(visible);
            if (visible)
                ShowMenu();
        }

        void ShowSettings()
        {
            _menu.SetActive(false);
            _settings.SetVisible(true);
        }

        void ShowMenu()
        {
            _settings.SetVisible(false);
            _menu.SetActive(true);
        }
    }
}
