using ParryArena.Data;
using UnityEngine;

namespace ParryArena.Arena
{
    /// <summary>
    /// The single, data-driven enemy brain (plan M3). Every frame it perceives →
    /// decides → acts off its <see cref="EnemyDefinition"/>'s Behaviour values:
    /// hold spacing, close the gap, strafe, attack from its moveset on an
    /// aggression-based cadence, and punish the player's openings. Defence is
    /// Sekiro-style auto-guard: while NOT mid-swing of its own, it deflects/blocks
    /// a large majority of the player's attacks instantly (no reaction delay — so
    /// fast spam doesn't slip through); the only true opening is while it's
    /// attacking, so the player must read it and parry instead of mashing. All
    /// opponents share this class and differ only by data: a passive training
    /// dummy and an aggressive, guard-happy brawler are the same brain, tuned
    /// apart. Defence drives the shared <see cref="ActorCombat"/> FSM exactly as
    /// the player does, and resolves through the same symmetric
    /// <see cref="CombatResolver"/> (so the enemy parrying the player interrupts
    /// the player just like the reverse).
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] float _turnSpeed = 8f;
        [Tooltip("Seeded from the EnemyDefinition on spawn; drag it in Play mode to tune the fight live.")]
        [SerializeField] float _maxHealth = 150f;

        const float WallClearance = 2f;        // how far inside the arena walls the body stays
        const float MinAttackInterval = 0.5f;  // at Aggression 1
        const float MaxAttackInterval = 2.0f;  // at Aggression 0
        const float StrafeFlipInterval = 2.5f;
        const float ThreatRange = 2.6f;        // within this, the player's swing can reach us
        const float MaxGuardHold = 1.2f;       // safety cap on how long it holds a guard
        const float DodgeSpeed = 8f;
        const float DodgeChance = 0.2f;        // chance a defence is a dodge instead of a guard

        EnemyDefinition _definition;
        Transform _target;
        ActorCombat _combat;
        ActorCombat _targetCombat;   // the player's FSM, for reading openings + telegraphs

        float _attackCooldown;
        float _strafeTimer;
        float _strafeSign = 1f;
        float _openingTimer;         // how long the player has been vulnerable
        bool _threatActive;          // the player is committing an attack at us right now
        bool _reactedToThreat;       // decided defend/eat once per incoming attack
        bool _defending;             // chose to hold a guard for this attack
        float _guardTimer;           // safety cap remaining on the held guard
        Vector3 _dodgeDir = Vector3.back;

        public Health Health { get; private set; }
        public EnemyDefinition Definition => _definition;

        public void Configure(EnemyDefinition definition, Transform target, ActorCombat combat, ActorCombat targetCombat)
        {
            _definition = definition;
            _target = target;
            _combat = combat;
            _targetCombat = targetCombat;
            Health = GetComponent<Health>();
            _maxHealth = definition.MaxHealth;   // seed the live-tunable knob from data
            Health.Init(_maxHealth);
            _attackCooldown = AttackInterval();
        }

        void Update()
        {
            if (_target == null || Health.IsDead)
                return;

            // Live tuning: re-apply (and refill) if max health was dragged in Play.
            if (!Mathf.Approximately(_maxHealth, Health.Max))
                Health.Init(_maxHealth);

            float dt = Time.deltaTime;

            Vector3 toTarget = _target.position - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            Vector3 forward = distance > 0.001f ? toTarget / distance : transform.forward;

            FaceTarget(forward);

            _openingTimer = PlayerIsOpen() ? _openingTimer + dt : 0f;
            UpdateThreat(distance, dt);
            ManageBlockHold(dt);

            // Knockback / scripted impulse always applies; the dodge slides the
            // body while invulnerable; voluntary motion + attacks + the decision to
            // defend only fire while the FSM lets the body act.
            Vector3 velocity = _combat != null ? _combat.ImpulseVelocity : Vector3.zero;
            if (_combat != null && _combat.State == CombatState.Dodge)
                velocity += _dodgeDir * (DodgeSpeed * Mathf.Clamp01(1f - _combat.DodgeNormalizedTime));

            if (_combat != null && _combat.IsIdle && !_defending && !TryDefend(forward))
            {
                velocity += Locomotion(distance, forward, dt);
                TryAttack(distance, dt);
            }

            transform.position = ClampToArena(transform.position + velocity * dt);
        }

        // ---- Perceive ----------------------------------------------------------

        /// <summary>The player is open if it's recovering from a whiff or already reeling.</summary>
        bool PlayerIsOpen()
        {
            if (_targetCombat == null)
                return false;
            var state = _targetCombat.State;
            return state == CombatState.Recovery
                || state == CombatState.Hitstun
                || state == CombatState.Staggered;
        }

        /// <summary>The player is threatening once it commits a swing (charging or active).</summary>
        bool PlayerIsThreatening()
        {
            if (_targetCombat == null)
                return false;
            var state = _targetCombat.State;
            return state == CombatState.Charge || state == CombatState.Active;
        }

        void UpdateThreat(float distance, float dt)
        {
            _threatActive = distance <= ThreatRange && PlayerIsThreatening();
            if (!_threatActive)
            {
                _reactedToThreat = false;   // ready to react to the next attack
                _defending = false;         // drop the guard once the attack is over
            }
        }

        /// <summary>
        /// Holds the raised guard for the life of the player's attack (Parry/Block
        /// are non-idle states), so it catches fast and slow swings alike. The
        /// front of the hold is a parry window (deflect); the rest blocks (chip).
        /// </summary>
        void ManageBlockHold(float dt)
        {
            if (_combat == null)
                return;
            if (_defending && _guardTimer > 0f)
            {
                _guardTimer -= dt;
                _combat.SetGuardHeld(true);
            }
            else
            {
                _defending = false;
                _combat.SetGuardHeld(false);
            }
        }

        // ---- Act ---------------------------------------------------------------

        /// <summary>
        /// Sekiro-style auto-guard: the instant the player commits an attack (and
        /// we aren't mid-swing of our own), decide once per attack to defend. With
        /// chance <c>Defensiveness</c> we guard, then <c>ParrySkill</c> picks a
        /// committed tap-parry (tight, baitable by slow/charged swings) vs. a held
        /// guard that catches anything (deflect at the front, block after); a small
        /// share dodge instead. No reaction-delay — that's what let fast spam
        /// through; the only true opening is while we're attacking. Returns true if
        /// it committed a defence (so it skips moving/attacking this frame).
        /// </summary>
        bool TryDefend(Vector3 forward)
        {
            if (_reactedToThreat || !_threatActive)
                return false;
            _reactedToThreat = true;

            if (Random.value > _definition.Defensiveness)
                return false;   // saw it coming but chose to eat / trade this one

            if (Random.value < DodgeChance)
            {
                if (_combat.RequestDodge())
                    _dodgeDir = -forward;            // roll back out of the swing
                return true;
            }

            if (Random.value < _definition.ParrySkill)
            {
                _combat.RequestParry();              // committed deflect — fast, but baitable
            }
            else if (_combat.RequestParry())
            {
                _defending = true;                   // hold the guard for the whole attack
                _guardTimer = MaxGuardHold;
            }
            return true;
        }

        Vector3 Locomotion(float distance, Vector3 forward, float dt)
        {
            float speed = _definition.MoveSpeed;
            if (speed <= 0f)
                return Vector3.zero;

            if (distance > _definition.AttackRange)
                return forward * speed;                            // close the gap
            if (distance < _definition.PreferredRange * 0.8f)
                return -forward * speed;                           // too close, give ground

            // In the pocket: circle the player, flipping direction periodically.
            _strafeTimer -= dt;
            if (_strafeTimer <= 0f)
            {
                _strafeSign = -_strafeSign;
                _strafeTimer = StrafeFlipInterval;
            }
            return Vector3.Cross(Vector3.up, forward) * (_strafeSign * speed * 0.6f);
        }

        void TryAttack(float distance, float dt)
        {
            if (_attackCooldown > 0f)
                _attackCooldown -= dt;

            if (distance > _definition.AttackRange)
                return;

            var attacks = _definition.Attacks;
            if (attacks == null || attacks.Length == 0)
                return;

            // Swing on cadence, or right away to punish a reaction-confirmed opening.
            bool punish = _openingTimer >= _definition.ReactionTime;
            if (_attackCooldown > 0f && !punish)
                return;

            if (_combat.RequestAttack(PickAttack(attacks, punish)))
                _attackCooldown = AttackInterval();
        }

        /// <summary>Punishes use the quickest attack so it lands; otherwise mix it up for varied speeds.</summary>
        static int PickAttack(AttackDefinition[] attacks, bool punish)
        {
            if (!punish)
                return Random.Range(0, attacks.Length);

            int fastest = 0;
            for (int i = 1; i < attacks.Length; i++)
                if (attacks[i].Windup < attacks[fastest].Windup)
                    fastest = i;
            return fastest;
        }

        float AttackInterval()
        {
            float baseInterval = Mathf.Lerp(MaxAttackInterval, MinAttackInterval,
                _definition != null ? _definition.Aggression : 0.5f);
            float variance = _definition != null ? _definition.AttackIntervalVariance : 0f;
            return baseInterval * Random.Range(1f - variance, 1f + variance);
        }

        void FaceTarget(Vector3 forward)
        {
            if (_combat != null && (_combat.IsInHitstun || _combat.IsStaggered))
                return;                                            // don't track while reeling
            transform.FaceDirection(forward, _turnSpeed);
        }

        static Vector3 ClampToArena(Vector3 p)
        {
            float limit = ArenaStage.HalfExtent - WallClearance;
            p.x = Mathf.Clamp(p.x, -limit, limit);
            p.z = Mathf.Clamp(p.z, -limit, limit);
            return p;
        }
    }
}
