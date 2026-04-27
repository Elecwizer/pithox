using System.Collections.Generic;
using UnityEngine;

namespace Pithox.Combat
{
    public class DamageDealer : MonoBehaviour
    {
        [SerializeField] float damageAmount = 10f;
        [SerializeField] bool canHitSameTargetMultipleTimes = false;
        [SerializeField] float sameTargetHitInterval = 0.25f;
        [SerializeField] LayerMask sweepMask = ~0;

        readonly HashSet<IDamageable> hitTargets = new();
        readonly Dictionary<IDamageable, float> lastHitTimes = new();

        GameObject source;
        int chainPosition;

        public void Initialize(GameObject sourceObject, int skillChainPosition, float damage)
        {
            source = sourceObject;
            chainPosition = skillChainPosition;
            damageAmount = damage;

            hitTargets.Clear();
            lastHitTimes.Clear();

            ImmediateOverlapSweep();
        }

        void OnTriggerEnter(Collider other) => TryDamage(other);

        void OnTriggerStay(Collider other)
        {
            if (canHitSameTargetMultipleTimes)
                TryDamage(other);
        }

        void ImmediateOverlapSweep()
        {
            Collider self = GetComponent<Collider>();
            if (self == null) return;

            Collider[] hits = OverlapByCollider(self);
            if (hits == null) return;

            foreach (Collider c in hits)
            {
                if (c == null || c == self) continue;
                TryDamage(c);
            }
        }

        Collider[] OverlapByCollider(Collider self)
        {
            switch (self)
            {
                case BoxCollider box:
                    {
                        Vector3 worldCenter = self.transform.TransformPoint(box.center);
                        Vector3 halfExtents = Vector3.Scale(box.size, self.transform.lossyScale) * 0.5f;
                        return Physics.OverlapBox(worldCenter, halfExtents, self.transform.rotation, sweepMask, QueryTriggerInteraction.Collide);
                    }
                case SphereCollider sphere:
                    {
                        Vector3 worldCenter = self.transform.TransformPoint(sphere.center);
                        Vector3 ls = self.transform.lossyScale;
                        float maxScale = Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.y), Mathf.Abs(ls.z));
                        return Physics.OverlapSphere(worldCenter, sphere.radius * maxScale, sweepMask, QueryTriggerInteraction.Collide);
                    }
                case CapsuleCollider capsule:
                    {
                        Vector3 ls = self.transform.lossyScale;
                        float radial = Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.z)) * capsule.radius;
                        float worldHeight = Mathf.Max(Mathf.Abs(ls.y) * capsule.height, radial * 2f);
                        Vector3 axis = self.transform.up;
                        Vector3 center = self.transform.TransformPoint(capsule.center);
                        float halfLen = (worldHeight * 0.5f) - radial;
                        Vector3 p1 = center + axis * halfLen;
                        Vector3 p2 = center - axis * halfLen;
                        return Physics.OverlapCapsule(p1, p2, radial, sweepMask, QueryTriggerInteraction.Collide);
                    }
                default:
                    {
                        Bounds b = self.bounds;
                        return Physics.OverlapBox(b.center, b.extents, Quaternion.identity, sweepMask, QueryTriggerInteraction.Collide);
                    }
            }
        }

        void TryDamage(Collider other)
        {
            IDamageable damageable = other.GetComponentInParent<IDamageable>();

            if (damageable == null) return;

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

        bool CanHitAgain(IDamageable damageable)
        {
            if (!lastHitTimes.ContainsKey(damageable)) return true;
            return Time.time >= lastHitTimes[damageable] + sameTargetHitInterval;
        }
    }
}
