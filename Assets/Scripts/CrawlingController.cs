using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody), typeof(Animator))]
public class CrawlingController : MonoBehaviour
{
    [Header("Crawling Setup")]
    [Range(0f, 1.0f)]
    public float turnSmoothTime = 0.5f; // 낮을수록 빨리 돌고, 높을수록 천천히 부드럽게 돕니다.
    public float mountableDistance = 2.5f;
    public float recoveryTime = 2.0f;

    [Header("UI Transition")]
    public Image fadeImage;
    public GameObject pressEButtonUI;

    [Header("Crawling Slope Settings")]
    public LayerMask groundLayer;
    public float rayHeight = 0.5f;      // 레이 시작 높이
    public float rayDistance = 1.0f;    // 레이 길이
    public float frontRayOffset = 0.4f; // 캐릭터 중심에서 머리/가슴 쪽으로의 거리
    public float mountableAngle = 45f;
    public float backRayOffset = 0.4f;  // 캐릭터 중심에서 골반 쪽으로의 거리

    [Header("Interaction Settings")]
    public float cameraViewAngle = 30f; // 카메라 중앙(정면) 기준 좌우 30도 (총 60도 범위)

    private Rigidbody humanRb;
    private Animator anim;
    private Collider[] humanColliders;
    private Camera mainCam;

    private PlayerController currentWheelchair;

    private Vector3 initialLocalPos;
    private Quaternion initialLocalRot;

    private bool isMounting = false;
    private bool canMove = false;

    private Vector2 currentInput;
    private float turnSmoothVelocity;

    void Awake()
    {
        humanRb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        humanColliders = GetComponentsInChildren<Collider>();
        mainCam = Camera.main;

        initialLocalPos = transform.localPosition;
        initialLocalRot = transform.localRotation;

        SetPhysicsEnabled(false);
        this.enabled = false;
    }

    void Update()
    {
        if (isMounting) return;

        GatherInput();
        CheckMountInput();
    }

    void FixedUpdate()
    {
        if (isMounting) return;

        MoveCharacterRelativeToCamera();
    }

    public void EjectFromWheelchair(Vector3 normal, PlayerController wheelchair)
    {
        currentWheelchair = wheelchair;
        this.enabled = true;
        canMove = false;

        transform.SetParent(null);
        SetPhysicsEnabled(true);

        Collider[] wheelchairCols = wheelchair.GetComponentsInChildren<Collider>();
        foreach (Collider wc in wheelchairCols)
        {
            foreach (Collider hc in humanColliders)
            {
                Physics.IgnoreCollision(wc, hc, true);
            }
        }

        // 1. 튕겨 나갈 X, Z 위치 계산
        Vector3 pushBackDir = new Vector3(normal.x, 0f, normal.z).normalized;
        Vector3 targetPos = transform.position + pushBackDir * 1.5f;

        // 2. 바닥에 딱 붙이기 위한 Raycast (레이저) 발사
        if (Physics.Raycast(targetPos + Vector3.up * 1.0f, Vector3.down, out RaycastHit hit, 3.0f))
        {
            targetPos.y = hit.point.y;
        }

        // 3. 최종 위치 적용
        transform.position = targetPos;

        anim.applyRootMotion = true;
        anim.SetTrigger("Fall");

        if (mainCam != null)
        {
            PlayerCamera pCam = mainCam.GetComponent<PlayerCamera>();
            if (pCam != null) pCam.isDetached = true;
        }

        StartCoroutine(RestoreCollision(wheelchairCols, humanColliders, 1.0f));
        StartCoroutine(RecoveryRoutine());
    }
    IEnumerator RecoveryRoutine()
    {
        yield return new WaitForSeconds(recoveryTime);
        canMove = true;
    }

