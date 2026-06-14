using ParryArena.Data;
using UnityEngine;

namespace ParryArena.Arena
{
    /// <summary>
    /// Greybox opponent: faces the player and throws a telegraphed attack on a
    /// loop (plan M0) via the shared <see cref="ActorCombat"/> FSM, giving the
    /// player something to parry. The data-driven one-brain AI from the plan
    /// (M3) replaces this loop later while reusing the same FSM and
    /// <see cref="EnemyDefinition"/>.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] float _turnSpeed = 6f;
        [SerializeField] float _attackInterval = 1.5f;
        [Tooltip("Seeded from the EnemyDefinition on spawn; drag it in Play mode to tune the fight live.")]
        [SerializeField] float _maxHealth = 150f;

        EnemyDefinition _definition;
        Transform _target;
        ActorCombat _combat;
        float _attackCooldown;

        public Health Health { get; private set; }
        public EnemyDefinition Definition => _definition;

        public void Configure(EnemyDefinition definition, Transform target, ActorCombat combat)
        {
            _definition = definition;
            _target = target;
            _combat = combat;
            Health = GetComponent<Health>();
            _maxHealth = definition.MaxHealth;   // seed the live-tunable knob from data
            Health.Init(_maxHealth);
            _attackCooldown = _attackInterval;
        }

        void Update()
        {
            if (_target == null || Health.IsDead)
                return;

            // Live tuning: re-apply (and refill) if max health was dragged in the
            // Inspector during Play. No-ops once Health.Max matches the field again.
            if (!Mathf.Approximately(_maxHealth, Health.Max))
                Health.Init(_maxHealth);

            if (_combat != null)
                transform.position += _combat.ImpulseVelocity * Time.deltaTime;

            FacePlayer();

            // Only count down / start a swing while idle, so the timer is the gap
            // between attacks rather than overlapping them.
            if (_combat != null && _combat.IsIdle)
            {
                _attackCooldown -= Time.deltaTime;
                if (_attackCooldown <= 0f)
                {
                    _combat.RequestAttack();
                    _attackCooldown = _attackInterval;
                }
            }
        }

        void FacePlayer()
        {
            Vector3 toTarget = _target.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0001f)
                return;

            var look = Quaternion.LookRotation(toTarget, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, _turnSpeed * Time.deltaTime);
        }
    }
}
