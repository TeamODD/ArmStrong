using System.Collections;
using UnityEngine;

public class CardKeyDoor : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject cardKeyRequiredUI;

    [Header("Door")]
    [SerializeField] private Animator doorAnimator;

    private bool isOpened;
    private Coroutine messageCoroutine;

    public void Interact(PlayerItemManager itemManager)
    {
        if (isOpened)
            return;

        // --------------------------------
        // 카드키 확인
        // --------------------------------

        if (itemManager != null &&
            itemManager.HasCardKey)
        {
            OpenDoor();
        }
        else
        {
            ShowCardKeyRequiredUI();
        }
    }

    private void OpenDoor()
    {
        isOpened = true;

        if (doorAnimator != null)
        {
            doorAnimator.SetTrigger("Open");
        }

        Debug.Log("카드키로 문을 열었습니다.");
    }

    private void ShowCardKeyRequiredUI()
    {
        if (cardKeyRequiredUI == null)
            return;

        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
        }

        cardKeyRequiredUI.SetActive(true);

        messageCoroutine =
            StartCoroutine(HideCardKeyRequiredUI());
    }

    private IEnumerator HideCardKeyRequiredUI()
    {
        yield return new WaitForSeconds(3f);

        if (cardKeyRequiredUI != null)
            cardKeyRequiredUI.SetActive(false);

        messageCoroutine = null;
    }
}