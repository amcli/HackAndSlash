using UnityEngine;

namespace ParryArena.Arena
{
    /// <summary>
    /// Builds the greybox avatar (capsule body, sphere head, swingable weapon)
    /// from primitives. One builder for both the player and enemies keeps them
    /// consistent and the weapon rig identical. Colliders on the visual
    /// primitives are stripped — the player uses a CharacterController and combat
    /// uses overlap queries, so the visual parts must not interfere.
    /// </summary>
    public static class ActorVisualFactory
    {
        const float ArmThickness = 0.15f;    // width of the limb capsules
        const float UpperArmLength = 0.2f;   // shoulder → elbow
        const float ForearmLength = 0.22f;   // elbow → wrist
        const float HandleLength = 0.22f;    // grip — the only holdable part; taken out of the weapon length
        const float OffArmLength = 0.7f;     // free arm, held in a loose guard

        const string PlayerModelResource = "character";  // Assets/Resources/character.fbx
        const float ModelTargetHeight = 2f;              // normalise the imported model to the greybox body height
        static readonly Vector3 BodySwordMount = new Vector3(0.2f, 1.35f, 0.25f); // fallback sword mount on the body (non-Humanoid rig)
        // Grip transform of the sword inside the right hand. Starting guess aims the
        // blade up-and-forward out of the fist; fine-tune live via WeaponGrip, then
        // bake the final values here.
        static readonly Vector3 HandGripOffset = new Vector3(0f, 0f, 0f);
        static readonly Vector3 HandGripEuler = new Vector3(-90f, 0f, 0f);

        public static AvatarParts CreateAvatar(string name, Color tint, float scale, float weaponLength)
        {
            var root = new GameObject(name);
            root.transform.localScale = Vector3.one * scale;

            // Body: capsule is 2 units tall, pivot at its centre -> raise to sit on the ground.
            var body = CreatePart(PrimitiveType.Capsule, root.transform, "Body", tint);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);

            var head = CreatePart(PrimitiveType.Sphere, root.transform, "Head",
                Color.Lerp(tint, Color.white, 0.35f));
            head.transform.localPosition = new Vector3(0f, 2.05f, 0f);
            head.transform.localScale = Vector3.one * 0.55f;

            // A small nose so facing direction is readable.
            var nose = CreatePart(PrimitiveType.Cube, head.transform, "Facing",
                Color.Lerp(tint, Color.black, 0.4f));
            nose.transform.localPosition = new Vector3(0f, 0f, 0.45f);
            nose.transform.localScale = new Vector3(0.25f, 0.25f, 0.35f);

            // Off-hand arm: a static, bent limb so the silhouette reads as a fighter
            // in a guard. The sword arm is built with the weapon (BuildWeapon) so it
            // swings with the blade.
            var offArm = CreatePart(PrimitiveType.Capsule, root.transform, "OffArm", tint);
            offArm.transform.localScale = new Vector3(ArmThickness, OffArmLength * 0.5f, ArmThickness);
            offArm.transform.localPosition = new Vector3(-0.26f, 1.3f, 0.22f);
            offArm.transform.localRotation = Quaternion.Euler(72f, 14f, 0f); // down-and-forward, in front of the chest

            return new AvatarParts
            {
                Root = root,
                HurtboxAnchor = CreateAnchor(root.transform, "Hurtbox", new Vector3(0f, 1f, 0f)),
                Weapon = BuildWeapon(root.transform, tint, weaponLength),
                DodgeTrail = BuildDodgeTrail(root.transform, tint),
            };
        }

        static TrailRenderer BuildDodgeTrail(Transform root, Color tint)
        {
            var anchor = CreateAnchor(root, "DodgeTrail", new Vector3(0f, 1f, 0f));
            var trail = anchor.gameObject.AddComponent<TrailRenderer>();
            ConfigureTrail(trail, Color.Lerp(tint, Color.white, 0.25f),
                time: 0.22f, startWidth: 0.7f, endWidth: 0.1f, startAlpha: 0.35f);
            return trail;
        }

