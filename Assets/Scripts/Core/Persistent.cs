using UnityEngine;

namespace ParryArena.Core
{
    /// <summary>
    /// The one place the "self-creating, survives-scene-loads singleton" boilerplate
    /// lives: spawn a named GameObject, mark it <c>DontDestroyOnLoad</c>, and host a
    /// single component on it. Used by the app root and the persistent feel services
    /// (hitstop, combat audio) so none of them re-implement the pattern.
    /// </summary>
    public static class Persistent
    {
        public static T Create<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            Object.DontDestroyOnLoad(go);
            return go.AddComponent<T>();
        }
    }
}
