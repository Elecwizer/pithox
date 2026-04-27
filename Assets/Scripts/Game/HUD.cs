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

        [SerializeField] TMP_Text scoreText;
        [SerializeField] TMP_Text levelText;
        [SerializeField] TMP_Text streakText;
        [SerializeField] Image streakTimerFill;
        [SerializeField] Image healthFill;
        [SerializeField] TMP_Text healthText;

        void OnEnable()
        {
            ScoreManager.OnScoreChanged += HandleScore;
            ScoreManager.OnStreakChanged += HandleStreak;
            LevelManager.OnLevelUp += HandleLevel;
            if (playerHealth != null) playerHealth.OnHealthChanged += HandleHealth;
        }

        void OnDisable()
        {
            ScoreManager.OnScoreChanged -= HandleScore;
            ScoreManager.OnStreakChanged -= HandleStreak;
            LevelManager.OnLevelUp -= HandleLevel;
            if (playerHealth != null) playerHealth.OnHealthChanged -= HandleHealth;
        }

        void Start()
        {
            HandleScore(scoreManager != null ? scoreManager.Score : 0);
            HandleStreak(scoreManager != null ? scoreManager.Streak : 0);
            HandleLevel(levelManager != null ? levelManager.CurrentLevel : 1);
            if (playerHealth != null)
                HandleHealth(playerHealth.CurrentHealth, playerHealth.MaxHealth);
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
            if (scoreText != null) scoreText.text = $"Score: {newScore}";
        }

        void HandleStreak(int newStreak)
        {
            if (streakText != null) streakText.text = newStreak > 0 ? $"x{newStreak}" : "";
        }

        void HandleLevel(int newLevel)
        {
            if (levelText != null) levelText.text = $"Lv {newLevel}";
        }

        void HandleHealth(float current, float max)
        {
            if (healthFill != null)
                healthFill.fillAmount = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            if (healthText != null)
                healthText.text = $"{Mathf.Max(0, Mathf.CeilToInt(current))} / {Mathf.CeilToInt(max)}";
        }
    }
}
