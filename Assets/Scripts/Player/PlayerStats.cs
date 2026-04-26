using UnityEngine;

namespace Pithox.Player
{
    // Stores temporary player combat stat modifiers
    public class PlayerStats : MonoBehaviour
    {
        float damageMultiplier = 1f;
        float damageBuffTimer;

        public float DamageMultiplier => damageMultiplier;

        void Update()
        {
            if (damageBuffTimer <= 0f)
                return;

            damageBuffTimer -= Time.deltaTime;

            if (damageBuffTimer <= 0f)
            {
                damageMultiplier = 1f;
                Debug.Log("Damage buff ended");
            }
        }

        // Applies a temporary damage boost
        public void ApplyDamageBoost(float multiplier, float duration)
        {
            damageMultiplier = multiplier;
            damageBuffTimer = duration;

            Debug.Log($"Damage buff applied: x{multiplier}");
        }
    }
}