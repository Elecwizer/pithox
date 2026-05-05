using System;
using System.Collections;
using Pithox.Combat;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Pithox.Player
{
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] float maxHealth = 100f;
        [SerializeField] float damageCameraShake = 0.25f;
        [SerializeField] UnityEvent onDeath;

        [Header("Health UI")]
        [SerializeField] GameObject healthUiRoot;
        [SerializeField] Image healthFillImage;
        [SerializeField] Image healthFillAnimationImage;
        [SerializeField] Component healthText;
        [SerializeField] bool roundHealthText = true;

        [Header("Health Animation")]
        [SerializeField] float damageBarDelay = 0.35f;
        [SerializeField] float damageBarSmoothTime = 0.45f;

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
        Coroutine damageBarRoutine;

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

            UpdateHealthUi(true);
        }

        void Start()
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            UpdateHealthUi(true);
        }

        public void TakeDamage(DamageData damageData)
        {
            if (isDead)
                return;

            currentHealth -= damageData.Amount;
            currentHealth = Mathf.Max(currentHealth, 0f);

            OnDamaged?.Invoke(damageData);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            UpdateHealthUi(false);

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
            UpdateHealthUi(true);
        }

        public void SetMaxHealth(float newMaxHealth, bool fillHealth)
        {
            maxHealth = Mathf.Max(1f, newMaxHealth);

            if (fillHealth)
                currentHealth = maxHealth;
            else
                currentHealth = Mathf.Min(currentHealth, maxHealth);

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            UpdateHealthUi(true);
        }

        void UpdateHealthUi(bool snapAnimation)
        {
            if (healthUiRoot != null && !healthUiRoot.activeSelf)
                healthUiRoot.SetActive(true);

            float fill = Mathf.Clamp01(currentHealth / Mathf.Max(1f, maxHealth));

            if (healthFillImage != null)
                healthFillImage.fillAmount = fill;

            if (snapAnimation)
            {
                if (damageBarRoutine != null)
                    StopCoroutine(damageBarRoutine);

                if (healthFillAnimationImage != null)
                    healthFillAnimationImage.fillAmount = fill;
            }
            else
            {
                if (healthFillAnimationImage != null)
                {
                    if (damageBarRoutine != null)
                        StopCoroutine(damageBarRoutine);

                    damageBarRoutine = StartCoroutine(AnimateDamageBar(fill));
                }
            }

            SetHealthText();
        }

        IEnumerator AnimateDamageBar(float targetFill)
        {
            yield return new WaitForSeconds(damageBarDelay);

            if (healthFillAnimationImage == null)
                yield break;

            float startFill = healthFillAnimationImage.fillAmount;
            float time = 0f;

            while (time < damageBarSmoothTime)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / Mathf.Max(0.01f, damageBarSmoothTime));

                healthFillAnimationImage.fillAmount = Mathf.Lerp(startFill, targetFill, t);

                yield return null;
            }

            healthFillAnimationImage.fillAmount = targetFill;
        }

        void SetHealthText()
        {
            if (healthText == null)
                return;

            string value;

            if (roundHealthText)
                value = Mathf.CeilToInt(currentHealth) + " / " + Mathf.CeilToInt(maxHealth);
            else
                value = currentHealth.ToString("0.0") + " / " + maxHealth.ToString("0.0");

            Text uiText = healthText as Text;

            if (uiText == null)
                uiText = healthText.GetComponent<Text>();

            if (uiText != null)
            {
                uiText.text = value;
                return;
            }

            Component[] components = healthText.GetComponents<Component>();

            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                    continue;

                var textProperty = components[i].GetType().GetProperty("text");

                if (textProperty != null && textProperty.CanWrite)
                {
                    textProperty.SetValue(components[i], value, null);
                    return;
                }
            }
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