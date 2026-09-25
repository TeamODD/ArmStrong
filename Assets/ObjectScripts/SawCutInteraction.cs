using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SawCutInteraction : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject sawRequiredUI;
    [SerializeField] private GameObject cuttingGaugeUI;
    [SerializeField] private Image cuttingGaugeImage;

    [Header("Cutting")]
    [SerializeField] private float cutDuration = 5f;

    private float cutTimer;
    private bool isCutting;
    private bool isCutComplete;

    private Coroutine sawRequiredCoroutine;

    public bool IsCutting => isCutting;
    public bool IsCutComplete => isCutComplete;

    private void Awake()
    {
        if (cuttingGaugeUI != null)
            cuttingGaugeUI.SetActive(false);

        if (cuttingGaugeImage != null)
            cuttingGaugeImage.fillAmount = 0f;

        if (sawRequiredUI != null)
            sawRequiredUI.SetActive(false);
    }

    private void Update()
    {
        if (!isCutting)
            return;

        cutTimer += Time.deltaTime;

        float progress =
            Mathf.Clamp01(cutTimer / cutDuration);

        // 게이지 증가
        if (cuttingGaugeImage != null)
            cuttingGaugeImage.fillAmount = progress;

        // 5초 완료
        if (cutTimer >= cutDuration)
        {
            CompleteCutting();
        }
    }

    // =========================================================
    // 톱질 시작
    // =========================================================

    public bool StartCutting(PlayerItemManager itemManager)
    {
        if (isCutComplete)
            return false;

        if (itemManager == null ||
            !itemManager.HasSaw)
        {
            ShowSawRequiredUI();
            return false;
        }

        if (isCutting)
            return true;

        isCutting = true;
        cutTimer = 0f;

        if (cuttingGaugeUI != null)
            cuttingGaugeUI.SetActive(true);

        if (cuttingGaugeImage != null)
            cuttingGaugeImage.fillAmount = 0f;

        Debug.Log("톱질 시작");

        return true;
    }

    // =========================================================
    // 톱질 중단
    // =========================================================

    public void StopCutting()
    {
        if (!isCutting)
            return;

        isCutting = false;
        cutTimer = 0f;

        if (cuttingGaugeUI != null)
            cuttingGaugeUI.SetActive(false);

        if (cuttingGaugeImage != null)
            cuttingGaugeImage.fillAmount = 0f;

        Debug.Log("톱질 중단");
    }

    // =========================================================
    // 톱질 완료
    // =========================================================

    private void CompleteCutting()
    {
        if (isCutComplete)
            return;

        isCutComplete = true;
        isCutting = false;

        // 게이지 100%
        if (cuttingGaugeImage != null)
            cuttingGaugeImage.fillAmount = 1f;

        Debug.Log("톱질 완료");

        // 게이지 UI 제거
        if (cuttingGaugeUI != null)
            cuttingGaugeUI.SetActive(false);

        // 오브젝트 제거
        gameObject.SetActive(false);
    }

    // =========================================================
    // 톱 필요 UI
    // =========================================================

    public void ShowSawRequiredUI()
    {
        if (sawRequiredUI == null)
            return;

        if (sawRequiredCoroutine != null)
            StopCoroutine(sawRequiredCoroutine);

        sawRequiredUI.SetActive(true);

        sawRequiredCoroutine =
            StartCoroutine(HideSawRequiredUI());
    }

    private IEnumerator HideSawRequiredUI()
    {
        yield return new WaitForSeconds(3f);

        if (sawRequiredUI != null)
            sawRequiredUI.SetActive(false);

        sawRequiredCoroutine = null;
    }
}