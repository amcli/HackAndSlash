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
            // Pivot near the hand, kept close to centre-front so swings sweep
            // across the body rather than off to the side.
            var pivot = CreateAnchor(root, "WeaponPivot", new Vector3(0.2f, 1.4f, 0.15f));

            var blade = CreatePart(PrimitiveType.Cube, pivot, "Blade", Color.Lerp(tint, Color.white, 0.2f));
            blade.transform.localScale = new Vector3(0.1f, 0.1f, weaponLength);
            blade.transform.localPosition = new Vector3(0f, 0f, weaponLength * 0.5f);

            // Empty at the blade centre — the Hitbox component lives here so its
            // overlap box swings with the pivot.
            var hitboxAnchor = CreateAnchor(pivot, "Hitbox", new Vector3(0f, 0f, weaponLength * 0.5f));

            var tip = CreateAnchor(pivot, "Tip", new Vector3(0f, 0f, weaponLength));
            var trail = tip.gameObject.AddComponent<TrailRenderer>();
            ConfigureTrail(trail, Color.Lerp(tint, Color.white, 0.5f),
                time: 0.18f, startWidth: 0.16f, endWidth: 0f, startAlpha: 0.7f);

            var rig = root.gameObject.AddComponent<WeaponRig>();
            rig.Pivot = pivot;
            rig.BladeRenderer = blade.GetComponent<Renderer>();
            rig.HitboxAnchor = hitboxAnchor;
            rig.Trail = trail;
            rig.RestLocalRotation = pivot.localRotation;
            rig.BladeLength = weaponLength;
            return rig;
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
