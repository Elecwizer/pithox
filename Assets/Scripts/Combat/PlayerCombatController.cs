using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Pithox.Player;

namespace Pithox.Combat
{
    public class PlayerCombatController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] PlayerController playerController;
        [SerializeField] PlayerTombCarry tombCarry;
        [SerializeField] Transform attackPoint;
        [SerializeField] AudioSource sfxSource;
        [SerializeField] Animator animator;
        [SerializeField] LayerMask enemyMask = ~0;

        [Header("Input")]
        [SerializeField] int lightAttackMouseButton = 0;
        [SerializeField] int heavyAttackMouseButton = 1;
        [SerializeField] bool canSlashWhileCarrying = false;

        [Header("Attack Visual")]
        [SerializeField] AttackArcPreview attackArcVisual;
        [SerializeField] bool showAttackArc = true;
        [SerializeField] float attackArcShowTime = 0.12f;
        [SerializeField] Color lightArcColor = new Color(1f, 0.75f, 0.1f, 0.8f);
        [SerializeField] Color heavyArcColor = new Color(1f, 0.2f, 0.1f, 0.85f);

        [Header("Slash VFX")]
        [SerializeField] ParticleSystem lightSlashVfx;
        [SerializeField] ParticleSystem heavySlashVfx;
        [SerializeField] bool playSlashVfxOnHitRegister = true;

        [Header("General MUL")]
        [SerializeField] float generalDamageMUL = 1f;
        [SerializeField] float slashDamageMUL = 1f;
        [SerializeField] float slashCooldownMUL = 1f;
        [SerializeField] float slashRangeMUL = 1f;
        [SerializeField] float slashEnemyKnockbackMUL = 1f;
        [SerializeField] float slashPlayerRecoilMUL = 1f;
        [SerializeField] float slashHitDelayMUL = 1f;

        [Header("Light Slash Base")]
        [SerializeField] float baseLightDamage = 8f;
        [SerializeField] float baseLightRange = 2f;
        [SerializeField] float baseLightArcDegrees = 90f;
        [SerializeField] float baseLightCooldown = 0.6f;
        [SerializeField] float baseLightEnemyKnockback = 0.45f;
        [SerializeField] float baseLightPlayerRecoil = 4f;
        [SerializeField] float baseLightHitDelay = 0.2f;

        [Header("Light Slash MUL")]
        [SerializeField] float lightDamageMUL = 1f;
        [SerializeField] float lightRangeMUL = 1f;
        [SerializeField] float lightCooldownMUL = 1f;
        [SerializeField] float lightEnemyKnockbackMUL = 1f;
        [SerializeField] float lightPlayerRecoilMUL = 1f;
        [SerializeField] float lightHitDelayMUL = 1f;

        [Header("Heavy Slash Base")]
        [SerializeField] float baseHeavyDamage = 16f;
        [SerializeField] float baseHeavyRange = 3f;
        [SerializeField] float baseHeavyArcDegrees = 120f;
        [SerializeField] float baseHeavyCooldown = 1.5f;
        [SerializeField] float baseHeavyEnemyKnockback = 1.1f;
        [SerializeField] float baseHeavyPlayerRecoil = 7f;
        [SerializeField] float baseHeavyHitDelay = 0.35f;

        [Header("Heavy Slash MUL")]
        [SerializeField] float heavyDamageMUL = 1f;
        [SerializeField] float heavyRangeMUL = 1f;
        [SerializeField] float heavyCooldownMUL = 1f;
        [SerializeField] float heavyEnemyKnockbackMUL = 1f;
        [SerializeField] float heavyPlayerRecoilMUL = 1f;
        [SerializeField] float heavyHitDelayMUL = 1f;

        [Header("Attack Feel")]
        [SerializeField] float faceMouseTime = 0.16f;
        [SerializeField] bool recoilOnlyOnHit = true;
        [SerializeField] bool playSlashSfxOnHitRegister = true;

        [Header("Light SFX")]
        [SerializeField] AudioClip[] lightSlashSfx;
        [SerializeField] float lightSlashVolume = 1f;

        [Header("Heavy SFX")]
        [SerializeField] AudioClip[] heavySlashSfx;
        [SerializeField] float heavySlashVolume = 1.2f;

        [Header("SFX Pitch")]
        [SerializeField] float pitchMin = 0.92f;
        [SerializeField] float pitchMax = 1.1f;

        float slashCooldownTimer;
        bool slashIsPending;
        float pendingSlashRange;
        readonly HashSet<Component> hitTargets = new();

        void Awake()
        {
            if (playerController == null)
                playerController = GetComponent<PlayerController>();

            if (tombCarry == null)
                tombCarry = GetComponent<PlayerTombCarry>();

            if (attackPoint == null)
                attackPoint = transform;

            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            if (sfxSource == null)
            {
                sfxSource = GetComponent<AudioSource>();

                if (sfxSource == null)
                    sfxSource = gameObject.AddComponent<AudioSource>();
            }

            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.dopplerLevel = 0f;
            sfxSource.volume = 1f;
            sfxSource.mute = false;
        }

        void Update()
        {
            if (slashCooldownTimer > 0f)
                slashCooldownTimer -= Time.deltaTime;

            if (Input.GetMouseButtonDown(lightAttackMouseButton))
                TryLightSlash();

            if (Input.GetMouseButtonDown(heavyAttackMouseButton))
                TryHeavySlash();
        }

        void TryLightSlash()
        {
            if (!CanSlash())
                return;

            if (animator != null)
                animator.SetTrigger("LightAttack");

            float damage = baseLightDamage * generalDamageMUL * slashDamageMUL * lightDamageMUL;
            float range = baseLightRange * slashRangeMUL * lightRangeMUL;
            float cooldown = baseLightCooldown * slashCooldownMUL * lightCooldownMUL;
            float enemyKnockback = baseLightEnemyKnockback * slashEnemyKnockbackMUL * lightEnemyKnockbackMUL;
            float playerRecoil = baseLightPlayerRecoil * slashPlayerRecoilMUL * lightPlayerRecoilMUL;
            float hitDelay = baseLightHitDelay * slashHitDelayMUL * lightHitDelayMUL;

            DoSlash(
                damage,
                range,
                baseLightArcDegrees,
                enemyKnockback,
                playerRecoil,
                cooldown,
                hitDelay,
                lightSlashSfx,
                lightSlashVolume,
                lightSlashVfx,
                lightArcColor
            );
        }

        void TryHeavySlash()
        {
            if (!CanSlash())
                return;

            if (animator != null)
                animator.SetTrigger("HeavyAttack");

            float damage = baseHeavyDamage * generalDamageMUL * slashDamageMUL * heavyDamageMUL;
            float range = baseHeavyRange * slashRangeMUL * heavyRangeMUL;
            float cooldown = baseHeavyCooldown * slashCooldownMUL * heavyCooldownMUL;
            float enemyKnockback = baseHeavyEnemyKnockback * slashEnemyKnockbackMUL * heavyEnemyKnockbackMUL;
            float playerRecoil = baseHeavyPlayerRecoil * slashPlayerRecoilMUL * heavyPlayerRecoilMUL;
            float hitDelay = baseHeavyHitDelay * slashHitDelayMUL * heavyHitDelayMUL;

            DoSlash(
                damage,
                range,
                baseHeavyArcDegrees,
                enemyKnockback,
                playerRecoil,
                cooldown,
                hitDelay,
                heavySlashSfx,
                heavySlashVolume,
                heavySlashVfx,
                heavyArcColor
            );
        }

        bool CanSlash()
        {
            if (slashIsPending)
                return false;

            if (slashCooldownTimer > 0f)
                return false;

            if (!canSlashWhileCarrying && tombCarry != null && tombCarry.IsCarrying)
                return false;

            return true;
        }

        void DoSlash(
            float damage,
            float range,
            float arcDegrees,
            float enemyKnockback,
            float playerRecoil,
            float cooldown,
            float hitDelay,
            AudioClip[] sfxClips,
            float sfxVolume,
            ParticleSystem slashVfx,
            Color arcColor
        )
        {
            slashCooldownTimer = Mathf.Max(0.01f, cooldown);
            slashIsPending = true;
            pendingSlashRange = range;

            if (playerController != null)
                playerController.FaceMouseFor(Mathf.Max(faceMouseTime, hitDelay + 0.05f));

            if (!playSlashSfxOnHitRegister)
                PlayRandomSfx(sfxClips, sfxVolume);

            if (!playSlashVfxOnHitRegister)
                PlayVfx(slashVfx);

            StartCoroutine(DelayedSlashHit(
                damage,
                range,
                arcDegrees,
                enemyKnockback,
                playerRecoil,
                Mathf.Max(0f, hitDelay),
                sfxClips,
                sfxVolume,
                slashVfx,
                arcColor
            ));
        }

        IEnumerator DelayedSlashHit(
            float damage,
            float range,
            float arcDegrees,
            float enemyKnockback,
            float playerRecoil,
            float hitDelay,
            AudioClip[] sfxClips,
            float sfxVolume,
            ParticleSystem slashVfx,
            Color arcColor
        )
        {
            if (hitDelay > 0f)
                yield return new WaitForSeconds(hitDelay);

            Vector3 slashDirection = GetSlashDirection();

            Vector3 origin = attackPoint != null ? attackPoint.position : transform.position;
            origin.y = 0f;

            if (showAttackArc && attackArcVisual != null)
                attackArcVisual.Show(origin, slashDirection, range, arcDegrees, attackArcShowTime, arcColor);

            if (playSlashSfxOnHitRegister)
                PlayRandomSfx(sfxClips, sfxVolume);

            if (playSlashVfxOnHitRegister)
                PlayVfx(slashVfx);

            int hitCount = HitEnemiesInArc(slashDirection, damage, range, arcDegrees, enemyKnockback);

            if (!recoilOnlyOnHit || hitCount > 0)
            {
                if (playerController != null && playerRecoil > 0f)
                    playerController.AddImpulse(-slashDirection, playerRecoil);
            }

            slashIsPending = false;
        }

        int HitEnemiesInArc(Vector3 slashDirection, float damage, float range, float arcDegrees, float enemyKnockback)
        {
            Vector3 origin = attackPoint != null ? attackPoint.position : transform.position;
            origin.y = 0f;

            slashDirection.y = 0f;

            if (slashDirection.sqrMagnitude < 0.001f)
                slashDirection = transform.forward;

            slashDirection.Normalize();

            hitTargets.Clear();

            Collider[] hits = Physics.OverlapSphere(
                origin,
                range,
                enemyMask,
                QueryTriggerInteraction.Collide
            );

            int hitCount = 0;

            foreach (Collider hit in hits)
            {
                if (hit == null)
                    continue;

                IDamageable damageable = hit.GetComponentInParent<IDamageable>();

                if (damageable == null)
                    continue;

                Component damageComponent = damageable as Component;

                if (damageComponent == null)
                    continue;

                if (damageComponent.gameObject == gameObject)
                    continue;

                if (!hitTargets.Add(damageComponent))
                    continue;

                Vector3 closestPoint = hit.ClosestPoint(origin);
                closestPoint.y = 0f;

                Vector3 toTarget = closestPoint - origin;

                if (toTarget.sqrMagnitude < 0.001f)
                {
                    toTarget = damageComponent.transform.position - origin;
                    toTarget.y = 0f;
                }

                if (toTarget.sqrMagnitude < 0.001f)
                {
                    Vector3 boundsCenter = hit.bounds.center;
                    boundsCenter.y = 0f;
                    toTarget = boundsCenter - origin;
                }

                if (toTarget.sqrMagnitude < 0.001f)
                    continue;

                Vector3 hitDirection = toTarget.normalized;
                float angle = Vector3.Angle(slashDirection, hitDirection);

                if (angle > arcDegrees * 0.5f)
                    continue;

                Vector3 hitPoint = closestPoint;
                hitPoint.y = damageComponent.transform.position.y;

                damageable.TakeDamage(new DamageData(
                    damage,
                    gameObject,
                    hitPoint,
                    hitDirection,
                    0
                ));

                ApplyExtraEnemyKnockback(damageComponent, hitDirection, enemyKnockback);

                hitCount++;
            }

            return hitCount;
        }

        void ApplyExtraEnemyKnockback(Component target, Vector3 direction, float distance)
        {
            if (target == null || distance <= 0f)
                return;

            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
                return;

            direction.Normalize();

            NavMeshAgent agent = target.GetComponentInParent<NavMeshAgent>();

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.Move(direction * distance);
                return;
            }

            CharacterController controller = target.GetComponentInParent<CharacterController>();

            if (controller != null && controller.enabled)
            {
                controller.Move(direction * distance);
                return;
            }

            target.transform.position += direction * distance;
        }

        Vector3 GetSlashDirection()
        {
            Vector3 direction = Vector3.zero;

            if (playerController != null)
                direction = playerController.AimDirection;

            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
            {
                direction = transform.forward;
                direction.y = 0f;
            }

            if (direction.sqrMagnitude < 0.001f)
                direction = Vector3.forward;

            return direction.normalized;
        }

        void PlayRandomSfx(AudioClip[] clips, float volume)
        {
            if (clips == null || clips.Length == 0 || sfxSource == null)
                return;

            AudioClip clip = clips[Random.Range(0, clips.Length)];

            if (clip == null)
                return;

            sfxSource.pitch = Random.Range(pitchMin, pitchMax);
            sfxSource.PlayOneShot(clip, volume);
            sfxSource.pitch = 1f;
        }

        void PlayVfx(ParticleSystem vfx)
        {
            if (vfx == null)
                return;

            vfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            vfx.Play(true);
        }

        public void SetGeneralDamageMUL(float value)
        {
            generalDamageMUL = Mathf.Max(0f, value);
        }

        public void SetSlashDamageMUL(float value)
        {
            slashDamageMUL = Mathf.Max(0f, value);
        }

        public void SetSlashCooldownMUL(float value)
        {
            slashCooldownMUL = Mathf.Max(0.01f, value);
        }

        public void SetSlashRangeMUL(float value)
        {
            slashRangeMUL = Mathf.Max(0f, value);
        }

        public void SetSlashEnemyKnockbackMUL(float value)
        {
            slashEnemyKnockbackMUL = Mathf.Max(0f, value);
        }

        public void SetSlashPlayerRecoilMUL(float value)
        {
            slashPlayerRecoilMUL = Mathf.Max(0f, value);
        }

        public void SetSlashHitDelayMUL(float value)
        {
            slashHitDelayMUL = Mathf.Max(0f, value);
        }

        /// <summary>True from slash input until the delayed hit frame has finished (windup / active swing).</summary>
        public bool IsSlashWindupPending => slashIsPending;

        /// <summary>Range of the slash currently in flight (0 if none).</summary>
        public float PendingSlashRange => slashIsPending ? pendingSlashRange : 0f;

        /// <summary>World XZ aim direction for the pending slash. Zero if not in a slash.</summary>
        public Vector3 GetPendingSlashFlatDirection()
        {
            if (!slashIsPending)
                return Vector3.zero;

            return GetSlashDirection();
        }

        void OnDrawGizmosSelected()
        {
            Transform point = attackPoint != null ? attackPoint : transform;

            Gizmos.color = new Color(1f, 0.8f, 0.1f, 0.25f);
            Gizmos.DrawWireSphere(point.position, baseLightRange * slashRangeMUL * lightRangeMUL);

            Gizmos.color = new Color(1f, 0.2f, 0.1f, 0.25f);
            Gizmos.DrawWireSphere(point.position, baseHeavyRange * slashRangeMUL * heavyRangeMUL);
        }
    }
}