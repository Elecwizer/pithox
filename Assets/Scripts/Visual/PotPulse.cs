using UnityEngine;
using Pithox.Player;

namespace Pithox.Visual
{
    public class PotPulse : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Light potLight;
        [SerializeField] Renderer liquidRenderer;

        [Header("Pulse")]
        [SerializeField] bool pulseOnTombPlaced = true;
        [SerializeField] float baseIntensity = 4f;
        [SerializeField] float pulseAmplitude = 1.2f;
        [SerializeField] float pulseSpeed = 1.6f;
        [SerializeField] float spikeMultiplier = 3f;
        [SerializeField] float spikeDecay = 4f;

        [Header("Place VFX")]
        [SerializeField] GameObject placeVfxPrefab;
        [SerializeField] Transform placeVfxPoint;
        [SerializeField] float placeVfxLifetime = 1f;

        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        Color baseEmission = new Color(0.8f, 0.2f, 1f);
        MaterialPropertyBlock block;
        float spike;

        void Awake()
        {
            block = new MaterialPropertyBlock();

            if (potLight == null)
                potLight = GetComponentInChildren<Light>();
        }

        void OnEnable()
        {
            if (pulseOnTombPlaced)
                PlayerTombCarry.OnTombPlaced += TriggerPulse;
        }

        void OnDisable()
        {
            PlayerTombCarry.OnTombPlaced -= TriggerPulse;
        }

        public void TriggerPulse()
        {
            spike = 1f;
            SpawnPlaceVfx();
        }

        void SpawnPlaceVfx()
        {
            if (placeVfxPrefab == null)
                return;

            Transform point = placeVfxPoint != null ? placeVfxPoint : transform;
            GameObject vfx = Instantiate(placeVfxPrefab, point.position, point.rotation);
            Destroy(vfx, Mathf.Max(0.05f, placeVfxLifetime));
        }

        void Update()
        {
            float wave = (Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f) * pulseAmplitude;
            float intensity = baseIntensity + wave;

            if (spike > 0f)
            {
                intensity *= 1f + spike * (spikeMultiplier - 1f);
                spike = Mathf.Max(0f, spike - Time.deltaTime * spikeDecay);
            }

            if (potLight != null)
                potLight.intensity = intensity;

            if (liquidRenderer != null)
            {
                liquidRenderer.GetPropertyBlock(block);
                block.SetColor(EmissionColorId, baseEmission * intensity);
                liquidRenderer.SetPropertyBlock(block);
            }
        }
    }
}
