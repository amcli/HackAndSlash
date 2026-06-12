using ParryArena.Core;
using TMPro;
using UnityEngine;

namespace ParryArena.UI
{
    /// <summary>
    /// The options panel. Reused as-is in both the main menu and the in-arena
    /// pause menu — changes write straight through to <see cref="GameSettings"/>,
    /// which applies and persists them.
    /// </summary>
    public class SettingsScreen : UIScreen
    {
        public System.Action OnBack;

        public override void BuildUI()
        {
            var settings = GameApp.Instance.Settings;

            var panel = UIFactory.CreatePanel(transform, "Panel", UITheme.Panel);
            UIFactory.AddVerticalLayout(panel, 14f, TextAnchor.UpperCenter, new RectOffset(40, 40, 48, 40));

            UIFactory.CreateLabel(panel.transform, "SETTINGS", UITheme.HeaderSize,
                TextAlignmentOptions.Center, UITheme.Accent);
            UIFactory.Spacer(panel.transform, 16f);

            UIFactory.CreateSlider(panel.transform, "Master Volume", 0f, 1f, settings.MasterVolume,
                v => { settings.MasterVolume = v; settings.Apply(); settings.Save(); }, Percent);
            UIFactory.CreateSlider(panel.transform, "Music Volume", 0f, 1f, settings.MusicVolume,
                v => { settings.MusicVolume = v; settings.Save(); }, Percent);
            UIFactory.CreateSlider(panel.transform, "SFX Volume", 0f, 1f, settings.SfxVolume,
                v => { settings.SfxVolume = v; settings.Save(); }, Percent);
            UIFactory.CreateSlider(panel.transform, "Mouse Sensitivity", 0.5f, 6f, settings.MouseSensitivity,
                v => { settings.MouseSensitivity = v; settings.Save(); });

            UIFactory.CreateToggle(panel.transform, "Invert Look Y", settings.InvertY,
                v => { settings.InvertY = v; settings.Save(); });
            UIFactory.CreateToggle(panel.transform, "Fullscreen", settings.Fullscreen,
                v => { settings.Fullscreen = v; settings.Apply(); settings.Save(); });

            UIFactory.Spacer(panel.transform, 24f);
            UIFactory.CreateButton(panel.transform, "Back", () => OnBack?.Invoke());
        }

        static string Percent(float v) => Mathf.RoundToInt(v * 100f) + "%";
    }
}
