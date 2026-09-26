using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

public class ChaseAudioManager : MonoBehaviour
{
    [Header("Chase Audio")]
    public AudioSource chaseSource;

    [Header("Audio Snapshots")]
    public AudioMixerSnapshot exploreSnapshot;
    public AudioMixerSnapshot chaseSnapshot;

    [Header("Fade Settings")]
    public float fadeDuration = 2f;

    private bool isChasing = false;
    private Coroutine stopCoroutine;

    void Start()
    {
        if (chaseSource != null)
        {
            chaseSource.playOnAwake = false;
            chaseSource.loop = true;
            chaseSource.Stop();
        }

        if (exploreSnapshot != null)
        {
            exploreSnapshot.TransitionTo(0f);
        }
    }

    // 괴물이 플레이어를 발견했을 때
    [ContextMenu("TEST - Start Chase")]
    public void StartChase()
    {
        if (isChasing) return;

        isChasing = true;

        if (stopCoroutine != null)
        {
            StopCoroutine(stopCoroutine);
            stopCoroutine = null;
        }

        if (chaseSource != null && !chaseSource.isPlaying)
        {
            chaseSource.Play();
        }

        if (chaseSnapshot != null)
        {
            chaseSnapshot.TransitionTo(fadeDuration);
        }
    }

    // 괴물이 플레이어를 놓쳤을 때
    [ContextMenu("TEST - Stop Chase")]
    public void StopChase()
    {
        if (!isChasing) return;

        isChasing = false;

        if (exploreSnapshot != null)
        {
            exploreSnapshot.TransitionTo(fadeDuration);
        }

        stopCoroutine = StartCoroutine(
            StopChaseAfterFade()
        );
    }

    private IEnumerator StopChaseAfterFade()
    {
        yield return new WaitForSeconds(fadeDuration);

        if (!isChasing && chaseSource != null)
        {
            chaseSource.Stop();
        }

        stopCoroutine = null;
    }
}