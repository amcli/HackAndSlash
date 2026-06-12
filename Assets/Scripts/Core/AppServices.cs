using UnityEngine;

namespace ParryArena.Core
{
    /// <summary>Small cross-cutting helpers shared by menus and gameplay.</summary>
    public static class UICursor
    {
        public static void ShowForMenus()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public static void LockForGameplay()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public static class AppQuit
    {
        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
