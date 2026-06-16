using UnityEngine;

namespace ParryArena.Arena
{
    /// <summary>
    /// Builds the greybox arena stage from primitives: a directional light, the
    /// ground plane, the four bounding walls, and the camera rig. Pure
    /// construction with no runtime state — <see cref="ArenaController"/> calls
    /// <see cref="Build"/> once at startup and then runs the match.
    /// </summary>
    public static class ArenaStage
    {
        /// <summary>Half the side length of the square arena — the wall ring sits at ±this on X and Z.</summary>
        public const float HalfExtent = 20f;

        /// <summary>Builds the environment and returns the camera rig (still unconfigured).</summary>
        public static ThirdPersonCamera Build()
        {
            BuildEnvironment();
            return BuildCamera();
        }

        static void BuildEnvironment()
        {
            var lightGo = new GameObject("Directional Light");
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.9f);
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(4f, 1f, 4f); // plane is 10x10 -> 40x40
            Tint(ground, new Color(0.18f, 0.19f, 0.21f));

            const float half = HalfExtent;
            CreateWall(new Vector3(0f, 1.5f, half), new Vector3(42f, 3f, 1f));
            CreateWall(new Vector3(0f, 1.5f, -half), new Vector3(42f, 3f, 1f));
            CreateWall(new Vector3(half, 1.5f, 0f), new Vector3(1f, 3f, 42f));
            CreateWall(new Vector3(-half, 1.5f, 0f), new Vector3(1f, 3f, 42f));
        }

        static void CreateWall(Vector3 position, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.transform.position = position;
            wall.transform.localScale = scale;
            Tint(wall, new Color(0.12f, 0.13f, 0.15f));
        }

        static ThirdPersonCamera BuildCamera()
        {
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener))
            {
                tag = "MainCamera"
            };
            return camGo.AddComponent<ThirdPersonCamera>();
        }

        static void Tint(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = color;
        }
    }
}
