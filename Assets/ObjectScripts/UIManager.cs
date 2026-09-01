using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Interaction UI")]
    [SerializeField] private GameObject interactionEButtonUI;
    [SerializeField] private GameObject wheelchairEButtonUI;

    [Header("Fade")]
    [SerializeField] private Image fadeImage;

    public void SetInteractionUI(bool active)
    {
        if (interactionEButtonUI != null)
            interactionEButtonUI.SetActive(active);
    }

    public void SetWheelchairUI(bool active)
    {
        if (wheelchairEButtonUI != null)
            wheelchairEButtonUI.SetActive(active);
    }

    public void HideAllInteractionUI()
    {
        SetInteractionUI(false);
        SetWheelchairUI(false);
    }

    public IEnumerator FadeScreen(
        float startAlpha,
        float endAlpha,
        float duration)
    {
        if (fadeImage == null)
            yield break;

        float time = 0f;
        Color c = fadeImage.color;

        while (time < duration)
        {
            time += Time.deltaTime;

            c.a = Mathf.Lerp(
                startAlpha,
                endAlpha,
                time / duration
            );

            fadeImage.color = c;

            yield return null;
        }

        c.a = endAlpha;
        fadeImage.color = c;
    }
}