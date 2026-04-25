using UnityEngine;

namespace Pithox.Player
{
    // Handles player movement using CharacterController
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 6f;

        CharacterController characterController;
        float speedMultiplier = 1f;

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

            float finalSpeed = moveSpeed * speedMultiplier;
            characterController.Move(worldMoveDirection * finalSpeed * Time.deltaTime);
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