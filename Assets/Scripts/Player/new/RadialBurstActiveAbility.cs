using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pithox.Combat;
using Pithox.Player;

public class RadialBurstActiveAbility : MonoBehaviour, IPlayerActiveAbility
{
    [Header("Ability")]
    [SerializeField] float cooldown = 12f;
    [SerializeField] float hitDelay = 0.3f;
    [SerializeField] float radius = 5f;
    [SerializeField] float damage = 20f;
    [SerializeField] LayerMask enemyMask = ~0;
    [SerializeField] bool usePlayerDamageBonus = true;

    [Header("VFX")]
    [SerializeField] GameObject vfxPrefab;
    [SerializeField] Transform vfxSpawnPoint;
    [SerializeField] bool parentVfxToPlayer = false;
    [SerializeField] Vector3 vfxLocalPosition;
    [SerializeField] Vector3 vfxLocalRotation;
    [SerializeField] float vfxLifetime = 2f;

    [Header("SFX")]
    [SerializeField] AudioSource sfxSource;
    [SerializeField] AudioClip castSfx;
    [SerializeField] float castVolume = 1f;
    [SerializeField] float sfxPitchMin = 0.95f;
    [SerializeField] float sfxPitchMax = 1.05f;

    GameObject player;
    PlayerStats stats;
    float cooldownTimer;
    bool casting;

    readonly HashSet<Component> hitTargets = new HashSet<Component>();

    public void OnEquip(GameObject playerObject)
    {
        player = playerObject;

        if (player != null)
        {
            stats = player.GetComponent<PlayerStats>();

            if (sfxSource == null)
                sfxSource = player.GetComponent<AudioSource>();
        }

        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();

        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
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

        StartCoroutine(CastRoutine());
    }

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    IEnumerator CastRoutine()
    {
        casting = true;
        cooldownTimer = cooldown;
        hitTargets.Clear();

        Vector3 origin = GetOrigin();

        SpawnVfx(origin);

        if (hitDelay > 0f)
            yield return new WaitForSeconds(hitDelay);

        PlaySfx(castSfx, castVolume);
        DamageAllAround(origin);

        casting = false;
    }

    Vector3 GetOrigin()
    {
        if (vfxSpawnPoint != null)
            return vfxSpawnPoint.position;

        if (player != null)
            return player.transform.position;

        return transform.position;
    }

    void DamageAllAround(Vector3 origin)
    {
        Collider[] hits = Physics.OverlapSphere(
            origin,
            radius,
            enemyMask,
            QueryTriggerInteraction.Collide
        );

        float finalDamage = GetFinalDamage();

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
            Vector3 direction = closestPoint - origin;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
            {
                direction = targetComponent.transform.position - origin;
                direction.y = 0f;
            }

            if (direction.sqrMagnitude < 0.001f)
                direction = Vector3.forward;

            hitTargets.Add(targetComponent);

            damageable.TakeDamage(new DamageData(
                finalDamage,
                player != null ? player : gameObject,
                closestPoint,
                direction.normalized,
                0
            ));
        }
    }

    float GetFinalDamage()
    {
        if (!usePlayerDamageBonus || stats == null)
            return damage;

        return damage * (1f + stats.AttackDamageBonus) * stats.DamageMultiplier;
    }

    void SpawnVfx(Vector3 origin)
    {
        if (vfxPrefab == null)
            return;

        GameObject vfx;

        if (parentVfxToPlayer && player != null)
        {
            vfx = Instantiate(vfxPrefab, player.transform);
            vfx.transform.localPosition = vfxLocalPosition;
            vfx.transform.localRotation = Quaternion.Euler(vfxLocalRotation);
        }
        else
        {
            Quaternion rotation = Quaternion.Euler(vfxLocalRotation);
            vfx = Instantiate(vfxPrefab, origin + vfxLocalPosition, rotation);
        }

        if (vfxLifetime > 0f)
            Destroy(vfx, vfxLifetime);
    }

    void PlaySfx(AudioClip clip, float volume)
    {
        if (clip == null || sfxSource == null)
            return;

        float oldPitch = sfxSource.pitch;

        sfxSource.pitch = Random.Range(sfxPitchMin, sfxPitchMax);
        sfxSource.PlayOneShot(clip, volume);
        sfxSource.pitch = oldPitch;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}