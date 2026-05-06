using Pithox.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace Pithox.Enemies
{
    /// <summary>
    /// NavMesh chase + animator-driven melee. Place on the same GameObject as the Animator
    /// so animation events (MeleeDealDamage / MeleeAttackEnded) resolve correctly.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class MeleeEnemy : EnemyBase
    {
        [Header("Animator")]
        [SerializeField] Animator animator;
        [SerializeField] string speedParameter = "Speed";
        [SerializeField] string attackTriggerParameter = "Attack";
        [SerializeField] string hitTriggerParameter = "Hit";
        [Tooltip("Minimum seconds between Hit animator triggers. Lower = snappier flinch restarts; too low re-enters TakeDamage and resets the clip (feels stuck).")]
        [SerializeField] float hitAnimatorRetriggerCooldown = 0.22f;

        [Header("Melee Attack")]
        [SerializeField] float attackRange = 1.6f;
        [SerializeField] float attackCooldown = 1.4f;
        [SerializeField] float meleeDamage = 12f;
        [SerializeField, Range(0f, 180f)] float attackArcDegrees = 120f;

        [Header("Death")]
        [SerializeField] string deathTriggerParameter = "Die";
        [Tooltip("Time after death starts when the die clip should be done (before tomb pad).")]
        [SerializeField] float deathDespawnDelay = 1.45f;
        [Tooltip("Extra wait after that before spawning the tomb and destroying (small gap so it does not pop the instant the clip ends).")]
        [SerializeField] float deathTombSpawnPadSeconds = 0.1f;

        [Header("Post-hit retreat")]
        [Tooltip("After a successful melee hit, step back once the attack clip ends to open space from the player.")]
        [SerializeField] bool retreatAfterSuccessfulHit = true;
        [SerializeField] float retreatDistance = 1.35f;
        [SerializeField] float retreatMaxDuration = 0.65f;
        [SerializeField, Range(0f, 45f)] float retreatAngleJitterDegrees = 22f;
        [SerializeField] float retreatNavSampleRadius = 2.5f;
        [Tooltip("Small NavMesh slide applied when a retreat starts so you always see a step even if pathfinding is slow.")]
        [SerializeField] float retreatMicroMove = 0.32f;
        [Tooltip("Extra retreat distance when the player is roughly facing this enemy (harder punish).")]
        [SerializeField, Range(1f, 1.6f)] float retreatFacingPlayerDistanceMul = 1.28f;

        [Header("Hurt dodge")]
        [SerializeField] bool retreatBrieflyWhenDamagedByPlayer = true;
        [SerializeField] float hurtRetreatDistanceMul = 0.62f;
        [SerializeField] float hurtRetreatDurationMul = 0.55f;
        [SerializeField] float hurtRetreatCooldown = 1.1f;

        [Header("Combat movement")]
        [SerializeField] bool useCombatChaseOffset = true;
        [Tooltip("When closing in, weave sideways so approach is not a straight line.")]
        [SerializeField] float chaseWeaveAmplitude = 0.85f;
        [SerializeField] float chaseWeaveSpeed = 1.85f;
        [Tooltip("When inside this fraction of attack range, bias destination outward to prefer a ring.")]
        [SerializeField, Range(0.2f, 0.95f)] float chaseRingInnerFrac = 0.58f;
        [SerializeField] float chaseRingPush = 0.55f;

        [Header("React to player slash (PlayerCombatController)")]
        [SerializeField] bool reactToPlayerSlashWindup = true;
        [Tooltip("When the player starts a slash and we are in front of their aim, take a short dodge along this vector blend.")]
        [SerializeField] float slashDodgeDistanceMul = 0.72f;
        [SerializeField] float slashDodgeDurationMul = 0.5f;
        [SerializeField] float slashDodgeCooldown = 0.35f;
        [Tooltip("While player slash is pending and we are in danger range, bias chase target sideways and slightly back from aim.")]
        [SerializeField] float slashChaseStrafeMagnitude = 1.05f;
        [SerializeField] float slashChaseBackstepMagnitude = 0.5f;
        [Tooltip("Do not start our melee if we are inside this cone in front of the pending slash (dot from player→us vs slash aim).")]
        [SerializeField] bool avoidAttackingIntoPlayerSlash = true;
        [SerializeField, Range(-1f, 1f)] float avoidSlashCommitDotThreshold = 0.22f;

        float nextAttackTime;
        bool inAttackWindow;
        bool dealtDamageThisSwing;
        bool pendingRetreatAfterSwing;
        bool retreating;
        float retreatEndTime;
        float retreatSavedStoppingDistance;
        bool retreatStoppingDistancePatched;
        float nextHurtRetreatAllowedTime;
        float chaseWeavePhase;
        PlayerCombatController playerCombat;
        bool wasPlayerSlashPending;
        float nextSlashDodgeAllowedTime;
        float nextHitAnimatorAllowedTime;
        float spawnSnapshotMeleeDamage;

        protected override float ChasePathRefreshSeconds => 0.18f;

        protected override void Awake()
        {
            useTouchDamage = false;
            base.Awake();

            if (animator == null)
                animator = GetComponent<Animator>();

            spawnSnapshotMeleeDamage = meleeDamage;
            chaseWeavePhase = (gameObject.GetEntityId().GetHashCode() & 1023) * 0.01f;
        }

        public override void ApplyWaveModifiers(int visualTier, float healthMultiplier, float speedMultiplier, float damageMultiplier)
        {
            base.ApplyWaveModifiers(visualTier, healthMultiplier, speedMultiplier, damageMultiplier);
            meleeDamage = spawnSnapshotMeleeDamage * damageMultiplier;
        }

        protected override void Start()
        {
            base.Start();
            TryCachePlayerCombat();
        }

        protected override void Update()
        {
            base.Update();

            if (IsDead || playerTarget == null || animator == null || agent == null || !agent.isOnNavMesh)
                return;

            TryCachePlayerCombat();

            if (!string.IsNullOrEmpty(speedParameter))
                animator.SetFloat(speedParameter, agent.velocity.magnitude);

            TickPlayerSlashReactions();
            TryBeginAttack();
        }

        protected override void TickMovement()
        {
            if (inAttackWindow)
                return;

            if (retreating)
            {
                TickRetreatMovement();
                return;
            }

            base.TickMovement();
        }

        protected override void OnAfterDamageApplied(DamageData damageData)
        {
            if (animator == null || string.IsNullOrEmpty(hitTriggerParameter))
                return;

            if (Time.time >= nextHitAnimatorAllowedTime)
            {
                nextHitAnimatorAllowedTime = Time.time + Mathf.Max(0.05f, hitAnimatorRetriggerCooldown);
                animator.SetTrigger(hitTriggerParameter);
            }

            if (!retreatBrieflyWhenDamagedByPlayer || IsDead)
                return;

            if (Time.time < nextHurtRetreatAllowedTime)
                return;

            if (damageData.Source == null || !damageData.Source.CompareTag(playerTag))
                return;

            if (inAttackWindow || retreating)
                return;

            nextHurtRetreatAllowedTime = Time.time + hurtRetreatCooldown;
            BeginRetreatInternal(hurtRetreatDistanceMul, hurtRetreatDurationMul, useFacingBonus: false, null);
        }

        protected override Vector3 GetChaseDestination()
        {
            if (!useCombatChaseOffset || playerTarget == null)
                return base.GetChaseDestination();

            Vector3 playerPos = playerTarget.position;
            Vector3 toPlayer = playerPos - transform.position;
            toPlayer.y = 0f;
            float dist = toPlayer.magnitude;
            if (dist < 0.05f)
                return playerPos;

            Vector3 target = playerPos;
            float inner = attackRange * chaseRingInnerFrac;
            if (dist < inner)
            {
                Vector3 outward = -toPlayer / dist;
                target += outward * Mathf.Min(chaseRingPush, inner - dist + 0.15f);
            }

            if (dist < attackRange * 1.08f && dist > attackRange * 0.28f)
            {
                Vector3 lateral = Vector3.Cross(Vector3.up, toPlayer.normalized).normalized;
                float weave = Mathf.Sin(Time.time * chaseWeaveSpeed + chaseWeavePhase) * chaseWeaveAmplitude;
                target += lateral * weave;
            }

            if (reactToPlayerSlashWindup && playerCombat != null && playerCombat.IsSlashWindupPending)
            {
                Vector3 sdir = playerCombat.GetPendingSlashFlatDirection();
                float sr = playerCombat.PendingSlashRange;
                if (sdir.sqrMagnitude > 0.0001f && sr > 0.05f && IsInSlashThreatZone(sr, sdir))
                {
                    sdir.Normalize();
                    Vector3 lateralSlash = Vector3.Cross(Vector3.up, sdir).normalized;
                    float wobble = Mathf.Sin(Time.time * 5.2f + chaseWeavePhase);
                    target += lateralSlash * (slashChaseStrafeMagnitude * (0.35f + 0.65f * wobble));
                    target -= sdir * slashChaseBackstepMagnitude;
                }
            }

            return target;
        }

        protected override void Die()
        {
            if (IsDead)
                return;

            isDead = true;
            CancelInvoke(nameof(FinishAnimatedDeath));

            if (animator != null && !string.IsNullOrEmpty(deathTriggerParameter))
            {
                PlayDeathCameraAndGlobalVfx();
                PlayDeathSounds();

                CancelInvoke(nameof(ReleaseAttackWindowIfStuck));
                inAttackWindow = false;
                dealtDamageThisSwing = false;
                pendingRetreatAfterSwing = false;
                retreating = false;
                wasPlayerSlashPending = false;
                RestoreRetreatNavAgentSettings();

                if (agent != null)
                {
                    agent.isStopped = true;
                    agent.enabled = false;
                }

                foreach (Collider col in GetComponentsInChildren<Collider>(true))
                    col.enabled = false;

                if (!string.IsNullOrEmpty(attackTriggerParameter))
                    animator.ResetTrigger(attackTriggerParameter);
                if (!string.IsNullOrEmpty(hitTriggerParameter))
                    animator.ResetTrigger(hitTriggerParameter);

                animator.SetTrigger(deathTriggerParameter);
                float delay = Mathf.Max(0.05f, deathDespawnDelay) + Mathf.Max(0f, deathTombSpawnPadSeconds);
                Invoke(nameof(FinishAnimatedDeath), delay);
                return;
            }

            RunDeathPresentation();
            Destroy(gameObject);
        }

        void FinishAnimatedDeath()
        {
            SpawnDeathTomb();
            Destroy(gameObject);
        }

        void TryCachePlayerCombat()
        {
            if (playerTarget == null)
            {
                playerCombat = null;
                return;
            }

            if (playerCombat == null)
                playerCombat = playerTarget.GetComponent<PlayerCombatController>();
        }

        void TickPlayerSlashReactions()
        {
            if (!reactToPlayerSlashWindup || IsDead || playerCombat == null || playerTarget == null)
                return;

            bool pending = playerCombat.IsSlashWindupPending;
            if (pending && !wasPlayerSlashPending)
            {
                if (Time.time >= nextSlashDodgeAllowedTime && !inAttackWindow && !retreating)
                {
                    Vector3 sdir = playerCombat.GetPendingSlashFlatDirection();
                    float sr = playerCombat.PendingSlashRange;
                    if (sdir.sqrMagnitude > 0.0001f && sr > 0.05f && IsInSlashThreatZone(sr, sdir))
                    {
                        nextSlashDodgeAllowedTime = Time.time + slashDodgeCooldown;
                        Vector3 dodge = ComputeSlashDodgeDirection(sdir);
                        BeginRetreatInternal(slashDodgeDistanceMul, slashDodgeDurationMul, false, dodge);
                    }
                }
            }

            wasPlayerSlashPending = pending;
        }

        bool IsInSlashThreatZone(float slashRange, Vector3 slashDirFlat)
        {
            slashDirFlat.y = 0f;
            if (playerTarget == null || slashDirFlat.sqrMagnitude < 0.0001f)
                return false;

            slashDirFlat.Normalize();
            Vector3 fromPlayer = transform.position - playerTarget.position;
            fromPlayer.y = 0f;
            float dist = fromPlayer.magnitude;
            if (dist > slashRange * 1.18f)
                return false;

            float aimDot = Vector3.Dot(fromPlayer.normalized, slashDirFlat);
            return aimDot > 0.08f;
        }

        bool ShouldHoldMeleeForPlayerSlash()
        {
            if (playerCombat == null || !playerCombat.IsSlashWindupPending)
                return false;

            Vector3 sdir = playerCombat.GetPendingSlashFlatDirection();
            float sr = playerCombat.PendingSlashRange;
            if (sdir.sqrMagnitude < 0.0001f || sr < 0.05f)
                return false;

            Vector3 fromPlayer = transform.position - playerTarget.position;
            fromPlayer.y = 0f;
            if (fromPlayer.sqrMagnitude < 0.0001f)
                return false;

            if (fromPlayer.magnitude > sr * 1.12f)
                return false;

            float dot = Vector3.Dot(fromPlayer.normalized, sdir.normalized);
            return dot >= avoidSlashCommitDotThreshold;
        }

        Vector3 ComputeSlashDodgeDirection(Vector3 slashDirFlat)
        {
            slashDirFlat.y = 0f;
            slashDirFlat.Normalize();
            Vector3 lateral = Vector3.Cross(Vector3.up, slashDirFlat).normalized;
            float sideSign = 1f;
            if (playerTarget != null)
            {
                Vector3 fromPlayer = transform.position - playerTarget.position;
                fromPlayer.y = 0f;
                if (fromPlayer.sqrMagnitude > 0.0001f)
                    sideSign = Vector3.Dot(fromPlayer.normalized, lateral) >= 0f ? 1f : -1f;
            }

            return (-slashDirFlat * 0.7f + lateral * sideSign * 0.75f).normalized;
        }

        bool TryBeginAttack()
        {
            if (inAttackWindow)
                return false;

            if (retreating)
                return false;

            if (Time.time < nextAttackTime)
                return false;

            if (playerTarget == null || animator == null)
                return false;

            if (avoidAttackingIntoPlayerSlash && ShouldHoldMeleeForPlayerSlash())
                return false;

            Vector3 toPlayer = playerTarget.position - transform.position;
            toPlayer.y = 0f;
            float dist = toPlayer.magnitude;
            if (dist > attackRange)
                return false;

            if (toPlayer.sqrMagnitude < 0.0001f)
                return false;

            // At standoff the NavMeshAgent can be nearly stopped, so velocity does not refresh
            // transform.forward; arc checks would fail while still in range. Face the target here.
            Vector3 toFlat = toPlayer.normalized;
            transform.rotation = Quaternion.LookRotation(new Vector3(toFlat.x, 0f, toFlat.z));

            Vector3 forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                return false;

            float angle = Vector3.Angle(forward.normalized, toPlayer.normalized);
            if (angle > attackArcDegrees * 0.5f)
                return false;

            animator.SetTrigger(attackTriggerParameter);
            inAttackWindow = true;
            dealtDamageThisSwing = false;
            nextAttackTime = Time.time + attackCooldown;
            CancelInvoke(nameof(ReleaseAttackWindowIfStuck));
            Invoke(nameof(ReleaseAttackWindowIfStuck), 2.5f);
            return true;
        }

        void ReleaseAttackWindowIfStuck()
        {
            if (IsDead || !inAttackWindow)
                return;

            MeleeAttackEnded();
        }

        public void MeleeDealDamage()
        {
            if (IsDead || dealtDamageThisSwing || playerTarget == null)
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

            if (playerTarget.TryGetComponent<IDamageable>(out IDamageable damageable))
            {
                Vector3 hitDir = toPlayer.normalized;
                damageable.TakeDamage(new DamageData(
                    meleeDamage,
                    gameObject,
                    playerTarget.position,
                    hitDir,
                    0
                ));
                dealtDamageThisSwing = true;
                pendingRetreatAfterSwing = retreatAfterSuccessfulHit;
            }
        }

        public void MeleeAttackEnded()
        {
            if (IsDead)
                return;

            CancelInvoke(nameof(ReleaseAttackWindowIfStuck));
            inAttackWindow = false;
            dealtDamageThisSwing = false;

            if (pendingRetreatAfterSwing)
            {
                pendingRetreatAfterSwing = false;
                BeginRetreatIfConfigured();
            }
        }

        void BeginRetreatIfConfigured()
        {
            if (!retreatAfterSuccessfulHit || playerTarget == null || agent == null || !agent.isOnNavMesh)
                return;

            BeginRetreatInternal(1f, 1f, useFacingBonus: true, null);
        }

        void BeginRetreatInternal(float distanceMul, float durationMul, bool useFacingBonus, Vector3? overrideFlatAway)
        {
            if (playerTarget == null || agent == null || !agent.isOnNavMesh)
                return;

            if (retreatDistance < 0.05f || retreatMaxDuration < 0.05f)
                return;

            float dist = retreatDistance * Mathf.Max(0.15f, distanceMul);
            if (useFacingBonus)
                dist *= GetPlayerFacingRetreatMultiplier();

            // Must exceed agent stopping distance or the path completes instantly and it never reads as a dodge.
            dist = Mathf.Max(dist, agent.stoppingDistance + 0.35f);

            Vector3 away;
            if (overrideFlatAway.HasValue)
            {
                away = overrideFlatAway.Value;
                away.y = 0f;
                if (away.sqrMagnitude < 0.0001f)
                    return;
                away.Normalize();
                float jit = Random.Range(-1f, 1f) * Mathf.Min(14f, retreatAngleJitterDegrees * 0.45f);
                away = Quaternion.AngleAxis(jit, Vector3.up) * away;
            }
            else
            {
                away = transform.position - playerTarget.position;
                away.y = 0f;
                if (away.sqrMagnitude < 0.0001f)
                    away = -transform.forward;
                away.Normalize();

                float jitter = Random.Range(-retreatAngleJitterDegrees, retreatAngleJitterDegrees);
                away = Quaternion.AngleAxis(jitter, Vector3.up) * away;
            }

            Vector3 desired = transform.position + away * dist;
            if (!NavMesh.SamplePosition(desired, out NavMeshHit hit, retreatNavSampleRadius, NavMesh.AllAreas))
            {
                if (!NavMesh.SamplePosition(transform.position + away * (dist * 0.65f), out hit, retreatNavSampleRadius, NavMesh.AllAreas))
                    return;
            }

            float flatDist = Vector3.Distance(
                new Vector3(transform.position.x, 0f, transform.position.z),
                new Vector3(hit.position.x, 0f, hit.position.z));
            if (flatDist < agent.stoppingDistance + 0.2f)
            {
                Vector3 push = transform.position + away * (agent.stoppingDistance + 0.55f);
                if (!NavMesh.SamplePosition(push, out hit, retreatNavSampleRadius * 1.25f, NavMesh.AllAreas))
                    return;
            }

            ApplyRetreatNavAgentTuning();

            retreating = true;
            retreatEndTime = Time.time + retreatMaxDuration * Mathf.Max(0.1f, durationMul);
            agent.isStopped = false;

            if (retreatMicroMove > 0f)
            {
                float step = Mathf.Clamp(retreatMicroMove, 0.12f, 0.55f);
                agent.Move(away * step);
            }

            agent.SetDestination(hit.position);
        }

        void ApplyRetreatNavAgentTuning()
        {
            if (agent == null || retreatStoppingDistancePatched)
                return;

            retreatSavedStoppingDistance = agent.stoppingDistance;
            retreatStoppingDistancePatched = true;
            agent.stoppingDistance = 0.06f;
        }

        void RestoreRetreatNavAgentSettings()
        {
            if (agent != null && retreatStoppingDistancePatched)
                agent.stoppingDistance = retreatSavedStoppingDistance;

            retreatStoppingDistancePatched = false;
            InvalidateMovementDestination();
        }

        void EndRetreatState()
        {
            retreating = false;
            RestoreRetreatNavAgentSettings();
        }

        float GetPlayerFacingRetreatMultiplier()
        {
            if (playerTarget == null)
                return 1f;

            Vector3 pFwd = playerTarget.forward;
            pFwd.y = 0f;
            Vector3 toSlime = transform.position - playerTarget.position;
            toSlime.y = 0f;
            if (pFwd.sqrMagnitude < 0.001f || toSlime.sqrMagnitude < 0.001f)
                return 1f;

            float dot = Vector3.Dot(pFwd.normalized, toSlime.normalized);
            if (dot < 0.2f)
                return 1f;

            return Mathf.Lerp(1f, retreatFacingPlayerDistanceMul, Mathf.InverseLerp(0.2f, 0.72f, dot));
        }

        void TickRetreatMovement()
        {
            if (agent == null || !agent.isOnNavMesh)
            {
                EndRetreatState();
                return;
            }

            if (Time.time >= retreatEndTime)
            {
                EndRetreatState();
                return;
            }

            if (!agent.pathPending && agent.hasPath && agent.remainingDistance <= agent.stoppingDistance + 0.18f)
                EndRetreatState();
        }
    }
}
