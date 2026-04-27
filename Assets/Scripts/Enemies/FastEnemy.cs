using UnityEngine;

namespace Pithox.Enemies
{
    public class FastEnemy : EnemyBase
    {
        [Header("Fast Enemy Behavior")]
        [SerializeField] float chaseSpeedMultiplier = 1.4f;
        [SerializeField] float dashSpeedMultiplier = 2.6f;
        [SerializeField] float dashTriggerDistance = 4f;
        [SerializeField] float dashDuration = 0.35f;
        [SerializeField] float dashCooldown = 1.5f;

        bool dashing;
        float dashEndTime;
        float nextDashTime;

        protected override void TickMovement()
        {
            if (agent == null || !agent.isOnNavMesh || playerTarget == null)
                return;

            float distance = Flatten(playerTarget.position - transform.position).magnitude;

            if (dashing)
            {
                if (Time.time >= dashEndTime)
                {
                    dashing = false;
                    nextDashTime = Time.time + dashCooldown;
                    agent.speed = moveSpeed * chaseSpeedMultiplier;
                }
            }
            else
            {
                agent.speed = moveSpeed * chaseSpeedMultiplier;

                if (Time.time >= nextDashTime && distance <= dashTriggerDistance && distance > stopDistance)
                {
                    dashing = true;
                    dashEndTime = Time.time + dashDuration;
                    agent.speed = moveSpeed * dashSpeedMultiplier;
                }
            }

            agent.SetDestination(playerTarget.position);

            Vector3 velocity = agent.velocity;
            velocity.y = 0f;
            if (velocity.sqrMagnitude > 0.01f)
                transform.forward = velocity.normalized;
        }

        static Vector3 Flatten(Vector3 v)
        {
            v.y = 0f;
            return v;
        }
    }
}
