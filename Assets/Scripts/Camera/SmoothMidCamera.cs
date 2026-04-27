using UnityEngine;

public class SmoothMidCamera : MonoBehaviour
{
    public Transform player;
    public Transform pot;

    public float height = 18f;
    public float zOffset = -8f;
    public float xOffset = 0f;
    public float tiltDegrees = 55f;

    public float smoothTime = 0.25f;

    public float shakeDamping = 12f;

    static SmoothMidCamera instance;

    Vector3 velocity;
    Vector3 shakeOffset;
    float shakeMagnitude;
    float shakeRemaining;

    void Awake()
    {
        instance = this;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public static void Shake(float magnitude, float duration)
    {
        if (instance == null) return;
        instance.shakeMagnitude = Mathf.Max(instance.shakeMagnitude, magnitude);
        instance.shakeRemaining = Mathf.Max(instance.shakeRemaining, duration);
    }

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

        Vector3 smoothed = Vector3.SmoothDamp(
            transform.position - shakeOffset,
            targetPosition,
            ref velocity,
            smoothTime
        );

        UpdateShake();
        transform.position = smoothed + shakeOffset;
        transform.rotation = Quaternion.Euler(tiltDegrees, 0f, 0f);
    }

    void UpdateShake()
    {
        if (shakeRemaining > 0f)
        {
            shakeRemaining -= Time.deltaTime;
            Vector2 r = Random.insideUnitCircle * shakeMagnitude;
            shakeOffset = new Vector3(r.x, 0f, r.y);
            if (shakeRemaining <= 0f) shakeMagnitude = 0f;
        }
        else
        {
            shakeOffset = Vector3.Lerp(shakeOffset, Vector3.zero, Time.deltaTime * shakeDamping);
        }
    }
}
