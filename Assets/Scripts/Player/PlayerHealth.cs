using Pithox.Combat;
using UnityEngine;
using UnityEngine.Events;

namespace Pithox.Player
{
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] float maxHealth = 100f;
        [SerializeField] UnityEvent onDeath;

        float currentHealth;
        bool isDead;

        void Awake()
        {
            currentHealth = maxHealth;
        }

        public void TakeDamage(DamageData damageData)
        {
            if (isDead)
                return;

            currentHealth -= damageData.Amount;
            Debug.Log($"Player took {damageData.Amount} damage. HP: {currentHealth}");

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        void Die()
        {
            isDead = true;
            Debug.Log("Player died.");
            onDeath?.Invoke();
        }

        // Restores player health
        public void Heal(float amount)
        {
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
            Debug.Log($"Player healed {amount}. HP: {currentHealth}/{maxHealth}");
        }
    }

}
