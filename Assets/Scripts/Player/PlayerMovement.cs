using UnityEngine;

namespace Pithox.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 6f;
        [SerializeField] float gravity = -30f;
        [SerializeField] float groundedStickForce = -2f;
        [SerializeField] float acceleration = 60f;
        [SerializeField] float deceleration = 80f;
        [SerializeField] PlayerStats stats;

        CharacterController characterController;
        Vector3 currentHorizontalVelocity;
        float speedMultiplier = 1f;
        float verticalVelocity;

        void Awake()
        {
            characterController = GetComponent<CharacterController>();

            if (stats == null)
                stats = GetComponent<PlayerStats>();
        }

        public void Move(Vector3 worldMoveDirection)
        {
            if (worldMoveDirection.sqrMagnitude > 1f)
                worldMoveDirection.Normalize();

            if (characterController.isGrounded && verticalVelocity < 0f)
                verticalVelocity = groundedStickForce;
            else
                verticalVelocity += gravity * Time.deltaTime;

            float upgradeMultiplier = stats != null ? 1f + stats.MoveSpeedBonus : 1f;
            float finalSpeed = moveSpeed * speedMultiplier * upgradeMultiplier;

            Vector3 desiredVelocity = worldMoveDirection * finalSpeed;
            float rate = worldMoveDirection.sqrMagnitude > 0.001f ? acceleration : deceleration;
            currentHorizontalVelocity = Vector3.MoveTowards(currentHorizontalVelocity, desiredVelocity, rate * Time.deltaTime);

            Vector3 fullMove = currentHorizontalVelocity + Vector3.up * verticalVelocity;
            characterController.Move(fullMove * Time.deltaTime);
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            speedMultiplier = Mathf.Clamp(multiplier, 0f, 1f);
        }

        public void ResetSpeedMultiplier()
        {
            speedMultiplier = 1f;
        }
    }
}