        static WeaponRig BuildWeapon(Transform root, Color tint, float weaponLength)
        {
            // An articulated right arm: the shoulder pivot is driven by the pose
            // system (the swing arc), while the elbow and wrist are driven
            // procedurally by ActorCombat (coil on windup, snap straight on the
            // strike, whip the blade through) so swings read as real arm motion.
            var pivot = CreateAnchor(root, "WeaponPivot", new Vector3(0.24f, 1.35f, 0.2f));

            var upperArm = CreatePart(PrimitiveType.Capsule, pivot, "UpperArm", tint);
            LayLimbAlongZ(upperArm.transform, UpperArmLength, ArmThickness);

            // Elbow at the end of the upper arm — flexes (UpdateArmBend).
            var elbow = CreateAnchor(pivot, "Elbow", new Vector3(0f, 0f, UpperArmLength));

            var forearm = CreatePart(PrimitiveType.Capsule, elbow, "Forearm", tint);
            LayLimbAlongZ(forearm.transform, ForearmLength, ArmThickness);

            // Wrist at the end of the forearm — whips the blade through the swing.
            var wrist = CreateAnchor(elbow, "Wrist", new Vector3(0f, 0f, ForearmLength));

            // The hand grips the handle at the wrist; the handle is the only part the
            // model holds. Its length is taken out of the weapon so reach is unchanged.
            var hand = CreatePart(PrimitiveType.Sphere, wrist, "Hand", Color.Lerp(tint, Color.white, 0.15f));
            hand.transform.localScale = Vector3.one * (ArmThickness * 1.3f);

            var handle = CreatePart(PrimitiveType.Cube, wrist, "Handle", new Color(0.16f, 0.14f, 0.12f));
            handle.transform.localScale = new Vector3(0.12f, 0.12f, HandleLength);
            handle.transform.localPosition = new Vector3(0f, 0f, HandleLength * 0.5f);

            // The blade is the cutting part — it starts past the handle, so the hand
            // can't hold it, and only it carries the hitbox.
            float bladeLength = Mathf.Max(0.1f, weaponLength - HandleLength);
            var blade = CreatePart(PrimitiveType.Cube, wrist, "Blade", Color.Lerp(tint, Color.white, 0.3f));
            blade.transform.localScale = new Vector3(0.1f, 0.1f, bladeLength);
            blade.transform.localPosition = new Vector3(0f, 0f, HandleLength + bladeLength * 0.5f);

            // Empty at the blade centre — the Hitbox lives here so its overlap box
            // tracks the blade through the articulated swing and covers only the blade.
            var hitboxAnchor = CreateAnchor(wrist, "Hitbox", new Vector3(0f, 0f, HandleLength + bladeLength * 0.5f));

            var tip = CreateAnchor(wrist, "Tip", new Vector3(0f, 0f, HandleLength + bladeLength));
            var trail = tip.gameObject.AddComponent<TrailRenderer>();
            ConfigureTrail(trail, Color.Lerp(tint, Color.white, 0.5f),
                time: 0.18f, startWidth: 0.16f, endWidth: 0f, startAlpha: 0.7f);

            var rig = root.gameObject.AddComponent<WeaponRig>();
            rig.Pivot = pivot;
            rig.Elbow = elbow;
            rig.Wrist = wrist;
            rig.BladeRenderer = blade.GetComponent<Renderer>();
            rig.HitboxAnchor = hitboxAnchor;
            rig.Trail = trail;
            rig.RestLocalRotation = pivot.localRotation;
            rig.RestElbowRotation = elbow.localRotation;
            rig.RestWristRotation = wrist.localRotation;
            rig.BladeLength = bladeLength;
            return rig;
        }

        /// <summary>Lays a capsule (2 units tall on its local Y) along the parent's +Z, from the origin out to <paramref name="length"/>.</summary>
        static void LayLimbAlongZ(Transform limb, float length, float thickness)
        {
            limb.localScale = new Vector3(thickness, length * 0.5f, thickness);
            limb.localPosition = new Vector3(0f, 0f, length * 0.5f);
            limb.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }

