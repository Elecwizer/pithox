using System;
using System.Collections.Generic;
using Pithox.Game;
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

        [Header("Spawn mix")]
        [Tooltip("No enemy prefab type may exceed this fraction of currently alive spawn-managed enemies (after the next spawn). First spawn ignores the cap.")]
        [SerializeField, Range(0.1f, 1f)] float maxPerPrefabFraction = 0.5f;
        [SerializeField] bool enforceMaxPerPrefabFraction = true;

        [Header("Waves")]
        [SerializeField] WaveManager waveManager;
        [SerializeField] bool disableTimeRampWhenWavesPresent = true;

        WaveManager.WaveConfig activeWaveConfig;

        struct SpawnedEnemy
        {
            public EnemyBase Instance;
            public int EntryIndex;
        }

        readonly List<SpawnedEnemy> aliveSpawns = new();

        float nextSpawnTime;
        float nextRampTime;
        Transform playerTarget;

        void Start()
        {
            if (waveManager == null)
                waveManager = FindAnyObjectByType<WaveManager>();

            playerTarget = GameObject.FindGameObjectWithTag(playerTag)?.transform;
            ScheduleNextSpawn();
            nextRampTime = Time.time + difficultyRampEvery;
        }

        void Update()
        {
            CleanupDeadEnemies();

            if (ShouldRampDifficulty())
                RampDifficulty();

            if (!CanSpawnNow())
                return;

            SpawnEnemy();
            ScheduleNextSpawn();
        }

        bool CanSpawnNow()
        {
            if (waveManager != null && !waveManager.SpawningAllowed)
                return false;

            return playerTarget != null
                && Time.time >= nextSpawnTime
                && spawnPoints != null
                && spawnPoints.Length > 0
                && aliveSpawns.Count < maxAliveEnemies
                && enemies.Count > 0;
        }

        bool ShouldRampDifficulty()
        {
            if (disableTimeRampWhenWavesPresent && waveManager != null)
                return false;

            return difficultyRampEvery > 0f && Time.time >= nextRampTime;
        }

        /// <summary>Called by <see cref="WaveManager"/> at each combat wave start.</summary>
        public void ApplyWaveConfig(WaveManager.WaveConfig cfg)
        {
            activeWaveConfig = cfg;
            if (cfg == null)
                return;

            maxAliveEnemies = Mathf.Max(1, cfg.maxAliveEnemies);
            spawnIntervalMin = Mathf.Max(minimumPossibleInterval, cfg.spawnIntervalMin);
            spawnIntervalMax = Mathf.Max(spawnIntervalMin + 0.05f, cfg.spawnIntervalMax);
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
            int entryIndex = ChooseWeightedSpawnEntryIndex();
            if (entryIndex < 0)
                return;

            SpawnEntry pick = enemies[entryIndex];
            EnemyBase prefab = pick.enemyPrefab;
            if (prefab == null)
                return;

            Transform spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
            EnemyBase enemy = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
            aliveSpawns.Add(new SpawnedEnemy { Instance = enemy, EntryIndex = entryIndex });

            if (waveManager != null && activeWaveConfig != null)
            {
                enemy.ApplyWaveModifiers(
                    activeWaveConfig.visualTier,
                    activeWaveConfig.healthMultiplier,
                    activeWaveConfig.speedMultiplier,
                    activeWaveConfig.damageMultiplier
                );
            }
        }

        int ChooseWeightedSpawnEntryIndex()
        {
            List<int> eligible = BuildEligibleEntryIndices();
            if (eligible.Count == 0)
                return -1;

            int totalWeight = 0;
            for (int e = 0; e < eligible.Count; e++)
            {
                int i = eligible[e];
                totalWeight += Mathf.Max(0, enemies[i].weight);
            }

            if (totalWeight <= 0)
                return eligible[0];

            int roll = UnityEngine.Random.Range(0, totalWeight);
            int cumulative = 0;
            for (int e = 0; e < eligible.Count; e++)
            {
                int i = eligible[e];
                cumulative += Mathf.Max(0, enemies[i].weight);
                if (roll < cumulative)
                    return i;
            }

            return eligible[eligible.Count - 1];
        }

        List<int> BuildEligibleEntryIndices()
        {
            var eligible = new List<int>();
            int t = aliveSpawns.Count;

            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] == null || enemies[i].enemyPrefab == null || enemies[i].weight <= 0)
                    continue;

                if (!enforceMaxPerPrefabFraction || t <= 0)
                {
                    eligible.Add(i);
                    continue;
                }

                int capAfterSpawn = Mathf.FloorToInt((t + 1) * maxPerPrefabFraction);
                if (capAfterSpawn < 1)
                    capAfterSpawn = 1;

                EnemyBase prefab = enemies[i].enemyPrefab;
                int countThisPrefab = CountAliveWithPrefab(prefab);

                if (countThisPrefab + 1 <= capAfterSpawn)
                    eligible.Add(i);
            }

            if (eligible.Count > 0)
                return eligible;

            // Cannot satisfy cap (e.g. single prefab in list). Fall back: least-represented prefab entries.
            int best = int.MaxValue;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] == null || enemies[i].enemyPrefab == null || enemies[i].weight <= 0)
                    continue;

                int c = CountAliveWithPrefab(enemies[i].enemyPrefab);
                if (c < best)
                    best = c;
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] == null || enemies[i].enemyPrefab == null || enemies[i].weight <= 0)
                    continue;

                if (CountAliveWithPrefab(enemies[i].enemyPrefab) == best)
                    eligible.Add(i);
            }

            return eligible;
        }

        int CountAliveWithPrefab(EnemyBase prefab)
        {
            int n = 0;
            for (int s = 0; s < aliveSpawns.Count; s++)
            {
                if (aliveSpawns[s].Instance == null)
                    continue;

                int idx = aliveSpawns[s].EntryIndex;
                if (idx < 0 || idx >= enemies.Count || enemies[idx] == null)
                    continue;

                if (enemies[idx].enemyPrefab == prefab)
                    n++;
            }

            return n;
        }

        void ScheduleNextSpawn()
        {
            float delay = UnityEngine.Random.Range(spawnIntervalMin, spawnIntervalMax);
            nextSpawnTime = Time.time + Mathf.Max(minimumPossibleInterval, delay);
        }

        void CleanupDeadEnemies()
        {
            for (int i = aliveSpawns.Count - 1; i >= 0; i--)
            {
                if (aliveSpawns[i].Instance == null)
                    aliveSpawns.RemoveAt(i);
            }
        }

        /// <summary>Spawn-tracked enemies still alive (used by <see cref="WaveManager"/> cleanup phase).</summary>
        public int AliveEnemyCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < aliveSpawns.Count; i++)
                {
                    if (aliveSpawns[i].Instance != null)
                        n++;
                }

                return n;
            }
        }
    }
}
