using UnityEngine;

namespace ParryArena.Arena
{
    /// <summary>
    /// The enemy's posture/stagger value (plan's Lies-of-P model). Perfect
    /// parries and landed hits fill it via <see cref="ActorCombat.AddStagger"/>;
    /// when full the enemy is staggered and open to a riposte. It bleeds back
    /// down after a short grace period so a drawn-out, passive fight doesn't
    /// trickle it to full.
    /// </summary>
    public class StaggerMeter : MonoBehaviour
    {
        [SerializeField] float _max = 100f;
        [SerializeField] float _decayPerSecond = 8f;
        [SerializeField] float _decayDelay = 1.5f;

        float _current;
        float _timeSinceGain;

        public float Max => _max;
        public float Current => _current;
        public float Fraction => _max > 0f ? _current / _max : 0f;
        public bool IsFull => _current >= _max;

        public event System.Action<StaggerMeter> Changed;

        public void Add(float amount)
        {
            if (amount <= 0f)
                return;
            _current = Mathf.Min(_max, _current + amount);
            _timeSinceGain = 0f;
            Changed?.Invoke(this);
        }

        // Named ResetMeter (not Reset) to avoid Unity's MonoBehaviour.Reset message.
        public void ResetMeter()
        {
            _current = 0f;
            Changed?.Invoke(this);
        }

        void Update()
        {
            if (_current <= 0f)
                return;

            _timeSinceGain += Time.deltaTime;
            if (_timeSinceGain >= _decayDelay)
            {
                _current = Mathf.Max(0f, _current - _decayPerSecond * Time.deltaTime);
                Changed?.Invoke(this);
            }
        }
    }
}
