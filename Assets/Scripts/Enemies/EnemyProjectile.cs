using Pithox.Combat;
using UnityEngine;

namespace Pithox.Enemies
{
    [RequireComponent(typeof(SphereCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class EnemyProjectile : MonoBehaviour
    {
        [Header("Motion")]
        [SerializeField] float speed = 8f;
        [SerializeField] float maxLifetime = 8f;

        [Header("Steering")]
        [Tooltip("How long the bolt may home toward the player; after this it flies straight along its last direction.")]
        [SerializeField] float homingDurationSeconds = 0.75f;
        [Tooltip("If the player is within this flat distance at spawn, the bolt snaps toward them quickly (near straight line).")]
        [SerializeField] float closeSpawnRange = 6f;
        [Tooltip("When spawn was farther than close range, steer gently for this many seconds, then turn harder (capped to fit inside homing duration).")]
        [SerializeField] float farLockDelaySeconds = 2f;
        [SerializeField] float turnRateCloseDegPerSec = 720f;
        [SerializeField] float turnRateFarInitialDegPerSec = 95f;
        [SerializeField] float turnRateFarLockedDegPerSec = 520f;

        [Header("Trail (lock read)")]
        [Tooltip("Assign a transparent / particles material. Trail widens and brightens as the projectile approaches the hard homing / ballistic commit.")]
        [SerializeField] Material trailMaterial;
        [SerializeField] TrailRenderer optionalTrail;
        [Tooltip("How many seconds before the hard lock the trail begins ramping toward the locked look.")]
        [SerializeField] float lockTrailAnticipateSeconds = 0.55f;
        [SerializeField] float trailTimeSteering = 0.42f;
        [SerializeField] float trailTimeLocked = 0.26f;
        [SerializeField] Color trailSteeringStart = new(1.1f, 0.18f, 0.1f, 0.52f);
        [SerializeField] Color trailSteeringEnd = new(0.65f, 0.08f, 0.06f, 0f);
        [SerializeField] Color trailLockedStart = new(1.45f, 0.32f, 0.12f, 1f);
        [SerializeField] Color trailLockedEnd = new(1f, 0.15f, 0.08f, 0f);
        [SerializeField] float trailWidthStartSteering = 0.1f;
        [SerializeField] float trailWidthEndSteering = 0.02f;
        [SerializeField] float trailWidthStartLocked = 0.22f;
        [SerializeField] float trailWidthEndLocked = 0.07f;

        GameObject sourceRoot;
        Transform aimTarget;
        Vector3 direction;
        float damage;
        bool dealt;
        float spawnTime;
        float spawnDistanceToPlayer;

        void Awake()
        {
            var rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var col = GetComponent<SphereCollider>();
            col.isTrigger = true;

            if (optionalTrail == null)
                optionalTrail = GetComponent<TrailRenderer>();

            if (optionalTrail == null && trailMaterial != null)
            {
                optionalTrail = gameObject.AddComponent<TrailRenderer>();
                optionalTrail.material = trailMaterial;
                optionalTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                optionalTrail.receiveShadows = false;
                optionalTrail.generateLightingData = false;
                optionalTrail.numCornerVertices = 2;
                optionalTrail.numCapVertices = 1;
                optionalTrail.minVertexDistance = 0.04f;
                optionalTrail.autodestruct = false;
                optionalTrail.emitting = false;
            }

            ApplyTrailLockVisual(0f);
        }

        public void Initialize(GameObject source, Vector3 flatDirection, float damageAmount, Transform aimTargetTransform)
        {
            sourceRoot = source;
            aimTarget = aimTargetTransform;
            direction = flatDirection;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector3.forward;
            direction.Normalize();

            damage = damageAmount;
            dealt = false;
            spawnTime = Time.time;
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

            if (aimTarget != null)
            {
                Vector3 flat = aimTarget.position - transform.position;
                flat.y = 0f;
                spawnDistanceToPlayer = flat.magnitude;
            }
            else
            {
                spawnDistanceToPlayer = 999f;
            }

            if (optionalTrail != null)
            {
                optionalTrail.Clear();
                optionalTrail.emitting = true;
                ApplyTrailLockVisual(0f);
            }
        }

        void Update()
        {
            if (Time.time - spawnTime > maxLifetime)
            {
                Destroy(gameObject);
                return;
            }

            Transform target = aimTarget;
            if (target == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                target = p != null ? p.transform : null;
            }

            float elapsed = Time.time - spawnTime;
            if (elapsed < homingDurationSeconds && target != null)
            {
                Vector3 to = target.position - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.0001f)
                {
                    Vector3 desired = to.normalized;
                    float phaseLockDelay = Mathf.Min(farLockDelaySeconds, homingDurationSeconds * 0.55f);
                    float turnDeg = turnRateCloseDegPerSec;
                    if (spawnDistanceToPlayer > closeSpawnRange)
                        turnDeg = elapsed < phaseLockDelay ? turnRateFarInitialDegPerSec : turnRateFarLockedDegPerSec;

                    float maxRad = turnDeg * Mathf.Deg2Rad * Time.deltaTime;
                    direction = Vector3.RotateTowards(direction, desired, maxRad, 0f).normalized;
                }
            }

            if (direction.sqrMagnitude < 0.0001f)
                direction = transform.forward;

            float lockVisualBlend = ComputeLockVisualBlend(elapsed);
            ApplyTrailLockVisual(lockVisualBlend);

            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.position += direction * (speed * Time.deltaTime);
        }

        /// <summary>
        /// 0 while the bolt is in the gentle curve phase, ramps up during the last
        /// <see cref="lockTrailAnticipateSeconds"/> before hard lock, 1 once it is homing hard (straight pursuit).
        /// </summary>
        float ComputeLockVisualBlend(float elapsed)
        {
            if (elapsed >= homingDurationSeconds)
                return 1f;

            if (spawnDistanceToPlayer <= closeSpawnRange)
                return 1f;

            float hardLockTime = Mathf.Min(farLockDelaySeconds, homingDurationSeconds * 0.58f);
            if (elapsed >= hardLockTime)
                return 1f;

            float anticipate = Mathf.Clamp(lockTrailAnticipateSeconds, 0.05f, Mathf.Max(0.05f, hardLockTime - 0.02f));
            float rampStart = Mathf.Max(0f, hardLockTime - anticipate);
            if (elapsed <= rampStart)
                return 0f;

            return Mathf.InverseLerp(rampStart, hardLockTime, elapsed);
        }

        void ApplyTrailLockVisual(float lockBlend)
        {
            if (optionalTrail == null)
                return;

            optionalTrail.time = Mathf.Lerp(trailTimeSteering, trailTimeLocked, lockBlend);
            optionalTrail.startColor = Color.Lerp(trailSteeringStart, trailLockedStart, lockBlend);
            optionalTrail.endColor = Color.Lerp(trailSteeringEnd, trailLockedEnd, lockBlend);
            optionalTrail.startWidth = Mathf.Lerp(trailWidthStartSteering, trailWidthStartLocked, lockBlend);
            optionalTrail.endWidth = Mathf.Lerp(trailWidthEndSteering, trailWidthEndLocked, lockBlend);
        }

        void OnTriggerEnter(Collider other)
        {
            if (dealt || other == null)
                return;

            if (!other.CompareTag("Player"))
                return;

            if (sourceRoot != null && other.transform.root == sourceRoot.transform.root)
                return;

            if (!other.TryGetComponent(out IDamageable damageable))
                damageable = other.GetComponentInParent<IDamageable>();

            if (damageable == null)
                return;

            dealt = true;
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            Vector3 hitDir = direction.sqrMagnitude > 0.0001f ? direction : (hitPoint - transform.position).normalized;
            damageable.TakeDamage(new DamageData(
                damage,
                sourceRoot != null ? sourceRoot : gameObject,
                hitPoint,
                hitDir,
                0
            ));
            Destroy(gameObject);
        }
    }
}
