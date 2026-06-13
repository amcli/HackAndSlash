using UnityEngine;

namespace ParryArena.Arena
{
    /// <summary>
    /// The per-actor combat state machine, shared by the player and enemies
    /// (plan: "one FSM per actor"). Owns the swing, the unified guard
    /// (parry window → held block), the dodge with i-frames, hitstun/knockback,
    /// stamina, the charged heavy strike, and the staggered state.
    ///
    /// The player attacks by charging (hold LMB → <see cref="RequestChargeStart"/>,
    /// release → <see cref="ReleaseCharge"/>); enemies use a timed windup
    /// (<see cref="RequestAttack"/>). Damage/stagger for each swing are pushed to
    /// the <see cref="Hitbox"/> at the active frame, scaled by charge.
    ///
    /// Resolution priority lives in <see cref="CombatResolver"/>:
    /// parry > dodge i-frames > block > riposte (vs staggered) > clean hit.
    /// </summary>
    public class ActorCombat : MonoBehaviour
    {
        [Header("Swing timing (seconds)")]
        [SerializeField] float _windup = 0.15f;
        [SerializeField] float _active = 0.12f;
        [SerializeField] float _recovery = 0.25f;

        [Header("Parry timing (seconds)")]
        [SerializeField] float _parryStartup = 0.04f;
        [SerializeField] float _parryActiveWindow = 0.22f;
        [SerializeField] float _parryRecovery = 0.30f;

        [Header("Dodge")]
        [SerializeField] float _dodgeDuration = 0.45f;
        [SerializeField] float _iFrameStart = 0.05f;
        [SerializeField] float _iFrameEnd = 0.30f;
        [SerializeField] float _dodgeCooldown = 1f;

        [Header("Hitstun / knockback")]
        [SerializeField] float _hitstunDuration = 0.30f;
        [SerializeField] float _knockbackSpeed = 4f;
        [SerializeField] float _knockbackDecay = 12f;

        [Header("Stamina")]
        [SerializeField] float _staminaRegenPerSecond = 22f;
        [SerializeField] float _dodgeStaminaCost = 28f;
        [SerializeField] float _blockStaminaDrainPerDamage = 1.6f;
        [SerializeField] float _blockChipFraction = 0.25f;

        [Header("Charged heavy strike")]
        [SerializeField] float _maxChargeTime = 1.5f;       // holding past this adds nothing
        [SerializeField] float _chargeRaiseTime = 0.18f;    // how fast the blade winds up
        [SerializeField] float _heavyDamageMultiplier = 2.2f;
        [SerializeField] float _heavyStaggerMultiplier = 2.5f;

        [Header("Staggered (when this actor is broken)")]
        [SerializeField] float _staggerDuration = 2.5f;

        [Header("Blade poses (pivot Euler degrees; blade points +Z at rest)")]
        [SerializeField] Vector3 _restPose = new Vector3(-20f, -10f, 0f);
        [SerializeField] Vector3 _guardPose = new Vector3(-30f, -70f, 35f);  // cross-body parry/block guard
        [SerializeField] Vector3 _staggeredPose = new Vector3(85f, 10f, 0f); // slumped, blade down

        /// <summary>Enemy attacks colour the blade during windup so the swing is readable.</summary>
        public bool TelegraphWindup;

        static readonly SwingProfile[] Swings =
        {
            new SwingProfile(new Vector3(-135f, 0f, 0f), new Vector3(55f, 0f, 0f)),    // overhead, top -> bottom
            new SwingProfile(new Vector3(-90f, 55f, 0f), new Vector3(40f, -55f, 0f)),  // diagonal, upper-right -> lower-left
            new SwingProfile(new Vector3(-10f, 80f, 0f), new Vector3(-10f, -80f, 0f)), // horizontal, right -> left
        };

        static readonly Color ParryColor = new Color(0.40f, 0.72f, 1.00f);
        static readonly Color TelegraphColor = new Color(0.95f, 0.25f, 0.20f);
        static readonly Color ChargeColor = new Color(1.00f, 0.82f, 0.35f);

        WeaponRig _rig;
        Hitbox _hitbox;
        StaggerMeter _stagger;
        public Health Health { get; private set; }

        CombatState _state = CombatState.Idle;
        float _timer;
        int _swingIndex;
        SwingProfile _swing;
        Vector3 _currentPose;
        Vector3 _recoveryStartPose;
        float _flashTimer;
        Color _bladeBaseColor = Color.white;

