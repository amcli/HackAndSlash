using ParryArena.Data;
using UnityEngine;

namespace ParryArena.Arena
{
    /// <summary>
    /// Third-person locomotion on a CharacterController, plus combat input
    /// forwarded to the shared <see cref="ActorCombat"/> FSM (LMB swing, RMB
    /// hold-to-block / tap-to-parry, Space dodge).
    ///
    /// Movement basis depends on the camera: free mode is camera-relative; while
    /// locked on (<see cref="ThirdPersonCamera.IsLocked"/>) the player faces the
    /// target and strafes around it (A/D circle, W/S approach/retreat). Dodge and
    /// knockback velocities come from the FSM. Disabled by
    /// <see cref="ArenaController"/> while paused / after the match ends.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] float _moveSpeed = 5.5f;
        [SerializeField] float _rotationLerp = 14f;
        [SerializeField] float _gravity = -22f;
        [SerializeField] float _dodgeSpeed = 9f;
        [SerializeField] float _inputBuffer = 0.14f; // ~8 frames @60: presses near a state boundary still fire
        [SerializeField] float _commandStepGap = 0.3f; // max gap between the RMB→LMB foresight-slash steps

        CharacterController _controller;
        ThirdPersonCamera _cameraRig;
        ActorCombat _combat;

        float _verticalSpeed;
        Vector3 _dodgeDir = Vector3.forward;

        enum BufferedInput { None, Attack, Parry, Dodge }
        BufferedInput _buffered;
        float _bufferTimer;

        readonly InputSequenceDetector _commands = new();

        public Health Health { get; private set; }
        public float Stamina => _combat != null ? _combat.Stamina : 0f;
        public float MaxStamina => _combat != null ? _combat.MaxStamina : 1f;

        public void Configure(LoadoutDefinition loadout, ThirdPersonCamera cameraRig, ActorCombat combat)
        {
            _cameraRig = cameraRig;
            _combat = combat;

            _controller = GetComponent<CharacterController>();
            Health = GetComponent<Health>();
            Health.Init(loadout.MaxHealth);

            // Foresight slash: guard, then attack in quick succession. More
            // abilities are added simply by registering more sequences here.
            _commands.Register(Command.ForesightSlash,
                new[] { InputToken.Guard, InputToken.Attack }, _commandStepGap);
        }

        void Update()
        {
            if (_controller == null)
                return;

            _combat.SetGuardHeld(Input.GetMouseButton(1));

            Move();

            if (_bufferTimer > 0f)
                _bufferTimer -= Time.deltaTime;
            else
                _buffered = BufferedInput.None;

            // Releasing LMB always fires immediately (it only matters mid-charge);
            // the press-actions are buffered so they survive a state boundary.
            if (Input.GetMouseButtonUp(0))
                _combat.ReleaseCharge();
            if (Input.GetMouseButtonDown(0))
                OnAttackPressed();
            if (Input.GetMouseButtonDown(1))
            {
                _commands.Feed(InputToken.Guard);  // first half of the foresight command
                Queue(BufferedInput.Parry);
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _commands.Feed(InputToken.Dodge);
                Queue(BufferedInput.Dodge);
            }

            TryConsumeBuffer();
        }

        // ---- Command + buffering -----------------------------------------------

        void OnAttackPressed()
        {
            // LMB can complete the RMB→LMB foresight command. If it does (and the
            // move actually starts), it cancels the just-started parry and we skip
            // the normal attack; otherwise LMB is an ordinary (charge) attack.
            bool foresight = _commands.Feed(InputToken.Attack) == Command.ForesightSlash
                             && _combat.RequestForesightSlash();
            if (!foresight)
                Queue(BufferedInput.Attack);       // hold to charge a heavy strike
        }

        void Queue(BufferedInput input)
        {
            _buffered = input;
            _bufferTimer = _inputBuffer;
            TryConsumeBuffer(); // fire this frame if the FSM is already ready
        }

        void TryConsumeBuffer()
        {
            bool consumed = false;
            switch (_buffered)
            {
                case BufferedInput.Attack:
                    if (Input.GetMouseButton(0))
                        consumed = _combat.RequestChargeStart();      // still held → charge
                    else if (_combat.RequestChargeStart())
                    {
                        _combat.ReleaseCharge();                      // already released → light tap
                        consumed = true;
                    }
                    break;
                case BufferedInput.Parry:
                    consumed = _combat.RequestParry();
                    break;
                case BufferedInput.Dodge:
                    consumed = TryDodge();
                    break;
            }

            if (consumed)
            {
                _buffered = BufferedInput.None;
                _bufferTimer = 0f;
            }
        }

        void Move()
        {
            bool locked = IsLocked();
            Vector3 wish = WishDirection(locked);

            Vector3 horizontal;
            if (_combat.State == CombatState.Dodge)
                horizontal = _dodgeDir * (_dodgeSpeed * Mathf.Clamp01(1f - _combat.DodgeNormalizedTime));
            else if (_combat.IsInHitstun || _combat.State == CombatState.Foresight)
                horizontal = Vector3.zero;        // committed: no free movement during hitstun or the foresight read
            else
                horizontal = wish * _moveSpeed;

            horizontal += _combat.ImpulseVelocity; // knockback + foresight backstep/lunge

            if (_controller.isGrounded && _verticalSpeed < 0f)
                _verticalSpeed = -1f;
            _verticalSpeed += _gravity * Time.deltaTime;

            _controller.Move((horizontal + Vector3.up * _verticalSpeed) * Time.deltaTime);

            UpdateFacing(locked, wish);
        }

        bool TryDodge()
        {
            Vector3 dir = WishDirection(IsLocked());
            if (dir.sqrMagnitude < 0.01f)
                dir = -transform.forward;          // backstep when no input
            if (!_combat.RequestDodge())
                return false;
            _dodgeDir = dir.normalized;
            return true;
        }

        Vector3 WishDirection(bool locked)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            Vector3 wish;
            if (locked)
            {
                Vector3 forward = FlatToTarget();
                Vector3 right = Vector3.Cross(Vector3.up, forward);
                wish = forward * v + right * h;
            }
            else
            {
                Transform cam = _cameraRig.transform;
                Vector3 camForward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
                Vector3 camRight = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
                wish = camForward * v + camRight * h;
            }

            if (wish.sqrMagnitude > 1f)
                wish.Normalize();
            return wish;
        }

        void UpdateFacing(bool locked, Vector3 wish)
        {
            Vector3 face;
            if (locked)
                face = FlatToTarget();
            else if (_combat.State != CombatState.Dodge && wish.sqrMagnitude > 0.01f)
                face = wish;
            else
                return;

            if (face.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(face, Vector3.up), _rotationLerp * Time.deltaTime);
        }

        Vector3 FlatToTarget()
        {
            Vector3 to = _cameraRig.LockTarget.position - transform.position;
            to.y = 0f;
            return to.sqrMagnitude > 0.0001f ? to.normalized : transform.forward;
        }

        bool IsLocked() => _cameraRig != null && _cameraRig.IsLocked && _cameraRig.LockTarget != null;
    }
}
