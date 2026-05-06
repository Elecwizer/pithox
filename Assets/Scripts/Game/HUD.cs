using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Pithox.Player;

namespace Pithox.Game
{
    public class HUD : MonoBehaviour
    {
        [SerializeField] ScoreManager scoreManager;
        [SerializeField] LevelManager levelManager;
        [SerializeField] PlayerHealth playerHealth;
        [SerializeField] PlayerStats playerStats;

        [Header("Score UI")]
        [SerializeField] TMP_Text scoreText;
        [SerializeField] TMP_Text streakText;
        [SerializeField] Image streakTimerFill;

        [Header("Health UI")]
        [SerializeField] Image healthFill;
        [SerializeField] TMP_Text healthText;

        [Header("XP UI")]
        [SerializeField] Image xpFill;
        [SerializeField] Image xpFillAnimation;
        [SerializeField] TMP_Text xpText;
        [SerializeField] TMP_Text levelText;
        [SerializeField] float xpAnimationTime = 0.2f;

        Coroutine xpRoutine;

        void Awake()
        {
            if (playerHealth == null)
                playerHealth = FindAnyObjectByType<PlayerHealth>();

            if (playerStats == null)
                playerStats = FindAnyObjectByType<PlayerStats>();

            if (scoreManager == null)
                scoreManager = FindAnyObjectByType<ScoreManager>();

            if (levelManager == null)
                levelManager = FindAnyObjectByType<LevelManager>();
        }

        void OnEnable()
        {
            ScoreManager.OnScoreChanged += HandleScore;
            ScoreManager.OnStreakChanged += HandleStreak;

            if (playerHealth != null)
                playerHealth.OnHealthChanged += HandleHealth;

            if (playerStats != null)
                playerStats.OnXpChanged += HandleXp;
        }

        void OnDisable()
        {
            ScoreManager.OnScoreChanged -= HandleScore;
            ScoreManager.OnStreakChanged -= HandleStreak;

            if (playerHealth != null)
                playerHealth.OnHealthChanged -= HandleHealth;

            if (playerStats != null)
                playerStats.OnXpChanged -= HandleXp;
        }

        void Start()
        {
            HandleScore(scoreManager != null ? scoreManager.Score : 0);
            HandleStreak(scoreManager != null ? scoreManager.Streak : 0);

            if (playerHealth != null)
                HandleHealth(playerHealth.CurrentHealth, playerHealth.MaxHealth);

            if (playerStats != null)
                HandleXp(playerStats.CurrentXp, playerStats.XpToNextLevel, playerStats.CurrentLevel);
            else if (levelManager != null && levelText != null)
                levelText.text = "Lv. " + levelManager.CurrentLevel;
        }

        void Update()
        {
            if (streakTimerFill == null || scoreManager == null)
                return;

            if (scoreManager.Streak <= 0 || scoreManager.StreakWindowDuration <= 0f)
                streakTimerFill.fillAmount = 0f;
            else
                streakTimerFill.fillAmount = scoreManager.StreakTimeRemaining / scoreManager.StreakWindowDuration;
        }

        void HandleScore(int newScore)
        {
            if (scoreText != null)
                scoreText.text = "Score: " + newScore;
        }

        void HandleStreak(int newStreak)
        {
            if (streakText != null)
                streakText.text = newStreak > 0 ? "x" + newStreak : "";
        }

        void HandleHealth(float current, float max)
        {
            if (healthFill != null)
                healthFill.fillAmount = max > 0f ? Mathf.Clamp01(current / max) : 0f;

            if (healthText != null)
                healthText.text = Mathf.Max(0, Mathf.CeilToInt(current)) + " / " + Mathf.CeilToInt(max);
        }

        void HandleXp(int currentXp, int xpNeeded, int level)
        {
            float fill = xpNeeded > 0 ? Mathf.Clamp01((float)currentXp / xpNeeded) : 0f;

            if (xpFill != null)
                xpFill.fillAmount = fill;

            if (xpFillAnimation != null)
            {
                if (xpRoutine != null)
                    StopCoroutine(xpRoutine);

                xpRoutine = StartCoroutine(AnimateXpFill(fill));
            }

            if (xpText != null)
                xpText.text = currentXp + " / " + xpNeeded;

            if (levelText != null)
                levelText.text = "Lv. " + level;
        }

        IEnumerator AnimateXpFill(float target)
        {
            float start = xpFillAnimation.fillAmount;
            float time = 0f;

            while (time < xpAnimationTime)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / Mathf.Max(0.01f, xpAnimationTime));
                xpFillAnimation.fillAmount = Mathf.Lerp(start, target, t);
                yield return null;
            }

            xpFillAnimation.fillAmount = target;
        }
    }
}
