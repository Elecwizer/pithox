using System.Collections;
using UnityEngine;
using Pithox.Combat;
using Pithox.Player;

public class ShieldActiveAbility : MonoBehaviour, IPlayerActiveAbility
{
    [Header("Shield")]
    [SerializeField] float duration = 3f;
    [SerializeField] float cooldown = 12f;
    [SerializeField] float invincibleTimeAfterBreak = 0.2f;

    [Header("VFX")]
    [SerializeField] GameObject shieldVfxPrefab;
    [SerializeField] Transform vfxParentOverride;
    [SerializeField] Vector3 vfxLocalPosition;
    [SerializeField] Vector3 vfxLocalRotation;
    [SerializeField] float vfxDestroyDelay = 0.2f;

    [Header("SFX")]
    [SerializeField] AudioSource sfxSource;
    [SerializeField] AudioClip activateSfx;
    [SerializeField] AudioClip breakSfx;
    [SerializeField] float activateVolume = 1f;
    [SerializeField] float breakVolume = 1f;

    GameObject player;
    PlayerHealth health;
    GameObject activeVfx;

    float cooldownTimer;
    bool active;
    bool breaking;
    Coroutine activeRoutine;

    public void OnEquip(GameObject playerObject)
    {
        player = playerObject;

        if (player != null)
        {
            health = player.GetComponent<PlayerHealth>();

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
        EndShield(false, true);
    }

    public void Use(GameObject playerObject)
    {
        if (active)
            return;

        if (cooldownTimer > 0f)
            return;

        if (player == null)
            OnEquip(playerObject);

        if (health == null)
            return;

        activeRoutine = StartCoroutine(ShieldRoutine());
    }

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    IEnumerator ShieldRoutine()
    {
        active = true;
        breaking = false;
        cooldownTimer = cooldown;

        health.SetInvincible(true);
        health.OnDamageBlocked += HandleBlockedHit;

        SpawnVfx();
        PlaySfx(activateSfx, activateVolume);

        yield return new WaitForSeconds(duration);

        EndShield(false, false);
    }

    void HandleBlockedHit(DamageData damageData)
    {
        if (!active || breaking)
            return;

        StartCoroutine(BreakAfterBlockedHit());
    }

    IEnumerator BreakAfterBlockedHit()
    {
        breaking = true;

        PlaySfx(breakSfx, breakVolume);

        if (invincibleTimeAfterBreak > 0f)
            yield return new WaitForSeconds(invincibleTimeAfterBreak);

        EndShield(true, false);
    }

    void EndShield(bool brokenByHit, bool instant)
    {
        if (!active)
            return;

        active = false;
        breaking = false;

        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        if (health != null)
        {
            health.OnDamageBlocked -= HandleBlockedHit;
            health.SetInvincible(false);
        }

        DestroyVfx();
    }

    void SpawnVfx()
    {
        if (shieldVfxPrefab == null || player == null)
            return;

        Transform parent = vfxParentOverride != null ? vfxParentOverride : player.transform;

        activeVfx = Instantiate(shieldVfxPrefab, parent);
        activeVfx.transform.localPosition = vfxLocalPosition;
        activeVfx.transform.localRotation = Quaternion.Euler(vfxLocalRotation);
    }

    void DestroyVfx()
    {
        if (activeVfx == null)
            return;

        activeVfx.transform.SetParent(null);

        if (vfxDestroyDelay > 0f)
            Destroy(activeVfx, vfxDestroyDelay);
        else
            Destroy(activeVfx);

        activeVfx = null;
    }

    void PlaySfx(AudioClip clip, float volume)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip, volume);
    }
}