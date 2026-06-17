using ParryArena.Data;
using UnityEngine;

namespace ParryArena.Arena
{
    /// <summary>
    /// The per-actor combat state machine, shared by the player and enemies
    /// (plan: "one FSM per actor"). Owns the swing, the unified guard
    /// (parry window → held block), the dodge with i-frames, hitstun/knockback,
    /// stamina, the charged heavy strike, and the staggered state.
    ///
    /// The player attacks with a fixed-timing combo (LMB per hit, see
    /// <see cref="RequestComboHit"/>); enemies use a timed windup
    /// (<see cref="RequestAttack"/>). A held charge (<see cref="RequestChargeStart"/> /
    /// <see cref="ReleaseCharge"/>) remains available for an optional heavy strike.
    /// Damage/stagger for each swing are pushed to the <see cref="Hitbox"/> at the active frame.
    ///
    /// Resolution priority lives in <see cref="CombatResolver"/>:
    /// parry > dodge i-frames > block > riposte (vs staggered) > clean hit.
    /// </summary>
    public class ActorCombat : MonoBehaviour
    {
        [Header("Light combo")]
        [SerializeField] float _comboWindow = 0.7f; // re-press within this to continue the string; else it resets

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

        [Header("Lunge")]
        [Tooltip("A swing's step-in won't carry the body closer to its target than this, so chaining attacks doesn't slide it through or past the opponent. Roughly striking distance.")]
        [SerializeField] float _lungeStopDistance = 1.6f;

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

        [Header("Foresight slash (read → counter)")]
        [SerializeField] float _foresightStartup = 0.05f;        // raise into the ready stance
        [SerializeField] float _foresightWindow = 0.25f;         // counter window: absorb a hit here
        [SerializeField] float _foresightStaminaCost = 20f;
        [SerializeField] float _foresightCooldown = 1.2f;
        [SerializeField] float _foresightCounterDamageMultiplier = 3.0f;
        [SerializeField] float _foresightCounterStaggerMultiplier = 3.0f;
        [SerializeField] float _foresightBackstepSpeed = 7f;     // snappy hop back so the read reads clearly
        [SerializeField] float _foresightLungeSpeed = 11f;       // lunge forward into the counter to re-close
        [SerializeField] float _foresightSensorHeight = 1f;      // body-centre offset for the phantom (matches the hurtbox)
        [Tooltip("Read-sensor box, deliberately bigger than the body so a lunging blade reliably passes through it; it only absorbs during the counter window, so a generous box can't cause stray hits.")]
        [SerializeField] Vector3 _foresightSensorSize = new Vector3(1.8f, 2.4f, 1.8f);

        [Header("Staggered (when this actor is broken)")]
        [SerializeField] float _staggerDuration = 2.5f;

        [Header("Arm articulation (procedural elbow + wrist)")]
        [Tooltip("Elbow flex (degrees) at full windup; the arm coils back, then snaps straight on the active strike.")]
        [SerializeField] float _cockedElbowBend = 95f;
        [Tooltip("Wrist lay-back (degrees) at full windup; the blade trails, then whips through on the strike.")]
        [SerializeField] float _cockedWristBend = 40f;
        [Tooltip("How quickly the elbow/wrist track their target (higher = snappier).")]
        [SerializeField] float _armBendLerp = 18f;

        [Header("Blade poses (pivot Euler degrees; blade points +Z at rest)")]
        [SerializeField] Vector3 _restPose = new Vector3(38f, -8f, 0f);       // arm low and forward (the elbow bend lifts the blade back toward level)
        [SerializeField] Vector3 _guardPose = new Vector3(-8f, 28f, 10f);     // blade held in front, angled diagonally up to the side
        [SerializeField] Vector3 _staggeredPose = new Vector3(85f, 10f, 0f); // slumped, blade down

        /// <summary>Enemy attacks colour the blade during windup so the swing is readable.</summary>
        public bool TelegraphWindup;

        static readonly Color ParryColor = new Color(0.40f, 0.72f, 1.00f);
        static readonly Color TelegraphColor = new Color(0.95f, 0.25f, 0.20f);
        static readonly Color ChargeColor = new Color(1.00f, 0.82f, 0.35f);

        WeaponRig _rig;
        Hitbox _hitbox;
        StaggerMeter _stagger;
        public Health Health { get; private set; }

