using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : MonoBehaviour
{
    [Header("Follow Target")]
    public Transform targetBody;           // PlayerModel 할당

    [Header("Look Settings")]
    public float mouseSensitivity = 1f;

    [Header("Angle Limits")]
    public float minVerticalAngle = -50f;
    public float maxVerticalAngle = 50f;
    public float maxHorizontalAngle = 70f;       // 휠체어 탑승 시 좌우 한계 각도
    public float crawlMaxHorizontalAngle = 60f;  // 기어갈 때 좌우 한계 각도

    [Header("Camera Offsets")]
    public Vector3 crawlOffset = new Vector3(0f, 0.35f, 0.5f);
    public Vector3 sitOffset = new Vector3(0f, -0.35f, -0.5f);
    public float transitionSpeed = 5f;
    public bool isDetached = false;
    public bool isHiding = false;

    private float absoluteYaw = 0f;
    private float absolutePitch = 0f;

    private float hidingBaseYaw = 0f;

    private Vector3 initialOffset;
    private Vector3 currentOffset;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (targetBody != null)
        {
            initialOffset = transform.localPosition + sitOffset;
            currentOffset = initialOffset;
            transform.SetParent(null);
            absoluteYaw = targetBody.eulerAngles.y;
        }
    }
    void LateUpdate()
    {
        if (targetBody == null) return;

        // 1. 상태에 따른 목표 오프셋 결정
        Vector3 targetOffset = isDetached ? crawlOffset : initialOffset;

        // 2. 부드러운 오프셋 전환
        currentOffset = Vector3.Lerp(currentOffset, targetOffset, Time.deltaTime * transitionSpeed);

        // 3. 카메라 위치 적용
        transform.position = targetBody.position + targetBody.TransformDirection(currentOffset);

        HandleCameraLook();
    }
    void HandleCameraLook()
    {
        if (Mouse.current == null) return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float mouseX = mouseDelta.x * mouseSensitivity * 0.1f;
        float mouseY = mouseDelta.y * mouseSensitivity * 0.1f;

        // ========================================
        // 침대 밑에 숨은 상태
        // ========================================
        if (isHiding)
        {
            // 상하 움직임은 기존과 동일
            absolutePitch -= mouseY;

            absolutePitch = Mathf.Clamp(
                absolutePitch,
                minVerticalAngle,
                maxVerticalAngle
            );

            // 좌우 움직임
            absoluteYaw += mouseX;

            // HidePoint 정면 기준 ±90도
            float angleDifference =
                Mathf.DeltaAngle(hidingBaseYaw, absoluteYaw);

            if (angleDifference > 80f)
            {
                absoluteYaw = hidingBaseYaw + 80f;
            }
            else if (angleDifference < -80f)
            {
                absoluteYaw = hidingBaseYaw - 80f;
            }

            transform.rotation =
                Quaternion.Euler(
                    absolutePitch,
                    absoluteYaw,
                    0f
                );

            return;
        }

        // ========================================
        // 일반 상태
        // ========================================

        absolutePitch -= mouseY;

        absolutePitch = Mathf.Clamp(
            absolutePitch,
            minVerticalAngle,
            maxVerticalAngle
        );

        absoluteYaw += mouseX;

        float currentLimitAngle =
            isDetached
            ? crawlMaxHorizontalAngle
            : maxHorizontalAngle;

        float bodyYaw = targetBody.eulerAngles.y;

        float angleDifferenceNormal =
            Mathf.DeltaAngle(bodyYaw, absoluteYaw);

        if (angleDifferenceNormal > currentLimitAngle)
        {
            absoluteYaw =
                bodyYaw + currentLimitAngle;
        }
        else if (angleDifferenceNormal < -currentLimitAngle)
        {
            absoluteYaw =
                bodyYaw - currentLimitAngle;
        }

        transform.rotation =
            Quaternion.Euler(
                absolutePitch,
                absoluteYaw,
                0f
            );
    }
    public void SetHidingView(Transform hidePoint)
    {
        if (hidePoint == null)
            return;

        isHiding = true;
        isDetached = true;

        Vector3 forward = hidePoint.forward;

        hidingBaseYaw =
            Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;

        absoluteYaw = hidingBaseYaw;
        absolutePitch = 0f;

        // 카메라 위치를 즉시 적용
        currentOffset = crawlOffset;

        transform.position =
            targetBody.position +
            targetBody.TransformDirection(currentOffset);

        // 카메라 회전도 즉시 적용
        transform.rotation =
            Quaternion.Euler(absolutePitch, absoluteYaw, 0f);
    }

    public void ResetView()
    {
        isDetached = false;
        isHiding = false;

        absoluteYaw = targetBody.eulerAngles.y;
        absolutePitch = 0f;
    }
}