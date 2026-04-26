using UnityEngine;

namespace Pithox.Enemies
{
    public class ProjectileEnemy : EnemyBase
    {
        [Header("Ranged Attack")]
        [SerializeField] EnemyProjectile projectilePrefab;
        [SerializeField] Transform firePoint;
        [SerializeField] float attackRange = 10f;
        [SerializeField] float attackCooldown = 3f;
        [SerializeField] float projectileSpeed = 10f;
        [SerializeField] float projectileDamage = 10f;
        [SerializeField] float projectileSpawnHeight = 0.4f;
        [SerializeField] float preferredDistance = 8f;
        [SerializeField] float distanceTolerance = 1f;
        [SerializeField, Range(0.1f, 1f)] float repositionSpeedMultiplier = 0.45f;

        float nextAttackTime;

        protected override void Update()
        {
            base.Update();

            if (playerTarget == null)
                return;

            AimFirePointAtPlayer();
            TryShoot();
        }

        protected override void TickMovement()
        {
            if (playerTarget == null)
                return;

            Vector3 toPlayer = playerTarget.position - transform.position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;

            if (distance <= 0.001f)
                return;

            Vector3 direction = toPlayer / distance;
            transform.forward = direction;

            float maxPreferredDistance = preferredDistance + distanceTolerance;
            float minPreferredDistance = Mathf.Max(stopDistance, preferredDistance - distanceTolerance);
            if (CanUseNavMesh())
            {
                float repositionSpeed = moveSpeed * repositionSpeedMultiplier;
                navMeshAgent.speed = repositionSpeed;
                navMeshAgent.stoppingDistance = 0.05f;

                if (distance > maxPreferredDistance || distance < minPreferredDistance)
                {
                    Vector3 desiredPosition = playerTarget.position - direction * preferredDistance;
                    navMeshAgent.SetDestination(desiredPosition);
                }
                else
                {
                    navMeshAgent.ResetPath();
                }
                return;
            }

            float fallbackRepositionSpeed = moveSpeed * repositionSpeedMultiplier;
            if (distance > maxPreferredDistance)
            {
                transform.position += direction * fallbackRepositionSpeed * Time.deltaTime;
            }
            else if (distance < minPreferredDistance)
            {
                transform.position -= direction * fallbackRepositionSpeed * Time.deltaTime;
            }
        }

        void TryShoot()
        {
            if (projectilePrefab == null || Time.time < nextAttackTime)
                return;

            Vector3 toPlayer = playerTarget.position - transform.position;
            toPlayer.y = 0f;
            float rangeSqr = attackRange * attackRange;

            if (toPlayer.sqrMagnitude > rangeSqr)
                return;

            Vector3 shotDirection = toPlayer.normalized;
            AimFirePoint(shotDirection);
            Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position + shotDirection;
            spawnPosition += Vector3.up * projectileSpawnHeight;
            EnemyProjectile projectile = Instantiate(
                projectilePrefab,
                spawnPosition,
                Quaternion.LookRotation(shotDirection, Vector3.up)
            );
            projectile.Initialize(shotDirection, projectileSpeed, projectileDamage, gameObject);

            nextAttackTime = Time.time + Mathf.Max(0.05f, attackCooldown);
        }

        void AimFirePointAtPlayer()
        {
            if (firePoint == null)
                return;

            Vector3 toPlayer = playerTarget.position - firePoint.position;
            toPlayer.y = 0f;

            if (toPlayer.sqrMagnitude <= 0.0001f)
                return;

            AimFirePoint(toPlayer.normalized);
        }

        void AimFirePoint(Vector3 direction)
        {
            if (firePoint == null || direction.sqrMagnitude <= 0.0001f)
                return;

            firePoint.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }
}