        /// <summary>
        /// Player avatar built from the imported Humanoid model (Resources/character):
        /// instantiated, normalised to body height, with the sword parented into the
        /// right-hand bone so the combat hitbox tracks the hand. Falls back to the
        /// greybox build if the model is missing or isn't a mapped Humanoid, so the
        /// match always runs. (Locomotion clips + the procedural arm override layer
        /// on top of this in Stage 2.)
        /// </summary>
        public static AvatarParts CreateModelAvatar(string name, Color tint, float weaponLength)
        {
            var prefab = Resources.Load<GameObject>(PlayerModelResource);
            if (prefab == null)
            {
                Debug.LogWarning($"[ActorVisualFactory] Resources/{PlayerModelResource} not found — using greybox.");
                return CreateAvatar(name, tint, 1f, weaponLength);
            }

            var root = new GameObject(name);
            var model = Object.Instantiate(prefab, root.transform);
            model.name = "Model";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            NormalizeHeight(model, ModelTargetHeight);

            var animator = model.GetComponentInChildren<Animator>();
            Transform hand = animator != null ? animator.GetBoneTransform(HumanBodyBones.RightHand) : null;
            if (hand == null)
                Debug.LogWarning("[ActorVisualFactory] character isn't a mapped Humanoid (no right-hand bone) — sword falls back to a body mount and locomotion won't play.");

            // With a Humanoid rig the sword goes in the hand (the locomotion clips
            // hold it there); otherwise mount it on the body so combat still works.
            var weapon = hand != null
                ? BuildSwordInHand(hand, tint, weaponLength)
                : BuildSwordOnBody(root.transform, tint, weaponLength);

            return new AvatarParts
            {
                Root = root,
                Animator = animator,
                HurtboxAnchor = CreateAnchor(root.transform, "Hurtbox", new Vector3(0f, 1f, 0f)),
                Weapon = weapon,
                DodgeTrail = BuildDodgeTrail(root.transform, tint),
            };
        }

        /// <summary>
        /// The sword (handle + blade) parented into the hand bone with a fixed grip
        /// transform. Fully clip-driven: the locomotion clips hold it and the slash
        /// clip swings it, so ActorCombat doesn't pose it — it only drives the hitbox
        /// timing. Tune HandGripOffset/Euler so the blade points out of the fist.
        /// </summary>
        static WeaponRig BuildSwordInHand(Transform hand, Color tint, float weaponLength)
        {
            var pivot = CreateAnchor(hand, "WeaponPivot", HandGripOffset);
            pivot.localRotation = Quaternion.Euler(HandGripEuler);
            // Counter the bone's world scale so the sword stays at world size.
            float handScale = hand.lossyScale.x;
            pivot.localScale = Vector3.one * (handScale > 0.0001f ? 1f / handScale : 1f);
            // Live-tunable grip: adjust in Play until the blade sits right in the fist.
            pivot.gameObject.AddComponent<WeaponGrip>().Init(HandGripOffset, HandGripEuler);

            var elbow = CreateAnchor(pivot, "Elbow", Vector3.zero);
            var wrist = CreateAnchor(elbow, "Wrist", Vector3.zero);

            var handle = CreatePart(PrimitiveType.Cube, wrist, "Handle", new Color(0.16f, 0.14f, 0.12f));
            handle.transform.localScale = new Vector3(0.12f, 0.12f, HandleLength);
            handle.transform.localPosition = new Vector3(0f, 0f, HandleLength * 0.5f);

            float bladeLength = Mathf.Max(0.1f, weaponLength - HandleLength);
            var blade = CreatePart(PrimitiveType.Cube, wrist, "Blade", Color.Lerp(tint, Color.white, 0.3f));
            blade.transform.localScale = new Vector3(0.1f, 0.1f, bladeLength);
            blade.transform.localPosition = new Vector3(0f, 0f, HandleLength + bladeLength * 0.5f);

            var hitboxAnchor = CreateAnchor(wrist, "Hitbox", new Vector3(0f, 0f, HandleLength + bladeLength * 0.5f));

            var tip = CreateAnchor(wrist, "Tip", new Vector3(0f, 0f, HandleLength + bladeLength));
            var trail = tip.gameObject.AddComponent<TrailRenderer>();
            ConfigureTrail(trail, Color.Lerp(tint, Color.white, 0.5f),
                time: 0.18f, startWidth: 0.16f, endWidth: 0f, startAlpha: 0.7f);

            var rig = pivot.gameObject.AddComponent<WeaponRig>();
            rig.Pivot = pivot;
            rig.Elbow = elbow;
            rig.Wrist = wrist;
            rig.BladeRenderer = blade.GetComponent<Renderer>();
            rig.HitboxAnchor = hitboxAnchor;
            rig.Trail = trail;
            rig.RestLocalRotation = pivot.localRotation;
            rig.RestElbowRotation = elbow.localRotation;
            rig.RestWristRotation = wrist.localRotation;
            rig.BladeLength = bladeLength;
            rig.ClipDriven = true;   // the animation clips move the hand; ActorCombat only drives the hitbox
            return rig;
        }

