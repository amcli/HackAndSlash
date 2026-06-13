using UnityEngine;

namespace ParryArena.Arena
{
    /// <summary>
    /// A one-shot spark burst built entirely from code (no imported VFX assets).
    /// Spawns a short-lived <see cref="ParticleSystem"/> at a contact point that
    /// throws a handful of colour-tinted particles outward, then destroys itself.
    /// One helper covers every combat impact: the parry spark, the hit VFX that
    /// fires whenever an actor is struck, and the heavier riposte burst.
    /// </summary>
    public static class ImpactVfx
    {
        /// <summary>Throw a radial spark burst at <paramref name="position"/>.</summary>
        public static void Play(Vector3 position, Color color, float scale = 1f, int count = 14)
        {
            var go = new GameObject("ImpactVfx");
            go.transform.position = position;

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            float lifetime = 0.32f;

            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = lifetime;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.16f, lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f * scale, 5.5f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f * scale, 0.13f * scale);
            main.startColor = color;
            main.gravityModifier = 0.35f;
            main.maxParticles = 96;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            // We emit the burst by hand below, so disable the auto emission.
            var emission = ps.emission;
            emission.enabled = false;

            // Sphere shape + per-particle speed = particles fly outward in all
            // directions from the contact point.
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.06f * scale;

            // Fade alpha to zero over life so sparks wink out instead of cutting.
            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            colorOverLife.color = fade;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            ps.Emit(count);
            ps.Play();

            Object.Destroy(go, lifetime + 0.2f);
        }
    }
}
