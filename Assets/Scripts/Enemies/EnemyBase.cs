using Pithox.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace Pithox.Enemies
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyBase : MonoBehaviour, IDamageable
    {
        [Header("Core Stats")]
        [SerializeField] protected float maxHealth = 20f;
        [SerializeField] protected float moveSpeed = 3f;
        [SerializeField] protected float stopDistance = 1.4f;
        [SerializeField] protected float repathThreshold = 0.5f;

        [Header("Touch Damage")]
        [SerializeField] protected bool useTouchDamage = true;
        [SerializeField] protected float touchDamage = 5f;
        [SerializeField] protected float touchDamageInterval = 0.5f;
        [SerializeField] protected float touchRange = 1.25f;

        [Header("Hit Reaction")]
        [SerializeField] float knockbackDistance = 0.4f;

        [Header("Death")]
        [SerializeField] GameObject tombPrefab;
        [SerializeField] AudioClip defaultDeathSfx;
        [SerializeField] AudioClip uniqueDeathSfx;
        [SerializeField] float deathSfxVolume = 1f;
        [SerializeField] float deathCameraShake = 0.05f;

        [SerializeField] protected string playerTag = "Player";

        protected Transform playerTarget;
        protected NavMeshAgent agent;
        HitFlash hitFlash;
        float currentHealth;
        float nextTouchDamageTime;
        protected bool IsDead => isDead;
        protected bool isDead;
        Vector3 lastRepathAnchor;
        float lastChasePathRefreshTime;
        bool hasDestination;

        protected virtual void Awake()
        {
            currentHealth = maxHealth;
            agent = GetComponent<NavMeshAgent>();
            hitFlash = GetComponent<HitFlash>();

            if (agent != null)
            {
                agent.speed = moveSpeed;
                agent.stoppingDistance = stopDistance;
                agent.updateRotation = false;
                agent.acceleration = 16f;
                agent.angularSpeed = 720f;
                agent.autoBraking = true;
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            }
        }

        protected virtual void Start()
        {
            FindPlayerTarget();
        }

        protected virtual void Update()
        {
            if (isDead)
                return;

            if (playerTarget == null)
            {
                FindPlayerTarget();
                return;
            }

            TickMovement();
            TickTouchDamage();
        }

        public virtual void TakeDamage(DamageData damageData)
        {
            if (isDead)
                return;

            currentHealth -= damageData.Amount;

            if (hitFlash != null)
                hitFlash.Flash();

            Pithox.Visual.HitVFX.PlayHit(damageData.HitPoint, new Color(1f, 0.9f, 0.7f));

            ApplyKnockback(damageData);

            if (currentHealth <= 0f)
            {
                Die();
            }
            else
            {
                OnAfterDamageApplied(damageData);
            }
        }

        protected virtual void OnAfterDamageApplied(DamageData damageData) { }

        void ApplyKnockback(DamageData damageData)
        {
            if (agent == null || !agent.isOnNavMesh || knockbackDistance <= 0f)
                return;

            Vector3 push = damageData.HitDirection;
            push.y = 0f;
            if (push.sqrMagnitude < 0.0001f)
                return;

            agent.Move(push.normalized * knockbackDistance);
        }

        protected virtual void Die()
        {
            if (isDead)
                return;

            isDead = true;
            RunDeathPresentation();
            DestroyEnemyGameObject();
        }

        protected void RunDeathPresentation()
        {
            PlayDeathCameraAndGlobalVfx();
            SpawnDeathTomb();
            PlayDeathSounds();
        }

        protected void PlayDeathCameraAndGlobalVfx()
        {
            SmoothMidCamera.Shake(deathCameraShake, 0.1f);
            Pithox.Visual.HitVFX.PlayDeath(transform.position, new Color(1f, 0.6f, 0.3f));
        }

        protected void SpawnDeathTomb()
        {
            if (tombPrefab == null)
                return;

            Vector3 tombPosition = new Vector3(transform.position.x, 0f, transform.position.z);
            Instantiate(tombPrefab, tombPosition, tombPrefab.transform.rotation);
        }

        protected void PlayDeathSounds()
        {
            AudioClip clip = uniqueDeathSfx != null ? uniqueDeathSfx : defaultDeathSfx;

            if (clip != null)
            {
                GameObject sfxObject = new GameObject("Enemy Death SFX");
                sfxObject.transform.position = transform.position;

                AudioSource audioSource = sfxObject.AddComponent<AudioSource>();
                audioSource.clip = clip;
                audioSource.volume = deathSfxVolume;
                audioSource.spatialBlend = 0f;
                audioSource.Play();

                Destroy(sfxObject, clip.length);
            }
        }

        protected virtual void DestroyEnemyGameObject()
        {
            Destroy(gameObject);
        }

        protected virtual void TickMovement()
        {
            if (agent == null || !agent.isOnNavMesh || playerTarget == null)
                return;

            Vector3 anchor = GetChaseAnchor();
            Vector3 target = GetChaseDestination();
            float thr2 = repathThreshold * repathThreshold;
            bool anchorMoved = (anchor - lastRepathAnchor).sqrMagnitude > thr2;
            float refresh = ChasePathRefreshSeconds;
            bool timedRefresh = refresh > 0f && (Time.time - lastChasePathRefreshTime >= refresh);

            if (!hasDestination || anchorMoved || timedRefresh)
            {
                agent.SetDestination(target);
                lastRepathAnchor = anchor;
                lastChasePathRefreshTime = Time.time;
                hasDestination = true;
            }

            Vector3 velocity = agent.velocity;
            velocity.y = 0f;
            if (velocity.sqrMagnitude > 0.01f)
                transform.forward = velocity.normalized;
        }

        protected virtual Vector3 GetChaseDestination()
        {
            return playerTarget != null ? playerTarget.position : transform.position;
        }

        /// <summary>Used for repath gating (player motion). Offsets from <see cref="GetChaseDestination"/> are applied without forcing a new path every frame.</summary>
        protected virtual Vector3 GetChaseAnchor()
        {
            return playerTarget != null ? playerTarget.position : transform.position;
        }

        /// <summary>When &gt; 0, chase destination is refreshed on this interval so lateral weave / strafe targets update without spamming SetDestination every frame.</summary>
        protected virtual float ChasePathRefreshSeconds => 0f;

        protected void InvalidateMovementDestination()
        {
            hasDestination = false;
        }

        protected virtual void TickTouchDamage()
        {
            if (!useTouchDamage)
                return;

            if (Time.time < nextTouchDamageTime)
                return;

            if (!IsInTouchRange())
                return;

            if (playerTarget.TryGetComponent<IDamageable>(out IDamageable playerDamageable))
            {
                Vector3 hitDirection = (playerTarget.position - transform.position).normalized;
                playerDamageable.TakeDamage(new DamageData(
                    touchDamage,
                    gameObject,
                    playerTarget.position,
                    hitDirection,
                    0
                ));
                nextTouchDamageTime = Time.time + Mathf.Max(0.01f, touchDamageInterval);
            }
        }

        bool IsInTouchRange()
        {
            Vector3 offset = playerTarget.position - transform.position;
            offset.y = 0f;
            return offset.sqrMagnitude <= touchRange * touchRange;
        }

        void FindPlayerTarget()
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
            playerTarget = playerObject != null ? playerObject.transform : null;
        }
    }
}