using Pithox.Combat;
using UnityEngine;

namespace Pithox.Enemies
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public class EnemyProjectile : MonoBehaviour
    {
        [SerializeField] float speed = 8f;
        [SerializeField] float lifetime = 4f;
        [SerializeField] float damage = 8f;

        Vector3 travelDirection;
        GameObject sourceEnemy;
        Collider projectileCollider;
        Rigidbody projectileRigidbody;

        void Awake()
        {
            projectileCollider = GetComponent<Collider>();
            projectileRigidbody = GetComponent<Rigidbody>();

            // Projectile uses trigger overlaps for reliable hit detection while moved manually.
            projectileCollider.isTrigger = true;
            projectileRigidbody.isKinematic = true;
            projectileRigidbody.useGravity = false;
            projectileRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        public void Initialize(Vector3 direction, float projectileSpeed, float projectileDamage, GameObject source)
        {
            travelDirection = direction.normalized;
            speed = projectileSpeed;
            damage = projectileDamage;
            sourceEnemy = source;
            Destroy(gameObject, lifetime);
        }

        void Update()
        {
            transform.position += travelDirection * speed * Time.deltaTime;
        }

        void OnTriggerEnter(Collider other)
        {
            if (sourceEnemy != null && other.gameObject == sourceEnemy)
                return;

            if (other.TryGetComponent<IDamageable>(out IDamageable damageable)
                || other.GetComponentInParent<IDamageable>() is IDamageable parentDamageable && (damageable = parentDamageable) != null)
            {
                damageable.TakeDamage(new DamageData(
                    damage,
                    sourceEnemy != null ? sourceEnemy : gameObject,
                    other.ClosestPoint(transform.position),
                    travelDirection,
                    0
                ));
            }

            Destroy(gameObject);
        }
    }
}