        /// <summary>
        /// Stage-1 sword: handle + blade on a swing pivot mounted on the body (front
        /// of the chest), in root space so swings arc forward and the hitbox connects
        /// — matching the greybox combat while the model stands in. Stage 2 reparents
        /// this into the right-hand bone and drives the real arm.
        /// </summary>
        static WeaponRig BuildSwordOnBody(Transform root, Color tint, float weaponLength)
        {
            var pivot = CreateAnchor(root, "WeaponPivot", BodySwordMount);

            // Coincident elbow/wrist anchors keep the WeaponRig contract; the procedural
            // bend whips the blade here until Stage 2 drives the real arm bones.
            var elbow = CreateAnchor(pivot, "Elbow", Vector3.zero);
            var wrist = CreateAnchor(elbow, "Wrist", Vector3.zero);

            var handle = CreatePart(PrimitiveType.Cube, wrist, "Handle", new Color(0.16f, 0.14f, 0.12f));
            handle.transform.localScale = new Vector3(0.12f, 0.12f, HandleLength);
            handle.transform.localPosition = new Vector3(0f, 0f, HandleLength * 0.5f);

            float bladeLength = Mathf.Max(0.1f, weaponLength - HandleLength);
            var blade = CreatePart(PrimitiveType.Cube, wrist, "Blade", Color.Lerp(tint, Color.white, 0.3f));
            blade.transform.localScale = new Vector3(0.1f, 0.1f, bladeLength);
            blade.transform.localPosition = new Vector3(0f, 0f, HandleLength + bladeLength * 0.5f);

            var hitboxAnchor = CreateAnchor(wrist, "Hitbox", new Vector3(0f, 0f, HandleLength + bladeLength * 0.5f));

            var tip = CreateAnchor(wrist, "Tip", new Vector3(0f, 0f, HandleLength + bladeLength));
            var trail = tip.gameObject.AddComponent<TrailRenderer>();
            ConfigureTrail(trail, Color.Lerp(tint, Color.white, 0.5f),
                time: 0.18f, startWidth: 0.16f, endWidth: 0f, startAlpha: 0.7f);

            var rig = pivot.gameObject.AddComponent<WeaponRig>();
            rig.Pivot = pivot;
            rig.Elbow = elbow;
            rig.Wrist = wrist;
            rig.BladeRenderer = blade.GetComponent<Renderer>();
            rig.HitboxAnchor = hitboxAnchor;
            rig.Trail = trail;
            rig.RestLocalRotation = pivot.localRotation;
            rig.RestElbowRotation = elbow.localRotation;
            rig.RestWristRotation = wrist.localRotation;
            rig.BladeLength = bladeLength;
            return rig;
        }

        /// <summary>Uniformly scales a model so its rendered height matches <paramref name="targetHeight"/>.</summary>
        static void NormalizeHeight(GameObject model, float targetHeight)
        {
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return;

            var bounds = renderers[0].bounds;
            foreach (var r in renderers)
                bounds.Encapsulate(r.bounds);

            if (bounds.size.y > 0.0001f)
                model.transform.localScale *= targetHeight / bounds.size.y;
        }

        static void ConfigureTrail(TrailRenderer trail, Color color,
            float time, float startWidth, float endWidth, float startAlpha)
        {
            trail.time = time;
            trail.startWidth = startWidth;
            trail.endWidth = endWidth;
            trail.minVertexDistance = 0.02f;
            trail.numCapVertices = 2;
            trail.emitting = false;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.startColor = new Color(color.r, color.g, color.b, startAlpha);
            trail.endColor = new Color(color.r, color.g, color.b, 0f);
        }

        static Transform CreateAnchor(Transform parent, string name, Vector3 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.identity;
            return go.transform;
        }

        static GameObject CreatePart(PrimitiveType type, Transform parent, string name, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);

            var collider = go.GetComponent<Collider>();
            if (collider != null)
                Object.Destroy(collider);

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = color; // .material -> per-instance, safe to edit

            return go;
        }
    }
}
