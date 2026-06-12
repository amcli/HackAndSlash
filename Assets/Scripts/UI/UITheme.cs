using UnityEngine;

namespace ParryArena.UI
{
    /// <summary>Centralised colours and font sizes so the UI stays consistent.</summary>
    public static class UITheme
    {
        public static readonly Color Background = new Color(0.07f, 0.08f, 0.10f, 1f);
        public static readonly Color Panel = new Color(0.12f, 0.14f, 0.17f, 0.96f);
        public static readonly Color Overlay = new Color(0f, 0f, 0f, 0.72f);

        public static readonly Color Accent = new Color(0.93f, 0.58f, 0.22f, 1f);
        public static readonly Color AccentDim = new Color(0.55f, 0.34f, 0.14f, 1f);

        public static readonly Color Text = new Color(0.92f, 0.93f, 0.95f, 1f);
        public static readonly Color TextMuted = new Color(0.60f, 0.64f, 0.70f, 1f);

        public static readonly Color ButtonNormal = new Color(0.19f, 0.22f, 0.26f, 1f);
        public static readonly Color ButtonHover = new Color(0.27f, 0.31f, 0.37f, 1f);
        public static readonly Color ButtonDisabled = new Color(0.14f, 0.15f, 0.17f, 1f);

        public static readonly Color BarTrack = new Color(0.05f, 0.06f, 0.07f, 0.9f);
        public static readonly Color Health = new Color(0.78f, 0.27f, 0.24f, 1f);
        public static readonly Color Stamina = new Color(0.36f, 0.70f, 0.45f, 1f);
        public static readonly Color EnemyHealth = new Color(0.85f, 0.50f, 0.22f, 1f);

        public static readonly Color Good = new Color(0.40f, 0.74f, 0.42f, 1f);
        public static readonly Color Danger = new Color(0.83f, 0.28f, 0.25f, 1f);

        public const float TitleSize = 64f;
        public const float HeaderSize = 38f;
        public const float BodySize = 24f;
        public const float ButtonSize = 26f;
        public const float SmallSize = 20f;
    }
}
