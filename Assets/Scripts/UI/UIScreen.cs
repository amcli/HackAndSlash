using UnityEngine;

namespace ParryArena.UI
{
    /// <summary>
    /// Base for a full-screen panel (menu pages and the pause/result overlays).
    /// The owning controller builds one via <see cref="UIFactory.CreateScreen{T}"/>,
    /// which adds the component and calls <see cref="BuildUI"/>; concrete screens
    /// expose <c>System.Action</c> callbacks for their buttons. Visibility is
    /// toggled by enabling/disabling the GameObject.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public abstract class UIScreen : MonoBehaviour
    {
        public abstract void BuildUI();

        public bool IsVisible => gameObject.activeSelf;

        public virtual void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
        }
    }
}
