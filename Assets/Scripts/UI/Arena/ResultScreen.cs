using ParryArena.Core;
using TMPro;
using UnityEngine;

namespace ParryArena.UI
{
    /// <summary>
    /// Victory / Defeat overlay shown when the match ends. Retry reloads the
    /// arena; Quit returns to the main menu. Attached to a stretched root by
    /// ArenaController and hidden until <see cref="Show"/>.
    /// </summary>
    public class ResultScreen : MonoBehaviour
    {
        TextMeshProUGUI _title;

        public void Build(System.Action onRetry, System.Action onQuit)
        {
            var panel = UIFactory.CreatePanel(transform, "ResultPanel", UITheme.Overlay);
            UIFactory.AddVerticalLayout(panel, 22f, TextAnchor.MiddleCenter);

            _title = UIFactory.CreateLabel(panel.transform, string.Empty, UITheme.TitleSize,
                TextAlignmentOptions.Center, UITheme.Text);
            UIFactory.Spacer(panel.transform, 16f);
            UIFactory.CreateButton(panel.transform, "Retry", () => onRetry?.Invoke());
            UIFactory.CreateButton(panel.transform, "Quit to Menu", () => onQuit?.Invoke());

            gameObject.SetActive(false);
        }

        public void Show(MatchResult result)
        {
            bool win = result == MatchResult.Victory;
            _title.text = win ? "VICTORY" : "DEFEAT";
            _title.color = win ? UITheme.Good : UITheme.Danger;
            gameObject.SetActive(true);
        }
    }
}
