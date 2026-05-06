using System;
using UnityEngine;
using Pithox.Player;

namespace Pithox.Game
{
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] PlayerStats playerStats;

        int currentLevel = 1;

        public int CurrentLevel => currentLevel;

        public static event Action<int> OnLevelUp;

        void Awake()
        {
            if (playerStats == null)
                playerStats = FindAnyObjectByType<PlayerStats>();

            if (playerStats != null)
                currentLevel = playerStats.CurrentLevel;
        }

        void OnEnable()
        {
            if (playerStats == null)
                playerStats = FindAnyObjectByType<PlayerStats>();

            if (playerStats != null)
                playerStats.OnLevelUp += HandlePlayerLevelUp;
        }

        void OnDisable()
        {
            if (playerStats != null)
                playerStats.OnLevelUp -= HandlePlayerLevelUp;
        }

        void HandlePlayerLevelUp(int newLevel)
        {
            currentLevel = newLevel;
            OnLevelUp?.Invoke(newLevel);
        }
    }
}
