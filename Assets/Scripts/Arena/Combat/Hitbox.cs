using System.Collections.Generic;
using UnityEngine;

namespace ParryArena.Arena
{
    /// <summary>
    /// A damage region on a weapon blade. Only detects while active (the swing's
    /// active frames). Detection is a box overlap filtered to opposing
    /// <see cref="Hurtbox"/>es, with a per-swing "already hit" set so one swing
    /// can't multi-hit the same target. All resolution goes through the single
    /// <see cref="CombatResolver"/> path.
    /// </summary>
    public class Hitbox : MonoBehaviour
    {
        public CombatTeam Team { get; private set; }
        public float Damage { get; private set; }
        public bool Parryable { get; private set; }
        public bool Blockable { get; private set; } = true;
        public ActorCombat Owner { get; private set; }

        Vector3 _halfExtents = new Vector3(0.14f, 0.14f, 0.6f);
        bool _active;

        readonly HashSet<Hurtbox> _alreadyHit = new();
        static readonly Collider[] _overlap = new Collider[16];

        DebugVolume _debug;

        void Awake()
        {
            _debug = DebugVolume.Attach(transform, _halfExtents * 2f, CombatDebug.HitboxIdleColor);
            _debug.SetActiveColor(CombatDebug.HitboxActiveColor);
        }

        public void Configure(CombatTeam team, float damage, bool parryable, bool blockable, Vector3 halfExtents, ActorCombat owner)
        {
            Team = team;
            Damage = damage;
            Parryable = parryable;
            Blockable = blockable;
            Owner = owner;
            _halfExtents = halfExtents;
            if (_debug != null)
                _debug.SetSize(halfExtents * 2f);
        }

        public void Activate()
        {
            _active = true;
            _alreadyHit.Clear();
        }

        public void Deactivate() => _active = false;

        void Update()
        {
            if (_debug != null)
                _debug.SetHighlight(_active);
        }

        void FixedUpdate()
        {
            if (!_active)
                return;

            Vector3 worldExtents = Vector3.Scale(_halfExtents, transform.lossyScale);
            int count = Physics.OverlapBoxNonAlloc(transform.position, worldExtents, _overlap,
                transform.rotation, ~0, QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
            {
                var hurtbox = _overlap[i].GetComponent<Hurtbox>();
                if (hurtbox == null || hurtbox.Team == Team || _alreadyHit.Contains(hurtbox))
                    continue;

                _alreadyHit.Add(hurtbox);
                CombatResolver.Resolve(this, hurtbox);
            }
        }
    }
}
