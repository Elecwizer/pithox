using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pithox.Combat;
using Pithox.Player;

public class CrystalActiveAbility : MonoBehaviour, IPlayerActiveAbility
{
    [System.Serializable]
    public class CrystalWave
    {
        public string name = "Wave";
        public float delayBeforeWave = 0f;
        public float minDistance = 0f;
        public float maxDistance = 2f;
        public float arcDegrees = 70f;
        public float damage = 10f;

        [Header("SFX")]
        public AudioClip waveSfx;
        public float waveSfxVolume = 1f;
    }

    [Header("General")]
    [SerializeField] float cooldown = 5f;
    [SerializeField] LayerMask enemyMask = ~0;
    [SerializeField] Transform castOrigin;
    [SerializeField] bool usePlayerAimDirection = true;

    [Header("Single Cast VFX")]
    [SerializeField] GameObject crystalVfxPrefab;
    [SerializeField] float vfxSpawnDistance = 0f;
    [SerializeField] float vfxYOffset = 0.05f;
    [SerializeField] float vfxLifetime = 2f;
    [SerializeField] bool vfxForwardIsLocalX = true;
    [SerializeField] Vector3 vfxRotationOffset;

    [Header("SFX")]
    [SerializeField] AudioSource sfxSource;
    [SerializeField] float sfxPitchMin = 0.95f;
    [SerializeField] float sfxPitchMax = 1.05f;

    [Header("Waves")]
    [SerializeField]
    CrystalWave[] waves =
    {
        new CrystalWave { name = "Close", delayBeforeWave = 0f, minDistance = 0f, maxDistance = 2f, arcDegrees = 70f, damage = 10f },
        new CrystalWave { name = "Middle", delayBeforeWave = 0.12f, minDistance = 2f, maxDistance = 4f, arcDegrees = 70f, damage = 14f },
        new CrystalWave { name = "Far", delayBeforeWave = 0.12f, minDistance = 4f, maxDistance = 6f, arcDegrees = 70f, damage = 18f }
    };

    GameObject player;
    PlayerController playerController;
    float cooldownTimer;
    bool casting;

    readonly HashSet<Component> hitTargets = new HashSet<Component>();

    public void OnEquip(GameObject playerObject)
    {
        player = playerObject;

        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();

            if (sfxSource == null)
                sfxSource = player.GetComponent<AudioSource>();
        }

        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();

        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
        sfxSource.volume = 1f;
        sfxSource.mute = false;
    }

    public void OnUnequip(GameObject playerObject)
    {
        StopAllCoroutines();
        casting = false;
        cooldownTimer = 0f;
        hitTargets.Clear();
    }

    public void Use(GameObject playerObject)
    {
        if (casting)
            return;

        if (cooldownTimer > 0f)
            return;

        if (player == null)
            OnEquip(playerObject);

        StartCoroutine(CastCrystalAttack());
    }

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    IEnumerator CastCrystalAttack()
    {
        casting = true;
        cooldownTimer = cooldown;
        hitTargets.Clear();

        Vector3 origin = GetOrigin();
        Vector3 direction = GetDirection();

        SpawnCastVfx(origin, direction);

        for (int i = 0; i < waves.Length; i++)
        {
            CrystalWave wave = waves[i];

            if (wave == null)
                continue;

            if (wave.delayBeforeWave > 0f)
                yield return new WaitForSeconds(wave.delayBeforeWave);

            PlayWaveSfx(wave);
            DamageWave(wave, origin, direction);
        }

        casting = false;
    }

    Vector3 GetOrigin()
    {
        if (castOrigin != null)
            return castOrigin.position;

        if (player != null)
            return player.transform.position;

        return transform.position;
    }

    Vector3 GetDirection()
    {
        Vector3 direction = Vector3.forward;

        if (usePlayerAimDirection && playerController != null)
            direction = playerController.AimDirection;
        else if (player != null)
            direction = player.transform.forward;
        else
            direction = transform.forward;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            direction = Vector3.forward;

        return direction.normalized;
    }

    void SpawnCastVfx(Vector3 origin, Vector3 direction)
    {
        if (crystalVfxPrefab == null)
            return;

        Vector3 spawnPosition = origin + direction * vfxSpawnDistance;
        spawnPosition.y += vfxYOffset;

        Quaternion rotation;

        if (vfxForwardIsLocalX)
            rotation = Quaternion.FromToRotation(Vector3.right, direction);
        else
            rotation = Quaternion.LookRotation(direction, Vector3.up);

        rotation *= Quaternion.Euler(vfxRotationOffset);

        GameObject vfx = Instantiate(crystalVfxPrefab, spawnPosition, rotation);

        if (vfxLifetime > 0f)
            Destroy(vfx, vfxLifetime);
    }

    void DamageWave(CrystalWave wave, Vector3 origin, Vector3 direction)
    {
        Vector3 flatOrigin = origin;
        flatOrigin.y = 0f;

        Collider[] hits = Physics.OverlapSphere(
            origin,
            wave.maxDistance,
            enemyMask,
            QueryTriggerInteraction.Collide
        );

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];

            if (hit == null)
                continue;

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();

            if (damageable == null)
                continue;

            Component targetComponent = damageable as Component;

            if (targetComponent == null)
                continue;

            if (player != null && targetComponent.gameObject == player)
                continue;

            if (hitTargets.Contains(targetComponent))
                continue;

            Vector3 closestPoint = hit.ClosestPoint(origin);
            Vector3 flatPoint = closestPoint;
            flatPoint.y = 0f;

            Vector3 toTarget = flatPoint - flatOrigin;
            float distance = toTarget.magnitude;

            if (distance < wave.minDistance)
                continue;

            if (distance > wave.maxDistance)
                continue;

            if (toTarget.sqrMagnitude < 0.001f)
                continue;

            float angle = Vector3.Angle(direction, toTarget.normalized);

            if (angle > wave.arcDegrees * 0.5f)
                continue;

            hitTargets.Add(targetComponent);

            damageable.TakeDamage(new DamageData(
                wave.damage,
                player != null ? player : gameObject,
                closestPoint,
                toTarget.normalized,
                0
            ));
        }
    }

    void PlayWaveSfx(CrystalWave wave)
    {
        if (wave.waveSfx == null || sfxSource == null)
            return;

        float oldPitch = sfxSource.pitch;

        sfxSource.pitch = Random.Range(sfxPitchMin, sfxPitchMax);
        sfxSource.PlayOneShot(wave.waveSfx, wave.waveSfxVolume);
        sfxSource.pitch = oldPitch;
    }
}