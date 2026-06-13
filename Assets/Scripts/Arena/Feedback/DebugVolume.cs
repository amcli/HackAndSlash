using UnityEngine;

namespace ParryArena.Arena
{
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