        bool _guardHeld;
        Vector3 _knockbackVelocity;
        float _dodgeCooldownRemaining;
        TrailRenderer _dodgeTrail;

        [SerializeField] float _attackDamage = 5f;
        float _attackStagger;
        float _chargeFractionForSwing;

        // Stamina (owned here so attack/block/dodge costs live in one place).
        bool _usesStamina;
        float _maxStamina = 100f;
        float _stamina = 100f;
        float _attackStaminaCost = 16f;

        public CombatState State => _state;
        public bool IsIdle => _state == CombatState.Idle;
        public bool IsBlocking => _state == CombatState.Block;
        public bool IsInHitstun => _state == CombatState.Hitstun;
        public bool IsStaggered => _state == CombatState.Staggered;
        public bool IsInvulnerable =>
            _state == CombatState.Dodge && _timer >= _iFrameStart && _timer < _iFrameEnd;

        public bool IsParrying =>
            _state == CombatState.Parry &&
            _timer >= _parryStartup &&
            _timer < _parryStartup + _parryActiveWindow;

        public float Stamina => _stamina;
        public float MaxStamina => _maxStamina;
        public float DodgeNormalizedTime =>
            _state == CombatState.Dodge ? Mathf.Clamp01(_timer / _dodgeDuration) : 0f;
        public Vector3 KnockbackVelocity => _knockbackVelocity;

        public void Configure(WeaponRig rig, Hitbox hitbox, Health health)
        {
            _rig = rig;
            _hitbox = hitbox;
            Health = health;

            if (_rig != null && _rig.BladeRenderer != null)
                _bladeBaseColor = _rig.BladeRenderer.material.color;

            ApplyPose(_restPose);
            if (_hitbox != null)
                _hitbox.Deactivate();
            if (_rig != null && _rig.Trail != null)
                _rig.Trail.emitting = false;
        }

        public void ConfigureStamina(bool usesStamina, float maxStamina, float attackCost)
        {
            _usesStamina = usesStamina;
            _maxStamina = Mathf.Max(1f, maxStamina);
            _stamina = _maxStamina;
            _attackStaminaCost = attackCost;
        }

        public void ConfigureOffense(float attackDamage, float attackStagger)
        {
            _attackDamage = attackDamage;
            _attackStagger = attackStagger;
        }

        public void SetSwingTimings(float windup, float active, float recovery)
        {
            _windup = windup;
            _active = active;
            _recovery = recovery;
        }

        public void SetDodgeTrail(TrailRenderer trail) => _dodgeTrail = trail;
        public void SetStaggerMeter(StaggerMeter meter) => _stagger = meter;

        // ---- Input / requests --------------------------------------------------

        /// <summary>Timed-windup attack used by AI. The player uses charge instead.</summary>
        public bool RequestAttack()
        {
            if (_state != CombatState.Idle)
                return false;
            if (!SpendStamina(_attackStaminaCost))
                return false;

            BeginSwing();
            _state = CombatState.Windup;
            return true;
        }

        /// <summary>Player presses LMB: wind the blade up and start charging.</summary>
        public bool RequestChargeStart()
        {
            if (_state != CombatState.Idle)
                return false;
            if (!SpendStamina(_attackStaminaCost))
                return false;

            BeginSwing();
            _state = CombatState.Charge;
            return true;
        }

        /// <summary>Player releases LMB: swing with damage/stagger scaled by how long it was held.</summary>
        public void ReleaseCharge()
        {
            if (_state != CombatState.Charge)
                return;
            _chargeFractionForSwing = Mathf.Clamp01(_timer / _maxChargeTime);
            EnterActive();
        }

        public bool RequestParry()
        {
            if (_state != CombatState.Idle && _state != CombatState.Block)
                return false;
            _state = CombatState.Parry;
            _timer = 0f;
            return true;
        }

        public bool RequestDodge()
        {
            if (_state != CombatState.Idle && _state != CombatState.Block)
                return false;
            if (_dodgeCooldownRemaining > 0f)
                return false;
            if (!SpendStamina(_dodgeStaminaCost))
                return false;

            _state = CombatState.Dodge;
            _timer = 0f;
            _dodgeCooldownRemaining = _dodgeCooldown;
            ApplyPose(_restPose);
            return true;
        }

        public void SetGuardHeld(bool held) => _guardHeld = held;

        // ---- Reactions ---------------------------------------------------------

