using UnityEngine;

namespace Pithox.Player
{
    // Handles player movement using CharacterController
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 6f;
        [SerializeField] float gravity = -30f;
        [SerializeField] float groundedStickForce = -2f;

        CharacterController characterController;
        float speedMultiplier = 1f;
        float verticalVelocity;

        void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        // Moves the player in world space
        public void Move(Vector3 worldMoveDirection)
        {
            if (worldMoveDirection.sqrMagnitude > 1f)
            {
                worldMoveDirection.Normalize();
            }

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = groundedStickForce;
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
            }

            float finalSpeed = moveSpeed * speedMultiplier;
            Vector3 horizontalMove = worldMoveDirection * finalSpeed;
            Vector3 fullMove = horizontalMove + Vector3.up * verticalVelocity;
            characterController.Move(fullMove * Time.deltaTime);
        }

        // Applies movement slow or speed modifier
        public void SetSpeedMultiplier(float multiplier)
        {
            speedMultiplier = Mathf.Clamp(multiplier, 0f, 1f);
        }

        // Resets movement speed to normal
        public void ResetSpeedMultiplier()
        {
            speedMultiplier = 1f;
        }
    }
}