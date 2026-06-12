namespace ParryArena.Core
{
    /// <summary>
    /// Single source of truth for scene names. Referencing scenes by these
    /// constants (instead of magic strings scattered around) keeps loads in
    /// sync with the files registered in Build Settings.
    /// </summary>
    public static class SceneId
    {
        public const string MainMenu = "MainMenu";
        public const string Arena = "Arena";
    }
}
