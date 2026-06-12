using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ParryArena.UI
{
    /// <summary>
    /// Shared layout + behaviour for a "pick one from a list" screen. The
    /// loadout and opponent screens differ only in their data and copy, so that
    /// difference is all that subclasses provide — the list, highlight,
    /// description and Back/Confirm wiring live here once.
    /// </summary>
    public abstract class SelectionScreen<T> : UIScreen where T : class
    {
        public System.Action OnBack;
        public System.Action OnConfirm;

        readonly List<(T item, TextMeshProUGUI label)> _entries = new();
        TextMeshProUGUI _description;

        protected abstract string Title { get; }
        protected abstract string ConfirmLabel { get; }
        protected abstract IReadOnlyList<T> Items { get; }
        protected abstract string DisplayName(T item);
        protected abstract string Describe(T item);
        protected abstract T CurrentSelection { get; }
        protected abstract void Apply(T item);

        public override void BuildUI()
        {
            var panel = UIFactory.CreatePanel(transform, "Panel", UITheme.Background);
            UIFactory.AddVerticalLayout(panel, 12f, TextAnchor.UpperCenter, new RectOffset(40, 40, 44, 36));

            UIFactory.CreateLabel(panel.transform, Title, UITheme.HeaderSize,
                TextAlignmentOptions.Center, UITheme.Accent);
            UIFactory.Spacer(panel.transform, 12f);

            foreach (var item in Items)
            {
                var captured = item;
                var button = UIFactory.CreateButton(panel.transform, DisplayName(item),
                    () => Select(captured), 540f, 54f);
                _entries.Add((item, button.GetComponentInChildren<TextMeshProUGUI>()));
            }

            UIFactory.Spacer(panel.transform, 16f);
            _description = UIFactory.CreateLabel(panel.transform, string.Empty, UITheme.BodySize,
                TextAlignmentOptions.Center, UITheme.TextMuted);
            var descLe = _description.gameObject.AddComponent<LayoutElement>();
            descLe.preferredWidth = 760f;
            descLe.preferredHeight = 140f;

            UIFactory.Spacer(panel.transform, 16f);
            var row = UIFactory.CreateHorizontalGroup(panel.transform, 24f, TextAnchor.MiddleCenter, 60f);
            UIFactory.CreateButton(row.transform, "Back", () => OnBack?.Invoke(), 240f, 58f);
            UIFactory.CreateButton(row.transform, ConfirmLabel, () => OnConfirm?.Invoke(), 240f, 58f);
        }

        /// <summary>Re-syncs the highlight/description with the current session selection.</summary>
        public void Refresh()
        {
            var selection = CurrentSelection ?? (Items.Count > 0 ? Items[0] : null);
            Select(selection);
        }

        void Select(T item)
        {
            if (item == null)
                return;

            Apply(item);
            foreach (var entry in _entries)
                entry.label.color = entry.item == item ? UITheme.Accent : UITheme.Text;
            _description.text = Describe(item);
        }
    }
}
