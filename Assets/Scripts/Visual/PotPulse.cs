using UnityEngine;
using Pithox.Player;

namespace Pithox.Visual
{
    public class PotPulse : MonoBehaviour
    {
        [SerializeField] Light potLight;
        [SerializeField] Renderer liquidRenderer;
        [SerializeField] float baseIntensity = 4f;
        [SerializeField] float pulseAmplitude = 1.2f;
        [SerializeField] float pulseSpeed = 1.6f;
        [SerializeField] float spikeMultiplier = 3f;
        [SerializeField] float spikeDecay = 4f;

        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        Color baseEmission = new Color(0.8f, 0.2f, 1f);
        MaterialPropertyBlock block;
        float spike;

        void Awake()
        {
            block = new MaterialPropertyBlock();
            if (potLight == null) potLight = GetComponentInChildren<Light>();
        }

        void OnEnable()
        {
            PlayerTombCarry.OnTombPlaced += HandleTombPlaced;
        }

        void OnDisable()
        {
            PlayerTombCarry.OnTombPlaced -= HandleTombPlaced;
        }

        void HandleTombPlaced()
        {
            spike = 1f;
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

            if (potLight != null) potLight.intensity = intensity;

            if (liquidRenderer != null)
            {
                liquidRenderer.GetPropertyBlock(block);
                block.SetColor(EmissionColorId, baseEmission * intensity);
                liquidRenderer.SetPropertyBlock(block);
            }
        }
    }
}
