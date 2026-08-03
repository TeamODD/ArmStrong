using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : MonoBehaviour
{
    [Header("Follow Target")]
    public Transform targetBody;           // PlayerModel 할당

    [Header("Look Settings")]
    public float mouseSensitivity = 1f;

    [Header("Angle Limits")]
    public float minVerticalAngle = -45f;
    public float maxVerticalAngle = 45f;
    public float maxHorizontalAngle = 85f;       // 휠체어 탑승 시 좌우 한계 각도
    public float crawlMaxHorizontalAngle = 60f;  // ★ 기어갈 때 좌우 한계 각도 (인스펙터에서 조절하세요)

    [Header("Camera Offsets")]
    public Vector3 crawlOffset = new Vector3(0f, 0.2f, 0.2f);
    public float transitionSpeed = 5f;

    public bool isDetached = false;

    private float absoluteYaw = 0f;
    private float absolutePitch = 0f;

    private Vector3 initialOffset;
    private Vector3 currentOffset;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (targetBody != null)
        {
            initialOffset = transform.localPosition;
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

        absolutePitch -= mouseY;
        absolutePitch = Mathf.Clamp(absolutePitch, minVerticalAngle, maxVerticalAngle);

        absoluteYaw += mouseX;

        // ★ 수정된 부분: 조건문 없이 항상 각도 제한을 하되, 한계값(Limit)만 상태에 따라 바꿉니다.
        float currentLimitAngle = isDetached ? crawlMaxHorizontalAngle : maxHorizontalAngle;
        float bodyYaw = targetBody.eulerAngles.y;

        float angleDifference = Mathf.DeltaAngle(bodyYaw, absoluteYaw);

        if (angleDifference > currentLimitAngle)
        {
            absoluteYaw = bodyYaw + currentLimitAngle;
        }
        else if (angleDifference < -currentLimitAngle)
        {
            absoluteYaw = bodyYaw - currentLimitAngle;
        }

        transform.rotation = Quaternion.Euler(absolutePitch, absoluteYaw, 0f);
    }

    public void ResetView()
    {
        isDetached = false;
        absoluteYaw = targetBody.eulerAngles.y;
        absolutePitch = 0f;
    }
}