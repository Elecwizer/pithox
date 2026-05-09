using System.Collections;
using System.Collections.Generic;
using Pithox.Combat;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Pithox.Enemies
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class GolemKingBoss : EnemyBase
    {
        [System.Serializable]
        public class BossUnityEvent : UnityEvent<GolemKingBoss> { }

        enum BossAction
        {
            None,
            BasicAttack,
            Stomp,
            RunWindup,
            Running,
            Recovery,
            Dead
        }

        public static event System.Action<GolemKingBoss> BossSpawned;
        public static event System.Action<GolemKingBoss> BossAnyDeathStarted;
        public static event System.Action<GolemKingBoss> BossPhase2Started;
        public static event System.Action<GolemKingBoss> BossFinalDeathStarted;

        public event System.Action<GolemKingBoss> OnBossSpawned;
        public event System.Action<GolemKingBoss> OnAnyDeathStarted;
        public event System.Action<GolemKingBoss> OnPhase2Started;
        public event System.Action<GolemKingBoss> OnFinalDeathStarted;

        [Header("Boss Events")]
        [SerializeField] BossUnityEvent onBossSpawned;
        [SerializeField] BossUnityEvent onAnyDeathStarted;
        [SerializeField] BossUnityEvent onPhase2Started;
        [SerializeField] BossUnityEvent onFinalDeathStarted;

        [Header("Boss UI")]
        [SerializeField] GameObject bossUiRoot;
        [SerializeField] Component bossNameText;
        [SerializeField] Image healthFillImage;
        [SerializeField] Image healthFillAnimationImage;
        [SerializeField] Slider healthSlider;

        [Header("Health Bar Animation")]
        [SerializeField] float damageBarDelay = 0.35f;
        [SerializeField] float damageBarSmoothTime = 0.45f;

        [Header("Phase 2 Visual")]
        [SerializeField] GameObject phase2OnlyObject;

        [Header("Activation")]
        [SerializeField] bool activeOnStart = true;
        [SerializeField] bool activateBossTrigger;

        [Header("Tomb Destruction")]
        [SerializeField] string tombTag = "Tomb";
        [SerializeField] GameObject tombDestroyVfxPrefab;
        [SerializeField] float tombDestroyVfxDestroyDelay = 2f;
        [SerializeField] bool destroyTombsOnCollision = true;
        [SerializeField] bool useTombBreakRadiusCheck = true;
        [SerializeField] float tombBreakCheckRadius = 2f;
        [SerializeField] LayerMask tombBreakCheckMask = ~0;
        [SerializeField] bool destroyAllTombsBeforePhase2Revive = true;
        [SerializeField] float phase2TombSuckDuration = 1.2f;
        [SerializeField] float phase2TombSuckArcHeight = 0.75f;

        [Header("Animator")]
        [SerializeField] Animator animator;
        [SerializeField] string speedParameter = "Speed";
        [SerializeField] string attackTriggerParameter = "Attack";
        [SerializeField] string stompTriggerParameter = "ChargedAttack";
        [SerializeField] string hitTriggerParameter = "Hit";
        [SerializeField] string deathTriggerParameter = "Die";
        [SerializeField] string reviveTriggerParameter = "Awake";
        [SerializeField] string runningBoolParameter = "IsRunning";
        [SerializeField] float hitAnimatorRetriggerCooldown = 0.22f;

        [Header("Turning")]
        [SerializeField] float chaseTurnSpeed = 540f;
        [SerializeField] float attackTurnSpeed = 150f;
        [SerializeField] float stompTurnSpeed = 120f;
        [SerializeField] float runWindupTurnSpeed = 240f;
        [SerializeField] bool turnDuringBasicBeforeHit = true;
        [SerializeField] bool turnDuringStompBeforeHit = true;

        [Header("Phase 1")]
        [SerializeField] string phase1BossName = "Golem King";
        [SerializeField] float phase1MaxHealth = 150f;
        [SerializeField] float phase1MoveSpeed = 2.4f;
        [SerializeField] float phase1BasicDamage = 18f;
        [SerializeField] float phase1StompDamage = 32f;
        [SerializeField] float phase1StompRadius = 5f;
        [SerializeField] float phase1StompCooldown = 5f;
        [SerializeField] float phase1StompMaxStartDistance = 5.4f;
        [SerializeField] float phase1RunDamage = 22f;
        [SerializeField] float phase1RunSpeed = 8.5f;
        [SerializeField] bool phase1CanStompAfterRun;

        [Header("Phase 2")]
        [SerializeField] string phase2BossName = "Pithox";
        [SerializeField] float phase2MaxHealth = 300f;
        [SerializeField] float phase2MoveSpeed = 3.1f;
        [SerializeField] float phase2BasicDamage = 24f;
        [SerializeField] float phase2StompDamage = 42f;
        [SerializeField] float phase2StompRadius = 7f;
        [SerializeField] float phase2StompCooldown = 4f;
        [SerializeField] float phase2StompMaxStartDistance = 6.5f;
        [SerializeField] float phase2RunDamage = 32f;
        [SerializeField] float phase2RunSpeed = 11f;
        [SerializeField] bool phase2CanStompAfterRun = true;
        [SerializeField] float phase2ScaleMultiplier = 1.5f;

        [Header("Basic Attack")]
        [SerializeField] float attackRange = 5.4f;
        [SerializeField] float attackCooldown = 1.1f;
        [SerializeField] float closeAttackCooldownMultiplier = 0.75f;
        [SerializeField, Range(0f, 180f)] float attackArcDegrees = 150f;
        [SerializeField] float basicHitDelay = 0.35f;
        [SerializeField] float basicAttackDuration = 1.1f;
        [SerializeField] float basicRecovery = 0.35f;

        [Header("Boss Stop Distance")]
        [SerializeField] float minimumStopDistance = 4.6f;
        [SerializeField, Range(0.35f, 1.2f)] float stopDistanceAttackRangeMul = 0.88f;
        [SerializeField] bool stopWhenInsideAttackRange = true;

        [Header("Stomp Attack")]
        [SerializeField] float stompImpactDelay = 0.8f;
        [SerializeField] float stompDuration = 1.45f;
        [SerializeField] float stompRecovery = 0.5f;
        [SerializeField] GameObject stompVfxPrefab;
        [SerializeField] float stompVfxForwardDistance = 3f;
        [SerializeField] float stompVfxUpOffset = 0.05f;
        [SerializeField] float stompVfxDestroyDelay = 1f;
        [SerializeField] float stompCameraShake = 1.2f;
        [SerializeField, Range(0f, 1f)] float closeRangeStompChance = 0.7f;

        [Header("Run Attack")]
        [SerializeField] float runCooldown = 7f;
        [SerializeField] float runRoarDelay = 1.5f;
        [SerializeField] float runMaxDuration = 4f;
        [SerializeField] float runMaxDistance = 15f;
        [SerializeField] float runHitRadius = 1.45f;
        [SerializeField] float runDamageInterval = 0.45f;
        [SerializeField] float postRunPause = 2f;
        [SerializeField] bool facePlayerDuringRunRoar = true;
        [SerializeField] bool stompAtEndOfRun = true;

        [Header("Run Chance By Distance")]
        [SerializeField] float runMinDistance = 3f;
        [SerializeField] float runMaxStartDistance = 20f;
        [SerializeField] float runChanceCloseDistance = 5.5f;
        [SerializeField] float runChanceFarDistance = 13f;
        [SerializeField, Range(0f, 1f)] float runChanceAtClose = 0f;
        [SerializeField, Range(0f, 1f)] float runChanceAtFar = 0.95f;

        [Header("Phase 1 Charge Run")]
        [SerializeField] float phase1RunPastPlayerDistance = 6f;
        [SerializeField] float phase1RunTargetTurnSpeed = 260f;
        [SerializeField] float phase1RunCommitAfterSeconds = 0.45f;

        [Header("Phase 2 Tracking Run")]
        [SerializeField] float phase2RunTurnSpeedDuringCharge = 420f;
        [SerializeField] float phase2RunStopAndStompDistance = 3.7f;
        [SerializeField] bool phase2RunStompsWhenClose = true;

        [Header("Run Animation")]
        [SerializeField] float runAnimationSpeedMultiplier = 1.7f;

        [Header("Special Attack Choice")]
        [SerializeField] float specialThinkMin = 1.4f;
        [SerializeField] float specialThinkMax = 2.7f;
        [SerializeField, Range(0f, 1f)] float stompChance = 0.45f;

        [Header("Boss SFX")]
        [SerializeField] AudioClip normalAttackSfx;
        [SerializeField] float normalAttackSfxDelay = 0f;
        [SerializeField] float normalAttackVolume = 1.25f;
        [SerializeField] Vector2 normalAttackPitchRange = new Vector2(0.95f, 1.05f);

        [SerializeField] AudioClip stompAttackSfx;
        [SerializeField] float stompAttackVolume = 1.35f;
        [SerializeField] Vector2 stompAttackPitchRange = new Vector2(0.92f, 1.04f);

        [SerializeField] AudioClip hitSfx;
        [SerializeField] float hitSfxVolume = 1.1f;
        [SerializeField] Vector2 hitSfxPitchRange = new Vector2(0.92f, 1.08f);
        [SerializeField] float hitSfxCooldown = 0.08f;

        [SerializeField] AudioClip runRoarSfx;
        [SerializeField] float runRoarVolume = 1.35f;
        [SerializeField] Vector2 runRoarPitchRange = new Vector2(0.95f, 1.05f);

        [Header("Footsteps")]
        [SerializeField] AudioClip[] footstepSfx;
        [SerializeField] float walkFootstepInterval = 0.42f;
        [SerializeField] float runFootstepInterval = 0.16f;
        [SerializeField] float minFootstepSpeed = 0.25f;
        [SerializeField] Vector2 footstepVolumeRange = new Vector2(0.8f, 1.1f);
        [SerializeField] Vector2 footstepPitchRange = new Vector2(0.85f, 1.15f);

        [Header("SFX Playback")]
        [SerializeField] float sfxDestroyExtraTime = 0.1f;

        [Header("Death / Phase Change")]
        [SerializeField] bool autoReviveFirstDeath = true;
        [SerializeField] float finalDeathUiHideDelay = 1.5f;
        [SerializeField] float finalDestroyDelay;
        [SerializeField] bool disableCollidersOnFinalDeath = true;

        [Header("Phase 2 Revival Sequence")]
        [SerializeField] Vector3 phase2RevivalCenterPosition = Vector3.zero;
        [SerializeField] float phase2DeadWaitBeforeDrag = 5f;
        [SerializeField] float phase2DragMoveDuration = 5f;
        [SerializeField] AudioClip phase2DraggingSfx;
        [SerializeField] float phase2DraggingSfxVolume = 1.2f;
        [SerializeField] Vector2 phase2DraggingPitchRange = new Vector2(0.95f, 1.05f);
        [SerializeField] float phase2CenterWaitBeforeObject1 = 3f;
        [SerializeField] GameObject phase2RevivalObject1;
        [SerializeField] float phase2Object1ActiveBeforeRevive = 5f;
        [SerializeField] GameObject phase2Object2DisableOnRevive;
        [SerializeField] float phase2Object1DisableDelayAfterRevive = 3f;
        [SerializeField] string phase2RevivalObject1SceneName = "Magic circle";
        [SerializeField] string phase2Object2DisableSceneName = "";
        [SerializeField] bool disableAgentDuringRevivalDrag = true;
        [SerializeField] bool bossWaitsWhileObject1Finishes = true;
        [SerializeField] AudioClip phase2RevivalRoarSfx;
        [SerializeField] float phase2RevivalRoarVolume = 1.5f;
        [SerializeField] Vector2 phase2RevivalRoarPitchRange = new Vector2(0.95f, 1.05f);
        [SerializeField] float phase2RevivalRoarDelay = 0f;

        BossAction bossAction;

        int phase = 1;
        float bossHealth;

        float nextAttackTime;
        float nextStompTime;
        float nextRunTime;
        float nextSpecialThinkTime;
        float nextHitAnimatorAllowedTime;
        float nextHitSfxAllowedTime;

        bool bossActive;
        bool dealtDamageThisSwing;
        bool dealtDamageThisStomp;
        bool normalAttackSfxPlayed;
        bool runRoarPlayed;
        bool stompStartedFromRun;
        bool phase1RunCommitted;

        float actionEndTime;
        float recoveryEndTime;

        Vector3 runDirection;
        Vector3 phase1RunTargetPoint;
        float runDistance;
        float nextRunDamageTime;
        float runStartTime;

        float footstepTimer;
        Vector3 originalScale;

        Coroutine damageBarRoutine;
        Coroutine phase2RevivalRoutine;
        readonly HashSet<GameObject> destroyedTombs = new HashSet<GameObject>();

        public int CurrentPhase => phase;
        public bool IsPhase2 => phase >= 2;
        public bool IsBossActive => bossActive;
        public bool IsBossDead => IsDead;
        public float CurrentHealth => bossHealth;
        public float CurrentMaxHealthValue => CurrentMaxHealth;
        public float Health01 => Mathf.Clamp01(bossHealth / Mathf.Max(1f, CurrentMaxHealth));
        public string CurrentBossName => phase == 1 ? phase1BossName : phase2BossName;

        float CurrentMaxHealth => phase == 1 ? phase1MaxHealth : phase2MaxHealth;
        float CurrentMoveSpeed => phase == 1 ? phase1MoveSpeed : phase2MoveSpeed;
        float CurrentBasicDamage => phase == 1 ? phase1BasicDamage : phase2BasicDamage;
        float CurrentStompDamage => phase == 1 ? phase1StompDamage : phase2StompDamage;
        float CurrentStompRadius => phase == 1 ? phase1StompRadius : phase2StompRadius;
        float CurrentStompCooldown => phase == 1 ? phase1StompCooldown : phase2StompCooldown;
        float CurrentStompMaxStartDistance => phase == 1 ? phase1StompMaxStartDistance : phase2StompMaxStartDistance;
        float CurrentRunDamage => phase == 1 ? phase1RunDamage : phase2RunDamage;
        float CurrentRunSpeed => phase == 1 ? phase1RunSpeed : phase2RunSpeed;
        bool CurrentCanStompAfterRun => phase == 1 ? phase1CanStompAfterRun : phase2CanStompAfterRun;

        protected override float ChasePathRefreshSeconds => 0.18f;

        protected override void Awake()
        {
            useTouchDamage = false;

            maxHealth = phase1MaxHealth;
            moveSpeed = phase1MoveSpeed;
            stopDistance = GetBossStopDistance();

            base.Awake();

            originalScale = transform.localScale;
            bossHealth = phase1MaxHealth;

            if (animator == null)
                animator = GetComponent<Animator>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            if (phase2RevivalObject1 != null)
                phase2RevivalObject1.SetActive(false);

            ResolveScenePhase2ObjectRefs();
            EnsureBossUiReferences();
            ApplyPhaseStatsToAgent();
            UpdatePhase2Visual();
            UpdateBossUi();
            SnapHealthAnimation();

            // Keep boss UI hidden before spawn/activation unless this boss is configured to start active.
            if (!activeOnStart)
                HideBossUi();
        }

        protected override void Start()
        {
            base.Start();

            if (activeOnStart)
                ActivateBoss();
            else if (!bossActive)
                HideBossUi();
        }

        protected override void Update()
        {
            if (activateBossTrigger)
            {
                activateBossTrigger = false;

                if (!bossActive)
                    ActivateBoss();
            }

            if (!bossActive)
                return;

            base.Update();

            if (!IsDead)
                TickTombBreakRadiusCheck();

            if (IsDead || playerTarget == null || agent == null || !agent.isOnNavMesh)
                return;

            UpdateBossUi();
            UpdateAnimatorSpeed();
            TickFootsteps();

            if (bossAction == BossAction.BasicAttack)
            {
                TickBasicAttackFacing();
                return;
            }

            if (bossAction == BossAction.Stomp)
            {
                TickStompFacing();
                return;
            }

            if (bossAction == BossAction.RunWindup)
            {
                TickRunWindup();
                return;
            }

            if (bossAction == BossAction.Recovery)
            {
                TickRecovery();
                return;
            }

            if (bossAction != BossAction.None)
                return;

            TryBeginBossAttack();
        }

        [ContextMenu("Activate Boss Now")]
        public void TriggerBossActivation()
        {
            if (!bossActive)
                ActivateBoss();
        }

        public void SetActivateBossTrigger(bool value)
        {
            activateBossTrigger = value;
        }

        public void SetBossUiRoot(GameObject uiRoot)
        {
            bossUiRoot = uiRoot;
            EnsureBossUiReferences();
            UpdateBossUi();
            SnapHealthAnimation();
        }

        public void ActivateBoss()
        {
            bool wasInactive = !bossActive;

            bossActive = true;
            bossAction = BossAction.None;
            isDead = false;

            ApplyPhaseStatsToAgent();
            UpdatePhase2Visual();
            ResetAttackTimers();
            EnsureBossUiReferences();

            ShowBossUi();
            UpdateBossUi();
            SnapHealthAnimation();

            if (wasInactive)
            {
                onBossSpawned?.Invoke(this);
                OnBossSpawned?.Invoke(this);
                BossSpawned?.Invoke(this);
            }
        }

        public override void TakeDamage(DamageData damageData)
        {
            if (!bossActive)
                ActivateBoss();

            if (IsDead)
                return;

            bossHealth -= damageData.Amount;

            if (hitFlash != null)
                hitFlash.Flash();

            Pithox.Visual.HitVFX.PlayHit(damageData.HitPoint, new Color(1f, 0.9f, 0.7f));

            if (bossHealth <= 0f)
            {
                bossHealth = 0f;
                UpdateBossUi();
                StartHealthAnimation();
                Die();
                return;
            }

            UpdateBossUi();
            StartHealthAnimation();
            PlayHitSfx();

            if (animator != null && !string.IsNullOrEmpty(hitTriggerParameter))
            {
                if (Time.time >= nextHitAnimatorAllowedTime)
                {
                    nextHitAnimatorAllowedTime = Time.time + Mathf.Max(0.05f, hitAnimatorRetriggerCooldown);
                    animator.SetTrigger(hitTriggerParameter);
                }
            }
        }

        protected override void TickMovement()
        {
            if (!bossActive)
                return;

            if (bossAction == BossAction.Running)
            {
                TickRunAttack();
                return;
            }

            if (bossAction != BossAction.None)
                return;

            BossChaseMovement();
        }

        void BossChaseMovement()
        {
            if (agent == null || !agent.isOnNavMesh || playerTarget == null)
                return;

            float dist = FlatDistanceToPlayer();

            moveSpeed = CurrentMoveSpeed;
            stopDistance = GetBossStopDistance();

            agent.speed = CurrentMoveSpeed;
            agent.stoppingDistance = stopDistance;

            if (stopWhenInsideAttackRange && dist <= attackRange)
            {
                agent.isStopped = true;
                agent.ResetPath();
                FacePlayer(chaseTurnSpeed);
                return;
            }

            agent.isStopped = false;
            agent.SetDestination(playerTarget.position);

            Vector3 velocity = agent.velocity;
            velocity.y = 0f;

            if (velocity.sqrMagnitude > 0.01f)
                FaceDirection(velocity.normalized, chaseTurnSpeed);
        }

        protected override void Die()
        {
            if (IsDead)
                return;

            isDead = true;
            bossAction = BossAction.Dead;

            CancelInvoke();
            StopAgent();

            if (animator != null)
                animator.speed = 1f;

            SafeSetBool(runningBoolParameter, false);
            SafeResetTrigger(attackTriggerParameter);
            SafeResetTrigger(stompTriggerParameter);
            SafeResetTrigger(hitTriggerParameter);
            SafeSetTrigger(deathTriggerParameter);

            PlayDeathCameraAndGlobalVfx();
            PlayDeathSounds();

            onAnyDeathStarted?.Invoke(this);
            OnAnyDeathStarted?.Invoke(this);
            BossAnyDeathStarted?.Invoke(this);

            if (phase == 1)
            {
                if (autoReviveFirstDeath)
                    StartPhase2RevivalSequence();

                return;
            }

            onFinalDeathStarted?.Invoke(this);
            OnFinalDeathStarted?.Invoke(this);
            BossFinalDeathStarted?.Invoke(this);

            if (disableCollidersOnFinalDeath)
                SetCollidersEnabled(false);

            if (finalDeathUiHideDelay >= 0f)
                Invoke(nameof(HideBossUi), finalDeathUiHideDelay);

            if (finalDestroyDelay > 0f)
                Destroy(gameObject, finalDestroyDelay);
        }

        public void StartPhase2RevivalSequence()
        {
            if (phase != 1)
                return;

            if (phase2RevivalRoutine != null)
                StopCoroutine(phase2RevivalRoutine);

            phase2RevivalRoutine = StartCoroutine(Phase2RevivalSequence());
        }

        IEnumerator Phase2RevivalSequence()
        {
            ResolveScenePhase2ObjectRefs();

            yield return new WaitForSeconds(phase2DeadWaitBeforeDrag);

            bool agentWasEnabled = agent != null && agent.enabled;

            if (disableAgentDuringRevivalDrag && agent != null)
                agent.enabled = false;

            GameObject dragSfxObject = PlayLoopingTempSfx2D(
                phase2DraggingSfx,
                transform.position,
                phase2DraggingSfxVolume,
                Random.Range(phase2DraggingPitchRange.x, phase2DraggingPitchRange.y),
                "Boss Dragging SFX"
            );

            Vector3 startPosition = transform.position;
            float moveTime = 0f;

            while (moveTime < phase2DragMoveDuration)
            {
                moveTime += Time.deltaTime;

                float t = Mathf.Clamp01(moveTime / Mathf.Max(0.01f, phase2DragMoveDuration));
                t = Mathf.SmoothStep(0f, 1f, t);

                transform.position = Vector3.Lerp(startPosition, phase2RevivalCenterPosition, t);

                if (dragSfxObject != null)
                    dragSfxObject.transform.position = transform.position;

                yield return null;
            }

            transform.position = phase2RevivalCenterPosition;

            if (dragSfxObject != null)
                Destroy(dragSfxObject);

            yield return new WaitForSeconds(phase2CenterWaitBeforeObject1);

            if (phase2RevivalObject1 != null)
            {
                phase2RevivalObject1.SetActive(true);
                PlayEnabledObjectEffects(phase2RevivalObject1);
            }

            yield return new WaitForSeconds(phase2Object1ActiveBeforeRevive);

            if (destroyAllTombsBeforePhase2Revive)
                yield return StartCoroutine(SuckAndDestroyAllTombsToBoss());

            if (agent != null && agentWasEnabled)
            {
                agent.enabled = true;

                if (agent.isOnNavMesh)
                    agent.Warp(transform.position);
            }

            ReviveToPhase2();

            if (phase2Object2DisableOnRevive != null)
                phase2Object2DisableOnRevive.SetActive(false);

            if (bossWaitsWhileObject1Finishes)
                BeginRecovery(phase2Object1DisableDelayAfterRevive);

            yield return new WaitForSeconds(phase2Object1DisableDelayAfterRevive);

            if (phase2RevivalObject1 != null)
                phase2RevivalObject1.SetActive(false);

            phase2RevivalRoutine = null;
        }

        public void ReviveToPhase2()
        {
            if (phase != 1)
                return;

            phase = 2;
            bossHealth = phase2MaxHealth;
            transform.localScale = originalScale * phase2ScaleMultiplier;

            isDead = false;
            bossAction = BossAction.None;

            SetCollidersEnabled(true);
            ApplyPhaseStatsToAgent();
            UpdatePhase2Visual();
            // If phase 2 visuals are enabled now (e.g. magic circle), replay particles/audio on activation.
            if (phase2OnlyObject != null && phase2OnlyObject.activeInHierarchy)
                PlayEnabledObjectEffects(phase2OnlyObject);
            ResetAttackTimers();

            if (animator != null)
                animator.speed = 1f;

            SafeSetTrigger(reviveTriggerParameter);
            PlayPhase2RevivalRoar();

            ShowBossUi();
            UpdateBossUi();
            SnapHealthAnimation();

            onPhase2Started?.Invoke(this);
            OnPhase2Started?.Invoke(this);
            BossPhase2Started?.Invoke(this);
        }

        void TryBeginBossAttack()
        {
            float dist = FlatDistanceToPlayer();

            if (dist <= attackRange)
            {
                if (TryBeginBasicAttack(dist))
                    return;

                if (CanStomp(dist) && Random.value <= closeRangeStompChance)
                {
                    BeginStomp(false);
                    return;
                }

                return;
            }

            if (Time.time >= nextSpecialThinkTime)
            {
                nextSpecialThinkTime = Time.time + Random.Range(specialThinkMin, specialThinkMax);

                float currentRunChance = GetRunChanceByDistance(dist);

                if (CanRunAttack(dist) && Random.value <= currentRunChance)
                {
                    BeginRunAttack();
                    return;
                }

                if (CanStomp(dist) && Random.value <= stompChance)
                {
                    BeginStomp(false);
                    return;
                }
            }
        }

        float GetRunChanceByDistance(float dist)
        {
            if (dist <= runChanceCloseDistance)
                return runChanceAtClose;

            if (dist >= runChanceFarDistance)
                return runChanceAtFar;

            float t = Mathf.InverseLerp(runChanceCloseDistance, runChanceFarDistance, dist);
            return Mathf.Lerp(runChanceAtClose, runChanceAtFar, t);
        }

        bool CanRunAttack(float dist)
        {
            if (Time.time < nextRunTime)
                return false;

            return dist >= runMinDistance && dist <= runMaxStartDistance;
        }

        bool CanStomp(float dist)
        {
            if (Time.time < nextStompTime)
                return false;

            return dist <= CurrentStompMaxStartDistance;
        }

        bool TryBeginBasicAttack(float dist)
        {
            if (Time.time < nextAttackTime)
                return false;

            if (dist > attackRange)
                return false;

            if (playerTarget == null || animator == null)
                return false;

            bossAction = BossAction.BasicAttack;
            dealtDamageThisSwing = false;
            normalAttackSfxPlayed = false;

            float cooldownMul = dist <= attackRange ? closeAttackCooldownMultiplier : 1f;
            nextAttackTime = Time.time + attackCooldown * Mathf.Max(0.05f, cooldownMul);

            StopAgent();

            SafeSetTrigger(attackTriggerParameter);

            CancelInvoke(nameof(PlayNormalAttackSfx));
            CancelInvoke(nameof(MeleeDealDamage));
            CancelInvoke(nameof(MeleeAttackEnded));

            if (normalAttackSfxDelay <= 0f)
                PlayNormalAttackSfx();
            else
                Invoke(nameof(PlayNormalAttackSfx), normalAttackSfxDelay);

            Invoke(nameof(MeleeDealDamage), basicHitDelay);
            Invoke(nameof(MeleeAttackEnded), basicAttackDuration);

            return true;
        }

        void TickBasicAttackFacing()
        {
            if (!turnDuringBasicBeforeHit || dealtDamageThisSwing)
                return;

            FacePlayer(attackTurnSpeed);
        }

        public void PlayNormalAttackSfx()
        {
            if (normalAttackSfxPlayed)
                return;

            normalAttackSfxPlayed = true;

            PlayTempSfx2D(
                normalAttackSfx,
                transform.position,
                normalAttackVolume,
                Random.Range(normalAttackPitchRange.x, normalAttackPitchRange.y),
                "Boss Normal Attack SFX"
            );
        }

        public void MeleeDealDamage()
        {
            if (IsDead || bossAction != BossAction.BasicAttack || dealtDamageThisSwing)
                return;

            if (playerTarget == null)
                return;

            Vector3 toPlayer = playerTarget.position - transform.position;
            toPlayer.y = 0f;

            if (toPlayer.sqrMagnitude > attackRange * attackRange)
                return;

            Vector3 forward = transform.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.0001f)
                return;

            if (Vector3.Angle(forward.normalized, toPlayer.normalized) > attackArcDegrees * 0.5f)
                return;

            DealDamageToPlayer(CurrentBasicDamage, playerTarget.position, toPlayer.normalized, 0);
            dealtDamageThisSwing = true;
        }

        public void MeleeAttackEnded()
        {
            if (IsDead || bossAction != BossAction.BasicAttack)
                return;

            CancelInvoke(nameof(PlayNormalAttackSfx));
            CancelInvoke(nameof(MeleeDealDamage));
            CancelInvoke(nameof(MeleeAttackEnded));

            dealtDamageThisSwing = false;
            BeginRecovery(basicRecovery);
        }

        void BeginStomp(bool fromRun)
        {
            if (playerTarget == null || animator == null)
                return;

            bossAction = BossAction.Stomp;
            dealtDamageThisStomp = false;
            stompStartedFromRun = fromRun;

            nextStompTime = Time.time + CurrentStompCooldown;

            StopAgent();

            SafeSetTrigger(stompTriggerParameter);

            CancelInvoke(nameof(StompDealDamage));
            CancelInvoke(nameof(StompAttackEnded));

            Invoke(nameof(StompDealDamage), stompImpactDelay);
            Invoke(nameof(StompAttackEnded), stompDuration);
        }

        void TickStompFacing()
        {
            if (!turnDuringStompBeforeHit || dealtDamageThisStomp)
                return;

            FacePlayer(stompTurnSpeed);
        }

        public void StompDealDamage()
        {
            if (IsDead || bossAction != BossAction.Stomp || dealtDamageThisStomp)
                return;

            Vector3 stompPos = GetStompImpactPosition();

            PlayTempSfx2D(
                stompAttackSfx,
                stompPos,
                stompAttackVolume,
                Random.Range(stompAttackPitchRange.x, stompAttackPitchRange.y),
                "Boss Stomp SFX"
            );

            if (stompVfxPrefab != null)
            {
                GameObject vfx = Instantiate(stompVfxPrefab, stompPos, stompVfxPrefab.transform.rotation);
                Destroy(vfx, stompVfxDestroyDelay);
            }

            SmoothMidCamera.Shake(stompCameraShake, 0.18f);

            if (FlatDistanceToPlayer() <= CurrentStompRadius)
            {
                Vector3 hitDir = playerTarget.position - transform.position;
                hitDir.y = 0f;

                if (hitDir.sqrMagnitude < 0.0001f)
                    hitDir = transform.forward;

                DealDamageToPlayer(CurrentStompDamage, playerTarget.position, hitDir.normalized, 0);
            }

            dealtDamageThisStomp = true;
        }

        public void StompAttackEnded()
        {
            if (IsDead || bossAction != BossAction.Stomp)
                return;

            CancelInvoke(nameof(StompDealDamage));
            CancelInvoke(nameof(StompAttackEnded));

            dealtDamageThisStomp = false;

            float pause = stompStartedFromRun ? postRunPause : stompRecovery;
            stompStartedFromRun = false;

            BeginRecovery(pause);
        }

        void BeginRunAttack()
        {
            if (playerTarget == null)
                return;

            Vector3 toPlayer = playerTarget.position - transform.position;
            toPlayer.y = 0f;

            if (toPlayer.sqrMagnitude < 0.0001f)
                return;

            runDirection = toPlayer.normalized;
            runDistance = 0f;
            runRoarPlayed = false;
            phase1RunCommitted = false;

            if (phase == 1)
            {
                Vector3 lockedTarget = playerTarget.position;
                phase1RunTargetPoint = lockedTarget + runDirection * phase1RunPastPlayerDistance;
            }

            bossAction = BossAction.RunWindup;
            actionEndTime = Time.time + Mathf.Max(0f, runRoarDelay);
            nextRunTime = Time.time + runCooldown;

            StopAgent();
            PlayRunRoarSfx();
        }

        void PlayRunRoarSfx()
        {
            if (runRoarPlayed)
                return;

            runRoarPlayed = true;

            PlayTempSfx2D(
                runRoarSfx,
                transform.position,
                runRoarVolume,
                Random.Range(runRoarPitchRange.x, runRoarPitchRange.y),
                "Boss Run Roar SFX"
            );
        }

        void TickRunWindup()
        {
            if (facePlayerDuringRunRoar && playerTarget != null)
            {
                Vector3 toPlayer = playerTarget.position - transform.position;
                toPlayer.y = 0f;

                if (toPlayer.sqrMagnitude > 0.0001f)
                    runDirection = toPlayer.normalized;
            }

            FaceDirection(runDirection, runWindupTurnSpeed);

            if (Time.time < actionEndTime)
                return;

            runDirection = transform.forward;
            runDirection.y = 0f;

            if (runDirection.sqrMagnitude < 0.0001f)
                runDirection = Vector3.forward;

            runDirection.Normalize();

            if (phase == 1 && playerTarget != null)
            {
                Vector3 lockedTarget = playerTarget.position;
                phase1RunTargetPoint = lockedTarget + runDirection * phase1RunPastPlayerDistance;
            }

            bossAction = BossAction.Running;
            runStartTime = Time.time;
            nextRunDamageTime = 0f;

            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.ResetPath();
            }

            SafeSetBool(runningBoolParameter, true);
        }

        void TickRunAttack()
        {
            if (agent == null || !agent.isOnNavMesh)
                return;

            float dist = FlatDistanceToPlayer();

            if (phase == 2 && phase2RunStompsWhenClose && dist <= phase2RunStopAndStompDistance)
            {
                EndRunAttack(true);
                return;
            }

            UpdateRunDirection();

            Vector3 move = runDirection * CurrentRunSpeed * Time.deltaTime;
            agent.Move(move);
            runDistance += move.magnitude;

            FaceDirection(runDirection, 9999f);

            if (Time.time >= nextRunDamageTime && dist <= runHitRadius)
            {
                nextRunDamageTime = Time.time + runDamageInterval;

                Vector3 hitDir = playerTarget.position - transform.position;
                hitDir.y = 0f;

                if (hitDir.sqrMagnitude < 0.0001f)
                    hitDir = runDirection;

                DealDamageToPlayer(CurrentRunDamage, playerTarget.position, hitDir.normalized, 0);
            }

            bool timeDone = Time.time - runStartTime >= runMaxDuration;
            bool distanceDone = runDistance >= runMaxDistance;

            if (!timeDone && !distanceDone)
                return;

            EndRunAttack(false);
        }

        void UpdateRunDirection()
        {
            if (phase == 1)
            {
                TickPhase1ChargeRun();
                return;
            }

            TickPhase2TrackingRun();
        }

        void TickPhase1ChargeRun()
        {
            if (!phase1RunCommitted)
            {
                if (Time.time - runStartTime >= phase1RunCommitAfterSeconds)
                    phase1RunCommitted = true;
            }

            Vector3 desired = phase1RunTargetPoint - transform.position;
            desired.y = 0f;

            if (desired.sqrMagnitude < 0.0001f)
                return;

            runDirection = Vector3.RotateTowards(
                runDirection,
                desired.normalized,
                phase1RunTargetTurnSpeed * Mathf.Deg2Rad * Time.deltaTime,
                0f
            ).normalized;
        }

        void TickPhase2TrackingRun()
        {
            if (playerTarget == null)
                return;

            Vector3 toPlayer = playerTarget.position - transform.position;
            toPlayer.y = 0f;

            if (toPlayer.sqrMagnitude < 0.0001f)
                return;

            runDirection = Vector3.RotateTowards(
                runDirection,
                toPlayer.normalized,
                phase2RunTurnSpeedDuringCharge * Mathf.Deg2Rad * Time.deltaTime,
                0f
            ).normalized;
        }

        void EndRunAttack(bool forceStomp)
        {
            SafeSetBool(runningBoolParameter, false);

            if (animator != null)
                animator.speed = 1f;

            if (forceStomp)
            {
                BeginStomp(true);
                return;
            }

            if (CurrentCanStompAfterRun && stompAtEndOfRun)
            {
                BeginStomp(true);
                return;
            }

            BeginRecovery(postRunPause);
        }

        void BeginRecovery(float duration)
        {
            bossAction = BossAction.Recovery;
            recoveryEndTime = Time.time + Mathf.Max(0.01f, duration);
            StopAgent();
        }

        void TickRecovery()
        {
            if (Time.time < recoveryEndTime)
                return;

            bossAction = BossAction.None;
            ResumeAgent();
        }

        void OnCollisionEnter(Collision collision)
        {
            if (collision == null)
                return;

            TryDestroyTomb(collision.gameObject);
        }

        void OnTriggerEnter(Collider other)
        {
            if (other == null)
                return;

            TryDestroyTomb(other.gameObject);
        }

        void TickTombBreakRadiusCheck()
        {
            if (!destroyTombsOnCollision || !useTombBreakRadiusCheck || tombBreakCheckRadius <= 0f)
                return;

            Collider[] hits = Physics.OverlapSphere(
                transform.position,
                tombBreakCheckRadius,
                tombBreakCheckMask,
                QueryTriggerInteraction.Collide
            );

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] == null)
                    continue;

                TryDestroyTomb(hits[i].gameObject);
            }
        }

        void TryDestroyTomb(GameObject hitObject)
        {
            if (!destroyTombsOnCollision || hitObject == null)
                return;

            GameObject tomb = FindTaggedTombObject(hitObject);

            if (tomb == null)
                return;

            DestroyTomb(tomb, tomb.transform.position);
        }

        GameObject FindTaggedTombObject(GameObject hitObject)
        {
            if (hitObject == null)
                return null;

            Transform current = hitObject.transform;

            while (current != null)
            {
                if (current.CompareTag(tombTag))
                    return current.gameObject;

                current = current.parent;
            }

            return null;
        }

        IEnumerator SuckAndDestroyAllTombsToBoss()
        {
            GameObject[] tombObjects = GameObject.FindGameObjectsWithTag(tombTag);

            if (tombObjects == null || tombObjects.Length == 0)
                yield break;

            Transform[] tombs = new Transform[tombObjects.Length];
            Vector3[] startPositions = new Vector3[tombObjects.Length];
            int count = 0;

            for (int i = 0; i < tombObjects.Length; i++)
            {
                if (tombObjects[i] == null || destroyedTombs.Contains(tombObjects[i]))
                    continue;

                tombs[count] = tombObjects[i].transform;
                startPositions[count] = tombObjects[i].transform.position;
                count++;
            }

            if (count == 0)
                yield break;

            float duration = Mathf.Max(0.01f, phase2TombSuckDuration);
            float time = 0f;

            while (time < duration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                Vector3 target = transform.position;

                for (int i = 0; i < count; i++)
                {
                    if (tombs[i] == null)
                        continue;

                    Vector3 pos = Vector3.Lerp(startPositions[i], target, smoothT);
                    pos.y += Mathf.Sin(smoothT * Mathf.PI) * phase2TombSuckArcHeight;
                    tombs[i].position = pos;
                }

                yield return null;
            }

            Vector3 destroyPosition = transform.position;

            for (int i = 0; i < count; i++)
            {
                if (tombs[i] == null)
                    continue;

                DestroyTomb(tombs[i].gameObject, destroyPosition);
            }
        }

        void DestroyTomb(GameObject tomb, Vector3 vfxPosition)
        {
            if (tomb == null || destroyedTombs.Contains(tomb))
                return;

            destroyedTombs.Add(tomb);
            SpawnTombDestroyVfx(vfxPosition);
            Destroy(tomb);
        }

        void SpawnTombDestroyVfx(Vector3 position)
        {
            if (tombDestroyVfxPrefab == null)
                return;

            GameObject vfx = Instantiate(tombDestroyVfxPrefab, position, tombDestroyVfxPrefab.transform.rotation);

            if (tombDestroyVfxDestroyDelay > 0f)
                Destroy(vfx, tombDestroyVfxDestroyDelay);
        }

        void DealDamageToPlayer(float damage, Vector3 hitPoint, Vector3 hitDirection, int knockback)
        {
            if (playerTarget == null)
                return;

            IDamageable damageable = playerTarget.GetComponent<IDamageable>();

            if (damageable == null)
            {
                MonoBehaviour[] scripts = playerTarget.GetComponentsInChildren<MonoBehaviour>();

                for (int i = 0; i < scripts.Length; i++)
                {
                    if (scripts[i] is IDamageable found)
                    {
                        damageable = found;
                        break;
                    }
                }
            }

            if (damageable == null)
                damageable = playerTarget.GetComponentInParent<IDamageable>();

            if (damageable == null)
                return;

            damageable.TakeDamage(new DamageData(
                damage,
                gameObject,
                hitPoint,
                hitDirection,
                knockback
            ));
        }

        void ApplyPhaseStatsToAgent()
        {
            moveSpeed = CurrentMoveSpeed;
            stopDistance = GetBossStopDistance();

            if (agent == null)
                return;

            if (!agent.enabled)
                agent.enabled = true;

            agent.speed = CurrentMoveSpeed;
            agent.stoppingDistance = stopDistance;
            agent.updateRotation = false;
            agent.acceleration = 18f;
            agent.angularSpeed = 720f;
            agent.autoBraking = true;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

            if (agent.isOnNavMesh)
                agent.isStopped = false;

            InvalidateMovementDestination();
        }

        float GetBossStopDistance()
        {
            return Mathf.Max(minimumStopDistance, attackRange * stopDistanceAttackRangeMul);
        }

        void ResetAttackTimers()
        {
            nextAttackTime = Time.time + 0.4f;
            nextStompTime = Time.time + Random.Range(2.3f, 4f);
            nextRunTime = Time.time + Random.Range(2.8f, 5f);
            nextSpecialThinkTime = Time.time + Random.Range(0.8f, 1.5f);
        }

        void StopAgent()
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
                return;

            agent.isStopped = true;
            agent.ResetPath();
            InvalidateMovementDestination();
        }

        void ResumeAgent()
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
                return;

            agent.speed = CurrentMoveSpeed;
            agent.stoppingDistance = stopDistance;
            agent.isStopped = false;
            InvalidateMovementDestination();
        }

        void UpdateAnimatorSpeed()
        {
            if (animator == null)
                return;

            if (bossAction == BossAction.Running)
                animator.speed = Mathf.Max(0.01f, runAnimationSpeedMultiplier);
            else
                animator.speed = 1f;

            if (string.IsNullOrEmpty(speedParameter))
                return;

            float speed = 0f;

            if (bossAction == BossAction.Running)
                speed = CurrentRunSpeed;
            else if (agent != null && agent.enabled)
                speed = agent.velocity.magnitude;

            animator.SetFloat(speedParameter, speed);
        }

        void TickFootsteps()
        {
            bool running = bossAction == BossAction.Running;
            bool walking = bossAction == BossAction.None;

            if (!running && !walking)
            {
                footstepTimer = 0.05f;
                return;
            }

            float speed = running ? CurrentRunSpeed : agent.velocity.magnitude;

            if (speed < minFootstepSpeed)
            {
                footstepTimer = 0.05f;
                return;
            }

            footstepTimer -= Time.deltaTime;

            if (footstepTimer > 0f)
                return;

            float baseSpeed = running ? CurrentRunSpeed : CurrentMoveSpeed;
            float baseInterval = running ? runFootstepInterval : walkFootstepInterval;
            float speedFactor = Mathf.Clamp(speed / Mathf.Max(0.01f, baseSpeed), 0.65f, 1.8f);

            footstepTimer = baseInterval / speedFactor;

            PlayRandomFootstep();
        }

        void PlayRandomFootstep()
        {
            if (footstepSfx == null || footstepSfx.Length == 0)
                return;

            AudioClip clip = footstepSfx[Random.Range(0, footstepSfx.Length)];

            if (clip == null)
                return;

            PlayTempSfx2D(
                clip,
                transform.position,
                Random.Range(footstepVolumeRange.x, footstepVolumeRange.y),
                Random.Range(footstepPitchRange.x, footstepPitchRange.y),
                "Boss Footstep SFX"
            );
        }

        void PlayHitSfx()
        {
            if (Time.time < nextHitSfxAllowedTime)
                return;

            nextHitSfxAllowedTime = Time.time + Mathf.Max(0.01f, hitSfxCooldown);

            PlayTempSfx2D(
                hitSfx,
                transform.position,
                hitSfxVolume,
                Random.Range(hitSfxPitchRange.x, hitSfxPitchRange.y),
                "Boss Hit SFX"
            );
        }

        void PlayPhase2RevivalRoar()
        {
            if (phase2RevivalRoarDelay > 0f)
            {
                Invoke(nameof(PlayPhase2RevivalRoarNow), phase2RevivalRoarDelay);
                return;
            }

            PlayPhase2RevivalRoarNow();
        }

        void PlayPhase2RevivalRoarNow()
        {
            PlayTempSfx2D(
                phase2RevivalRoarSfx,
                transform.position,
                phase2RevivalRoarVolume,
                Random.Range(phase2RevivalRoarPitchRange.x, phase2RevivalRoarPitchRange.y),
                "Boss Phase 2 Revival Roar SFX"
            );
        }

        void PlayTempSfx2D(AudioClip clip, Vector3 position, float volume, float pitch, string objectName)
        {
            if (clip == null)
                return;

            GameObject sfxObject = new GameObject(objectName);
            sfxObject.transform.position = position;

            AudioSource audioSource = sfxObject.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.volume = volume;
            audioSource.pitch = Mathf.Max(0.01f, pitch);
            audioSource.spatialBlend = 0f;
            audioSource.playOnAwake = false;
            audioSource.dopplerLevel = 0f;
            audioSource.priority = 0;
            audioSource.mute = false;

            audioSource.Play();

            Destroy(sfxObject, (clip.length / audioSource.pitch) + sfxDestroyExtraTime);
        }

        GameObject PlayLoopingTempSfx2D(AudioClip clip, Vector3 position, float volume, float pitch, string objectName)
        {
            if (clip == null)
                return null;

            GameObject sfxObject = new GameObject(objectName);
            sfxObject.transform.position = position;

            AudioSource audioSource = sfxObject.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.volume = volume;
            audioSource.pitch = Mathf.Max(0.01f, pitch);
            audioSource.spatialBlend = 0f;
            audioSource.playOnAwake = false;
            audioSource.dopplerLevel = 0f;
            audioSource.priority = 0;
            audioSource.mute = false;
            audioSource.loop = true;

            audioSource.Play();

            return sfxObject;
        }

        void PlayEnabledObjectEffects(GameObject targetObject)
        {
            if (targetObject == null)
                return;

            ParticleSystem[] particles = targetObject.GetComponentsInChildren<ParticleSystem>(true);

            for (int i = 0; i < particles.Length; i++)
                particles[i].Play(true);

            AudioSource[] audioSources = targetObject.GetComponentsInChildren<AudioSource>(true);

            for (int i = 0; i < audioSources.Length; i++)
                audioSources[i].Play();
        }

        Vector3 GetStompImpactPosition()
        {
            Vector3 forward = transform.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;

            forward.Normalize();

            Vector3 pos = transform.position + forward * stompVfxForwardDistance;
            pos.y += stompVfxUpOffset;

            return pos;
        }

        void UpdatePhase2Visual()
        {
            if (phase2OnlyObject != null)
                phase2OnlyObject.SetActive(phase >= 2);
        }

        void ShowBossUi()
        {
            if (bossUiRoot != null)
                bossUiRoot.SetActive(true);

            SetBossNameText(CurrentBossName);
        }

        void EnsureBossUiReferences()
        {
            if (bossUiRoot == null)
                bossUiRoot = FindSceneObjectByName("BossHealthBar");

            if (bossUiRoot == null)
                return;

            if (healthFillImage == null)
                healthFillImage = FindComponentInBossUi<Image>("HealthFill")
                    ?? FindComponentInBossUi<Image>("BossHealthFill")
                    ?? FindFilledImage(preferAnimation: false);

            if (healthFillAnimationImage == null)
                healthFillAnimationImage = FindComponentInBossUi<Image>("HealthFillAnimation")
                    ?? FindComponentInBossUi<Image>("BossHealthFillAnimation")
                    ?? FindFilledImage(preferAnimation: true);

            if (healthSlider == null)
                healthSlider = FindComponentInBossUi<Slider>("HealthFill")
                    ?? FindComponentInBossUi<Slider>("BossHealthFill");
        }

        void ResolveScenePhase2ObjectRefs()
        {
            if (phase2RevivalObject1 == null && !string.IsNullOrWhiteSpace(phase2RevivalObject1SceneName))
                phase2RevivalObject1 = FindSceneObjectByName(phase2RevivalObject1SceneName);

            if (phase2Object2DisableOnRevive == null && !string.IsNullOrWhiteSpace(phase2Object2DisableSceneName))
                phase2Object2DisableOnRevive = FindSceneObjectByName(phase2Object2DisableSceneName);
        }

        Image FindFilledImage(bool preferAnimation)
        {
            if (bossUiRoot == null)
                return null;

            Image[] images = bossUiRoot.GetComponentsInChildren<Image>(true);
            Image fallback = null;

            for (int i = 0; i < images.Length; i++)
            {
                Image img = images[i];
                if (img == null)
                    continue;
                if (img.type != Image.Type.Filled)
                    continue;

                string n = img.name ?? string.Empty;
                bool looksLikeAnimation = n.IndexOf("animation", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("delay", System.StringComparison.OrdinalIgnoreCase) >= 0;

                if (preferAnimation == looksLikeAnimation)
                    return img;

                if (fallback == null)
                    fallback = img;
            }

            return fallback;
        }

        T FindComponentInBossUi<T>(string objectName) where T : Component
        {
            if (bossUiRoot == null || string.IsNullOrEmpty(objectName))
                return null;

            Transform[] all = bossUiRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == null)
                    continue;

                if (string.Equals(t.name, objectName, System.StringComparison.OrdinalIgnoreCase))
                    return t.GetComponent<T>();
            }

            return null;
        }

        static GameObject FindSceneObjectByName(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
                return null;

            GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < all.Length; i++)
            {
                GameObject go = all[i];
                if (go == null)
                    continue;
                if (!go.scene.IsValid() || !go.scene.isLoaded)
                    continue;
                if (go.name == objectName)
                    return go;
            }

            return null;
        }

        void HideBossUi()
        {
            if (bossUiRoot != null)
                bossUiRoot.SetActive(false);
        }

        void UpdateBossUi()
        {
            float t = Health01;

            if (healthFillImage != null)
                healthFillImage.fillAmount = t;

            if (healthSlider != null)
                healthSlider.value = t;

            SetBossNameText(CurrentBossName);
        }

        void SnapHealthAnimation()
        {
            if (damageBarRoutine != null)
                StopCoroutine(damageBarRoutine);

            if (healthFillAnimationImage != null)
                healthFillAnimationImage.fillAmount = Health01;
        }

        void StartHealthAnimation()
        {
            if (healthFillAnimationImage == null)
                return;

            if (damageBarRoutine != null)
                StopCoroutine(damageBarRoutine);

            damageBarRoutine = StartCoroutine(AnimateHealthBar(Health01));
        }

        IEnumerator AnimateHealthBar(float targetFill)
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

        void SetBossNameText(string value)
        {
            if (bossNameText == null)
                return;

            Text uiText = bossNameText as Text;

            if (uiText == null)
                uiText = bossNameText.GetComponent<Text>();

            if (uiText != null)
            {
                uiText.text = value;
                return;
            }

            Component[] components = bossNameText.GetComponents<Component>();

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

        void FacePlayer(float turnSpeed)
        {
            if (playerTarget == null)
                return;

            Vector3 toPlayer = playerTarget.position - transform.position;
            toPlayer.y = 0f;

            if (toPlayer.sqrMagnitude < 0.0001f)
                return;

            FaceDirection(toPlayer.normalized, turnSpeed);
        }

        void FaceDirection(Vector3 direction, float turnSpeed)
        {
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                Mathf.Max(1f, turnSpeed) * Time.deltaTime
            );
        }

        float FlatDistanceToPlayer()
        {
            if (playerTarget == null)
                return 999f;

            Vector3 offset = playerTarget.position - transform.position;
            offset.y = 0f;
            return offset.magnitude;
        }

        void SetCollidersEnabled(bool value)
        {
            Collider[] colliders = GetComponentsInChildren<Collider>(true);

            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = value;
        }

        void SafeSetTrigger(string parameter)
        {
            if (animator == null || string.IsNullOrEmpty(parameter))
                return;

            animator.SetTrigger(parameter);
        }

        void SafeResetTrigger(string parameter)
        {
            if (animator == null || string.IsNullOrEmpty(parameter))
                return;

            animator.ResetTrigger(parameter);
        }

        void SafeSetBool(string parameter, bool value)
        {
            if (animator == null || string.IsNullOrEmpty(parameter))
                return;

            animator.SetBool(parameter, value);
        }

        void OnDisable()
        {
            HideBossUi();
            SafeSetBool(runningBoolParameter, false);

            if (animator != null)
                animator.speed = 1f;
        }
    }
}