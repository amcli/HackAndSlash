using UnityEngine;

namespace ParryArena.Core
{
    /// <summary>
    /// Persistent root object that owns the cross-scene services
    /// (<see cref="GameSession"/> and <see cref="GameSettings"/>). Created once
    /// by <c>AppBootstrap</c> and kept alive with DontDestroyOnLoad.
    /// </summary>
    public class GameApp : MonoBehaviour
    {
        public static GameApp Instance { get; private set; }

        public GameSession Session { get; private set; }
        public GameSettings Settings { get; private set; }

        /// <summary>Creates the singleton if it does not already exist. Idempotent.</summary>
        public static GameApp Ensure()
        {
            if (Instance != null)
                return Instance;

            var go = new GameObject("[GameApp]");
            DontDestroyOnLoad(go);
            return go.AddComponent<GameApp>(); // Awake wires everything up
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Session = new GameSession();
            Settings = new GameSettings();
            Settings.Load();
            Settings.Apply();
        }
    }
}
