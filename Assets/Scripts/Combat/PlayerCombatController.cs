using UnityEngine;
using Pithox.Player;
using Pithox.Skills;

namespace Pithox.Combat
{
    public class PlayerCombatController : MonoBehaviour
    {
        [Header("Slash")]
        [SerializeField] Transform slashPoint;
        [SerializeField] GameObject slashPrefab;
        [SerializeField] float attackCooldown = 0.4f;
        [SerializeField] float searchRadius = 9f;
        [SerializeField] LayerMask enemyMask = ~0;
        [SerializeField] KeyCode attackKey = KeyCode.Space;

        [Header("Refs")]
        [SerializeField] PlayerTombCarry tombCarry;

        ArcSlashSkill basicAttack;

        void Awake()
        {
            basicAttack = new ArcSlashSkill(slashPrefab, slashPoint, attackCooldown);
        }

        void Update()
        {
            basicAttack.Cooldown.Tick(Time.deltaTime);

            if (!Input.GetKeyDown(attackKey))
                return;

            if (tombCarry != null && tombCarry.IsCarrying)
                return;

            if (!basicAttack.Cooldown.IsReady)
                return;

            AimSlash();
            basicAttack.Execute(transform, 1);
            basicAttack.Cooldown.StartCooldown();
        }

        void AimSlash()
        {
            if (slashPoint == null)
                return;

            Transform target = FindNearestEnemy();
            Vector3 dir;

            if (target != null)
            {
                dir = target.position - slashPoint.position;
                dir.y = 0f;
            }
            else
            {
                dir = transform.forward;
                dir.y = 0f;
            }

            if (dir.sqrMagnitude < 0.0001f)
                return;

            slashPoint.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        Transform FindNearestEnemy()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, searchRadius, enemyMask, QueryTriggerInteraction.Collide);
            Transform best = null;
            float bestSqr = Mathf.Infinity;

            foreach (Collider c in hits)
            {
                if (c == null) continue;
                if (c.GetComponentInParent<IDamageable>() == null) continue;

                Vector3 to = c.transform.position - transform.position;
                to.y = 0f;
                float sqr = to.sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = c.transform;
                }
            }
            return best;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, searchRadius);
        }
    }
}
