using UnityEngine;

namespace ParryArena.UI
{
    /// <summary>
    /// Base for a full-screen menu panel. The owning controller creates a
    /// stretched root GameObject, adds the concrete screen component, then calls
    /// <see cref="BuildUI"/>. Visibility is toggled by enabling/disabling the
    /// GameObject.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public abstract class UIScreen : MonoBehaviour
    {
        public abstract void BuildUI();

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
        }
    }
}
