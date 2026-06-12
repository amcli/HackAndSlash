using UnityEngine;

namespace ParryArena.Arena
{
    /// <summary>Global toggle + palette for the runtime hitbox/hurtbox visualisers.</summary>
    public static class CombatDebug
    {
        public static bool Enabled = true;

        public static readonly Color HurtboxColor = new Color(0.25f, 0.90f, 0.35f, 0.16f);
        public static readonly Color HitboxIdleColor = new Color(0.95f, 0.30f, 0.22f, 0.10f);
        public static readonly Color HitboxActiveColor = new Color(1.00f, 0.35f, 0.25f, 0.50f);
    }

    /// <summary>
    /// Renders a translucent box so a hitbox/hurtbox is visible in-game. Attaches
    /// a borderless cube child with an unlit transparent material, shown only
    /// while <see cref="CombatDebug.Enabled"/>. One component covers both the
    /// "weapon hitbox" and "player/enemy hurtbox" visualisation asks.
    /// </summary>
    public class DebugVolume : MonoBehaviour
    {
        Renderer _renderer;
        Color _idleColor;
        Color _activeColor;
        bool _highlight;

        public static DebugVolume Attach(Transform parent, Vector3 size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "DebugVolume";

            var collider = go.GetComponent<Collider>();
            if (collider != null)
                Object.Destroy(collider);

            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = size;

            var volume = go.AddComponent<DebugVolume>();
            volume.Init(color);
            return volume;
        }

        void Init(Color color)
        {
            _renderer = GetComponent<Renderer>();
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.material = new Material(Shader.Find("Sprites/Default"));
            _idleColor = color;
            _activeColor = color;
            ApplyColor();
        }

        public void SetSize(Vector3 size) => transform.localScale = size;
        public void SetActiveColor(Color color) => _activeColor = color;

        public void SetHighlight(bool on) => _highlight = on;

        void LateUpdate()
        {
            if (_renderer.enabled != CombatDebug.Enabled)
                _renderer.enabled = CombatDebug.Enabled;
            if (CombatDebug.Enabled)
                ApplyColor();
        }

        void ApplyColor() => _renderer.material.color = _highlight ? _activeColor : _idleColor;
    }
}
