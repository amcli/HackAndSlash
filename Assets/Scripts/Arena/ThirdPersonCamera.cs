using ParryArena.Core;
using UnityEngine;

namespace ParryArena.Arena
{
    /// <summary>
    /// A hand-rolled orbit/follow camera (no Cinemachine dependency). Free mode:
    /// mouse controls yaw/pitch. Lock-on mode (toggled with middle mouse): the
    /// yaw snaps to the player→target axis so the target stays framed, while the
    /// player strafes around it (the strafe itself lives in
    /// <see cref="PlayerController"/>, which reads <see cref="IsLocked"/> /
    /// <see cref="LockTarget"/>).
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] float _distance = 6f;
        [SerializeField] float _height = 2.2f;
        [SerializeField] float _minPitch = -15f;
        [SerializeField] float _maxPitch = 60f;
        [SerializeField] float _lockYawLerp = 12f;

        Transform _target;       // the player (followed)
        Transform _lockTarget;   // the enemy (focused when locked)
        GameSettings _settings;
        bool _locked;
        float _yaw;
        float _pitch = 15f;

        public bool IsLocked => _locked && _lockTarget != null;
        public Transform LockTarget => _lockTarget;

        public void Configure(Transform target, GameSettings settings)
        {
            _target = target;
            _settings = settings;
            _yaw = target.eulerAngles.y;
        }

        public void SetLockTarget(Transform lockTarget) => _lockTarget = lockTarget;

        void LateUpdate()
        {
            if (_target == null)
                return;

            if (Input.GetMouseButtonDown(2) && _lockTarget != null)
                _locked = !_locked;

            float sensitivity = _settings?.MouseSensitivity ?? 2.5f;
            bool invert = _settings?.InvertY ?? false;

            // Pitch is always mouse-controlled; yaw is free or snapped to the target.
            float pitchDelta = Input.GetAxis("Mouse Y") * sensitivity;
            _pitch += invert ? pitchDelta : -pitchDelta;
            _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);

            if (IsLocked)
            {
                Vector3 toTarget = _lockTarget.position - _target.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.001f)
                {
                    float desiredYaw = Quaternion.LookRotation(toTarget, Vector3.up).eulerAngles.y;
                    _yaw = Mathf.LerpAngle(_yaw, desiredYaw, _lockYawLerp * Time.unscaledDeltaTime);
                }
            }
            else
            {
                _yaw += Input.GetAxis("Mouse X") * sensitivity;
            }

            var rotation = Quaternion.Euler(_pitch, _yaw, 0f);

            // When locked, bias the focus slightly toward the enemy so both frame.
            Vector3 focus = _target.position + Vector3.up * _height;
            if (IsLocked)
            {
                Vector3 mid = (_target.position + _lockTarget.position) * 0.5f + Vector3.up * _height;
                focus = Vector3.Lerp(focus, mid, 0.35f);
            }

            Vector3 position = focus - rotation * Vector3.forward * _distance;
            transform.position = position;
            transform.rotation = Quaternion.LookRotation(focus - position, Vector3.up);

            // Screen shake (e.g. on a successful parry), applied in screen space.
            Vector3 shake = ScreenShake.Evaluate(Time.unscaledDeltaTime);
            transform.position += transform.right * shake.x + transform.up * shake.y;
        }
    }
}
