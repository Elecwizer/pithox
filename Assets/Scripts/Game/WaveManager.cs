using System;
using UnityEngine;

namespace Pithox.Game
{
    /// <summary>
    /// Five waves: <b>30s spawn window</b> → stop spawning → player <b>clears all spawned enemies</b> →
    /// <b>10s</b> intermission → next wave. Wave 1 starts immediately. After wave 5 is clear,
    /// waits <see cref="bossSpawnDelaySeconds"/> then spawns the boss prefab.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public class WaveManager : MonoBehaviour
    {
        public enum WavePhase
        {
            /// <summary>Enemies spawn for <see cref="combatDurationSeconds"/>.</summary>
            CombatSpawning,
            /// <summary>Spawning off; wait until <see cref="Enemies.SpawnManager.AliveEnemyCount"/> is zero.</summary>
            CombatCleanup,
            Intermission,
            BossCountdown,
            Finished
        }

        [Serializable]
        public class WaveConfig
        {
            [Range(1, 3)] public int visualTier = 1;
            [Min(0.01f)] public float healthMultiplier = 1f;
            [Min(0.01f)] public float speedMultiplier = 1f;
            [Min(0.01f)] public float damageMultiplier = 1f;
            [Min(1)] public int maxAliveEnemies = 14;
            public float spawnIntervalMin = 2f;
            public float spawnIntervalMax = 4f;
        }

        [Header("Timing")]
        [SerializeField] float combatDurationSeconds = 30f;
        [SerializeField] float intermissionSeconds = 10f;
        [SerializeField] float bossSpawnDelaySeconds = 5f;
        [SerializeField] int totalWaves = 5;

        [Header("Waves (size should match Total Waves)")]
        [SerializeField] WaveConfig[] waves =
        {
            new WaveConfig
            {
                visualTier = 1,
                healthMultiplier = 1f,
                speedMultiplier = 1f,
                damageMultiplier = 1f,
                maxAliveEnemies = 10,
                spawnIntervalMin = 2.2f,
                spawnIntervalMax = 4.2f
            },
            new WaveConfig
            {
                visualTier = 1,
                healthMultiplier = 1.12f,
                speedMultiplier = 1.06f,
                damageMultiplier = 1.08f,
                maxAliveEnemies = 14,
                spawnIntervalMin = 2f,
                spawnIntervalMax = 3.8f
            },
            new WaveConfig
            {
                visualTier = 2,
                healthMultiplier = 1.28f,
                speedMultiplier = 1.12f,
                damageMultiplier = 1.15f,
                maxAliveEnemies = 18,
                spawnIntervalMin = 1.7f,
                spawnIntervalMax = 3.4f
            },
            new WaveConfig
            {
                visualTier = 2,
                healthMultiplier = 1.45f,
                speedMultiplier = 1.18f,
                damageMultiplier = 1.22f,
                maxAliveEnemies = 22,
                spawnIntervalMin = 1.45f,
                spawnIntervalMax = 3f
            },
            new WaveConfig
            {
                visualTier = 3,
                healthMultiplier = 1.65f,
                speedMultiplier = 1.25f,
                damageMultiplier = 1.3f,
                maxAliveEnemies = 28,
                spawnIntervalMin = 1.2f,
                spawnIntervalMax = 2.6f
            }
        };

        [Header("Refs")]
        [SerializeField] Enemies.SpawnManager spawnManager;
        [SerializeField] WaveBannerUI waveBanner;
        [SerializeField] GameObject bossPrefab;
        [SerializeField] Transform bossSpawnPoint;

        WavePhase phase = WavePhase.CombatSpawning;
        int currentWaveIndex = 1;
        float phaseTimer;

        public WavePhase Phase => phase;
        public int CurrentWaveIndex => currentWaveIndex;
        public bool SpawningAllowed =>
            phase == WavePhase.CombatSpawning && currentWaveIndex <= totalWaves;

        public static event Action<int> OnWaveCombatStarted;
        public static event Action<int> OnWaveSpawnWindowEnded;
        public static event Action<int> OnArenaCleared;
        public static event Action<int> OnIntermissionStarted;
        public static event Action OnBossCountdownStarted;
        public static event Action<GameObject> OnBossSpawned;

        void Awake()
        {
            if (spawnManager == null)
                spawnManager = FindAnyObjectByType<Enemies.SpawnManager>();

            if (waves == null || waves.Length < totalWaves)
                Debug.LogWarning($"WaveManager: assign at least {totalWaves} wave configs.");

            phase = WavePhase.CombatSpawning;
            currentWaveIndex = 1;
            phaseTimer = combatDurationSeconds;
            ApplyWaveToSpawnManager();
            waveBanner?.ShowWave(currentWaveIndex);
            OnWaveCombatStarted?.Invoke(currentWaveIndex);
        }

        void Update()
        {
            float dt = Time.deltaTime;

            switch (phase)
            {
                case WavePhase.CombatSpawning:
                    phaseTimer -= dt;
                    if (phaseTimer > 0f)
                        return;

                    OnWaveSpawnWindowEnded?.Invoke(currentWaveIndex);
                    phase = WavePhase.CombatCleanup;
                    break;

                case WavePhase.CombatCleanup:
                    if (spawnManager != null && spawnManager.AliveEnemyCount > 0)
                        return;

                    OnArenaCleared?.Invoke(currentWaveIndex);

                    if (currentWaveIndex < totalWaves)
                    {
                        phase = WavePhase.Intermission;
                        phaseTimer = intermissionSeconds;
                        OnIntermissionStarted?.Invoke(currentWaveIndex + 1);
                    }
                    else
                    {
                        phase = WavePhase.BossCountdown;
                        phaseTimer = bossSpawnDelaySeconds;
                        OnBossCountdownStarted?.Invoke();
                    }

                    break;

                case WavePhase.Intermission:
                    phaseTimer -= dt;
                    if (phaseTimer > 0f)
                        return;

                    currentWaveIndex++;
                    phase = WavePhase.CombatSpawning;
                    phaseTimer = combatDurationSeconds;
                    ApplyWaveToSpawnManager();
                    waveBanner?.ShowWave(currentWaveIndex);
                    OnWaveCombatStarted?.Invoke(currentWaveIndex);
                    break;

                case WavePhase.BossCountdown:
                    phaseTimer -= dt;
                    if (phaseTimer > 0f)
                        return;

                    SpawnBoss();
                    phase = WavePhase.Finished;
                    break;

                case WavePhase.Finished:
                    break;
            }
        }

        void ApplyWaveToSpawnManager()
        {
            if (spawnManager == null)
                return;

            WaveConfig cfg = GetConfigForWave(currentWaveIndex);
            spawnManager.ApplyWaveConfig(cfg);
        }

        WaveConfig GetConfigForWave(int waveIndex)
        {
            int i = Mathf.Clamp(waveIndex - 1, 0, waves.Length > 0 ? waves.Length - 1 : 0);
            return waves.Length > 0 ? waves[i] : new WaveConfig();
        }

        void SpawnBoss()
        {
            if (bossPrefab == null)
            {
                Debug.LogWarning("WaveManager: Boss prefab not assigned.");
                return;
            }

            Vector3 pos = bossSpawnPoint != null ? bossSpawnPoint.position : Vector3.zero;
            Quaternion rot = bossSpawnPoint != null ? bossSpawnPoint.rotation : Quaternion.identity;
            GameObject boss = Instantiate(bossPrefab, pos, rot);
            OnBossSpawned?.Invoke(boss);
        }
    }
}
