using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace ParryArena.Arena
{
    /// <summary>
    /// Drives the player model's animation from the imported Mixamo Humanoid clips
    /// through a code-built <see cref="PlayableGraph"/> (no AnimatorController asset).
    /// A directional locomotion blend (idle / walk / run-fwd-L,R / strafe-L,R / back)
    /// picks the clip from the body's local-space velocity; a combat layer crossfades
    /// over it to play the 3-hit slash combo clip, <em>scrubbed in lockstep with the
    /// ActorCombat combo</em> so each of the clip's three slashes lands on the matching
    /// gameplay hit. Root motion is off; locomotion clips loop manually. Block /
    /// foresight clips layer on next.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimation : MonoBehaviour
    {
        const string IdleClip = "Great Sword Idle";
        const string WalkClip = "Great Sword Walk";
        const string RunForwardLeftClip = "Great Sword Run Forward Left";
        const string RunForwardRightClip = "Great Sword Run Forward Right";
        const string StrafeLeftClip = "Great Sword Strafe Left";
        const string StrafeRightClip = "Great Sword Strafe Right";
        const string RunBackwardClip = "Great Sword Run Backward";
        const string SlashComboClip = "Great Sword Slash Combo Right";

        [SerializeField] float _blendLerp = 12f;        // locomotion weight tracking
        [SerializeField] float _blendEpsilon = 0.1f;    // inverse-distance softening (smaller = snappier)
        [SerializeField] float _combatBlendLerp = 20f;  // how fast the attack layer fades in/out
        [Tooltip("Normalized clip times where the combo hands off slash 1→2 and 2→3; tune to the clip's three hits.")]
        [SerializeField] float _comboSeg1 = 0.34f;
        [SerializeField] float _comboSeg2 = 0.67f;
        [Tooltip("Within each combo segment, the fraction where the blade is live (the hit/damage window) — windup before it, recovery after.")]
        [SerializeField] float _activeStart = 0.3f;
        [SerializeField] float _activeEnd = 0.6f;

        Animator _animator;
        CharacterController _controller;
        ActorCombat _combat;

        PlayableGraph _graph;
        AnimationMixerPlayable _locomotion;  // 7 directional clips
        AnimationMixerPlayable _top;         // [0] = locomotion, [1] = slash combo
        AnimationClipPlayable _slash;
        AnimationClip _slashClip;
        float _combatWeight;

        // Each locomotion input's representative planar velocity (x = right, z = forward), local space.
        static readonly Vector2[] SampleVelocity =
        {
            new Vector2(0f, 0f),     // 0 idle
            new Vector2(0f, 1.6f),   // 1 walk
            new Vector2(-1.8f, 3f),  // 2 run forward-left
            new Vector2(1.8f, 3f),   // 3 run forward-right
            new Vector2(-2.6f, 0f),  // 4 strafe left
            new Vector2(2.6f, 0f),   // 5 strafe right
            new Vector2(0f, -3f),    // 6 run backward
        };
        readonly AnimationClip[] _clips = new AnimationClip[7];
        readonly float[] _weights = new float[7];
        readonly float[] _scratch = new float[7];

        public void Configure(CharacterController controller, ActorCombat combat)
        {
            _controller = controller;
            _combat = combat;
            _animator = GetComponent<Animator>();
            _animator.applyRootMotion = false;

            _clips[0] = LoadClip(IdleClip);
            _clips[1] = LoadClip(WalkClip);
            _clips[2] = LoadClip(RunForwardLeftClip);
            _clips[3] = LoadClip(RunForwardRightClip);
            _clips[4] = LoadClip(StrafeLeftClip);
            _clips[5] = LoadClip(StrafeRightClip);
            _clips[6] = LoadClip(RunBackwardClip);
            _slashClip = LoadClip(SlashComboClip);
            if (_clips[0] == null)
            {
                Debug.LogWarning("[PlayerAnimation] locomotion clips not found in Resources — model stays in bind pose.");
                enabled = false;
                return;
            }

            _graph = PlayableGraph.Create("PlayerAnimation");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            _locomotion = AnimationMixerPlayable.Create(_graph, _clips.Length);
            for (int i = 0; i < _clips.Length; i++)
                if (_clips[i] != null)
                    _graph.Connect(AnimationClipPlayable.Create(_graph, _clips[i]), 0, _locomotion, i);

            _top = AnimationMixerPlayable.Create(_graph, 2);
            _graph.Connect(_locomotion, 0, _top, 0);
            if (_slashClip != null)
            {
                _slash = AnimationClipPlayable.Create(_graph, _slashClip);
                _graph.Connect(_slash, 0, _top, 1);
            }
            _top.SetInputWeight(0, 1f);
            _top.SetInputWeight(1, 0f);

            AnimationPlayableOutput.Create(_graph, "Player", _animator).SetSourcePlayable(_top);

            _weights[0] = 1f;
            ApplyLocomotionWeights();
            _graph.Play();

            if (_slashClip != null && _combat != null)
                PushComboTimings();
        }

        // Derive each combo hit's phase durations from the clip's breakpoints and the
        // clip length, and hand them to ActorCombat so its combo swings last exactly
        // as long as the clip's slashes — the scrub then plays the clip at its natural
        // rhythm (1:1 with real time) instead of time-warping it.
        void PushComboTimings()
        {
            float len = _slashClip.length;
            float[] bounds = { 0f, Mathf.Clamp01(_comboSeg1), Mathf.Clamp01(_comboSeg2), 1f };
            var windup = new float[3];
            var active = new float[3];
            var recovery = new float[3];
            for (int k = 0; k < 3; k++)
            {
                float segDur = Mathf.Max(0.05f, (bounds[k + 1] - bounds[k]) * len);
                windup[k] = segDur * _activeStart;
                active[k] = segDur * Mathf.Max(0.02f, _activeEnd - _activeStart);
                recovery[k] = segDur * Mathf.Max(0f, 1f - _activeEnd);
            }
            _combat.SetComboTimings(windup, active, recovery);
        }

        void Update()
        {
            if (!_graph.IsValid())
                return;

            UpdateLocomotion();
            UpdateCombat();
        }

        void UpdateLocomotion()
        {
            Vector3 world = _controller != null ? _controller.velocity : Vector3.zero;
            world.y = 0f;
            Vector3 local = transform.InverseTransformDirection(world);
            Vector2 vel = new Vector2(local.x, local.z);

            // Inverse-distance (Shepard) weighting toward the nearest direction samples.
            float sum = 0f;
            for (int i = 0; i < _clips.Length; i++)
            {
                if (_clips[i] == null) { _scratch[i] = 0f; continue; }
                float w = 1f / ((vel - SampleVelocity[i]).sqrMagnitude + _blendEpsilon);
                _scratch[i] = w;
                sum += w;
            }

            float k = 1f - Mathf.Exp(-_blendLerp * Time.deltaTime);
            for (int i = 0; i < _weights.Length; i++)
            {
                float target = sum > 0f ? _scratch[i] / sum : (i == 0 ? 1f : 0f);
                _weights[i] = Mathf.Lerp(_weights[i], target, k);
            }
            ApplyLocomotionWeights();
            WrapLocomotionTimes();
        }

        // Crossfade the slash clip over locomotion while a combo hit swings, scrubbing
        // the clip's matching third in lockstep with the swing so the animated slash
        // lands with the gameplay hit.
        void UpdateCombat()
        {
            float target = 0f;
            if (_slashClip != null && _combat != null && _combat.IsSwinging && _combat.ComboAnimIndex >= 0)
            {
                int idx = Mathf.Clamp(_combat.ComboAnimIndex, 0, 2);
                target = 1f;
                float len = _slashClip.length;
                float a = SegStart(idx) * len;
                float b = SegStart(idx + 1) * len;
                _slash.SetTime(Mathf.Lerp(a, b, _combat.SwingProgress01));
            }

            _combatWeight = Mathf.Lerp(_combatWeight, target, 1f - Mathf.Exp(-_combatBlendLerp * Time.deltaTime));
            _top.SetInputWeight(0, 1f - _combatWeight);
            _top.SetInputWeight(1, _combatWeight);
        }

        float SegStart(int boundary)
        {
            switch (boundary)
            {
                case 0: return 0f;
                case 1: return _comboSeg1;
                case 2: return _comboSeg2;
                default: return 1f;
            }
        }

        void ApplyLocomotionWeights()
        {
            float sum = 0f;
            for (int i = 0; i < _weights.Length; i++) sum += _weights[i];
            if (sum < 0.0001f) { _weights[0] = 1f; sum = 1f; }
            for (int i = 0; i < _weights.Length; i++)
                _locomotion.SetInputWeight(i, _weights[i] / sum);
        }

        // Clips advance with the graph; wrap each so they loop without the import Loop flag.
        void WrapLocomotionTimes()
        {
            for (int i = 0; i < _clips.Length; i++)
            {
                if (_clips[i] == null)
                    continue;
                var input = _locomotion.GetInput(i);
                double len = _clips[i].length;
                if (input.IsValid() && len > 0.0 && input.GetTime() >= len)
                    input.SetTime(input.GetTime() % len);
            }
        }

        static AnimationClip LoadClip(string resourceName)
        {
            var all = Resources.LoadAll<AnimationClip>(resourceName);
            if (all == null || all.Length == 0)
                return null;
            foreach (var c in all)
                if (c != null && !c.name.StartsWith("__preview__"))
                    return c;
            return all[0];
        }

        void OnDestroy()
        {
            if (_graph.IsValid())
                _graph.Destroy();
        }
    }
}