        CombatState _state = CombatState.Idle;
        float _timer;
        AttackDefinition[] _moveset;
        AttackDefinition _currentAttack;
        int _comboStep;        // index of the next attack in the combo string
        int _swingingComboIndex = -1; // which moveset entry the current swing is (for syncing the attack clip)
        float _comboExpiry;    // unscaled time after which the string lapses back to the start
        float _swWindup, _swActive, _swRecovery; // current swing's phase durations (combo hits override these from the clip)
        float[] _comboWindup, _comboActive, _comboRecovery; // per-hit timings derived from the combo clip's breakpoints
        Vector3 _currentPose;
        Vector3 _recoveryStartPose;
        float _flashTimer;
        Color _bladeBaseColor = Color.white;

        bool _guardHeld;
        Vector3 _impulseVelocity; // decaying scripted body motion: knockback, foresight backstep/lunge
        bool _lunging;            // a forward step-in is in flight (clamped at striking distance)
        Transform _lungeTarget;   // opponent, so the lunge knows when to stop
        const float IdleArmExtension = 0.72f; // resting flex (0 = coiled, 1 = straight); stances stay mostly extended so the pose aims the blade, while swings still coil to 0
        float _armExtension = IdleArmExtension;
        float _dodgeCooldownRemaining;
        float _foresightCooldownRemaining;
        bool _foresightCounter; // the next EnterActive is an empowered foresight counter
        GameObject _foresightSensor; // phantom hurtbox parked at the trigger spot during the read
        TrailRenderer _dodgeTrail;

        float _chargeFractionForSwing;

        // Stamina (owned here so attack/block/dodge costs live in one place).
        bool _usesStamina;
        float _maxStamina = 100f;
        float _stamina = 100f;

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

        /// <summary>True during the foresight slash's counter window — a hit landed here is absorbed.</summary>
        public bool IsInForesightWindow =>
            _state == CombatState.Foresight &&
            _timer >= _foresightStartup &&
            _timer < _foresightStartup + _foresightWindow;

        public float Stamina => _stamina;
        public float MaxStamina => _maxStamina;
        public float DodgeNormalizedTime =>
            _state == CombatState.Dodge ? Mathf.Clamp01(_timer / _dodgeDuration) : 0f;
        /// <summary>Decaying scripted body velocity the controller applies (knockback, foresight backstep/lunge).</summary>
        public Vector3 ImpulseVelocity => _impulseVelocity;

        /// <summary>True while an empowered foresight counter swing is connecting — used to give its hit a special look.</summary>
        public bool IsForesightCounter => _foresightCounter && _state == CombatState.Active;

        /// <summary>True while a swing plays out (windup/active/recovery) — drives the attack animation.</summary>
        public bool IsSwinging =>
            _state == CombatState.Windup || _state == CombatState.Active || _state == CombatState.Recovery;

        /// <summary>Which moveset entry the current swing is (0-based), or -1 when not swinging.</summary>
        public int ComboAnimIndex => IsSwinging ? _swingingComboIndex : -1;

