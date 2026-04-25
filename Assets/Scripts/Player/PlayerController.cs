using UnityEngine;

namespace Pithox.Player
{
    // Handles player input and passes it to movement and facing systems
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(PlayerFacing))]
    public class PlayerController : MonoBehaviour
    {
        PlayerMovement playerMovement;
        PlayerFacing playerFacing;

        void Awake()
        {
            playerMovement = GetComponent<PlayerMovement>();
            playerFacing = GetComponent<PlayerFacing>();
        }

        void Update()
        {
            Vector2 moveInput = ReadMoveInput();
            Vector3 worldMoveDirection = ConvertInputToWorldDirection(moveInput);

            playerMovement.Move(worldMoveDirection);

            if (worldMoveDirection.sqrMagnitude > 0.001f)
            {
                playerFacing.FaceDirection(worldMoveDirection);
            }
        }

        // Reads WASD input
        Vector2 ReadMoveInput()
        {
            float horizontal = 0f;
            float vertical = 0f;

            if (Input.GetKey(KeyCode.W)) vertical += 1f;
            if (Input.GetKey(KeyCode.S)) vertical -= 1f;
            if (Input.GetKey(KeyCode.D)) horizontal += 1f;
            if (Input.GetKey(KeyCode.A)) horizontal -= 1f;

            return new Vector2(horizontal, vertical);
        }

        // Converts 2D input into world space direction
        Vector3 ConvertInputToWorldDirection(Vector2 moveInput)
        {
            return new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        }
    }
}