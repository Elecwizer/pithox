using System;
using Pithox.Combat;
using UnityEngine;
using UnityEngine.Events;

namespace Pithox.Player
{
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] float maxHealth = 100f;
        [SerializeField] float damageCameraShake = 0.25f;
        [SerializeField] UnityEvent onDeath;

        float currentHealth;
        bool isDead;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;

        public event Action<float, float> OnHealthChanged;
        public event Action<DamageData> OnDamaged;

        void Awake()
        {
            currentHealth = maxHealth;
        }

        void Start()
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void TakeDamage(DamageData damageData)
        {
            if (isDead)
                return;

            currentHealth -= damageData.Amount;

            OnDamaged?.Invoke(damageData);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            SmoothMidCamera.Shake(damageCameraShake, 0.12f);

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        void Die()
        {
            isDead = true;
            onDeath?.Invoke();
        }

        public void Heal(float amount)
        {
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }
}
