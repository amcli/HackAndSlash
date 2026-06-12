using ParryArena.Arena;
using ParryArena.Core;
using ParryArena.UI;
using UnityEngine;

namespace ParryArena
{
    /// <summary>
    /// Composition root: maps a loaded scene to the single controller that
    /// builds it. This is the one class allowed to know about every feature
    /// area; everything else stays decoupled behind it.
    /// </summary>
    public static class SceneComposer
    {
        public static void Compose(string sceneName)
        {
            switch (sceneName)
            {
                case SceneId.MainMenu:
                    new GameObject("MenuController").AddComponent<MenuController>();
                    break;
                case SceneId.Arena:
                    new GameObject("ArenaController").AddComponent<ArenaController>();
                    break;
                // Unknown scenes (e.g. the leftover SampleScene) are intentionally
                // left untouched.
            }
        }
    }
}
