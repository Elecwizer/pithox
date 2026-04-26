using UnityEngine;

namespace Pithox.Enemies
{
    public class FastEnemy : EnemyBase
    {
        enum FastEnemyState
        {
            Chase,
            Dash,
            Orbit
        }

        [Header("Fast Enemy Behavior")]
        [SerializeField] float chaseSpeedMultiplier = 1.4f;
        [SerializeField] float dashSpeedMultiplier = 2.6f;
        [SerializeField] float dashTriggerDistance = 2.5f;
        [SerializeField] float dashDuration = 0.45f;
        [SerializeField] float orbitDuration = 1f;
        [SerializeField] float orbitRadius = 3f;
        [SerializeField] float orbitAngularSpeed = 230f;

        FastEnemyState state = FastEnemyState.Chase;
        Vector3 dashDirection;
        float stateTimer;
        int orbitDirection = 1;

        protected override void TickMovement()
        {
            if (playerTarget == null || moveSpeed <= 0f)
                return;

            switch (state)
            {
                case FastEnemyState.Chase:
                    TickChase();
                    break;
                case FastEnemyState.Dash:
                    TickDash();
                    break;
                case FastEnemyState.Orbit:
                    TickOrbit();
                    break;
            }
        }

        void TickChase()
        {
            Vector3 toPlayer = Flatten(playerTarget.position - transform.position);
            float sqrDistance = toPlayer.sqrMagnitude;
            if (sqrDistance <= 0.001f)
                return;

            float distance = Mathf.Sqrt(sqrDistance);
            Vector3 chaseDirection = toPlayer / distance;

            transform.position += chaseDirection * moveSpeed * chaseSpeedMultiplier * Time.deltaTime;
            transform.forward = chaseDirection;

            if (distance <= dashTriggerDistance)
            {
                state = FastEnemyState.Dash;
                stateTimer = dashDuration;
                dashDirection = chaseDirection;
                orbitDirection = Random.value < 0.5f ? -1 : 1;
            }
        }

        void TickDash()
        {
            stateTimer -= Time.deltaTime;
            transform.position += dashDirection * moveSpeed * dashSpeedMultiplier * Time.deltaTime;
            transform.forward = dashDirection;

            if (stateTimer <= 0f)
            {
                state = FastEnemyState.Orbit;
                stateTimer = orbitDuration;
            }
        }

        void TickOrbit()
        {
            stateTimer -= Time.deltaTime;

            Vector3 toPlayer = Flatten(transform.position - playerTarget.position);
            if (toPlayer.sqrMagnitude <= 0.001f)
            {
                toPlayer = transform.right;
            }

            Vector3 radial = toPlayer.normalized;
            Vector3 desiredPosition = playerTarget.position + radial * orbitRadius;
            desiredPosition.y = transform.position.y;
            transform.position = Vector3.MoveTowards(
                transform.position,
                desiredPosition,
                moveSpeed * chaseSpeedMultiplier * Time.deltaTime
            );

            Vector3 tangent = Vector3.Cross(Vector3.up, radial) * orbitDirection;
            if (tangent.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(tangent, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    orbitAngularSpeed * Time.deltaTime
                );
            }

            if (stateTimer <= 0f)
            {
                state = FastEnemyState.Chase;
            }
        }

        static Vector3 Flatten(Vector3 value)
        {
            value.y = 0f;
            return value;
        }
    }
}
