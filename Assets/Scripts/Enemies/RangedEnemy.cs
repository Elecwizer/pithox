using Pithox.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace Pithox.Enemies
{
    /// <summary>
    /// Keeps spacing from the player, shoots on a cadence while in a distance band, and spawns
    /// <see cref="EnemyProjectile"/> on a timer after the cast trigger (no FBX animation events on the shared clip).
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class RangedEnemy : EnemyBase
    {
        [Header("Animator")]
        [SerializeField] Animator animator;
        [SerializeField] string speedParameter = "Speed";
        [SerializeField] string attackTriggerParameter = "Attack";
        [SerializeField] string hitTriggerParameter = "Hit";
        [SerializeField] string deathTriggerParameter = "Die";
        [SerializeField] float hitAnimatorRetriggerCooldown = 0.22f;

        [Header("Death (animated)")]
        [SerializeField] float deathDespawnDelay = 1.35f;
        [SerializeField] float deathTombSpawnPadSeconds = 0.1f;

        [Header("Ranged")]
        [SerializeField] EnemyProjectile projectilePrefab;
        [SerializeField] Transform projectileSpawn;
        [Tooltip("Try to stay at least this far from the player while kiting.")]
        [SerializeField] float minComfortRange = 5.5f;
        [Tooltip("Extra push outward when too close (added on top of comfort range).")]
        [SerializeField] float comfortRingPadding = 2f;
        [Tooltip("Flat distance from the player the wizard tries to hold while kiting (clamped into the shot band).")]
        [SerializeField] float preferredCombatDistance = 11.5f;
        [Tooltip("Sideways offset amplitude while holding range (NavMesh destination, not raw slide).")]
        [SerializeField] float strafeAmplitude = 2.75f;
        [Tooltip("How quickly the strafe target oscillates (higher = faster weave).")]
        [SerializeField] float strafeOscillationSpeed = 2.8f;
        [Tooltip("Will not start a cast closer than this (avoids spam in hug range).")]
        [SerializeField] float minShotDistance = 3.25f;
        [Tooltip("Will not start a cast beyond this flat distance.")]
        [SerializeField] float maxShotDistance = 22f;
        [SerializeField] float attackCooldown = 2.85f;
        [SerializeField] float projectileDamage = 12f;
        [SerializeField, Range(10f, 170f)] float attackArcDegrees = 125f;
        [Tooltip("Seconds after Attack trigger before the bolt spawns (tune to match cast pose).")]
        [SerializeField] float projectileSpawnDelay = 0.28f;
        [Tooltip("Seconds after Attack trigger before movement resumes (tune to match clip).")]
        [SerializeField] float castResumeMoveDelay = 0.58f;

        [Header("Smarter AI")]
        [Tooltip("Raycast mask for wall / prop blocking. Should not include the player (LOS passes when the ray hits the player).")]
        [SerializeField] LayerMask lineOfSightBlockMask = ~0;
        [SerializeField] float lineOfSightHeight = 1.15f;
        [Tooltip("NavMesh.SamplePosition radius when choosing a kite point.")]
        [SerializeField] float navSampleRadius = 2.4f;
        [Tooltip("How much the kite target leads the player from their recent movement.")]
        [SerializeField] float pursuitLeadSeconds = 0.28f;
        [Tooltip("Extra ring push (meters) when the player is clearly running toward the wizard.")]
        [SerializeField] float chargeRepulseMeters = 1.35f;
        [Tooltip("Player flat speed above this counts as a committed charge for repulse.")]
        [SerializeField] float chargeSpeedThreshold = 3.4f;

        float nextAttackTime;
        bool casting;
        float nextHitAnimatorAllowedTime;
        float strafePhase;
        Vector3 lastPlayerFlat;
        float lastPlayerSampleTime = -1f;
        Vector3 smoothedPlayerVelocityFlat;
        Collider[] selfColliders;

        protected override float ChasePathRefreshSeconds => 0.18f;

        protected override void Awake()
        {
            useTouchDamage = false;
            base.Awake();

            if (animator == null)
                animator = GetComponent<Animator>();

            strafePhase = Random.Range(0f, Mathf.PI * 2f);
            selfColliders = GetComponentsInChildren<Collider>(true);
        }

        protected override void Update()
        {
            base.Update();

            if (IsDead || playerTarget == null || animator == null || agent == null || !agent.isOnNavMesh)
                return;

            if (!string.IsNullOrEmpty(speedParameter))
                animator.SetFloat(speedParameter, agent.velocity.magnitude);

            TrackPlayerMotion();
            TryBeginRangedAttack();
        }

        protected override void TickMovement()
        {
            if (!casting)
                base.TickMovement();

            FacePlayerOnFlatPlane();
        }

        void FacePlayerOnFlatPlane()
        {
            if (playerTarget == null || IsDead)
                return;

            Vector3 to = playerTarget.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.0001f)
                return;

            transform.rotation = Quaternion.LookRotation(to.normalized, Vector3.up);
        }

        void TrackPlayerMotion()
        {
            if (playerTarget == null)
                return;

            Vector3 p = playerTarget.position;
            p.y = 0f;
            if (lastPlayerSampleTime < 0f)
            {
                lastPlayerFlat = p;
                lastPlayerSampleTime = Time.time;
                smoothedPlayerVelocityFlat = Vector3.zero;
                return;
            }

            float dt = Time.time - lastPlayerSampleTime;
            lastPlayerSampleTime = Time.time;
            if (dt < 0.0001f)
                return;

            Vector3 instant = (p - lastPlayerFlat) / dt;
            lastPlayerFlat = p;
            smoothedPlayerVelocityFlat = Vector3.Lerp(smoothedPlayerVelocityFlat, instant, Mathf.Clamp01(dt * 6f));
        }

        Vector3 KiteFocusFlat()
        {
            if (playerTarget == null)
                return transform.position;

            Vector3 p = playerTarget.position;
            p.y = 0f;
            Vector3 lead = smoothedPlayerVelocityFlat * pursuitLeadSeconds;
            if (lead.sqrMagnitude > 64f)
                lead = lead.normalized * 8f;
            return p + lead;
        }

        static bool NavClamp(Vector3 world, float radius, out Vector3 onNav)
        {
            if (NavMesh.SamplePosition(world, out NavMeshHit hit, radius, NavMesh.AllAreas))
            {
                onNav = hit.position;
                return true;
            }

            onNav = world;
            return false;
        }

        bool HasFlatLineOfSight(Vector3 fromWorld, Vector3 toWorld)
        {
            Vector3 a = new Vector3(fromWorld.x, fromWorld.y + lineOfSightHeight, fromWorld.z);
            Vector3 b = new Vector3(toWorld.x, toWorld.y + lineOfSightHeight, toWorld.z);
            Vector3 delta = b - a;
            float dist = delta.magnitude;
            if (dist < 0.05f)
                return true;

            Vector3 dir = delta / dist;
            RaycastHit[] hits = Physics.RaycastAll(a, dir, dist, lineOfSightBlockMask, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0)
                return true;

            System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                if (IsSelfCollider(hits[i].collider))
                    continue;
                return HitIsPlayer(hits[i]);
            }

            return true;
        }

        bool HitIsPlayer(RaycastHit hit)
        {
            return playerTarget != null && hit.collider != null &&
                   (hit.collider.transform == playerTarget || hit.collider.transform.IsChildOf(playerTarget));
        }

        bool IsSelfCollider(Collider c)
        {
            if (c == null || selfColliders == null)
                return false;
            for (int i = 0; i < selfColliders.Length; i++)
            {
                if (selfColliders[i] == c)
                    return true;
            }

            return false;
        }

        protected override Vector3 GetChaseDestination()
        {
            if (playerTarget == null)
                return base.GetChaseDestination();

            Vector3 focus = KiteFocusFlat();
            Vector3 fromFocus = transform.position - focus;
            fromFocus.y = 0f;
            float d = fromFocus.magnitude;

            if (d < 0.08f)
                return transform.position + transform.forward * 4f;

            Vector3 outward = fromFocus / d;

            if (d < minComfortRange)
            {
                Vector3 retreat = focus - outward * (minComfortRange + comfortRingPadding);
                NavClamp(retreat, navSampleRadius, out retreat);
                return retreat;
            }

            float inner = Mathf.Max(minShotDistance + 0.75f, minComfortRange + 0.25f);
            float outer = Mathf.Max(inner + 0.5f, maxShotDistance - 0.75f);
            float ideal = Mathf.Clamp(preferredCombatDistance, inner, outer);

            Vector3 ringHold = focus + outward * ideal;

            if (smoothedPlayerVelocityFlat.sqrMagnitude > chargeSpeedThreshold * chargeSpeedThreshold)
            {
                Vector3 run = smoothedPlayerVelocityFlat.normalized;
                float closing = Vector3.Dot(-outward, run);
                if (closing > 0.25f)
                    ringHold += outward * (chargeRepulseMeters * Mathf.Clamp01((closing - 0.25f) / 0.55f));
            }

            Vector3 tangent = Vector3.Cross(Vector3.up, outward).normalized;
            float weave = Mathf.Sin(Time.time * strafeOscillationSpeed + strafePhase) * strafeAmplitude;

            if (smoothedPlayerVelocityFlat.sqrMagnitude > 0.12f)
            {
                Vector3 side = Vector3.Cross(Vector3.up, smoothedPlayerVelocityFlat.normalized).normalized;
                float align = Vector3.Dot(side, tangent);
                weave += align * (strafeAmplitude * 0.35f);
            }

            Vector3 raw = ringHold + tangent * weave;
            NavClamp(raw, navSampleRadius, out raw);
            return raw;
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
        }

        void CancelCastScheduledCalls()
        {
            CancelInvoke(nameof(SpawnProjectileDelayed));
            CancelInvoke(nameof(ResumeAfterCast));
            CancelInvoke(nameof(ReleaseCastIfStuck));
        }

        void TryBeginRangedAttack()
        {
            if (casting || Time.time < nextAttackTime)
                return;

            Vector3 toPlayer = playerTarget.position - transform.position;
            toPlayer.y = 0f;
            float dist = toPlayer.magnitude;
            if (dist > maxShotDistance || dist < minShotDistance)
                return;

            if (toPlayer.sqrMagnitude < 0.0001f)
                return;

            Vector3 forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                return;

            if (Vector3.Angle(forward.normalized, toPlayer.normalized) > attackArcDegrees * 0.5f)
                return;

            if (!HasFlatLineOfSight(transform.position, playerTarget.position))
                return;

            agent.isStopped = true;
            animator.SetTrigger(attackTriggerParameter);
            casting = true;
            nextAttackTime = Time.time + attackCooldown;

            CancelCastScheduledCalls();
            float spawnT = Mathf.Max(0.02f, projectileSpawnDelay);
            float resumeT = Mathf.Max(spawnT + 0.02f, castResumeMoveDelay);
            Invoke(nameof(SpawnProjectileDelayed), spawnT);
            Invoke(nameof(ResumeAfterCast), resumeT);
            Invoke(nameof(ReleaseCastIfStuck), 3.5f);
        }

        void SpawnProjectileDelayed()
        {
            if (IsDead || !casting || playerTarget == null || projectilePrefab == null)
                return;

            Transform origin = projectileSpawn != null ? projectileSpawn : transform;
            Vector3 start = origin.position;
            Vector3 toP = playerTarget.position - start;
            toP.y = 0f;
            float dist = toP.magnitude;
            if (dist > maxShotDistance + 0.75f || dist < minShotDistance - 0.15f)
            {
                ResumeAfterCast();
                return;
            }

            if (!HasFlatLineOfSight(start, playerTarget.position))
            {
                ResumeAfterCast();
                return;
            }

            Vector3 aim = toP.sqrMagnitude > 0.0001f ? toP.normalized : transform.forward;
            EnemyProjectile proj = Instantiate(projectilePrefab, start, Quaternion.LookRotation(aim, Vector3.up));
            proj.Initialize(gameObject, aim, projectileDamage, playerTarget);
        }

        void ResumeAfterCast()
        {
            if (!casting)
                return;

            casting = false;
            if (agent != null && agent.isOnNavMesh)
                agent.isStopped = false;

            CancelInvoke(nameof(ReleaseCastIfStuck));
        }

        void ReleaseCastIfStuck()
        {
            if (casting)
                ResumeAfterCast();
        }

        protected override void Die()
        {
            if (IsDead)
                return;

            isDead = true;
            CancelCastScheduledCalls();
            CancelInvoke(nameof(FinishAnimatedDeath));

            if (animator != null && !string.IsNullOrEmpty(deathTriggerParameter))
            {
                PlayDeathCameraAndGlobalVfx();
                PlayDeathSounds();

                casting = false;
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
            DestroyEnemyGameObject();
        }

        void FinishAnimatedDeath()
        {
            SpawnDeathTomb();
            Destroy(gameObject);
        }
    }
}
