namespace ParryArena.Core
{
    /// <summary>
    /// Quits the application — stops Play mode in the editor, exits the build at
    /// runtime. The one place this branch lives so callers just say "quit".
    /// </summary>
    public static class AppQuit
    {
        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }
    }
}
