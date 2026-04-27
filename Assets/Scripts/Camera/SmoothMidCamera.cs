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

    [Header("Mouse Influence")]
    public bool useMouseInfluence = true;
    public float playerWeight = 1f;
    public float potWeight = 1f;
    public float mouseWeight = 0.5f;
    public float maxMouseDistance = 7f;
    public float mouseSmoothTime = 0.12f;

    public float shakeDamping = 12f;

    static SmoothMidCamera instance;

    Camera cam;
    Vector3 velocity;
    Vector3 shakeOffset;
    Vector3 smoothedMousePoint;
    Vector3 mouseVelocity;

    float shakeMagnitude;
    float shakeRemaining;
    bool mouseStarted;

    void Awake()
    {
        instance = this;
        cam = GetComponent<Camera>();
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public static void Shake(float magnitude, float duration)
    {
        if (instance == null)
            return;

        instance.shakeMagnitude = Mathf.Max(instance.shakeMagnitude, magnitude);
        instance.shakeRemaining = Mathf.Max(instance.shakeRemaining, duration);
    }

    void LateUpdate()
    {
        if (player == null)
            return;

        Vector3 playerPoint = player.position;
        Vector3 potPoint = pot != null ? pot.position : Vector3.zero;

        playerPoint.y = 0f;
        potPoint.y = 0f;

        Vector3 focusPoint = GetFocusPoint(playerPoint, potPoint);

        Vector3 targetPosition = new Vector3(
            focusPoint.x + xOffset,
            height,
            focusPoint.z + zOffset
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

    Vector3 GetFocusPoint(Vector3 playerPoint, Vector3 potPoint)
    {
        float totalWeight = playerWeight + potWeight;
        Vector3 total = playerPoint * playerWeight + potPoint * potWeight;

        if (useMouseInfluence && TryGetMouseWorldPoint(out Vector3 mousePoint))
        {
            mousePoint.y = 0f;

            Vector3 fromPlayer = mousePoint - playerPoint;

            if (fromPlayer.magnitude > maxMouseDistance)
                mousePoint = playerPoint + fromPlayer.normalized * maxMouseDistance;

            if (!mouseStarted)
            {
                smoothedMousePoint = mousePoint;
                mouseStarted = true;
            }

            smoothedMousePoint = Vector3.SmoothDamp(
                smoothedMousePoint,
                mousePoint,
                ref mouseVelocity,
                mouseSmoothTime
            );

            total += smoothedMousePoint * mouseWeight;
            totalWeight += mouseWeight;
        }

        return total / Mathf.Max(0.001f, totalWeight);
    }

    bool TryGetMouseWorldPoint(out Vector3 point)
    {
        point = Vector3.zero;

        if (cam == null)
            cam = GetComponent<Camera>();

        if (cam == null)
            return false;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (!groundPlane.Raycast(ray, out float distance))
            return false;

        point = ray.GetPoint(distance);
        return true;
    }

    void UpdateShake()
    {
        if (shakeRemaining > 0f)
        {
            shakeRemaining -= Time.deltaTime;

            Vector2 random = Random.insideUnitCircle * shakeMagnitude;
            shakeOffset = new Vector3(random.x, 0f, random.y);

            if (shakeRemaining <= 0f)
                shakeMagnitude = 0f;
        }
        else
        {
            shakeOffset = Vector3.Lerp(
                shakeOffset,
                Vector3.zero,
                Time.deltaTime * shakeDamping
            );
        }
    }
}