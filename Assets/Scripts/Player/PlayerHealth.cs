using UnityEngine;

namespace Pithox.Player
{
    // Handles player health and healing
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] float maxHealth = 100f;

        float currentHealth;

        void Awake()
        {
            currentHealth = maxHealth;
        }

        // Restores player health
        public void Heal(float amount)
        {
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
            Debug.Log($"Player healed {amount}. HP: {currentHealth}/{maxHealth}");
        }
    }
}