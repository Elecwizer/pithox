using UnityEngine;
using Pithox.Player;

public class SmoothMidCamera : MonoBehaviour
{
    public Transform player;
    public Transform pot;

    public float height = 18f;
    public float zOffset = -8f;
    public float xOffset = 0f;
    public float tiltDegrees = 55f;

    public float smoothTime = 0.25f;

    [Header("Focus Weights")]
    public float playerWeight = 1f;
    public float centerWeight = 1f;
    public bool usePotAsCenterPoint = false;

    [Header("Boss Focus")]
    public string bossTag = "boss";
    public float bossWeight = 1f;
    public float bossCheckInterval = 0.35f;

    [Header("Mouse / Controller Influence")]
    public bool useAimInfluence = true;
    public float aimWeight = 0.5f;
    public float maxAimDistance = 7f;
    public float aimSmoothTime = 0.12f;
    public float controllerDeadzone = 0.25f;

    [Header("Legacy Axis Names")]
    public string rightStickHorizontalAxis = "RightStickHorizontal";
    public string rightStickVerticalAxis = "RightStickVertical";

    public float shakeDamping = 12f;

    static SmoothMidCamera instance;

    Camera cam;
    PlayerController playerController;
    Transform boss;

    Vector3 velocity;
    Vector3 shakeOffset;
    Vector3 smoothedAimPoint;
    Vector3 aimVelocity;

    float shakeMagnitude;
    float shakeRemaining;
    float nextBossCheckTime;

    bool aimStarted;

    void Awake()
    {
        instance = this;
        cam = GetComponent<Camera>();

        if (player != null)
            playerController = player.GetComponent<PlayerController>();
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

        if (playerController == null)
            playerController = player.GetComponent<PlayerController>();

        PlayerInputRouter.UpdateInputType();
        UpdateBossReference();

        Vector3 focusPoint = GetFocusPoint();

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

    Vector3 GetFocusPoint()
    {
        Vector3 playerPoint = Flat(player.position);
        Vector3 centerPoint = Vector3.zero;

        if (usePotAsCenterPoint && pot != null)
            centerPoint = Flat(pot.position);

        Vector3 total = playerPoint * playerWeight + centerPoint * centerWeight;
        float totalWeight = playerWeight + centerWeight;

        if (boss != null && boss.gameObject.activeInHierarchy)
        {
            Vector3 bossPoint = Flat(boss.position);
            total += bossPoint * bossWeight;
            totalWeight += bossWeight;
        }

        if (useAimInfluence && TryGetAimPoint(playerPoint, out Vector3 aimPoint))
        {
            total += aimPoint * aimWeight;
            totalWeight += aimWeight;
        }

        return total / Mathf.Max(0.001f, totalWeight);
    }

    bool TryGetAimPoint(Vector3 playerPoint, out Vector3 point)
    {
        point = Vector3.zero;

        if (PlayerInputRouter.UsingController)
        {
            Vector2 stick = PlayerInputRouter.GetAimStick();
            Vector3 direction = Vector3.zero;

            if (stick.magnitude > controllerDeadzone)
                direction = new Vector3(stick.x, 0f, stick.y);
            else if (playerController != null)
                direction = playerController.AimDirection;

            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
                return false;

            Vector3 rawPoint = playerPoint + direction.normalized * maxAimDistance;
            point = SmoothAimPoint(rawPoint);
            return true;
        }

        if (!TryGetMouseWorldPoint(out Vector3 mousePoint))
            return false;

        mousePoint = Flat(mousePoint);

        Vector3 fromPlayer = mousePoint - playerPoint;

        if (fromPlayer.magnitude > maxAimDistance)
            mousePoint = playerPoint + fromPlayer.normalized * maxAimDistance;

        point = SmoothAimPoint(mousePoint);
        return true;
    }

    Vector3 SmoothAimPoint(Vector3 rawPoint)
    {
        if (!aimStarted)
        {
            smoothedAimPoint = rawPoint;
            aimStarted = true;
        }

        smoothedAimPoint = Vector3.SmoothDamp(
            smoothedAimPoint,
            rawPoint,
            ref aimVelocity,
            aimSmoothTime
        );

        return smoothedAimPoint;
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

    void UpdateBossReference()
    {
        if (Time.time < nextBossCheckTime)
            return;

        nextBossCheckTime = Time.time + bossCheckInterval;
        boss = null;

        if (string.IsNullOrEmpty(bossTag))
            return;

        try
        {
            GameObject[] bosses = GameObject.FindGameObjectsWithTag(bossTag);

            float bestDistance = float.MaxValue;

            for (int i = 0; i < bosses.Length; i++)
            {
                if (bosses[i] == null || !bosses[i].activeInHierarchy)
                    continue;

                float distance = (bosses[i].transform.position - player.position).sqrMagnitude;

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    boss = bosses[i].transform;
                }
            }
        }
        catch (UnityException)
        {
            boss = null;
        }
    }

    Vector3 Flat(Vector3 value)
    {
        value.y = 0f;
        return value;
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