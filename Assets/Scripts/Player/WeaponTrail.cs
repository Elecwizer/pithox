using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Pithox.Combat
{
    public class WeaponAttackTrail : MonoBehaviour
    {
        [Header("Reference")]
        [SerializeField] Transform wandTip;

        [Header("Trail Look")]
        [SerializeField] float trailTime = 0.18f;
        [SerializeField] float startWidth = 0.16f;
        [SerializeField] float endWidth = 0.02f;
        [SerializeField] float minVertexDistance = 0.015f;
        [SerializeField] Color startColor = new Color(1f, 1f, 1f, 0.85f);
        [SerializeField] Color endColor = new Color(1f, 1f, 1f, 0f);

        [Header("Timing")]
        [SerializeField] float defaultStartDelay = 0.05f;
        [SerializeField] float defaultDuration = 0.25f;

        TrailRenderer trail;
        Coroutine trailRoutine;

        void Awake()
        {
            if (wandTip == null)
                wandTip = transform;

            trail = wandTip.GetComponent<TrailRenderer>();

            if (trail == null)
                trail = wandTip.gameObject.AddComponent<TrailRenderer>();

            SetupTrail();
        }

        void SetupTrail()
        {
            trail.time = trailTime;
            trail.startWidth = startWidth;
            trail.endWidth = endWidth;
            trail.minVertexDistance = minVertexDistance;
            trail.emitting = false;
            trail.autodestruct = false;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.numCapVertices = 4;
            trail.numCornerVertices = 4;
            trail.alignment = LineAlignment.View;
            trail.textureMode = LineTextureMode.Stretch;
            trail.material = CreateTrailMaterial();

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(startColor, 0f),
                    new GradientColorKey(endColor, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(startColor.a, 0f),
                    new GradientAlphaKey(endColor.a, 1f)
                }
            );

            trail.colorGradient = gradient;
            trail.Clear();
        }

        Material CreateTrailMaterial()
        {
            Shader shader = Shader.Find("Sprites/Default");

            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");

            Material mat = new Material(shader);
            mat.name = "Runtime Weapon Trail";
            mat.color = Color.white;
            mat.renderQueue = 3000;
            return mat;
        }

        public void Play()
        {
            Play(defaultStartDelay, defaultDuration);
        }

        public void Play(float startDelay, float duration)
        {
            if (trailRoutine != null)
                StopCoroutine(trailRoutine);

            trailRoutine = StartCoroutine(PlayRoutine(startDelay, duration));
        }

        IEnumerator PlayRoutine(float startDelay, float duration)
        {
            trail.emitting = false;
            trail.Clear();

            if (startDelay > 0f)
                yield return new WaitForSeconds(startDelay);

            trail.Clear();
            trail.emitting = true;

            yield return new WaitForSeconds(duration);

            trail.emitting = false;
            trailRoutine = null;
        }

        public void Stop()
        {
            if (trailRoutine != null)
                StopCoroutine(trailRoutine);

            trail.emitting = false;
            trailRoutine = null;
        }
    }
}