    void GatherInput()
    {
        if (Keyboard.current == null || !canMove)
        {
            currentInput = Vector2.zero;
            anim.SetBool("IsCrawlingMove", false);
            return;
        }

        currentInput = Vector2.zero;
        if (Keyboard.current.wKey.isPressed) currentInput.y += 1f;
        // if (Keyboard.current.sKey.isPressed) currentInput.y -= 1f; 
        if (Keyboard.current.aKey.isPressed) currentInput.x -= 1f;
        if (Keyboard.current.dKey.isPressed) currentInput.x += 1f;

        bool isMoving = currentInput.sqrMagnitude > 0.01f;
        anim.SetBool("IsCrawlingMove", isMoving);
    }
    void MoveCharacterRelativeToCamera()
    {
        if (mainCam == null || !canMove) return;

        Vector3 camForward = mainCam.transform.forward;
        Vector3 camRight = mainCam.transform.right;

        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = (camForward * currentInput.y + camRight * currentInput.x).normalized;

        // 1. 짐벌락(각도 꼬임) 방지를 위해 평면 기준 현재 Y 각도 계산
        Vector3 currentFlatDir = transform.forward;
        currentFlatDir.y = 0f;
        if (currentFlatDir.sqrMagnitude < 0.001f) currentFlatDir = transform.up;
        currentFlatDir.Normalize();

        float currentYAngle = Mathf.Atan2(currentFlatDir.x, currentFlatDir.z) * Mathf.Rad2Deg;

        // 입력이 있을 때만 좌우 회전 스무딩
        if (moveDir.sqrMagnitude > 0.01f)
        {
            float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
            currentYAngle = Mathf.SmoothDampAngle(currentYAngle, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
        }

        // 캐릭터가 바라볼 평지 기준 앞방향
        Vector3 flatForward = Quaternion.Euler(0f, currentYAngle, 0f) * Vector3.forward;

        // 2. 상체 앞뒤 멀티 레이캐스트 쏘기
        Vector3 rayOriginCenter = transform.position + (Vector3.up * rayHeight);

        // 휠체어와 달리 캐릭터가 유연하게 도는 것을 반영하여, 목표 방향(flatForward)을 기준으로 레이 위치를 잡습니다.
        Vector3 frontRayOrigin = rayOriginCenter + (flatForward * frontRayOffset);
        Vector3 backRayOrigin = rayOriginCenter - (flatForward * backRayOffset);

        // [디버그] 씬 뷰에서 확인용 빨간 선
        Debug.DrawRay(frontRayOrigin, Vector3.down * rayDistance, Color.red);
        Debug.DrawRay(backRayOrigin, Vector3.down * rayDistance, Color.red);

        bool frontGrounded = Physics.Raycast(frontRayOrigin, Vector3.down, out RaycastHit frontHit, rayDistance, groundLayer);
        bool backGrounded = Physics.Raycast(backRayOrigin, Vector3.down, out RaycastHit backHit, rayDistance, groundLayer);

        Vector3 finalNormal = Vector3.up;
        Vector3 projectedForward = flatForward;

        // 3. 앞뒤 모두 바닥에 닿았을 때 (가장 완벽한 경사도 산출)
        if (frontGrounded && backGrounded)
        {
            // [디버그] 닿은 곳은 초록 선
            Debug.DrawRay(frontRayOrigin, Vector3.down * frontHit.distance, Color.green);
            Debug.DrawRay(backRayOrigin, Vector3.down * backHit.distance, Color.green);

            // 뒷점 -> 앞점을 잇는 벡터가 실제 바닥 표면의 기울어진 앞방향이 됩니다.
            Vector3 surfaceForward = (frontHit.point - backHit.point).normalized;
            finalNormal = (frontHit.normal + backHit.normal).normalized;

            projectedForward = Vector3.ProjectOnPlane(surfaceForward, finalNormal).normalized;
        }
        // 한쪽만 닿았을 경우의 예외 처리
        else if (frontGrounded)
        {
            finalNormal = frontHit.normal;
            projectedForward = Vector3.ProjectOnPlane(flatForward, finalNormal).normalized;
        }
        else if (backGrounded)
        {
            finalNormal = backHit.normal;
            projectedForward = Vector3.ProjectOnPlane(flatForward, finalNormal).normalized;
        }

        // 4. 최종 회전 적용 (좌우 회전 + 지형 굴곡)
        if (projectedForward.sqrMagnitude > 0)
        {
            Quaternion targetRotation = Quaternion.LookRotation(projectedForward, finalNormal);
            // Slerp를 통해 부드럽게 기울어지게 만듭니다. (15f 수치를 조절하여 눕혀지는 속도 조절 가능)
            humanRb.MoveRotation(Quaternion.Slerp(humanRb.rotation, targetRotation, Time.fixedDeltaTime * 15f));
        }
    }
    void CheckMountInput()
    {
        // mainCam 체크 추가
        if (currentWheelchair == null || !canMove || mainCam == null)
        {
            if (pressEButtonUI != null && pressEButtonUI.activeSelf)
                pressEButtonUI.SetActive(false);

            return;
        }

        // 1. 거리는 '캐릭터'와 휠체어 사이의 거리를 잽니다. (탑승은 캐릭터가 하므로)
        float distance = Vector3.Distance(transform.position, currentWheelchair.transform.position);

        // 2. 각도는 '카메라'를 기준으로 잽니다.
        // 휠체어의 Pivot이 바닥에 있다면 중심을 맞추기 위해 Y축으로 살짝(예: 0.5f) 올려서 계산하는 것이 좋습니다.
        Vector3 targetPos = currentWheelchair.transform.position + (Vector3.up * 0.5f);

        // 카메라 위치에서 휠체어를 향하는 방향 벡터
        Vector3 dirFromCameraToWheelchair = (targetPos - mainCam.transform.position).normalized;

        // 카메라의 진짜 정면(화면 정중앙)과 휠체어 사이의 각도 계산
        float angle = Vector3.Angle(mainCam.transform.forward, dirFromCameraToWheelchair);

        // 3. 거리 조건(가깝고)과 카메라 시야각 조건(화면 중앙 근처에 있음)을 모두 만족할 때
        if (distance <= mountableDistance && angle <= cameraViewAngle)
        {
            // UI 켜기
            if (pressEButtonUI != null && !pressEButtonUI.activeSelf)
                pressEButtonUI.SetActive(true);

            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                if (pressEButtonUI != null)
                    pressEButtonUI.SetActive(false);

                StartCoroutine(MountRoutine());
            }
        }
        // 4. 거리가 멀거나, 거리는 가깝지만 카메라를 돌려 화면에서 휠체어가 벗어났을 때
        else
        {
            // UI 끄기
            if (pressEButtonUI != null && pressEButtonUI.activeSelf)
                pressEButtonUI.SetActive(false);
        }
    }

