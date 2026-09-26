using UnityEngine;
using System.Collections;

public class MonsterHowlAudio : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource idleSource;
    public AudioSource detectionSource;
    public AudioSource chaseSource;

    [Header("Idle Howling")]
    public AudioClip[] idleClips;
    public float minIdleInterval = 8f;
    public float maxIdleInterval = 20f;

    [Header("Chase Settings")]
    public float chaseVolume = 0.5f;
    public float fadeDuration = 1.5f;

    private Coroutine idleRoutine;
    private Coroutine fadeRoutine;

    private enum MonsterState
    {
        Idle,
        Detected,
        Chasing,
        Searching
    }

    private MonsterState state = MonsterState.Idle;

    void Start()
    {
        if (chaseSource != null)
        {
            chaseSource.playOnAwake = false;
            chaseSource.loop = true;
            chaseSource.volume = 0f;
            chaseSource.Stop();
        }

        if (detectionSource != null)
            detectionSource.playOnAwake = false;

        if (idleSource != null)
            idleSource.playOnAwake = false;

        idleRoutine = StartCoroutine(RandomIdleHowl());
    }

    // 평상시 랜덤 울음소리
    private IEnumerator RandomIdleHowl()
    {
        while (state == MonsterState.Idle)
        {
            float waitTime = Random.Range(
                minIdleInterval,
                maxIdleInterval
            );

            yield return new WaitForSeconds(waitTime);

            if (state != MonsterState.Idle)
                yield break;

            if (idleSource != null &&
                idleClips != null &&
                idleClips.Length > 0)
            {
                int index = Random.Range(
                    0,
                    idleClips.Length
                );

                if (idleClips[index] != null)
                {
                    idleSource.clip = idleClips[index];
                    idleSource.Play();
                }
            }
        }
    }

    // 플레이어 발견
    public void OnDetected()
    {
        if (state == MonsterState.Detected ||
            state == MonsterState.Chasing)
            return;

        state = MonsterState.Detected;

        if (idleRoutine != null)
        {
            StopCoroutine(idleRoutine);
            idleRoutine = null;
        }

        if (idleSource != null)
            idleSource.Stop();

        if (detectionSource != null)
            detectionSource.Play();
    }

    // 추격 시작
    public void OnChase()
    {
        if (state == MonsterState.Chasing)
            return;

        state = MonsterState.Chasing;

        if (idleRoutine != null)
        {
            StopCoroutine(idleRoutine);
            idleRoutine = null;
        }

        if (idleSource != null)
            idleSource.Stop();

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        if (chaseSource != null)
        {
            if (!chaseSource.isPlaying)
                chaseSource.Play();

            fadeRoutine = StartCoroutine(
                FadeChase(chaseVolume, false)
            );
        }
    }

    // 플레이어를 놓치고 수색 중
    public void OnSearch()
    {
        if (state == MonsterState.Searching)
            return;

        state = MonsterState.Searching;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(
            FadeChase(0f, true)
        );
    }

    // 순찰 복귀
    public void OnCalm()
    {
        if (state == MonsterState.Idle)
            return;

        state = MonsterState.Idle;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(
            FadeChase(0f, true)
        );

        if (idleRoutine != null)
            StopCoroutine(idleRoutine);

        idleRoutine = StartCoroutine(RandomIdleHowl());
    }

    // 추격 소리 페이드
    private IEnumerator FadeChase(
        float targetVolume,
        bool stopAfterFade)
    {
        if (chaseSource == null)
            yield break;

        float startVolume = chaseSource.volume;
        float timer = 0f;
        float duration = Mathf.Max(
            0.01f,
            fadeDuration
        );

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / duration);

            chaseSource.volume = Mathf.Lerp(
                startVolume,
                targetVolume,
                t
            );

            yield return null;
        }

        chaseSource.volume = targetVolume;

        if (stopAfterFade)
            chaseSource.Stop();

        fadeRoutine = null;
    }
}