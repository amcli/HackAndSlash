using UnityEngine;

namespace ParryArena.Arena
{
    /// <summary>
    /// Reusable hit-points component shared by the player and enemies. The
    /// combat systems in later milestones route all damage through
    /// <see cref="TakeDamage"/>; UI subscribes to <see cref="Changed"/>.
    /// </summary>
    public class Health : MonoBehaviour
    {
        public float Max { get; private set; } = 1f;
        public float Current { get; private set; } = 1f;

        public bool IsDead => Current <= 0f;
        public float Fraction => Max > 0f ? Current / Max : 0f;

        /// <summary>Raised on any change to current/max (including <see cref="Init"/>).</summary>
        public event System.Action<Health> Changed;
        public event System.Action<Health> Died;

        public void Init(float max)
        {
            Max = Mathf.Max(1f, max);
            Current = Max;
            Changed?.Invoke(this);
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f)
                return;

            Current = Mathf.Max(0f, Current - amount);
            Changed?.Invoke(this);
            if (IsDead)
                Died?.Invoke(this);
        }
    }
}
