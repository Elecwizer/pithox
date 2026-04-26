using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pithox.Enemies
{
    public class SpawnManager : MonoBehaviour
    {
        [Serializable]
        public class SpawnEntry
        {
            public EnemyBase enemyPrefab;
            [Min(0)] public int weight = 1;
        }

        [Header("Spawn Setup")]
        [SerializeField] List<SpawnEntry> enemies = new();
        [SerializeField] Transform[] spawnPoints;
        [SerializeField] string playerTag = "Player";

        [Header("Timing")]
        [SerializeField] float spawnIntervalMin = 1.5f;
        [SerializeField] float spawnIntervalMax = 3.5f;
        [SerializeField] float difficultyRampEvery = 20f;
        [SerializeField] float intervalReduction = 0.15f;
        [SerializeField] float minimumPossibleInterval = 0.5f;

        [Header("Population")]
        [SerializeField] int maxAliveEnemies = 25;

        readonly List<EnemyBase> aliveEnemies = new();

        float nextSpawnTime;
        float nextRampTime;
        Transform playerTarget;

        void Start()
        {
            playerTarget = GameObject.FindGameObjectWithTag(playerTag)?.transform;
            ScheduleNextSpawn();
            nextRampTime = Time.time + difficultyRampEvery;
        }

        void Update()
        {
            CleanupDeadEnemies();

            if (ShouldRampDifficulty())
            {
                RampDifficulty();
            }

            if (!CanSpawnNow())
                return;

            SpawnEnemy();
            ScheduleNextSpawn();
        }

        bool CanSpawnNow()
        {
            return playerTarget != null
                && Time.time >= nextSpawnTime
                && spawnPoints != null
                && spawnPoints.Length > 0
                && aliveEnemies.Count < maxAliveEnemies
                && enemies.Count > 0;
        }

        bool ShouldRampDifficulty()
        {
            return difficultyRampEvery > 0f && Time.time >= nextRampTime;
        }

        void RampDifficulty()
        {
            spawnIntervalMin = Mathf.Max(minimumPossibleInterval, spawnIntervalMin - intervalReduction);
            spawnIntervalMax = Mathf.Max(
                spawnIntervalMin + 0.1f,
                Mathf.Max(minimumPossibleInterval, spawnIntervalMax - intervalReduction)
            );
            nextRampTime = Time.time + difficultyRampEvery;
        }

        void SpawnEnemy()
        {
            EnemyBase prefab = ChooseWeightedEnemyPrefab();
            if (prefab == null)
                return;

            Transform spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
            EnemyBase enemy = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
            aliveEnemies.Add(enemy);
        }

        EnemyBase ChooseWeightedEnemyPrefab()
        {
            int totalWeight = 0;

            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null && enemies[i].enemyPrefab != null)
                {
                    totalWeight += Mathf.Max(0, enemies[i].weight);
                }
            }

            if (totalWeight <= 0)
                return null;

            int roll = UnityEngine.Random.Range(0, totalWeight);
            int cumulative = 0;

            for (int i = 0; i < enemies.Count; i++)
            {
                SpawnEntry entry = enemies[i];
                if (entry == null || entry.enemyPrefab == null)
                    continue;

                cumulative += Mathf.Max(0, entry.weight);
                if (roll < cumulative)
                {
                    return entry.enemyPrefab;
                }
            }

            return enemies[0].enemyPrefab;
        }

        void ScheduleNextSpawn()
        {
            float delay = UnityEngine.Random.Range(spawnIntervalMin, spawnIntervalMax);
            nextSpawnTime = Time.time + Mathf.Max(minimumPossibleInterval, delay);
        }

        void CleanupDeadEnemies()
        {
            for (int i = aliveEnemies.Count - 1; i >= 0; i--)
            {
                if (aliveEnemies[i] == null)
                {
                    aliveEnemies.RemoveAt(i);
                }
            }
        }
    }
}
