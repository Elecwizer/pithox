using UnityEngine;

namespace Pithox.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 6f;

        CharacterController characterController;

        void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        public void Move(Vector3 worldMoveDirection)
        {
            if (worldMoveDirection.sqrMagnitude > 1f)
            {
                worldMoveDirection.Normalize();
            }

            characterController.Move(worldMoveDirection * moveSpeed * Time.deltaTime);
        }

        public float GetMoveSpeed()
        {
            return moveSpeed;
        }
    }
}