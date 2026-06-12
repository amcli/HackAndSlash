using System.Collections;
using UnityEngine;

namespace ParryArena.Arena
{
    /// <summary>
    /// Brief global freeze-frame on impactful events (parries). The single
    /// biggest feel multiplier for the effort (per the plan). Self-creating
    /// persistent runner; <see cref="CancelAll"/> lets the pause menu reclaim
    /// control of Time.timeScale so the two never fight.
    /// </summary>
    public class Hitstop : MonoBehaviour
    {
        static Hitstop _instance;
        Coroutine _running;

        public static void Freeze(float realSeconds)
        {
            EnsureInstance();
            _instance.Begin(realSeconds);
        }

        public static void CancelAll()
        {
            if (_instance != null)
                _instance.Stop();
        }

        static void EnsureInstance()
        {
            if (_instance != null)
                return;
            var go = new GameObject("[Hitstop]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<Hitstop>();
        }

        void Begin(float seconds)
        {
            if (_running != null || Time.timeScale <= 0f) // already frozen, or paused — don't interfere
                return;
            _running = StartCoroutine(Run(seconds));
        }

        void Stop()
        {
            if (_running != null)
            {
                StopCoroutine(_running);
                _running = null;
            }
        }

        IEnumerator Run(float seconds)
        {
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(seconds);
            // Only resume if nobody else (e.g. the pause menu) is now holding the freeze.
            if (Time.timeScale == 0f)
                Time.timeScale = 1f;
            _running = null;
        }
    }
}