        /// <summary>Called on this actor when it successfully parries an incoming attack.</summary>
        public void OnParrySuccess() => _flashTimer = 0.12f;

        /// <summary>Called on the attacker when its swing is parried — dump into recovery (basic stagger).</summary>
        public void OnGotParried()
        {
            if (_state != CombatState.Windup && _state != CombatState.Active && _state != CombatState.Charge)
                return;
            EndSwingVisuals();
            _recoveryStartPose = _currentPose;
            _state = CombatState.Recovery;
            _timer = 0f;
        }

        /// <summary>Called when a hit is blocked: chip damage + stamina drain, guard break at 0.</summary>
        public void OnBlocked(float damage)
        {
            if (Health != null)
                Health.TakeDamage(damage * _blockChipFraction);

            ScreenShake.Shake(0.2f);

            if (!_usesStamina)
                return;

            _stamina = Mathf.Max(0f, _stamina - damage * _blockStaminaDrainPerDamage);
            if (_stamina <= 0f)
                EnterHitstun(-transform.forward); // guard break: knocked back, briefly open
        }

        /// <summary>Called when a clean hit lands: enter hitstun and take knockback along the hit direction.</summary>
        public void OnHit(Vector3 fromDirection) => EnterHitstun(fromDirection);

        /// <summary>Adds to this actor's stagger meter (if any); fills → staggered.</summary>
        public void AddStagger(float amount)
        {
            if (_stagger == null || _state == CombatState.Staggered)
                return;
            if (Health != null && Health.IsDead)
                return;

            _stagger.Add(amount);
            if (_stagger.IsFull)
            {
                _stagger.ResetMeter();
                EnterStaggered();
            }
        }

        /// <summary>Called on a staggered actor when it eats a riposte — recoil out of the stagger.</summary>
        public void OnRiposted()
        {
            if (_state == CombatState.Staggered)
                EnterHitstun(-transform.forward);
        }

        // ---- Update / state machine -------------------------------------------

        void Update()
        {
            float dt = Time.deltaTime;
            _timer += dt;

            if (_flashTimer > 0f)
                _flashTimer -= Time.unscaledDeltaTime;

            _knockbackVelocity = Vector3.MoveTowards(_knockbackVelocity, Vector3.zero, _knockbackDecay * dt);

            if (_dodgeCooldownRemaining > 0f)
                _dodgeCooldownRemaining -= dt;

            // Stamina regenerates except while actively guarding.
            if (_usesStamina && _state != CombatState.Block && _state != CombatState.Parry)
                _stamina = Mathf.Min(_maxStamina, _stamina + _staminaRegenPerSecond * dt);

            switch (_state)
            {
                case CombatState.Charge:
                    ApplyPose(Vector3.Lerp(_restPose, _swing.Windup, Smooth(_timer, _chargeRaiseTime)));
                    break;

                case CombatState.Windup:
                    ApplyPose(Vector3.Lerp(_restPose, _swing.Windup, Smooth(_timer, _windup)));
                    if (_timer >= _windup)
                        EnterActive();
                    break;

                case CombatState.Active:
                    ApplyPose(Vector3.Lerp(_swing.Windup, _swing.ActiveEnd, Smooth(_timer, _active)));
                    if (_timer >= _active)
                        EnterRecovery();
                    break;

                case CombatState.Recovery:
                    ApplyPose(Vector3.Lerp(_recoveryStartPose, _restPose, Smooth(_timer, _recovery)));
                    if (_timer >= _recovery)
                        EnterIdle();
                    break;

                case CombatState.Parry:
                    UpdateParryPose();
                    float windowEnd = _parryStartup + _parryActiveWindow;
                    if (_timer >= windowEnd)
                    {
                        if (_guardHeld)
                            EnterBlock();
                        else if (_timer >= windowEnd + _parryRecovery)
                            EnterIdle();
                    }
                    break;

                case CombatState.Block:
                    ApplyPose(_guardPose);
                    if (!_guardHeld)
                        EnterIdle();
                    break;

                case CombatState.Dodge:
                    ApplyPose(_restPose);
                    if (_timer >= _dodgeDuration)
                        EnterIdle();
                    break;

                case CombatState.Hitstun:
                    if (_timer >= _hitstunDuration)
                        EnterIdle();
                    break;

                case CombatState.Staggered:
                    ApplyPose(_staggeredPose);
                    if (_timer >= _staggerDuration)
                        EnterIdle();
                    break;
            }

            if (_dodgeTrail != null)
                _dodgeTrail.emitting = _state == CombatState.Dodge;

            UpdateBladeColor();
        }

