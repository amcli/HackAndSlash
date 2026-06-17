#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ParryArena.EditorTools
{
    /// <summary>
    /// Logs the selected Transform's local position + rotation (relative to its
    /// parent). Use it to read a weapon's grip out of a reference FBX — drag the
    /// reference into a scene, select the sword node parented under the hand, and run
    /// this — or to read back a hand-tuned <c>WeaponGrip</c> so the value can be baked
    /// as the default. Menu: Tools ▸ Parry Arena ▸ Log Local Transform.
    /// </summary>
    static class LocalTransformLogger
    {
        [MenuItem("Tools/Parry Arena/Log Local Transform")]
        static void Log()
        {
            var t = Selection.activeTransform;
            if (t == null)
            {
                Debug.LogWarning("[Grip] Select a Transform first (e.g. the sword node under the hand).");
                return;
            }

            Vector3 p = t.localPosition;
            Vector3 e = t.localEulerAngles;
            Debug.Log(
                $"[Grip] {t.name}  (parent: {(t.parent ? t.parent.name : "none")})\n" +
                $"localPosition = new Vector3({p.x:F4}f, {p.y:F4}f, {p.z:F4}f)\n" +
                $"localEuler    = new Vector3({e.x:F2}f, {e.y:F2}f, {e.z:F2}f)");
        }
    }
}
#endif
