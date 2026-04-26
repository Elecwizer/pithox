using System.Collections.Generic;
using UnityEngine;

namespace Pithox.Combat
{
    // Applies damage to any object implementing IDamageable
    public class DamageDealer : MonoBehaviour
    {
        [SerializeField] float damageAmount = 10f;
        [SerializeField] bool canHitSameTargetMultipleTimes = false;
        [SerializeField] float sameTargetHitInterval = 0.25f;

        readonly HashSet<IDamageable> hitTargets = new();
        readonly Dictionary<IDamageable, float> lastHitTimes = new();

        GameObject source;
        int chainPosition;

        // Initializes damage values when skill is created
        public void Initialize(GameObject sourceObject, int skillChainPosition, float damage)
        {
            source = sourceObject;
            chainPosition = skillChainPosition;
            damageAmount = damage;

            hitTargets.Clear();
            lastHitTimes.Clear();
        }

        // Called when collider enters
        void OnTriggerEnter(Collider other)
        {
            TryDamage(other);
        }

        // Called every frame while inside collider
        void OnTriggerStay(Collider other)
        {
            if (canHitSameTargetMultipleTimes)
                TryDamage(other);
        }

        // Attempts to apply damage
        void TryDamage(Collider other)
        {
            IDamageable damageable = other.GetComponent<IDamageable>();
            damageable ??= other.GetComponentInParent<IDamageable>();
            damageable ??= other.GetComponentInChildren<IDamageable>();

            if (damageable == null)
                return;

            if (source != null && damageable is Component damageableComponent)
            {
                Transform sourceTransform = source.transform;
                Transform targetTransform = damageableComponent.transform;

                // Prevent skills from damaging the caster or the caster hierarchy.
                if (targetTransform == sourceTransform
                    || targetTransform.IsChildOf(sourceTransform)
                    || sourceTransform.IsChildOf(targetTransform))
                {
                    return;
                }
            }

            if (!canHitSameTargetMultipleTimes && hitTargets.Contains(damageable))
                return;

            if (canHitSameTargetMultipleTimes && !CanHitAgain(damageable))
                return;

            damageable.TakeDamage(new DamageData(
                damageAmount,
                source,
                other.ClosestPoint(transform.position),
                (other.transform.position - transform.position).normalized,
                chainPosition
            ));

            hitTargets.Add(damageable);
            lastHitTimes[damageable] = Time.time;
        }

        // Checks if enough time passed to damage same target again
        bool CanHitAgain(IDamageable damageable)
        {
            if (!lastHitTimes.ContainsKey(damageable)) return true;

            return Time.time >= lastHitTimes[damageable] + sameTargetHitInterval;
        }
    }
}