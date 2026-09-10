using System.Collections;
using UnityEngine;

public class BedManager : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private PlayerController wheelChairController;
    [SerializeField] private CrawlingController humanController;

    [SerializeField] private float exitDistance = 3.0f;
    [SerializeField] private float exitHeight = 1.0f;
    [SerializeField] private float groundRayDistance = 3.0f;

    [Header("UI Transition")]
    [SerializeField] private UIManager uiManager;

    private Camera mainCam;

    private bool isHiding = false;

    private BedInteraction currentBed;
    private Transform currentHidePoint;

    public bool IsHiding => isHiding;

    private void Awake()
    {
        mainCam = Camera.main;
    }

    public void Interact(
        BedInteraction bed,
        Transform hidePoint)
    {
        if (!isHiding)
        {
            StartCoroutine(EnterBedRoutine(bed, hidePoint));
        }
        else
        {
            StartCoroutine(ExitBedRoutine());
        }
    }

    private IEnumerator EnterBedRoutine(
    BedInteraction bed,
    Transform hidePoint)
    {
        if (humanController == null)
        {
            Debug.LogWarning("[BedManager] CrawlingController가 없습니다.");
            yield break;
        }

        if (hidePoint == null)
        {
            Debug.LogWarning("[BedManager] HidePoint가 없습니다.");
            yield break;
        }

        currentBed = bed;
        currentHidePoint = hidePoint;

        // 1. 화면 암전
        yield return StartCoroutine(
            uiManager.FadeScreen(0f, 1f, 1.0f)
        );

        // 2. 휠체어에 타고 있다면 분리
        if (wheelChairController != null &&
            humanController.transform.parent ==
            wheelChairController.transform)
        {
            wheelChairController.isFallenOver = true;

            humanController.DetachFromWheelchair();
        }

        // 3. 물리 OFF
        humanController.SetPhysicsEnabled(false);

        // 4. HidePoint로 이동
        humanController.transform.position =
            currentHidePoint.position;

        humanController.transform.rotation =
            currentHidePoint.rotation;

        // 5. 숨기 상태
        humanController.SetHiding(true);
        humanController.SetHidingAnimation(true);

        // 6. 카메라
        if (mainCam != null)
        {
            PlayerCamera pCam =
                mainCam.GetComponent<PlayerCamera>();

            if (pCam != null)
            {
                pCam.SetHidingView(currentHidePoint);
            }
        }

        isHiding = true;

        // 7. 화면 밝아짐
        yield return StartCoroutine(
            uiManager.FadeScreen(1f, 0f, 1.0f)
        );

        Debug.Log("침대 밑으로 숨음");
    }
    public void ExitBed()
    {
        StartCoroutine(ExitBedRoutine());
    }
    private IEnumerator ExitBedRoutine()
    {
        if (humanController == null)
            yield break;

        // --------------------------------
        // 화면 암전
        // --------------------------------

        yield return StartCoroutine(
            uiManager.FadeScreen(0f, 1f, 1.0f)
        );

        // --------------------------------
        // 플레이어가 바라보던 좌우 방향으로 탈출
        // --------------------------------

        if (mainCam != null)
        {
            Vector3 lookDirection = mainCam.transform.forward;

            // 상하 방향 제거
            lookDirection.y = 0f;

            if (lookDirection.sqrMagnitude > 0.001f)
            {
                lookDirection.Normalize();

                // 플레이어를 바라보던 방향을
                // 플레이어의 정면(0도)으로 설정
                humanController.transform.rotation =
                    Quaternion.LookRotation(lookDirection);

                // --------------------------------
                // 현재 위치에서 정면으로 이동
                // --------------------------------

                Vector3 exitPosition =
                    humanController.transform.position +
                    humanController.transform.forward * exitDistance;

                // --------------------------------
                // 바닥 탐색용 시작 위치
                // --------------------------------

                Vector3 rayOrigin =
                    exitPosition + Vector3.up * exitHeight;

                if (Physics.Raycast(
                    rayOrigin,
                    Vector3.down,
                    out RaycastHit groundHit,
                    groundRayDistance,
                    humanController.groundLayer))
                {
                    exitPosition.y = groundHit.point.y;
                }

                humanController.transform.position = exitPosition;
            }
        }

        // --------------------------------
        // 숨기 상태 종료
        // --------------------------------

        humanController.SetHiding(false);
        humanController.SetHidingAnimation(false);

        // --------------------------------
        // CrawlingController 활성화
        // --------------------------------

        humanController.enabled = true;

        // --------------------------------
        // 물리 ON
        // --------------------------------

        humanController.SetPhysicsEnabled(true);

        // --------------------------------
        // 카메라 Crawling 상태
        // --------------------------------

        if (mainCam != null)
        {
            PlayerCamera pCam =
                mainCam.GetComponent<PlayerCamera>();

            if (pCam != null)
            {
                pCam.isHiding = false;
                pCam.isDetached = true;
            }
        }

        isHiding = false;

        currentBed = null;
        currentHidePoint = null;

        // --------------------------------
        // 화면 밝아짐
        // --------------------------------

        yield return StartCoroutine(
            uiManager.FadeScreen(1f, 0f, 1.0f)
        );

        Debug.Log("침대에서 나옴");
    }
}