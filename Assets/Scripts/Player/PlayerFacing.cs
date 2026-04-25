using UnityEngine;

namespace Pithox.Player
{
    // Rotates player toward movement direction
    public class PlayerFacing : MonoBehaviour
    {
        [SerializeField] float rotationSpeed = 15f;

        // Rotates player to face a direction
        public void FaceDirection(Vector3 worldFaceDirection)
        {
            worldFaceDirection.y = 0f;

            if (worldFaceDirection.sqrMagnitude < 0.0001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(worldFaceDirection);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        // Returns forward direction of player
        public Vector3 GetForwardDirection()
        {
            return transform.forward;
        }
    }
}