using UnityEngine;

public class SmoothMidCamera : MonoBehaviour
{
    public Transform player;
    public Transform pot;

    public float height = 22f;
    public float zOffset = -10f;
    public float xOffset = 0f;

    public float smoothTime = 0.25f;

    private Vector3 velocity;

    void LateUpdate()
    {
        if (player == null || pot == null)
            return;

        Vector3 middlePoint = (player.position + pot.position) / 2f;

        Vector3 targetPosition = new Vector3(
            middlePoint.x + xOffset,
            height,
            middlePoint.z + zOffset
        );

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            smoothTime
        );

        transform.rotation = Quaternion.Euler(65f, 0f, 0f);
    }
}