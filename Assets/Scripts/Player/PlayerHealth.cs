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

        [Header("Invincibility")]
        [SerializeField] bool invincible;
        [SerializeField] float invincibleTimer;

        [Header("SFX")]
        [SerializeField] AudioSource sfxSource;
        [SerializeField] AudioClip hurtSfx;
        [SerializeField] AudioClip deathSfx;
        [SerializeField] float hurtVolume = 1f;
        [SerializeField] float deathVolume = 1f;
        [SerializeField] bool playHurtSfxOnDeath = false;

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
        public bool IsInvincible => invincible || invincibleTimer > 0f;

        public event Action<float, float> OnHealthChanged;
        public event Action<DamageData> OnDamaged;
        public event Action<DamageData> OnDamageBlocked;
        public event Action OnDied;

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

            if (sfxSource == null)
            {
                sfxSource = GetComponent<AudioSource>();

                if (sfxSource == null)
                    sfxSource = gameObject.AddComponent<AudioSource>();
            }

            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.volume = 1f;
            sfxSource.mute = false;

            UpdateHealthUi(true);
        }

        void Start()
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            UpdateHealthUi(true);
        }

        void Update()
        {
            if (invincibleTimer > 0f)
                invincibleTimer -= Time.deltaTime;
        }

        public void TakeDamage(DamageData damageData)
        {
            if (isDead)
                return;

            if (IsInvincible)
            {
                OnDamageBlocked?.Invoke(damageData);
                return;
            }

            currentHealth -= damageData.Amount;
            currentHealth = Mathf.Max(currentHealth, 0f);

            OnDamaged?.Invoke(damageData);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            UpdateHealthUi(false);

            global::SmoothMidCamera.Shake(damageCameraShake, 0.12f);

            if (currentHealth <= 0f)
            {
                if (playHurtSfxOnDeath)
                    PlaySfx(hurtSfx, hurtVolume);

                Die();
                return;
            }

            PlaySfx(hurtSfx, hurtVolume);
            PlayTakeDamageAnimation();
        }

        void Die()
        {
            if (isDead)
                return;

            isDead = true;

            PlayDeathSfx();
            PlayDieAnimation();

            if (playerController != null)
                playerController.enabled = false;

            if (playerCombatController != null)
                playerCombatController.enabled = false;

            if (playerTombCarry != null)
                playerTombCarry.enabled = false;

            OnDied?.Invoke();
            onDeath?.Invoke();
        }

        public void Heal(float amount)
        {
            if (isDead)
                return;

            if (amount <= 0f)
                return;

            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            UpdateHealthUi(true);
        }

        public void HealPercent(float percent)
        {
            if (isDead)
                return;

            if (percent <= 0f)
                return;

            Heal(maxHealth * percent);
        }

        public void GiveInvincibility(float duration)
        {
            if (isDead)
                return;

            invincibleTimer = Mathf.Max(invincibleTimer, duration);
        }

        public void ClearTimedInvincibility()
        {
            invincibleTimer = 0f;
        }

        public void SetInvincible(bool value)
        {
            invincible = value;
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

        void PlaySfx(AudioClip clip, float volume)
        {
            if (clip == null || sfxSource == null)
                return;

            sfxSource.PlayOneShot(clip, volume);
        }

        void PlayDeathSfx()
        {
            if (deathSfx == null)
                return;

            GameObject audioObject = new GameObject("PlayerDeathSfx");
            audioObject.transform.position = transform.position;

            AudioSource audioSource = audioObject.AddComponent<AudioSource>();
            audioSource.clip = deathSfx;
            audioSource.volume = deathVolume;
            audioSource.spatialBlend = sfxSource != null ? sfxSource.spatialBlend : 0f;
            audioSource.playOnAwake = false;

            audioSource.Play();

            Destroy(audioObject, deathSfx.length + 0.1f);
        }
    }
}