        void UpdateParryPose()
        {
            if (_timer < _parryStartup)
                ApplyPose(Vector3.Lerp(_restPose, _guardPose, Smooth(_timer, _parryStartup)));
            else if (_timer < _parryStartup + _parryActiveWindow)
                ApplyPose(_guardPose);
            else
                ApplyPose(Vector3.Lerp(_guardPose, _restPose, Smooth(_timer - _parryStartup - _parryActiveWindow, _parryRecovery)));
        }

        void BeginSwing()
        {
            _swing = Swings[_swingIndex];
            _swingIndex = (_swingIndex + 1) % Swings.Length;
            _chargeFractionForSwing = 0f;
            _timer = 0f;
        }

        void EnterActive()
        {
            _state = CombatState.Active;
            _timer = 0f;
            if (_hitbox != null)
            {
                float damage = _attackDamage * Mathf.Lerp(1f, _heavyDamageMultiplier, _chargeFractionForSwing);
                float stagger = _attackStagger * Mathf.Lerp(1f, _heavyStaggerMultiplier, _chargeFractionForSwing);
                _hitbox.SetSwingPower(damage, stagger);
                _hitbox.Activate();
            }
            if (_rig != null && _rig.Trail != null)
                _rig.Trail.emitting = true;

            CombatAudio.Play(CombatSound.Swing, 0.55f);
        }

        void EnterRecovery()
        {
            _state = CombatState.Recovery;
            _timer = 0f;
            _recoveryStartPose = _currentPose;
            EndSwingVisuals();
        }

        void EnterBlock()
        {
            _state = CombatState.Block;
            _timer = 0f;
            ApplyPose(_guardPose);
        }

        void EnterStaggered()
        {
            EndSwingVisuals();
            _state = CombatState.Staggered;
            _timer = 0f;
            ApplyPose(_staggeredPose);
        }

        void EnterHitstun(Vector3 direction)
        {
            EndSwingVisuals();
            ApplyPose(_restPose);
            _state = CombatState.Hitstun;
            _timer = 0f;

            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
                _knockbackVelocity = direction.normalized * _knockbackSpeed;
        }

        void EnterIdle()
        {
            _state = CombatState.Idle;
            _timer = 0f;
            _chargeFractionForSwing = 0f;
            ApplyPose(_restPose);
        }

        void EndSwingVisuals()
        {
            if (_hitbox != null)
                _hitbox.Deactivate();
            if (_rig != null && _rig.Trail != null)
                _rig.Trail.emitting = false;
        }

        bool SpendStamina(float cost)
        {
            if (!_usesStamina)
                return true;
            if (_stamina < cost)
                return false;
            _stamina -= cost;
            return true;
        }

        void ApplyPose(Vector3 euler)
        {
            _currentPose = euler;
            if (_rig != null && _rig.Pivot != null)
                _rig.Pivot.localRotation = _rig.RestLocalRotation * Quaternion.Euler(euler);
        }

        void UpdateBladeColor()
        {
            if (_rig == null || _rig.BladeRenderer == null)
                return;

            Color color;
            if (_flashTimer > 0f)
                color = Color.white;
            else
                switch (_state)
                {
                    case CombatState.Parry:
                        color = IsParrying ? ParryColor : _bladeBaseColor;
                        break;
                    case CombatState.Block:
                        color = Color.Lerp(_bladeBaseColor, ParryColor, 0.35f);
                        break;
                    case CombatState.Charge:
                        color = Color.Lerp(_bladeBaseColor, ChargeColor, Mathf.Clamp01(_timer / _maxChargeTime));
                        break;
                    case CombatState.Windup:
                        color = TelegraphWindup
                            ? Color.Lerp(_bladeBaseColor, TelegraphColor, Smooth(_timer, _windup))
                            : _bladeBaseColor;
                        break;
                    case CombatState.Active:
                        color = TelegraphWindup
                            ? TelegraphColor
                            : Color.Lerp(_bladeBaseColor, ChargeColor, _chargeFractionForSwing);
                        break;
                    default:
                        color = _bladeBaseColor;
                        break;
                }

            _rig.BladeRenderer.material.color = color;
        }

        static float Smooth(float t, float duration) =>
            Mathf.SmoothStep(0f, 1f, t / Mathf.Max(0.0001f, duration));
    }
}
