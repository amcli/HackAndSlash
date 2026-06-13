using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ParryArena.UI
{
    /// <summary>
    /// One place that knows how to build every uGUI widget the game uses
    /// (canvas, panels, labels, buttons, sliders, toggles, bars). Screens call
    /// these helpers instead of re-implementing layout boilerplate, which keeps
    /// the UI consistent and avoids duplicated construction code.
    /// </summary>
    public static class UIFactory
    {
        static bool _checkedTmp;

        // ---- Root / containers -------------------------------------------------

        public static Canvas CreateCanvas(string name, int sortOrder = 0)
        {
            WarnIfTmpEssentialsMissing();
            EnsureEventSystem();

            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null)
                return;

            // StandaloneInputModule reads the legacy Input Manager, which is the
            // active input handler for this project.
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        /// <summary>Full-screen image used as a screen background / click blocker.</summary>
        public static GameObject CreatePanel(Transform parent, string name, Color background)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform);
            go.GetComponent<Image>().color = background;
            return go;
        }

        public static VerticalLayoutGroup AddVerticalLayout(GameObject go, float spacing,
            TextAnchor alignment, RectOffset padding = null)
        {
            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.padding = padding ?? new RectOffset(0, 0, 0, 0);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return layout;
        }

        /// <summary>A content-sized horizontal group, e.g. a row of buttons.</summary>
        public static GameObject CreateHorizontalGroup(Transform parent, float spacing,
            TextAnchor alignment, float height = 64f)
        {
            var go = new GameObject("Row", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.flexibleWidth = 0;
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return go;
        }

        public static RectTransform CreateAnchoredBox(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            return rt;
        }

        public static void Spacer(Transform parent, float height)
        {
            var go = new GameObject("Spacer", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.flexibleWidth = 0;
            le.flexibleHeight = 0;
        }

        // ---- Atoms -------------------------------------------------------------

        public static Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            return img;
        }

        public static TextMeshProUGUI CreateLabel(Transform parent, string text, float size,
            TextAlignmentOptions alignment, Color color)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            return tmp;
        }

        public static Button CreateButton(Transform parent, string label, UnityAction onClick,
            float width = 420f, float height = 66f)
        {
            var go = new GameObject(label + " Button",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = height;
            le.flexibleWidth = 0;

            var img = go.GetComponent<Image>();
            img.color = UITheme.ButtonNormal;

            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = UITheme.ButtonNormal;
            colors.highlightedColor = UITheme.ButtonHover;
            colors.pressedColor = UITheme.AccentDim;
            colors.selectedColor = UITheme.ButtonHover;
            colors.disabledColor = UITheme.ButtonDisabled;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            if (onClick != null)
                button.onClick.AddListener(onClick);

            var text = CreateLabel(go.transform, label, UITheme.ButtonSize, TextAlignmentOptions.Center, UITheme.Text);
            Stretch(text.rectTransform);
            return button;
        }

        // ---- Composite widgets -------------------------------------------------

        /// <summary>A labelled slider row with a live value readout.</summary>
        public static Slider CreateSlider(Transform parent, string label, float min, float max,
            float value, UnityAction<float> onChanged, Func<float, string> format = null)
        {
            format ??= v => v.ToString("0.0");

            var row = CreateRow(parent, label + " Row", 56f, 560f);

            var labelText = CreateLabel(row.transform, label, UITheme.BodySize, TextAlignmentOptions.Left, UITheme.Text);
            labelText.raycastTarget = false;
            SetWidth(labelText.gameObject, 230f);

            var sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            sliderGo.transform.SetParent(row.transform, false);
            var sliderLe = sliderGo.AddComponent<LayoutElement>();
            sliderLe.preferredHeight = 22f;
            sliderLe.flexibleWidth = 1f;
            var slider = sliderGo.GetComponent<Slider>();

            var track = CreateImage(sliderGo.transform, "Track", UITheme.BarTrack);
            var trackRt = track.rectTransform;
            trackRt.anchorMin = new Vector2(0f, 0.3f);
            trackRt.anchorMax = new Vector2(1f, 0.7f);
            trackRt.offsetMin = Vector2.zero;
            trackRt.offsetMax = Vector2.zero;

            var fillArea = CreateAnchoredBox(sliderGo.transform, "Fill Area",
                new Vector2(0f, 0.3f), new Vector2(1f, 0.7f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            fillArea.offsetMin = new Vector2(8f, 0f);
            fillArea.offsetMax = new Vector2(-8f, 0f);
            var fill = CreateImage(fillArea, "Fill", UITheme.Accent);
            Stretch(fill.rectTransform);

            var handleArea = CreateAnchoredBox(sliderGo.transform, "Handle Slide Area",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            handleArea.offsetMin = new Vector2(8f, 0f);
            handleArea.offsetMax = new Vector2(-8f, 0f);
            var handle = CreateImage(handleArea, "Handle", UITheme.Text);
            handle.rectTransform.sizeDelta = new Vector2(16f, 0f);
            handle.rectTransform.anchorMin = new Vector2(0f, 0f);
            handle.rectTransform.anchorMax = new Vector2(0f, 1f);

            // Wire references before assigning the value so UpdateVisuals has them.
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = false;
            slider.SetValueWithoutNotify(value);

            var valueText = CreateLabel(row.transform, format(value), UITheme.BodySize, TextAlignmentOptions.Right, UITheme.TextMuted);
            SetWidth(valueText.gameObject, 72f);

            slider.onValueChanged.AddListener(v =>
            {
                valueText.text = format(v);
                onChanged?.Invoke(v);
            });
            return slider;
        }

        public static Toggle CreateToggle(Transform parent, string label, bool value, UnityAction<bool> onChanged)
        {
            var go = new GameObject(label + " Toggle", typeof(RectTransform), typeof(Toggle));
            go.transform.SetParent(parent, false);

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 46f;
            le.preferredWidth = 560f;
            le.flexibleWidth = 0;

            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            var toggle = go.GetComponent<Toggle>();

            var box = CreateImage(go.transform, "Box", UITheme.ButtonNormal);
            var boxLe = box.gameObject.AddComponent<LayoutElement>();
            boxLe.preferredWidth = 34f;
            boxLe.preferredHeight = 34f;
            boxLe.flexibleWidth = 0;

            var check = CreateImage(box.transform, "Check", UITheme.Accent);
            Stretch(check.rectTransform);
            check.rectTransform.offsetMin = new Vector2(6f, 6f);
            check.rectTransform.offsetMax = new Vector2(-6f, -6f);

            var labelText = CreateLabel(go.transform, label, UITheme.BodySize, TextAlignmentOptions.Left, UITheme.Text);
            labelText.raycastTarget = true; // make the word clickable too

            toggle.targetGraphic = box;
            toggle.graphic = check;
            toggle.SetIsOnWithoutNotify(value);
            toggle.onValueChanged.AddListener(v => onChanged?.Invoke(v));
            return toggle;
        }

        /// <summary>Track + fill bar. Returns the <see cref="StatBar"/> handle for live updates.</summary>
        public static StatBar CreateBar(Transform parent, Color fillColor, float height, float width = 0f)
        {
            var track = CreateImage(parent, "Bar", UITheme.BarTrack);
            track.raycastTarget = false;
            var le = track.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            if (width > 0f)
            {
                le.preferredWidth = width;
                le.flexibleWidth = 0;
            }
            else
            {
                le.flexibleWidth = 1;
            }

            var fill = CreateImage(track.transform, "Fill", fillColor);
            fill.raycastTarget = false;
            var frt = fill.rectTransform;
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = Vector2.zero;
            frt.offsetMax = Vector2.zero;

            var bar = track.gameObject.AddComponent<StatBar>();
            bar.Init(frt);
            return bar;
        }

        // ---- Screens / helpers -------------------------------------------------

        /// <summary>Creates a full-screen stretched child GameObject under <paramref name="parent"/>.</summary>
        public static GameObject CreateStretchedChild(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform);
            return go;
        }

        /// <summary>
        /// Builds a <see cref="UIScreen"/> on a stretched child and runs its
        /// <see cref="UIScreen.BuildUI"/>. The single creation path for every
        /// menu page and overlay, used by both the menu and arena controllers.
        /// </summary>
        public static T CreateScreen<T>(Transform parent, string name) where T : UIScreen
        {
            var screen = CreateStretchedChild(parent, name).AddComponent<T>();
            screen.BuildUI();
            return screen;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static GameObject CreateRow(Transform parent, string name, float height, float width)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.preferredWidth = width;
            le.flexibleWidth = 0;
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            return go;
        }

        static void SetWidth(GameObject go, float width)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.flexibleWidth = 0;
        }

        static void WarnIfTmpEssentialsMissing()
        {
            if (_checkedTmp)
                return;
            _checkedTmp = true;

            if (TMP_Settings.instance == null || TMP_Settings.defaultFontAsset == null)
            {
                Debug.LogError(
                    "[ParryArena] TextMeshPro essentials are not imported, so UI text will be invisible. " +
                    "Fix once via: Window > TextMeshPro > Import TMP Essential Resources.");
            }
        }
    }
}
