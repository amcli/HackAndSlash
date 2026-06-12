using ParryArena.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ParryArena
{
    /// <summary>
    /// Application entry point. Runs automatically before the first scene loads
    /// (no GameObject wiring required), spins up the persistent services, and
    /// hands each loaded scene to the <see cref="SceneComposer"/> so it can build
    /// its content from code. Building content this way keeps the scene files
    /// trivial and free of fragile serialized references.
    /// </summary>
    public static class AppBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            GameApp.Ensure();

            // Subscribed before the first scene finishes loading, so the initial
            // scene is composed too.
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneComposer.Compose(scene.name);
        }
    }
}
