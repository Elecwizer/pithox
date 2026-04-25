using Pithox.Combat;
using UnityEngine;

namespace Pithox.Enemies
{
    public class EnemyProjectile : MonoBehaviour
    {
        [SerializeField] float speed = 8f;
        [SerializeField] float lifetime = 4f;
        [SerializeField] float damage = 8f;

        Vector3 travelDirection;
        GameObject sourceEnemy;

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

            if (other.TryGetComponent<IDamageable>(out IDamageable damageable))
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
