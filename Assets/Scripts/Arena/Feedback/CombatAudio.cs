using ParryArena.Core;
using UnityEngine;

namespace ParryArena.Arena
{
    /// <summary>
    /// Procedural combat SFX. No audio assets are imported (none exist in the
    /// project): every clip is synthesised once at startup by filling sample
    /// buffers — a bright metallic parry "ping", a muffled block thud, a body
    /// hit, a beefier riposte, and an airy swing swoosh.
    ///
    /// Self-creating persistent service (like <see cref="Hitstop"/>). Master
    /// volume is already applied globally via <c>AudioListener.volume</c>; this
    /// additionally scales each play by <see cref="GameSettings.SfxVolume"/>, so
    /// both sliders in Settings affect combat audio.
    /// </summary>
    public class CombatAudio : MonoBehaviour
    {
        const int SampleRate = 44100;

        static CombatAudio _instance;

        AudioSource _source;
        AudioClip _swing, _parry, _block, _hit, _riposte;

        public static void Play(CombatSound sound, float volumeScale = 1f)
        {
            EnsureInstance();
            _instance.PlayInternal(sound, volumeScale);
        }

        static void EnsureInstance()
        {
            if (_instance != null)
                return;
            var go = new GameObject("[CombatAudio]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<CombatAudio>();
        }

        void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f; // 2D — feel, not positional

            _parry = Ping("Parry", 880f, 0.18f);
            _block = NoiseHit("Block", 0.12f, 1200f);
            _hit = Thump("Hit", 165f, 0.14f, heavy: false);
            _riposte = Thump("Riposte", 110f, 0.30f, heavy: true);
            _swing = Whoosh("Swing", 0.10f, 3200f);
        }

        void PlayInternal(CombatSound sound, float volumeScale)
        {
            var clip = Pick(sound);
            if (clip == null || _source == null)
                return;

            float sfx = GameApp.Instance != null ? GameApp.Instance.Settings.SfxVolume : 1f;
            _source.PlayOneShot(clip, Mathf.Clamp01(sfx) * volumeScale);
        }

        AudioClip Pick(CombatSound sound) => sound switch
        {
            CombatSound.Swing => _swing,
            CombatSound.Parry => _parry,
            CombatSound.Block => _block,
            CombatSound.Hit => _hit,
            CombatSound.Riposte => _riposte,
            _ => null,
        };

        // ---- Synthesis ---------------------------------------------------------
        // Each method fills a mono float buffer in [-1, 1] and wraps it in a clip.

        /// <summary>Bright metallic ping: fundamental + slightly inharmonic partials, fast decay.</summary>
        static AudioClip Ping(string name, float freq, float duration)
        {
            var data = new float[(int)(SampleRate * duration)];
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 14f);
                float s = Mathf.Sin(Tau * freq * t)
                        + 0.45f * Mathf.Sin(Tau * freq * 2.01f * t)   // inharmonic shimmer
                        + 0.22f * Mathf.Sin(Tau * freq * 2.99f * t);
                data[i] = s * env * 0.32f;
            }
            return ToClip(name, data);
        }

        /// <summary>Low body hit: a pitch-dropping sine with a click transient.</summary>
        static AudioClip Thump(string name, float freq, float duration, bool heavy)
        {
            var data = new float[(int)(SampleRate * duration)];
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * (heavy ? 9f : 16f));
                float pitch = freq * (1f + (heavy ? 2.5f : 1.8f) * Mathf.Exp(-t * 30f));
                float tone = Mathf.Sin(Tau * pitch * t);
                float click = (Random.value * 2f - 1f) * Mathf.Exp(-t * 45f) * 0.4f;
                data[i] = (tone * 0.85f + click) * env * (heavy ? 0.9f : 0.7f);
            }
            return ToClip(name, data);
        }

        /// <summary>Muffled impact: low-passed noise with a quick decay.</summary>
        static AudioClip NoiseHit(string name, float duration, float tone)
        {
            var data = new float[(int)(SampleRate * duration)];
            float lp = 0f, a = LowPassCoeff(tone);
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / SampleRate;
                lp = Mathf.Lerp(lp, Random.value * 2f - 1f, a);
                data[i] = lp * Mathf.Exp(-t * 22f) * 0.5f;
            }
            return ToClip(name, data);
        }

        /// <summary>Airy swing swoosh: brighter noise that swells then fades.</summary>
        static AudioClip Whoosh(string name, float duration, float tone)
        {
            var data = new float[(int)(SampleRate * duration)];
            float lp = 0f, a = LowPassCoeff(tone);
            for (int i = 0; i < data.Length; i++)
            {
                float frac = (float)i / data.Length;
                lp = Mathf.Lerp(lp, Random.value * 2f - 1f, a);
                data[i] = lp * Mathf.Sin(Mathf.PI * frac) * 0.22f; // sin swell in/out
            }
            return ToClip(name, data);
        }

        const float Tau = 2f * Mathf.PI;

        static float LowPassCoeff(float tone) => Mathf.Clamp01(tone / SampleRate * 6f);

        static AudioClip ToClip(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
