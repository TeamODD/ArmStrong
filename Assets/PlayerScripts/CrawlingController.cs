using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody), typeof(Animator))]
public class CrawlingController : MonoBehaviour
{
    [Header("Crawling Setup")]
    [Range(0f, 1.0f)]
    public float turnSmoothTime = 0.5f;
    public float mountableDistance = 2.5f;
    public float recoveryTime = 2.0f;

    [Header("UI Transition")]
    [SerializeField] private UIManager uiManager;

    [Header("Crawling Slope Settings")]
    public LayerMask groundLayer;
    public float rayHeight = 0.5f;
    public float rayDistance = 1.0f;
    public float frontRayOffset = 0.4f;
    public float mountableAngle = 45f;
    public float backRayOffset = 0.4f;

    [Header("Interaction Settings")]
    public float cameraViewAngle = 30f;

    [Header("Specific Child Scale Settings")]
    [Tooltip("자식 구조 속에서 크기를 조절할 오브젝트의 이름 혹은 경로 (예: Hips/Spine/Armature_Mesh)")]
    public string targetChildName = "TargetObjectName"; // 여기에 깊숙이 있는 자식 이름을 적으세요!
    public Vector3 ejectChildScale = new Vector3(0.5f, 0.5f, 0.5f); //  Eject 시 바뀔 크기
    private Transform targetChildTransform;
    private Vector3 originalChildScale; // 원래 크기 저장용

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

        // [추가] 깊은 곳에 있는 자식을 이름(또는 경로)으로 찾아옵니다.
        Transform foundChild = transform.Find(targetChildName);
        if (foundChild != null)
        {
            targetChildTransform = foundChild;
            originalChildScale = targetChildTransform.localScale; // 원래 크기 기억
        }
        else
        {
            Debug.LogWarning($"[CrawlingController] '{targetChildName}'에 해당하는 자식 오브젝트를 찾지 못했습니다! 경로를 확인해주세요.");
        }

        SetPhysicsEnabled(false);
        this.enabled = false;
    }
    void Update()
    {
        if (isMounting) return;

        GatherInput();
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

        // [추가] Eject 시 특정 자식 오브젝트만 크기 변경
        if (targetChildTransform != null)
        {
            targetChildTransform.localScale = ejectChildScale;
        }

        Collider[] wheelchairCols = wheelchair.GetComponentsInChildren<Collider>();
        foreach (Collider wc in wheelchairCols)
        {
            foreach (Collider hc in humanColliders)
            {
                Physics.IgnoreCollision(wc, hc, true);
            }
        }

        Vector3 pushBackDir = new Vector3(normal.x, 0f, normal.z).normalized;
        Vector3 targetPos = transform.position + pushBackDir * 1.5f;

        if (Physics.Raycast(targetPos + Vector3.up * 1.0f, Vector3.down, out RaycastHit hit, 3.0f))
        {
            targetPos.y = hit.point.y;
        }

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
    } // 휠체어에서 튕겨나는 처리
    IEnumerator RecoveryRoutine()
    {
        yield return new WaitForSeconds(recoveryTime);
        canMove = true;
    } // 떨어진 후와 조작 가능 시점 사이 회복 루틴
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
        if (Keyboard.current.aKey.isPressed) currentInput.x -= 1f;
        if (Keyboard.current.dKey.isPressed) currentInput.x += 1f;

        bool isMoving = currentInput.sqrMagnitude > 0.01f;
        anim.SetBool("IsCrawlingMove", isMoving);
    } // 기어가는 움직임 관련 키 입력
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

        Vector3 currentFlatDir = transform.forward;
        currentFlatDir.y = 0f;
        if (currentFlatDir.sqrMagnitude < 0.001f) currentFlatDir = transform.up;
        currentFlatDir.Normalize();

        float currentYAngle = Mathf.Atan2(currentFlatDir.x, currentFlatDir.z) * Mathf.Rad2Deg;

        if (moveDir.sqrMagnitude > 0.01f)
        {
            float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
            currentYAngle = Mathf.SmoothDampAngle(currentYAngle, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
        }

        Vector3 flatForward = Quaternion.Euler(0f, currentYAngle, 0f) * Vector3.forward;

        Vector3 rayOriginCenter = transform.position + (Vector3.up * rayHeight);
        Vector3 frontRayOrigin = rayOriginCenter + (flatForward * frontRayOffset);
        Vector3 backRayOrigin = rayOriginCenter - (flatForward * backRayOffset);

        Debug.DrawRay(frontRayOrigin, Vector3.down * rayDistance, Color.red);
        Debug.DrawRay(backRayOrigin, Vector3.down * rayDistance, Color.red);

        bool frontGrounded = Physics.Raycast(frontRayOrigin, Vector3.down, out RaycastHit frontHit, rayDistance, groundLayer);
        bool backGrounded = Physics.Raycast(backRayOrigin, Vector3.down, out RaycastHit backHit, rayDistance, groundLayer);

        Vector3 finalNormal = Vector3.up;
        Vector3 projectedForward = flatForward;

        if (frontGrounded && backGrounded)
        {
            Debug.DrawRay(frontRayOrigin, Vector3.down * frontHit.distance, Color.green);
            Debug.DrawRay(backRayOrigin, Vector3.down * backHit.distance, Color.green);

            Vector3 surfaceForward = (frontHit.point - backHit.point).normalized;
            finalNormal = (frontHit.normal + backHit.normal).normalized;

            projectedForward = Vector3.ProjectOnPlane(surfaceForward, finalNormal).normalized;
        }
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

        if (projectedForward.sqrMagnitude > 0)
        {
            Quaternion targetRotation = Quaternion.LookRotation(projectedForward, finalNormal);
            humanRb.MoveRotation(Quaternion.Slerp(humanRb.rotation, targetRotation, Time.fixedDeltaTime * 15f));
        }
    } // 움직임 관련 플레이어 및 카메라 처리
    
    IEnumerator MountRoutine()
    {
        isMounting = true;
        canMove = false;

        // 1. 화면 암전
        yield return StartCoroutine(uiManager.FadeScreen(0f, 1f, 1.0f));

        // [추가] Remount 시 특정 자식 오브젝트의 크기를 원래대로 복구
        if (targetChildTransform != null)
        {
            targetChildTransform.localScale = originalChildScale;
        }

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

        yield return new WaitForSeconds(0.5f);

        // 4. 화면 밝아짐
        yield return StartCoroutine(uiManager.FadeScreen(1f, 0f, 1.0f));

        isMounting = false;
        this.enabled = false;
    } // 휠체어 재탑승 루틴
    public void Mount(PlayerController targetWheelchair)
    {
        if (isMounting)
            return;

        // 전달받은 휠체어를 내 목표 휠체어로 설정!
        currentWheelchair = targetWheelchair;

        StartCoroutine(MountRoutine());
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
    } // 인간 관련 물리 ON/OFF
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
    } // 휠체어와 사람 충돌하지 않도록 처리

}