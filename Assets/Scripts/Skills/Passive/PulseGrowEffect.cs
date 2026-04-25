using UnityEngine;

namespace Pithox.Skills
{
    // Handles pulse expansion over time
    public class PulseGrowEffect : MonoBehaviour
    {
        [SerializeField] float maxRadius = 5f;
        [SerializeField] float growDuration = 0.35f;
        [SerializeField] float height = 0.1f;

        float timer;

        void Update()
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / growDuration);
            float currentRadius = Mathf.Lerp(0.1f, maxRadius, t);

            transform.localScale = new Vector3(currentRadius, height, currentRadius);
        }
    }
}