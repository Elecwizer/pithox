using UnityEngine;
using Pithox.Player;

namespace Pithox.Combat
{
    public class DamageAura : MonoBehaviour
    {
        [SerializeField] PlayerStats stats;
        [SerializeField] float radius = 3f;
        [SerializeField] float tickInterval = 0.75f;
        [SerializeField] float damagePerTick = 4f;
        [SerializeField] LayerMask enemyMask = ~0;

        float timer;

        void Update()
        {
            if (stats == null || !stats.DamageAuraEnabled)
                return;

            timer -= Time.deltaTime;
            if (timer > 0f)
                return;

            timer = tickInterval;

            float scaledDamage = damagePerTick * stats.DamageMultiplier * (1f + stats.AttackDamageBonus);
            Collider[] hits = Physics.OverlapSphere(transform.position, radius, enemyMask, QueryTriggerInteraction.Collide);

            foreach (Collider hit in hits)
            {
                IDamageable damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable == null)
                    continue;

                Vector3 hitPoint = hit.ClosestPoint(transform.position);
                Vector3 dir = (hit.transform.position - transform.position).normalized;
                damageable.TakeDamage(new DamageData(scaledDamage, gameObject, hitPoint, dir, 0));
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
