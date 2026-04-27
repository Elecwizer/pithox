using UnityEngine;

namespace Pithox.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Transform visualRoot;
        [SerializeField] Camera mainCamera;
        [SerializeField] PlayerStats stats;

        [Header("Movement")]
        [SerializeField] float moveSpeed = 6f;
        [SerializeField] float acceleration = 28f;
        [SerializeField] float deceleration = 18f;
        [SerializeField] float turnAcceleration = 42f;
        [SerializeField] float externalDrag = 18f;
        [SerializeField] bool cameraRelativeMovement = true;
        [SerializeField] bool lockYToZero = true;

        [Header("Facing")]
        [SerializeField] float rotationSpeed = 18f;
        [SerializeField] bool faceMouse;
        [SerializeField] float mouseAimSmoothTime = 0.04f;

        [Header("Dash")]
        [SerializeField] bool canDash;
        [SerializeField] KeyCode dashKey = KeyCode.LeftShift;
        [SerializeField] float dashSpeed = 16f;
        [SerializeField] float dashDuration = 0.12f;
        [SerializeField] float dashCooldown = 0.65f;

        [Header("SFX")]
        [SerializeField] AudioSource sfxSource;
        [SerializeField] AudioClip[] footstepSfx;
        [SerializeField] AudioClip dashSfx;
        [SerializeField] float footstepVolume = 1f;
        [SerializeField] float dashVolume = 1f;
        [SerializeField] float footstepInterval = 0.32f;
        [SerializeField] float minFootstepSpeed = 0.6f;
        [SerializeField] float footstepPitchMin = 0.9f;
        [SerializeField] float footstepPitchMax = 1.12f;

        CharacterController characterController;

        Vector3 currentHorizontalVelocity;
        Vector3 externalVelocity;
        Vector3 dashVelocity;

        Vector3 lastMoveDirection = Vector3.forward;
        Vector3 smoothedMouseWorld;
        Vector3 mouseAimVelocity;

        float speedMultiplier = 1f;
        float dashTimer;
        float dashCooldownTimer;
        float attackFaceTimer;
        float footstepTimer;
        bool mouseStarted;

        public Vector3 AimDirection { get; private set; } = Vector3.forward;
        public Vector3 MoveDirection => lastMoveDirection;
        public Vector3 CurrentVelocity => currentHorizontalVelocity;
        public bool IsDashing => dashTimer > 0f;

        void Awake()
        {
            characterController = GetComponent<CharacterController>();

            if (stats == null)
                stats = GetComponent<PlayerStats>();

            if (mainCamera == null)
                mainCamera = Camera.main;

            if (visualRoot == null && transform.childCount > 0)
                visualRoot = transform.GetChild(0);

            if (sfxSource == null)
            {
                sfxSource = GetComponent<AudioSource>();

                if (sfxSource == null)
                    sfxSource = gameObject.AddComponent<AudioSource>();
            }

            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.volume = 1f;
            sfxSource.mute = false;

            smoothedMouseWorld = transform.position;
        }

        void Update()
        {
            float dt = Time.deltaTime;

            Vector2 moveInput = ReadMoveInput();
            Vector3 worldMoveDirection = ConvertInputToWorldDirection(moveInput);

            UpdateMouseAim(dt);
            UpdateDash(dt, worldMoveDirection);
            UpdateMovement(dt, worldMoveDirection);
            UpdateFacing(dt, worldMoveDirection);
            UpdateFootsteps(dt, worldMoveDirection);

            if (lockYToZero)
                LockY();
        }

        Vector2 ReadMoveInput()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            if (Input.GetKey(KeyCode.A)) horizontal -= 1f;
            if (Input.GetKey(KeyCode.D)) horizontal += 1f;
            if (Input.GetKey(KeyCode.S)) vertical -= 1f;
            if (Input.GetKey(KeyCode.W)) vertical += 1f;

            return Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
        }

        Vector3 ConvertInputToWorldDirection(Vector2 moveInput)
        {
            if (moveInput.sqrMagnitude < 0.001f)
                return Vector3.zero;

            if (!cameraRelativeMovement || mainCamera == null)
                return new Vector3(moveInput.x, 0f, moveInput.y).normalized;

            Vector3 camForward = mainCamera.transform.forward;
            Vector3 camRight = mainCamera.transform.right;

            camForward.y = 0f;
            camRight.y = 0f;

            camForward.Normalize();
            camRight.Normalize();

            return (camRight * moveInput.x + camForward * moveInput.y).normalized;
        }

        void UpdateMovement(float dt, Vector3 worldMoveDirection)
        {
            if (worldMoveDirection.sqrMagnitude > 0.001f)
                lastMoveDirection = worldMoveDirection;

            float upgradeMultiplier = stats != null ? 1f + stats.MoveSpeedBonus : 1f;
            float finalSpeed = moveSpeed * speedMultiplier * upgradeMultiplier;

            Vector3 desiredVelocity = worldMoveDirection * finalSpeed;

            bool hasInput = worldMoveDirection.sqrMagnitude > 0.001f;
            float rate = hasInput ? acceleration : deceleration;

            if (hasInput && currentHorizontalVelocity.sqrMagnitude > 0.01f)
            {
                float alignment = Vector3.Dot(currentHorizontalVelocity.normalized, worldMoveDirection);

                if (alignment < 0.25f)
                    rate = turnAcceleration;
            }

            currentHorizontalVelocity = Vector3.MoveTowards(
                currentHorizontalVelocity,
                desiredVelocity,
                rate * dt
            );

            externalVelocity = Vector3.MoveTowards(
                externalVelocity,
                Vector3.zero,
                externalDrag * dt
            );

            Vector3 finalVelocity = currentHorizontalVelocity + dashVelocity + externalVelocity;
            characterController.Move(finalVelocity * dt);
        }

        void UpdateDash(float dt, Vector3 worldMoveDirection)
        {
            if (dashCooldownTimer > 0f)
                dashCooldownTimer -= dt;

            if (dashTimer > 0f)
            {
                dashTimer -= dt;

                if (dashTimer <= 0f)
                    dashVelocity = Vector3.zero;
            }

            if (!canDash)
                return;

            if (!Input.GetKeyDown(dashKey))
                return;

            if (dashCooldownTimer > 0f || dashTimer > 0f)
                return;

            Vector3 dashDirection = worldMoveDirection.sqrMagnitude > 0.001f
                ? worldMoveDirection
                : lastMoveDirection;

            dashDirection.y = 0f;

            if (dashDirection.sqrMagnitude < 0.001f)
                return;

            dashVelocity = dashDirection.normalized * dashSpeed;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;

            PlaySfx(dashSfx, dashVolume, 1f);
        }

        void UpdateFacing(float dt, Vector3 worldMoveDirection)
        {
            if (attackFaceTimer > 0f)
                attackFaceTimer -= dt;

            bool shouldFaceMouse = faceMouse || attackFaceTimer > 0f;

            Vector3 faceDirection = Vector3.zero;

            if (shouldFaceMouse)
                faceDirection = AimDirection;
            else if (worldMoveDirection.sqrMagnitude > 0.001f)
                faceDirection = worldMoveDirection;
            else if (currentHorizontalVelocity.sqrMagnitude > 0.15f)
                faceDirection = currentHorizontalVelocity.normalized;

            RotateVisual(faceDirection, dt);
        }

        void RotateVisual(Vector3 faceDirection, float dt)
        {
            faceDirection.y = 0f;

            if (faceDirection.sqrMagnitude < 0.001f)
                return;

            Transform target = visualRoot != null ? visualRoot : transform;
            Quaternion targetRotation = Quaternion.LookRotation(faceDirection.normalized);

            target.rotation = Quaternion.Slerp(
                target.rotation,
                targetRotation,
                rotationSpeed * dt
            );
        }

        void UpdateMouseAim(float dt)
        {
            if (!TryGetMouseWorldPosition(out Vector3 mouseWorld))
                return;

            mouseWorld.y = 0f;

            if (!mouseStarted)
            {
                smoothedMouseWorld = mouseWorld;
                mouseStarted = true;
            }

            smoothedMouseWorld = Vector3.SmoothDamp(
                smoothedMouseWorld,
                mouseWorld,
                ref mouseAimVelocity,
                mouseAimSmoothTime
            );

            Vector3 direction = smoothedMouseWorld - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
                AimDirection = direction.normalized;
        }

        void UpdateFootsteps(float dt, Vector3 worldMoveDirection)
        {
            if (IsDashing)
                return;

            bool hasInput = worldMoveDirection.sqrMagnitude > 0.001f;
            float speed = currentHorizontalVelocity.magnitude;

            if (!hasInput || speed < minFootstepSpeed)
            {
                footstepTimer = 0.05f;
                return;
            }

            footstepTimer -= dt;

            if (footstepTimer > 0f)
                return;

            float speedFactor = Mathf.Clamp(speed / Mathf.Max(0.01f, moveSpeed), 0.75f, 1.35f);
            footstepTimer = footstepInterval / speedFactor;

            PlayRandomFootstep();
        }

        void PlayRandomFootstep()
        {
            if (footstepSfx == null || footstepSfx.Length == 0)
                return;

            AudioClip clip = footstepSfx[Random.Range(0, footstepSfx.Length)];

            if (clip == null)
                return;

            float pitch = Random.Range(footstepPitchMin, footstepPitchMax);
            PlaySfx(clip, footstepVolume, pitch);
        }

        void PlaySfx(AudioClip clip, float volume, float pitch)
        {
            if (clip == null || sfxSource == null)
                return;

            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(clip, volume);
            sfxSource.pitch = 1f;
        }

        bool TryGetMouseWorldPosition(out Vector3 position)
        {
            position = transform.position;

            if (mainCamera == null)
                return false;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

            if (!groundPlane.Raycast(ray, out float distance))
                return false;

            position = ray.GetPoint(distance);
            return true;
        }

        void LockY()
        {
            Vector3 pos = transform.position;
            pos.y = 0f;
            transform.position = pos;
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            speedMultiplier = Mathf.Clamp(multiplier, 0f, 2f);
        }

        public void ResetSpeedMultiplier()
        {
            speedMultiplier = 1f;
        }

        public void SetDashUnlocked(bool unlocked)
        {
            canDash = unlocked;
        }

        public void SetFaceMouse(bool active)
        {
            faceMouse = active;
        }

        public void FaceMouseFor(float seconds)
        {
            attackFaceTimer = Mathf.Max(attackFaceTimer, seconds);
        }

        public void AddImpulse(Vector3 direction, float strength)
        {
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
                return;

            externalVelocity += direction.normalized * strength;
        }

        public Vector3 GetForwardDirection()
        {
            Transform target = visualRoot != null ? visualRoot : transform;
            return target.forward;
        }
    }
}