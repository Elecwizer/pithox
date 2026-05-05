using System.Collections;
using UnityEngine;
using Pithox.Combat;
using Pithox.Player;

public class HealActiveAbility : MonoBehaviour, IPlayerActiveAbility
{
    [Header("Heal")]
    [SerializeField] float duration = 3f;
    [SerializeField] float healPerSecond = 5f;
    [SerializeField] float cooldown = 12f;

    [Header("Break On Hit")]
    [SerializeField] bool breakOnHit = true;
    [SerializeField] float breakDelayAfterHit = 0.2f;
    [SerializeField] bool invincibleDuringBreakDelay = true;

    [Header("VFX")]
    [SerializeField] GameObject healVfxPrefab;
    [SerializeField] Transform vfxParentOverride;
    [SerializeField] Vector3 vfxLocalPosition;
    [SerializeField] Vector3 vfxLocalRotation;
    [SerializeField] float vfxDestroyDelay = 0.2f;

    [Header("SFX")]
    [SerializeField] AudioSource sfxSource;
    [SerializeField] AudioClip activateSfx;
    [SerializeField] AudioClip breakSfx;
    [SerializeField] AudioClip finishSfx;
    [SerializeField] float activateVolume = 1f;
    [SerializeField] float breakVolume = 1f;
    [SerializeField] float finishVolume = 1f;

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
        EndHeal(false);
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

        activeRoutine = StartCoroutine(HealRoutine());
    }

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    IEnumerator HealRoutine()
    {
        active = true;
        breaking = false;
        cooldownTimer = cooldown;

        if (breakOnHit)
            health.OnDamaged += HandleDamaged;

        SpawnVfx();
        PlaySfx(activateSfx, activateVolume);

        float timer = 0f;

        while (timer < duration && active && !breaking)
        {
            float dt = Time.deltaTime;
            timer += dt;

            health.Heal(healPerSecond * dt);

            yield return null;
        }

        if (active && !breaking)
            EndHeal(false);
    }

    void HandleDamaged(DamageData damageData)
    {
        if (!active || breaking)
            return;

        StartCoroutine(BreakAfterHit());
    }

    IEnumerator BreakAfterHit()
    {
        breaking = true;

        if (invincibleDuringBreakDelay && health != null)
            health.GiveInvincibility(breakDelayAfterHit);

        if (breakDelayAfterHit > 0f)
            yield return new WaitForSeconds(breakDelayAfterHit);

        EndHeal(true);
    }

    void EndHeal(bool brokenByHit)
    {
        if (!active)
            return;

        active = false;

        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        if (health != null)
            health.OnDamaged -= HandleDamaged;

        if (brokenByHit)
            PlaySfx(breakSfx, breakVolume);
        else
            PlaySfx(finishSfx, finishVolume);

        DestroyVfx();
    }

    void SpawnVfx()
    {
        if (healVfxPrefab == null || player == null)
            return;

        Transform parent = vfxParentOverride != null ? vfxParentOverride : player.transform;

        activeVfx = Instantiate(healVfxPrefab, parent);
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