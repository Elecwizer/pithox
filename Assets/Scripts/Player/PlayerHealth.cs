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

        [Header("Animation")]
        [SerializeField] Animator animator;
        [SerializeField] string takeDamageTrigger = "TakeDamage";
        [SerializeField] string dieTrigger = "Die";

        [Header("Disable On Death")]
        [SerializeField] PlayerController playerController;
        [SerializeField] PlayerCombatController playerCombatController;
        [SerializeField] PlayerTombCarry playerTombCarry;

        float currentHealth;
        bool isDead;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDead => isDead;

        public event Action<float, float> OnHealthChanged;
        public event Action<DamageData> OnDamaged;

        void Awake()
        {
            currentHealth = maxHealth;

            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            if (playerController == null)
                playerController = GetComponent<PlayerController>();

            if (playerCombatController == null)
                playerCombatController = GetComponent<PlayerCombatController>();

            if (playerTombCarry == null)
                playerTombCarry = GetComponent<PlayerTombCarry>();
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
            currentHealth = Mathf.Max(currentHealth, 0f);

            OnDamaged?.Invoke(damageData);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            SmoothMidCamera.Shake(damageCameraShake, 0.12f);

            if (currentHealth <= 0f)
            {
                Die();
                return;
            }

            PlayTakeDamageAnimation();
        }

        void Die()
        {
            if (isDead)
                return;

            isDead = true;

            PlayDieAnimation();

            if (playerController != null)
                playerController.enabled = false;

            if (playerCombatController != null)
                playerCombatController.enabled = false;

            if (playerTombCarry != null)
                playerTombCarry.enabled = false;

            onDeath?.Invoke();
        }

        public void Heal(float amount)
        {
            if (isDead)
                return;

            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        void PlayTakeDamageAnimation()
        {
            if (animator == null)
                return;

            animator.SetTrigger(takeDamageTrigger);
        }

        void PlayDieAnimation()
        {
            if (animator == null)
                return;

            animator.SetTrigger(dieTrigger);
        }
    }
}