using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody), typeof(Animator))]
public class CrawlingController : MonoBehaviour
{
    [Header("Crawling Setup")]
    [Range(0f, 0.5f)]
    public float turnSmoothTime = 0.1f; // 낮을수록 빨리 돌고, 높을수록 천천히 부드럽게 돕니다.
    public float mountableDistance = 2.0f;
    public float recoveryTime = 1.0f;

    [Header("UI Transition")]
    public Image fadeImage;

    private Rigidbody humanRb;
    private Animator anim;
    private Collider[] humanColliders;

    private PlayerController currentWheelchair;
    private Vector3 initialLocalPos;
    private Quaternion initialLocalRot;
    private bool isMounting = false;
    private bool canMove = false;

    private Camera mainCam;
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

        // ★ 2. 바닥에 딱 붙이기 위한 Raycast (레이저) 발사
        // 목표 위치보다 살짝 위(1.0f)에서 아래(Vector3.down)로 레이저를 쏴서 바닥을 찾습니다.
        if (Physics.Raycast(targetPos + Vector3.up * 1.0f, Vector3.down, out RaycastHit hit, 3.0f))
        {
            // 바닥을 감지했다면, 캐릭터의 Y(높이) 값을 바닥 위치에 완벽하게 일치시킵니다.
            targetPos.y = hit.point.y;

            // 만약 캐릭터가 바닥에 너무 파묻힌다면 이 수치를 살짝 올려주세요. (예: hit.point.y + 0.1f)
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
        if (Keyboard.current.sKey.isPressed) currentInput.y -= 1f;
        if (Keyboard.current.aKey.isPressed) currentInput.x -= 1f;
        if (Keyboard.current.dKey.isPressed) currentInput.x += 1f;

        bool isMoving = currentInput.sqrMagnitude > 0.01f;
        anim.SetBool("IsCrawlingMove", isMoving);
    }

    void MoveCharacterRelativeToCamera()
    {
        if (mainCam == null || !canMove) return; // ★ 여기서도 조작 불가 상태면 회전을 막습니다.

        Vector3 camForward = mainCam.transform.forward;
        Vector3 camRight = mainCam.transform.right;

        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = (camForward * currentInput.y + camRight * currentInput.x).normalized;

        if (moveDir.sqrMagnitude > 0.01f)
        {
            // ★ 이동은 루트 모션(애니메이션)이 알아서 하므로, 스크립트는 방향에 맞게 회전만 시켜줍니다.
            float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            humanRb.MoveRotation(Quaternion.Euler(0f, angle, 0f));
        }
    }

    void CheckMountInput()
    {
        if (currentWheelchair == null || !canMove) return; // ★ 일어나기 전에는 탑승도 불가능하게 처리

        float distance = Vector3.Distance(transform.position, currentWheelchair.transform.position);

        if (distance <= mountableDistance)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                StartCoroutine(MountRoutine());
            }
        }
    }

    IEnumerator MountRoutine()
    {
        isMounting = true;
        canMove = false;

        yield return StartCoroutine(FadeScreen(0f, 1f, 1.0f));

        SetPhysicsEnabled(false);
        transform.SetParent(currentWheelchair.transform);
        transform.localPosition = initialLocalPos;
        transform.localRotation = initialLocalRot;

        // ★ 핵심 변경: 다시 휠체어에 타면 루트 모션을 끕니다!
        anim.applyRootMotion = false;
        anim.Play("Idle");

        if (mainCam != null)
        {
            PlayerCamera pCam = mainCam.GetComponent<PlayerCamera>();
            if (pCam != null) pCam.isDetached = false;
        }

        currentWheelchair.ResetWheelchairPhysics();

        yield return new WaitForSeconds(0.5f);

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