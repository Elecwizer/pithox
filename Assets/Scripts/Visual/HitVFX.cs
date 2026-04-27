using UnityEngine;

namespace Pithox.Visual
{
    public class HitVFX : MonoBehaviour
    {
        public static HitVFX Instance { get; private set; }

        ParticleSystem hitBurst;
        ParticleSystem deathBurst;

        void Awake()
        {
            Instance = this;
            hitBurst = BuildSystem("HitBurst", 0.25f, 12, 4f, 0.6f);
            deathBurst = BuildSystem("DeathBurst", 0.6f, 30, 6f, 1.1f);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static void PlayHit(Vector3 position, Color tint)
        {
            if (Instance == null || Instance.hitBurst == null) return;
            Emit(Instance.hitBurst, position, tint, 12);
        }

        public static void PlayDeath(Vector3 position, Color tint)
        {
            if (Instance == null || Instance.deathBurst == null) return;
            Emit(Instance.deathBurst, position, tint, 30);
        }

        static void Emit(ParticleSystem ps, Vector3 position, Color tint, int count)
        {
            ps.transform.position = position + Vector3.up * 0.5f;
            ParticleSystem.EmitParams p = new ParticleSystem.EmitParams { startColor = tint };
            ps.Emit(p, count);
        }

        ParticleSystem BuildSystem(string name, float lifetime, int maxParticles, float speed, float startSize)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(transform, false);

            ParticleSystem ps = child.AddComponent<ParticleSystem>();
            ParticleSystemRenderer renderer = child.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.duration = 1f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = lifetime;
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(startSize * 0.5f, startSize);
            main.startColor = Color.white;
            main.maxParticles = maxParticles * 4;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 1.5f;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;

            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = g;

            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            AnimationCurve curve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(1f, 0f)
            );
            size.size = new ParticleSystem.MinMaxCurve(1f, curve);

            ps.Play();
            return ps;
        }
    }
}
