using UnityEngine;

namespace Pithox.Player
{
    public class PlayerFacing : MonoBehaviour
    {
        [SerializeField] float rotationSpeed = 15f;

        public void FaceDirection(Vector3 worldFaceDirection)
        {
            worldFaceDirection.y = 0f;

            if (worldFaceDirection.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(worldFaceDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        public Vector3 GetForwardDirection()
        {
            return transform.forward;
        }
    }
}