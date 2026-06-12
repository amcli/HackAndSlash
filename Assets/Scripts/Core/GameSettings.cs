using UnityEngine;

namespace ParryArena.Core
{
    /// <summary>
    /// Player-facing options, persisted via PlayerPrefs. Plain C# (not a
    /// MonoBehaviour) so it can live on the persistent <see cref="GameApp"/> and
    /// be read from anywhere without a scene reference.
    ///
    /// Music/SFX volumes are stored now and wired to gameplay audio later (no
    /// AudioMixer exists yet); Master volume drives the global AudioListener.
    /// </summary>
    public class GameSettings
    {
        const string KMaster = "settings.masterVolume";
        const string KMusic = "settings.musicVolume";
        const string KSfx = "settings.sfxVolume";
        const string KSensitivity = "settings.mouseSensitivity";
        const string KInvertY = "settings.invertY";
        const string KFullscreen = "settings.fullscreen";

        public float MasterVolume = 0.8f;
        public float MusicVolume = 0.8f;
        public float SfxVolume = 0.8f;
        public float MouseSensitivity = 2.5f;
        public bool InvertY = false;
        public bool Fullscreen = true;

        /// <summary>Raised whenever <see cref="Apply"/> runs, so live systems can re-read values.</summary>
        public event System.Action Changed;

        public void Load()
        {
            MasterVolume = PlayerPrefs.GetFloat(KMaster, MasterVolume);
            MusicVolume = PlayerPrefs.GetFloat(KMusic, MusicVolume);
            SfxVolume = PlayerPrefs.GetFloat(KSfx, SfxVolume);
            MouseSensitivity = PlayerPrefs.GetFloat(KSensitivity, MouseSensitivity);
            InvertY = PlayerPrefs.GetInt(KInvertY, InvertY ? 1 : 0) == 1;
            Fullscreen = PlayerPrefs.GetInt(KFullscreen, Fullscreen ? 1 : 0) == 1;
        }

        public void Save()
        {
            PlayerPrefs.SetFloat(KMaster, MasterVolume);
            PlayerPrefs.SetFloat(KMusic, MusicVolume);
            PlayerPrefs.SetFloat(KSfx, SfxVolume);
            PlayerPrefs.SetFloat(KSensitivity, MouseSensitivity);
            PlayerPrefs.SetInt(KInvertY, InvertY ? 1 : 0);
            PlayerPrefs.SetInt(KFullscreen, Fullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>Pushes the current values into the engine (audio, display).</summary>
        public void Apply()
        {
            AudioListener.volume = Mathf.Clamp01(MasterVolume);
            if (Screen.fullScreen != Fullscreen)
                Screen.fullScreen = Fullscreen;
            Changed?.Invoke();
        }
    }
}
