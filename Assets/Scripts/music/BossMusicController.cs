using System.Collections;
using Pithox.Enemies;
using UnityEngine;

public class BossMusicController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] GolemKingBoss boss;
    [SerializeField] AudioSource musicSource;

    [Header("Music")]
    [SerializeField] AudioClip normalMusic;
    [SerializeField] AudioClip bossPhase1Music;
    [SerializeField] AudioClip bossPhase2Music;

    [Header("Settings")]
    [SerializeField] float volume = 1f;
    [SerializeField] float fadeTime = 1f;
    [SerializeField] bool playNormalOnStart = true;

    Coroutine musicRoutine;

    void Awake()
    {
        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();

            if (musicSource == null)
                musicSource = gameObject.AddComponent<AudioSource>();
        }

        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = volume;
    }

    void OnEnable()
    {
        if (boss == null)
            return;

        boss.OnBossSpawned += HandleBossSpawned;
        boss.OnAnyDeathStarted += HandleBossDeathStarted;
        boss.OnPhase2Started += HandlePhase2Started;
        boss.OnFinalDeathStarted += HandleFinalDeathStarted;
    }

    void Start()
    {
        if (playNormalOnStart)
            PlayMusic(normalMusic);

        if (boss != null && boss.IsBossActive && !boss.IsBossDead)
        {
            if (boss.IsPhase2)
                PlayMusic(bossPhase2Music);
            else
                PlayMusic(bossPhase1Music);
        }
    }

    void OnDisable()
    {
        if (boss == null)
            return;

        boss.OnBossSpawned -= HandleBossSpawned;
        boss.OnAnyDeathStarted -= HandleBossDeathStarted;
        boss.OnPhase2Started -= HandlePhase2Started;
        boss.OnFinalDeathStarted -= HandleFinalDeathStarted;
    }

    void HandleBossSpawned(GolemKingBoss currentBoss)
    {
        PlayMusic(bossPhase1Music);
    }

    void HandleBossDeathStarted(GolemKingBoss currentBoss)
    {
        if (currentBoss.CurrentPhase == 1)
            StopMusic();
    }

    void HandlePhase2Started(GolemKingBoss currentBoss)
    {
        PlayMusic(bossPhase2Music);
    }

    void HandleFinalDeathStarted(GolemKingBoss currentBoss)
    {
        StopMusic();
    }

    public void PlayNormalMusic()
    {
        PlayMusic(normalMusic);
    }

    public void PlayBossPhase1Music()
    {
        PlayMusic(bossPhase1Music);
    }

    public void PlayBossPhase2Music()
    {
        PlayMusic(bossPhase2Music);
    }

    public void StopMusic()
    {
        if (musicRoutine != null)
            StopCoroutine(musicRoutine);

        musicRoutine = StartCoroutine(FadeOutAndStop());
    }

    void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource == null)
            return;

        if (musicSource.clip == clip && musicSource.isPlaying)
            return;

        if (musicRoutine != null)
            StopCoroutine(musicRoutine);

        musicRoutine = StartCoroutine(FadeToClip(clip));
    }

    IEnumerator FadeToClip(AudioClip clip)
    {
        float startVolume = musicSource.volume;
        float time = 0f;

        while (time < fadeTime && musicSource.isPlaying)
        {
            time += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, time / Mathf.Max(0.01f, fadeTime));
            yield return null;
        }

        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.volume = 0f;
        musicSource.loop = true;
        musicSource.Play();

        time = 0f;

        while (time < fadeTime)
        {
            time += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, volume, time / Mathf.Max(0.01f, fadeTime));
            yield return null;
        }

        musicSource.volume = volume;
        musicRoutine = null;
    }

    IEnumerator FadeOutAndStop()
    {
        float startVolume = musicSource.volume;
        float time = 0f;

        while (time < fadeTime)
        {
            time += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, time / Mathf.Max(0.01f, fadeTime));
            yield return null;
        }

        musicSource.Stop();
        musicSource.clip = null;
        musicSource.volume = volume;
        musicRoutine = null;
    }
}