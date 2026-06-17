using UnityEngine;

namespace ParryArena.Arena
{
    /// <summary>
    /// Holds the weapon at a fixed local transform inside its parent hand bone, so it
    /// rides the character's animation (the locomotion clips hold it, the slash clip
    /// swings it). Position and rotation are re-applied every frame, so they're
    /// <b>live-tunable in Play mode</b>: select the WeaponPivot under the model's right
    /// hand and adjust until the blade sits correctly in the fist — it then swings
    /// along with the hand automatically. (Scale is left to the rig's hand-scale
    /// compensation and not touched here.)
    /// </summary>
    public class WeaponGrip : MonoBehaviour
    {
        [Tooltip("Local position of the grip inside the hand bone (hand-local space).")]
        [SerializeField] Vector3 _localPosition;
        [Tooltip("Local rotation of the grip; tune so the blade points out of the fist.")]
        [SerializeField] Vector3 _localEuler;

        public void Init(Vector3 localPosition, Vector3 localEuler)
        {
            _localPosition = localPosition;
            _localEuler = localEuler;
        }

        void LateUpdate()
        {
            transform.localPosition = _localPosition;
            transform.localRotation = Quaternion.Euler(_localEuler);
        }
    }
}
