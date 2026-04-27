using System;
using UnityEngine;
using Pithox.Player;

namespace Pithox.Game
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        [SerializeField] float baseStreakWindow = 5f;
        [SerializeField] PlayerStats playerStats;

        int score;
        int streak;
        float streakTimeRemaining;

        public int Score => score;
        public int Streak => streak;
        public float StreakTimeRemaining => streakTimeRemaining;
        public float StreakWindowDuration => baseStreakWindow + (playerStats != null ? playerStats.StreakWindowBonus : 0f);

        public static event Action<int> OnScoreChanged;
        public static event Action<int> OnStreakChanged;
        public static event Action<int> OnPointsScored;

        void Awake()
        {
            Instance = this;
        }

        void OnEnable()
        {
            PlayerTombCarry.OnTombPlaced += HandleTombPlaced;
        }

        void OnDisable()
        {
            PlayerTombCarry.OnTombPlaced -= HandleTombPlaced;
        }

        void Update()
        {
            if (streak <= 0)
                return;

            streakTimeRemaining -= Time.deltaTime;

            if (streakTimeRemaining <= 0f)
            {
                streak = 0;
                streakTimeRemaining = 0f;
                OnStreakChanged?.Invoke(0);
            }
        }

        void HandleTombPlaced()
        {
            streak += 1;
            int points = 100 * streak;
            score += points;
            streakTimeRemaining = StreakWindowDuration;

            OnPointsScored?.Invoke(points);
            OnScoreChanged?.Invoke(score);
            OnStreakChanged?.Invoke(streak);
        }
    }
}