        /// <summary>Progress 0..1 across the current swing's windup+active+recovery, to sync the attack clip.</summary>
        public float SwingProgress01
        {
            get
            {
                if (!IsSwinging || _currentAttack == null)
                    return 0f;
                float w = _swWindup, a = _swActive, r = _swRecovery;
                float elapsed =
                    _state == CombatState.Windup ? _timer :
                    _state == CombatState.Active ? w + _timer :
                    w + a + _timer;
                return Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, w + a + r));
            }
        }

        public void Configure(WeaponRig rig, Hitbox hitbox, Health health)
        {
            _rig = rig;
            _hitbox = hitbox;
            Health = health;

            if (_rig != null && _rig.BladeRenderer != null)
                _bladeBaseColor = _rig.BladeRenderer.material.color;

            ApplyPose(_restPose);
            _armExtension = IdleArmExtension;
            UpdateArmBend();
            if (_hitbox != null)
                _hitbox.Deactivate();
            if (_rig != null && _rig.Trail != null)
                _rig.Trail.emitting = false;
        }

        public void ConfigureStamina(bool usesStamina, float maxStamina)
        {
            _usesStamina = usesStamina;
            _maxStamina = Mathf.Max(1f, maxStamina);
            _stamina = _maxStamina;
        }

        /// <summary>The ordered attacks this actor swings (light combo for the player, loop for the AI).</summary>
        public void SetMoveset(AttackDefinition[] moveset)
        {
            _moveset = moveset;
            _comboStep = 0;
        }

        /// <summary>
        /// Per-combo-hit phase durations derived from the attack clip's breakpoints
        /// (set by PlayerAnimation), so each combo swing lasts exactly its slash's
        /// length and the clip plays at its natural rhythm instead of being time-warped.
        /// </summary>
        public void SetComboTimings(float[] windup, float[] active, float[] recovery)
        {
            _comboWindup = windup;
            _comboActive = active;
            _comboRecovery = recovery;
        }

        public void SetDodgeTrail(TrailRenderer trail) => _dodgeTrail = trail;
        public void SetStaggerMeter(StaggerMeter meter) => _stagger = meter;

        /// <summary>The opponent this actor lunges toward — so a swing's step-in stops at striking distance instead of sliding through.</summary>
        public void SetTarget(Transform target) => _lungeTarget = target;

        // ---- Input / requests --------------------------------------------------

        /// <summary>
        /// Timed-windup attack used by AI: swings a chosen moveset entry directly
        /// (no player-style combo chaining), so the brain controls which attack —
        /// and thus the swing speed — each time. The player uses charge instead.
        /// </summary>
        public bool RequestAttack(int attackIndex)
        {
            if (!HasMoveset || _state != CombatState.Idle)
                return false;
            var attack = _moveset[Mathf.Clamp(attackIndex, 0, _moveset.Length - 1)];
            if (!SpendStamina(attack.StaminaCost))
                return false;

            BeginSwing(attack);
            _state = CombatState.Windup;
            return true;
        }

        /// <summary>Player presses LMB: wind the blade up and start charging.</summary>
        public bool RequestChargeStart()
        {
            // From idle, or cancelling a mid-combo recovery to chain the next hit.
            if (!HasMoveset || (_state != CombatState.Idle && !CanComboCancel()))
                return false;
            if (!SpendStamina(CurrentComboAttack().StaminaCost))
                return false;

            BeginComboSwing();
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

        /// <summary>
        /// Player presses LMB: swing the current combo step on fixed, clip-synced
        /// timing (windup → active → recovery) and advance the string. Re-pressing
        /// during a hit's recovery chains the next; otherwise the combo lapses. The
        /// model plays the matching slash from its 3-hit combo clip (see PlayerAnimation).
        /// </summary>
        public bool RequestComboHit()
        {
            if (!HasMoveset || (_state != CombatState.Idle && !CanComboCancel()))
                return false;
            if (!SpendStamina(CurrentComboAttack().StaminaCost))
                return false;

            BeginComboSwing();
            _state = CombatState.Windup;
            return true;
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

        /// <summary>
        /// Foresight slash: a read. Enter a brief counter window (see
        /// <see cref="IsInForesightWindow"/>); absorbing a hit there fires the
        /// empowered counter via <see cref="OnForesightSuccess"/>, otherwise it
        /// resolves into an ordinary slash. Allowed from idle or out of a guard so
        /// the RMB→LMB command can cancel the just-started parry.
        /// </summary>
        public bool RequestForesightSlash()
        {
            // Todo - Change into state machine
            if (!HasMoveset)
                return false;
            if (_state != CombatState.Idle && _state != CombatState.Parry && _state != CombatState.Block)
                return false;
            if (_foresightCooldownRemaining > 0f)
                return false;
            if (!SpendStamina(_foresightStaminaCost))
                return false;

            _state = CombatState.Foresight;
            _timer = 0f;
            _foresightCounter = false;
            _foresightCooldownRemaining = _foresightCooldown;
            EndSwingVisuals();
            ApplyPose(_restPose);

            // Drop the phantom at our current (original) spot, then dash clear — the
            // read is judged against the phantom, not the body we're moving away.
            EnsureForesightSensor();
            if (_foresightSensor != null)
            {
                _foresightSensor.transform.SetPositionAndRotation(
                    transform.position + Vector3.up * _foresightSensorHeight, transform.rotation);
                _foresightSensor.SetActive(true);
            }

            ApplyImpulse(-transform.forward, _foresightBackstepSpeed); // snappy hop back = clear "it triggered"
            return true;
        }

        public void SetGuardHeld(bool held) => _guardHeld = held;

        // ---- Reactions ---------------------------------------------------------

        /// <summary>Called on this actor when it successfully parries an incoming attack.</summary>
        public void OnParrySuccess() => _flashTimer = 0.12f;

        /// <summary>
        /// Called on this actor when a hit is absorbed during its foresight
        /// window: flash, then immediately launch the empowered counter slash
        /// (an Active swing scaled by the counter multipliers, see
        /// <see cref="EnterActive"/>).
        /// </summary>
        public void OnForesightSuccess()
        {
            if (_state != CombatState.Foresight)
                return;
            _flashTimer = 0.16f;
            _foresightCounter = true;
            LungeForward(_foresightLungeSpeed); // lunge in so the counter re-closes the gap (capped at striking distance)
            BeginSwing(FinisherAttack());   // the counter always swings the dramatic finisher
            EnterActive();
        }

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

            _impulseVelocity = Vector3.MoveTowards(_impulseVelocity, Vector3.zero, _knockbackDecay * dt);

            // A forward step-in halts as soon as it reaches striking distance (or
            // peters out), so chaining swings can't walk the body through the target.
            if (_lunging && (_impulseVelocity.sqrMagnitude < 0.0001f || FlatDistanceToTarget() <= _lungeStopDistance))
            {
                _impulseVelocity = Vector3.zero;
                _lunging = false;
            }

            if (_dodgeCooldownRemaining > 0f)
                _dodgeCooldownRemaining -= dt;
            if (_foresightCooldownRemaining > 0f)
                _foresightCooldownRemaining -= dt;

            // Stamina regenerates except while actively guarding.
            if (_usesStamina && _state != CombatState.Block && _state != CombatState.Parry)
                _stamina = Mathf.Min(_maxStamina, _stamina + _staminaRegenPerSecond * dt);

            // The combo string lapses back to the first hit if you don't continue it.
            if (_state == CombatState.Idle && _comboStep != 0 && Time.unscaledTime > _comboExpiry)
                _comboStep = 0;

            switch (_state)
            {
                case CombatState.Charge:
                    ApplyPose(Vector3.Lerp(_restPose, _currentAttack.WindupPose, Smooth(_timer, _chargeRaiseTime)));
                    break;

                case CombatState.Windup:
                    ApplyPose(Vector3.Lerp(_restPose, _currentAttack.WindupPose, Smooth(_timer, _swWindup)));
                    if (_timer >= _swWindup)
                        EnterActive();
                    break;

                case CombatState.Active:
                    ApplyPose(Vector3.Lerp(_currentAttack.WindupPose, _currentAttack.ActiveEndPose, Smooth(_timer, _swActive)));
                    if (_timer >= _swActive)
                        EnterRecovery();
                    break;

                case CombatState.Recovery:
                    ApplyPose(Vector3.Lerp(_recoveryStartPose, _restPose, Smooth(_timer, _swRecovery)));
                    if (_timer >= _swRecovery)
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

                case CombatState.Foresight:
                    UpdateForesightPose();
                    // Window elapsed without absorbing a hit → the read whiffs
                    // into an ordinary slash (not a counter).
                    if (_timer >= _foresightStartup + _foresightWindow)
                    {
                        BeginComboSwing();   // wasted read → an ordinary slash
                        EnterActive();
                    }
                    break;
            }

            if (_dodgeTrail != null)
                _dodgeTrail.emitting = _state == CombatState.Dodge
                    || _state == CombatState.Foresight       // streak on the backstep
                    || IsForesightCounter;                   // and on the lunge

            // The phantom only lives during the read window.
            if (_foresightSensor != null && _foresightSensor.activeSelf && _state != CombatState.Foresight)
                _foresightSensor.SetActive(false);

            UpdateArmBend();
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

        void UpdateForesightPose()
        {
            // Raise into the cross-body ready stance, then hold it through the
            // counter window so the read is readable.
            if (_timer < _foresightStartup)
                ApplyPose(Vector3.Lerp(_restPose, _guardPose, Smooth(_timer, _foresightStartup)));
            else
                ApplyPose(_guardPose);
        }

        /// <summary>
        /// Procedural secondary motion layered on the pose-driven shoulder: the
        /// elbow coils through windup and snaps straight on the active strike while
        /// the wrist whips the blade through, so each swing has a natural articulated
        /// trajectory rather than a rigid sweep.
        /// </summary>
        void UpdateArmBend()
        {
            if (_rig == null || _rig.ClipDriven)
                return;

            _armExtension = Mathf.Lerp(_armExtension, TargetArmExtension(),
                1f - Mathf.Exp(-_armBendLerp * Time.deltaTime));

            float elbow = Mathf.Lerp(_cockedElbowBend, 0f, _armExtension);
            float wrist = Mathf.Lerp(_cockedWristBend, 0f, _armExtension);

            if (_rig.Elbow != null)
                _rig.Elbow.localRotation = _rig.RestElbowRotation * Quaternion.Euler(-elbow, 0f, 0f);
            if (_rig.Wrist != null)
                _rig.Wrist.localRotation = _rig.RestWristRotation * Quaternion.Euler(-wrist, 0f, 0f);
        }

        /// <summary>Target arm extension for the phase: coiled (0) at the windup peak, fully extended (1) at the end of the active strike, relaxed otherwise.</summary>
        float TargetArmExtension()
        {
            switch (_state)
            {
                case CombatState.Charge:
                    return Mathf.Lerp(IdleArmExtension, 0f, Smooth(_timer, _chargeRaiseTime));
                case CombatState.Windup:
                    return Mathf.Lerp(IdleArmExtension, 0f, Smooth(_timer, _swWindup));
                case CombatState.Active:
                    return Smooth(_timer, _swActive);          // snap straight through the strike
                case CombatState.Recovery:
                    return Mathf.Lerp(1f, IdleArmExtension, Smooth(_timer, _swRecovery));
                default:
                    return IdleArmExtension;   // idle / block / parry / foresight hold a mostly-extended guard
            }
        }

        bool HasMoveset => _moveset != null && _moveset.Length > 0;

        AttackDefinition CurrentComboAttack() => _moveset[Mathf.Clamp(_comboStep, 0, _moveset.Length - 1)];

        AttackDefinition FinisherAttack() => _moveset[_moveset.Length - 1];

        /// <summary>True while a started combo can be continued by cancelling the current recovery.</summary>
        bool CanComboCancel() =>
            _state == CombatState.Recovery && _comboStep != 0 && Time.unscaledTime <= _comboExpiry;

        /// <summary>Swing the current combo step and advance the string (the finisher loops back to the start).</summary>
        void BeginComboSwing()
        {
            _swingingComboIndex = Mathf.Clamp(_comboStep, 0, _moveset.Length - 1);
            var attack = _moveset[_swingingComboIndex];
            bool isFinisher = _comboStep >= _moveset.Length - 1;
            _comboStep = isFinisher ? 0 : _comboStep + 1;
            _comboExpiry = Time.unscaledTime + _comboWindow;
            BeginSwing(attack);
            ApplyComboTimingOverride(_swingingComboIndex);
        }

        void BeginSwing(AttackDefinition attack)
        {
            _currentAttack = attack;
            _swWindup = attack.Windup;
            _swActive = attack.Active;
            _swRecovery = attack.Recovery;
            _chargeFractionForSwing = 0f;
            _timer = 0f;
        }

        /// <summary>Replaces the current swing's phase durations with the combo clip's breakpoint-derived timings, so the FSM follows the animation's rhythm.</summary>
        void ApplyComboTimingOverride(int index)
        {
            if (_comboWindup == null || index < 0 || index >= _comboWindup.Length)
                return;
            _swWindup = _comboWindup[index];
            _swActive = _comboActive[index];
            _swRecovery = _comboRecovery[index];
        }

        void EnterActive()
        {
            _state = CombatState.Active;
            _timer = 0f;

            // Step into the swing so it closes the gap rather than needing the
            // target already inside the blade's arc — but only across the gap that's
            // left (see LungeForward). The foresight counter already applied its own
            // (stronger) lunge, so don't overwrite it.
            if (!_foresightCounter)
                LungeForward(_currentAttack.LungeSpeed);

            if (_hitbox != null)
            {
                // Foresight counter overrides the charge scaling with its own
                // (bigger) multipliers; otherwise damage scales with hold time.
                float damageMult = _foresightCounter
                    ? _foresightCounterDamageMultiplier
                    : Mathf.Lerp(1f, _heavyDamageMultiplier, _chargeFractionForSwing);
                float staggerMult = _foresightCounter
                    ? _foresightCounterStaggerMultiplier
                    : Mathf.Lerp(1f, _heavyStaggerMultiplier, _chargeFractionForSwing);
                _hitbox.SetSwing(_currentAttack.Damage * damageMult, _currentAttack.StaggerDamage * staggerMult,
                    _currentAttack.Parryable, _currentAttack.Blockable);
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
            _lunging = false;            // knockback owns the impulse now, not a lunge
            ApplyImpulse(direction, _knockbackSpeed);
        }

        /// <summary>Sets the decaying scripted body velocity (knockback, foresight backstep/lunge).</summary>
        void ApplyImpulse(Vector3 worldDirection, float speed)
        {
            worldDirection.y = 0f;
            if (worldDirection.sqrMagnitude > 0.0001f)
                _impulseVelocity = worldDirection.normalized * speed;
        }

        /// <summary>
        /// Step into a swing, but only across the gap that's actually left: skip it
        /// when already within striking distance, and cap the speed so the decaying
        /// glide settles around <see cref="_lungeStopDistance"/> instead of sliding
        /// through or past the target. The per-frame clamp in Update then halts it
        /// the instant it arrives (covering a target that closes in to meet it).
        /// </summary>
        void LungeForward(float speed)
        {
            if (speed <= 0f)
                return;
            float gap = FlatDistanceToTarget() - _lungeStopDistance;
            if (gap <= 0f)
                return;                                  // already in range — stay put
            // A decaying impulse travels v^2 / (2*decay); invert to cap it at the gap.
            float maxSpeed = Mathf.Sqrt(2f * _knockbackDecay * gap);
            ApplyImpulse(transform.forward, Mathf.Min(speed, maxSpeed));
            _lunging = true;
        }

        /// <summary>Flat (XZ) distance to the lunge target, or +infinity if none is set.</summary>
        float FlatDistanceToTarget()
        {
            if (_lungeTarget == null)
                return Mathf.Infinity;
            Vector3 d = _lungeTarget.position - transform.position;
            d.y = 0f;
            return d.magnitude;
        }

        void EnterIdle()
        {
            _state = CombatState.Idle;
            _timer = 0f;
            _chargeFractionForSwing = 0f;
            _foresightCounter = false;
            _lunging = false;
            ApplyPose(_restPose);
        }

        void EndSwingVisuals()
        {
            if (_hitbox != null)
                _hitbox.Deactivate();
            if (_rig != null && _rig.Trail != null)
                _rig.Trail.emitting = false;
        }

        /// <summary>
        /// Lazily creates this ability's detached phantom hurtbox the first time a
        /// foresight read is triggered, so the ability owns its own tooling and only
        /// an actor that actually uses it ever spawns one. Flagged so it only ever
        /// absorbs (during the counter window) and starts disabled; the read
        /// positions and enables it.
        /// </summary>
        void EnsureForesightSensor()
        {
            if (_foresightSensor != null || _hitbox == null)
                return;
            var go = new GameObject("ForesightSensor");
            var hurtbox = go.AddComponent<Hurtbox>();
            hurtbox.Configure(_hitbox.Team, Health, this, _foresightSensorSize, absorbOnly: true);
            go.SetActive(false);
            _foresightSensor = go;
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
            if (_rig == null || _rig.Pivot == null || _rig.ClipDriven)
                return; // clip-driven (model) swords follow the animated hand; the clip drives the swing
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
                            ? Color.Lerp(_bladeBaseColor, TelegraphColor, Smooth(_timer, _swWindup))
                            : _bladeBaseColor;
                        break;
                    case CombatState.Active:
                        if (_foresightCounter)
                            color = ParryColor;                  // empowered counter glows blue
                        else
                            color = TelegraphWindup
                                ? TelegraphColor
                                : Color.Lerp(_bladeBaseColor, ChargeColor, _chargeFractionForSwing);
                        break;
                    case CombatState.Foresight:
                        color = IsInForesightWindow
                            ? ParryColor
                            : Color.Lerp(_bladeBaseColor, ParryColor, 0.3f);
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
