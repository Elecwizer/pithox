using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Pithox.Player
{
    public class PlayerStats : MonoBehaviour
    {
        [Header("XP")]
        [SerializeField] int currentLevel = 1;
        [SerializeField] int currentXp;
        [SerializeField] int baseXpToNextLevel = 100;
        [SerializeField] int flatXpIncreasePerLevel = 25;
        [SerializeField] float xpGrowthMultiplier = 1f;
        [SerializeField] bool useXpTable;
        [SerializeField] int[] xpNeededPerLevel;

        [Header("XP UI")]
        [SerializeField] GameObject xpUiRoot;
        [SerializeField] Image xpFillImage;
        [SerializeField] Image xpFillAnimationImage;
        [SerializeField] Component xpText;
        [SerializeField] Component levelText;

        [Header("XP Animation")]
        [SerializeField] float xpBarSmoothTime = 0.15f;

        [Header("Level Up VFX")]
        [SerializeField] GameObject levelUpVfxPrefab;
        [SerializeField] Transform levelUpVfxSpawnPoint;
        [SerializeField] float levelUpVfxLifetime = 1f;
        [SerializeField] bool levelUpVfxUseUnscaledTime = true;

        [Header("Current Upgrade Stats")]
        [SerializeField] float pickupRangeBonus;
        [SerializeField] float streakWindowBonus;
        [SerializeField] float attackDamageBonus;
        [SerializeField] float moveSpeedBonus;
        [SerializeField] float dashSpeedBonus;
        [SerializeField] float dashCooldownReduction;
        [SerializeField] float orbitDamageBonus;
        [SerializeField] float beamDamageBonus;

        [Header("Unlocks")]
        [SerializeField] bool orbitUnlocked;
        [SerializeField] bool beamUnlocked;
        [SerializeField] bool damageAuraEnabled;

        float damageMultiplier = 1f;
        float damageBuffTimer;
        Coroutine xpBarRoutine;

        public int CurrentLevel => currentLevel;
        public int CurrentXp => currentXp;
        public int XpToNextLevel => GetXpRequiredForLevel(currentLevel);

        public float DamageMultiplier => damageMultiplier;
        public float PickupRangeBonus => pickupRangeBonus;
        public float StreakWindowBonus => streakWindowBonus;
        public float AttackDamageBonus => attackDamageBonus;
        public float MoveSpeedBonus => moveSpeedBonus;
        public float DashSpeedBonus => dashSpeedBonus;
        public float DashCooldownReduction => dashCooldownReduction;
        public float OrbitDamageBonus => orbitDamageBonus;
        public float BeamDamageBonus => beamDamageBonus;

        public bool OrbitUnlocked => orbitUnlocked;
        public bool BeamUnlocked => beamUnlocked;
        public bool DamageAuraEnabled => damageAuraEnabled;

        public event Action<int> OnLevelUp;
        public event Action<int, int, int> OnXpChanged;

        void Awake()
        {
            currentLevel = Mathf.Max(1, currentLevel);
            currentXp = Mathf.Max(0, currentXp);
            UpdateXpUi(true);
        }

        void Start()
        {
            UpdateXpUi(true);
        }

        void Update()
        {
            if (damageBuffTimer <= 0f)
                return;

            damageBuffTimer -= Time.deltaTime;

            if (damageBuffTimer <= 0f)
                damageMultiplier = 1f;
        }

        public void AddExperience(int amount)
        {
            if (amount <= 0)
                return;

            currentXp += amount;

            bool leveledUp = false;

            while (currentXp >= GetXpRequiredForLevel(currentLevel))
            {
                currentXp -= GetXpRequiredForLevel(currentLevel);
                currentLevel++;
                leveledUp = true;

                OnLevelUp?.Invoke(currentLevel);
            }

            if (leveledUp)
                SpawnLevelUpVfx();

            UpdateXpUi(false);
        }

        public int GetXpRequiredForLevel(int level)
        {
            level = Mathf.Max(1, level);

            if (useXpTable && xpNeededPerLevel != null && xpNeededPerLevel.Length > 0)
            {
                int index = Mathf.Clamp(level - 1, 0, xpNeededPerLevel.Length - 1);
                return Mathf.Max(1, xpNeededPerLevel[index]);
            }

            float growth = Mathf.Pow(Mathf.Max(1f, xpGrowthMultiplier), level - 1);
            float needed = (baseXpToNextLevel + flatXpIncreasePerLevel * (level - 1)) * growth;

            return Mathf.Max(1, Mathf.RoundToInt(needed));
        }

        void SpawnLevelUpVfx()
        {
            if (levelUpVfxPrefab == null)
                return;

            Vector3 spawnPos = levelUpVfxSpawnPoint != null ? levelUpVfxSpawnPoint.position : transform.position;
            Quaternion spawnRot = levelUpVfxSpawnPoint != null ? levelUpVfxSpawnPoint.rotation : Quaternion.identity;

            GameObject vfx = Instantiate(levelUpVfxPrefab, spawnPos, spawnRot);

            if (levelUpVfxUseUnscaledTime)
            {
                ParticleSystem[] particles = vfx.GetComponentsInChildren<ParticleSystem>();

                for (int i = 0; i < particles.Length; i++)
                {
                    ParticleSystem.MainModule main = particles[i].main;
                    main.useUnscaledTime = true;
                }
            }

            if (levelUpVfxLifetime > 0f)
                StartCoroutine(DestroyVfxAfterRealtime(vfx, levelUpVfxLifetime));
        }

        IEnumerator DestroyVfxAfterRealtime(GameObject vfx, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);

            if (vfx != null)
                Destroy(vfx);
        }

        public void ApplyDamageBoost(float multiplier, float duration)
        {
            damageMultiplier = Mathf.Max(0f, multiplier);
            damageBuffTimer = Mathf.Max(0f, duration);
        }

        public void AddPickupRange(float amount)
        {
            pickupRangeBonus += amount;
        }

        public void AddStreakWindow(float amount)
        {
            streakWindowBonus += amount;
        }

        public void AddAttackDamage(float amount)
        {
            attackDamageBonus += amount;
        }

        public void AddAttackDamagePercent(float percent)
        {
            attackDamageBonus += percent;
        }

        public void AddMoveSpeed(float amount)
        {
            moveSpeedBonus += amount;
        }

        public void AddMoveSpeedPercent(float percent)
        {
            moveSpeedBonus += percent;
        }

        public void AddDashSpeedPercent(float percent)
        {
            dashSpeedBonus += percent;
        }

        public void AddDashCooldownReduction(float percent)
        {
            dashCooldownReduction = Mathf.Clamp01(dashCooldownReduction + percent);
        }

        public void AddOrbitDamage(float amount)
        {
            orbitDamageBonus += amount;
        }

        public void AddBeamDamage(float amount)
        {
            beamDamageBonus += amount;
        }

        public void UnlockOrbit()
        {
            orbitUnlocked = true;
        }

        public void UnlockBeam()
        {
            beamUnlocked = true;
        }

        public void EnableDamageAura()
        {
            damageAuraEnabled = true;
        }

        void UpdateXpUi(bool snap)
        {
            if (xpUiRoot != null && !xpUiRoot.activeSelf)
                xpUiRoot.SetActive(true);

            int needed = GetXpRequiredForLevel(currentLevel);
            float fill = Mathf.Clamp01((float)currentXp / Mathf.Max(1, needed));

            if (xpFillImage != null)
                xpFillImage.fillAmount = fill;

            if (snap)
            {
                if (xpBarRoutine != null)
                    StopCoroutine(xpBarRoutine);

                if (xpFillAnimationImage != null)
                    xpFillAnimationImage.fillAmount = fill;
            }
            else
            {
                if (xpFillAnimationImage != null)
                {
                    if (xpBarRoutine != null)
                        StopCoroutine(xpBarRoutine);

                    xpBarRoutine = StartCoroutine(AnimateXpBar(fill));
                }
            }

            SetText(xpText, currentXp + " / " + needed);
            SetText(levelText, "Lv. " + currentLevel);

            OnXpChanged?.Invoke(currentXp, needed, currentLevel);
        }

        IEnumerator AnimateXpBar(float targetFill)
        {
            float startFill = xpFillAnimationImage.fillAmount;
            float time = 0f;

            while (time < xpBarSmoothTime)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / Mathf.Max(0.01f, xpBarSmoothTime));
                xpFillAnimationImage.fillAmount = Mathf.Lerp(startFill, targetFill, t);
                yield return null;
            }

            xpFillAnimationImage.fillAmount = targetFill;
        }

        void SetText(Component target, string value)
        {
            if (target == null)
                return;

            Text uiText = target as Text;

            if (uiText == null)
                uiText = target.GetComponent<Text>();

            if (uiText != null)
            {
                uiText.text = value;
                return;
            }

            Component[] components = target.GetComponents<Component>();

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
    }
}