using System;
using UnityEngine;

namespace Pithox.Game
{
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] ScoreManager scoreManager;
        [SerializeField] int[] thresholds = { 500, 1500, 3500, 7000, 12000 };

        int currentLevel = 1;
        int nextThresholdIndex;

        public int CurrentLevel => currentLevel;

        public static event Action<int> OnLevelUp;

        void OnEnable()
        {
            ScoreManager.OnScoreChanged += HandleScoreChanged;
        }

        void OnDisable()
        {
            ScoreManager.OnScoreChanged -= HandleScoreChanged;
        }

        void HandleScoreChanged(int newScore)
        {
            while (nextThresholdIndex < thresholds.Length && newScore >= thresholds[nextThresholdIndex])
            {
                currentLevel += 1;
                nextThresholdIndex += 1;
                OnLevelUp?.Invoke(currentLevel);
            }
        }
    }
}