    IEnumerator MountRoutine()
    {
        isMounting = true;
        canMove = false;

        // 1. 화면 암전
        yield return StartCoroutine(FadeScreen(0f, 1f, 1.0f));

        // 2. 플레이어 물리 끄기 및 휠체어 종속
        SetPhysicsEnabled(false);
        transform.SetParent(currentWheelchair.transform);
        transform.localPosition = initialLocalPos;
        transform.localRotation = initialLocalRot;

        anim.applyRootMotion = false;
        anim.Play("Idle");

        if (mainCam != null)
        {
            PlayerCamera pCam = mainCam.GetComponent<PlayerCamera>();
            if (pCam != null) pCam.isDetached = false;
        }

        Vector3 raisedPosition = currentWheelchair.transform.position;
        raisedPosition.y += 1f;
        currentWheelchair.transform.position = raisedPosition;

        currentWheelchair.ResetWheelchairPhysics();

        // 3. 휠체어가 바닥에 완전히 닿을 때까지 대기
        yield return new WaitForSeconds(0.5f);

        // 4. 화면 밝아짐
        yield return StartCoroutine(FadeScreen(1f, 0f, 1.0f));

        isMounting = false;
        this.enabled = false;
    }

    void SetPhysicsEnabled(bool enabled)
    {
        humanRb.isKinematic = !enabled;

        if (enabled)
        {
            humanRb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            humanRb.interpolation = RigidbodyInterpolation.Interpolate;
        }
        else
        {
            humanRb.interpolation = RigidbodyInterpolation.None;
        }

        foreach (Collider col in humanColliders)
        {
            col.isTrigger = !enabled;
        }
    }

    IEnumerator FadeScreen(float startAlpha, float endAlpha, float duration)
    {
        if (fadeImage == null) yield break;
        float time = 0;
        Color c = fadeImage.color;

        while (time < duration)
        {
            time += Time.deltaTime;
            c.a = Mathf.Lerp(startAlpha, endAlpha, time / duration);
            fadeImage.color = c;
            yield return null;
        }
        c.a = endAlpha;
        fadeImage.color = c;
    }

    IEnumerator RestoreCollision(Collider[] wcCols, Collider[] hcCols, float delay)
    {
        yield return new WaitForSeconds(delay);
        foreach (Collider wc in wcCols)
        {
            foreach (Collider hc in hcCols)
            {
                if (wc != null && hc != null)
                {
                    Physics.IgnoreCollision(wc, hc, false);
                }
            }
        }
    }
}