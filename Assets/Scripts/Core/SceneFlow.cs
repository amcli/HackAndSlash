using UnityEngine.SceneManagement;

namespace ParryArena.Core
{
    /// <summary>
    /// The one place that loads scenes. Centralising this avoids scattered
    /// <c>SceneManager.LoadScene</c> calls and lets us guarantee invariants
    /// (here: timescale is always restored, so a scene never starts frozen
    /// because we paused before leaving the previous one).
    /// </summary>
    public static class SceneFlow
    {
        public static void GoToMainMenu() => Load(SceneId.MainMenu);

        public static void GoToArena() => Load(SceneId.Arena);

        public static void RestartCurrent() => Load(SceneManager.GetActiveScene().name);

        static void Load(string sceneName)
        {
            UnityEngine.Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }
    }
}
