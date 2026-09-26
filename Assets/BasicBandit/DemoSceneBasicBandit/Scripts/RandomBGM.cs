using UnityEngine;
using System.Collections;

public class RandomBGM : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource bgmA;
    public AudioSource bgmB;

    [Header("Background Music")]
    public AudioClip[] bgmList;

    [Header("Settings")]
    [Range(0f, 1f)]
    public float maxVolume = 0.25f;

    public float fadeDuration = 3f;

    private int previousIndex = -1;

    private IEnumerator Start()
    {
        if (bgmA == null || bgmB == null ||
            bgmList == null || bgmList.Length == 0)
        {
            Debug.LogError("BGM 설정을 확인해 주세요!");
            yield break;
        }

        bgmA.playOnAwake = false;
        bgmB.playOnAwake = false;

        bgmA.loop = false;
        bgmB.loop = false;

        bgmA.spatialBlend = 0f;
        bgmB.spatialBlend = 0f;

        bgmA.volume = 0f;
        bgmB.volume = 0f;

        AudioSource current = bgmA;
        AudioSource next = bgmB;

        current.clip = GetRandomBGM();
        current.Play();

        float introFade = Mathf.Min(
            fadeDuration,
            current.clip.length * 0.25f
        );

        // 게임 시작 시 페이드 인
        float timer = 0f;

        while (timer < introFade)
        {
            timer += Time.deltaTime;

            current.volume = Mathf.Lerp(
                0f,
                maxVolume,
                Mathf.Clamp01(timer / introFade)
            );

            yield return null;
        }

        current.volume = maxVolume;

        // 이후 계속 랜덤 음악 재생
        while (true)
        {
            float fade = Mathf.Min(
                fadeDuration,
                current.clip.length * 0.25f
            );

            // 음악 종료 직전까지 기다림
            float waitTime = Mathf.Max(
                0f,
                current.clip.length - current.time - fade
            );

            yield return new WaitForSeconds(waitTime);

            // 다음 음악 랜덤 선택
            next.clip = GetRandomBGM();
            next.volume = 0f;
            next.Play();

            // 두 음악의 볼륨을 동시에 변경
            timer = 0f;

            while (timer < fade)
            {
                timer += Time.deltaTime;

                float t = Mathf.Clamp01(timer / fade);

                current.volume = Mathf.Lerp(
                    maxVolume, 0f, t
                );

                next.volume = Mathf.Lerp(
                    0f, maxVolume, t
                );

                yield return null;
            }

            current.Stop();
            current.volume = 0f;
            next.volume = maxVolume;

            // 오디오 소스 교체
            AudioSource temp = current;
            current = next;
            next = temp;
        }
    }

    private AudioClip GetRandomBGM()
    {
        if (bgmList.Length == 1)
            return bgmList[0];

        int index;

        do
        {
            index = Random.Range(0, bgmList.Length);
        }
        while (index == previousIndex);

        previousIndex = index;

        return bgmList[index];
    }
}