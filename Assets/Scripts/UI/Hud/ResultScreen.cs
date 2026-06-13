using ParryArena.Core;
using TMPro;
using UnityEngine;

namespace ParryArena.UI
{
    /// <summary>
    /// Victory / Defeat overlay shown when the match ends. Retry reloads the
    /// arena; Quit returns to the main menu. A <see cref="UIScreen"/> built by the
    /// arena controller, hidden until <see cref="Show"/>.
    /// </summary>
    public class ResultScreen : UIScreen
    {
        public System.Action OnRetry;
        public System.Action OnQuit;

        TextMeshProUGUI _title;

        public override void BuildUI()
        {
            var panel = UIFactory.CreatePanel(transform, "ResultPanel", UITheme.Overlay);
            UIFactory.AddVerticalLayout(panel, 22f, TextAnchor.MiddleCenter);

            _title = UIFactory.CreateLabel(panel.transform, string.Empty, UITheme.TitleSize,
                TextAlignmentOptions.Center, UITheme.Text);
            UIFactory.Spacer(panel.transform, 16f);
            UIFactory.CreateButton(panel.transform, "Retry", () => OnRetry?.Invoke());
            UIFactory.CreateButton(panel.transform, "Quit to Menu", () => OnQuit?.Invoke());

            gameObject.SetActive(false);
        }

        public void Show(MatchResult result)
        {
            bool win = result == MatchResult.Victory;
            _title.text = win ? "VICTORY" : "DEFEAT";
            _title.color = win ? UITheme.Good : UITheme.Danger;
            SetVisible(true);
        }
    }
}
