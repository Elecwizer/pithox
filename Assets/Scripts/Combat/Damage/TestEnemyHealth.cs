using UnityEngine;

namespace Pithox.Combat
{
    public class TestEnemyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 50f;

        private float currentHealth;

        private void Awake()
        {
            currentHealth = maxHealth;
        }

        public void TakeDamage(DamageData damageData)
        {
            currentHealth -= damageData.Amount;

            Debug.Log(
                $"{gameObject.name} took {damageData.Amount} damage from chain position {damageData.ChainPosition}. HP: {currentHealth}"
            );

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        private void Die()
        {
            Debug.Log($"{gameObject.name} died.");
            Destroy(gameObject);
        }
    }
}