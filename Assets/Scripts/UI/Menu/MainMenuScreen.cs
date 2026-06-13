using TMPro;
using UnityEngine;

namespace ParryArena.UI
{
    public class MainMenuScreen : UIScreen
    {
        public System.Action OnPlay;
        public System.Action OnSettings;
        public System.Action OnQuit;

        public override void BuildUI()
        {
            var panel = UIFactory.CreatePanel(transform, "Panel", UITheme.Background);
            UIFactory.AddVerticalLayout(panel, 22f, TextAnchor.MiddleCenter);

            UIFactory.CreateLabel(panel.transform, "PARRY ARENA", UITheme.TitleSize,
                TextAlignmentOptions.Center, UITheme.Accent);
            UIFactory.CreateLabel(panel.transform, "Vertical Slice", UITheme.BodySize,
                TextAlignmentOptions.Center, UITheme.TextMuted);
            UIFactory.Spacer(panel.transform, 28f);

            UIFactory.CreateButton(panel.transform, "Play", () => OnPlay?.Invoke());
            UIFactory.CreateButton(panel.transform, "Settings", () => OnSettings?.Invoke());
            UIFactory.CreateButton(panel.transform, "Quit", () => OnQuit?.Invoke());
        }
    }
}
