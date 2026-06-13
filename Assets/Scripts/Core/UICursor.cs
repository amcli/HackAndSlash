using UnityEngine;

namespace ParryArena.Core
{
    /// <summary>
    /// Cursor visibility/lock helper shared by menus and gameplay. Menus need a
    /// free, visible cursor; gameplay locks it to the centre and hides it.
    /// </summary>
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
}
