using UnityEngine;

namespace ParryArena.Data
{
    /// <summary>
    /// Frame data for a single attack: how long each phase lasts, the blade poses
    /// the procedural swing animates between, and the hit's power. A moveset is an
    /// ordered array of these (see <see cref="LoadoutDefinition.Combo"/> /
    /// <see cref="EnemyDefinition.Attacks"/>); the per-actor FSM reads the current
    /// one instead of carrying hard-coded timings.
    ///
    /// Authored as an asset (Create > Parry Arena > Attack Definition) or supplied
    /// as a built-in default by <c>GameContent</c>, matching how loadouts and
    /// enemies resolve. This is the seam the plan calls "AttackSO frame data".
    /// </summary>
    [CreateAssetMenu(fileName = "Attack", menuName = "Parry Arena/Attack Definition")]
    public class AttackDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string Id = "attack";

        [Header("Frame data (seconds)")]
        public float Windup = 0.15f;
        public float Active = 0.12f;
        public float Recovery = 0.25f;

        [Header("Swing pose (weapon-pivot Euler degrees; blade points +Z at rest)")]
        public Vector3 WindupPose = new Vector3(-135f, 0f, 0f);
        public Vector3 ActiveEndPose = new Vector3(55f, 0f, 0f);

        [Header("Power")]
        public float Damage = 20f;
        public float StaggerDamage = 8f;
        public bool Parryable = true;
        public bool Blockable = true;

        [Header("Cost")]
        [Tooltip("Stamina spent to start this attack. Ignored by actors that don't use stamina.")]
        public float StaminaCost = 12f;
    }
}
