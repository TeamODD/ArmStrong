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
    public float maxHorizontalAngle = 85f; // 탑승 시 좌우 한계 각도

    public bool isDetached = false;        // 쓰러져 있는지 여부

    // ★ 로컬이 아닌 '월드(World)' 기준의 절대 각도로 변경
    private float absoluteYaw = 0f;
    private float absolutePitch = 0f;

    private Vector3 initialOffset;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (targetBody != null)
        {
            initialOffset = transform.localPosition;
            transform.SetParent(null); // 자식에서 분리

            // 게임 시작 시, 카메라가 휠체어의 정면을 바라보도록 초기화
            absoluteYaw = targetBody.eulerAngles.y;
        }
    }

    void LateUpdate()
    {
        if (targetBody == null) return;

        // 위치 따라가기 (항상 플레이어 몸체의 위치 기준)
        transform.position = targetBody.position + targetBody.TransformDirection(initialOffset);

        HandleCameraLook();
    }

    void HandleCameraLook()
    {
        if (Mouse.current == null) return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        float mouseX = mouseDelta.x * mouseSensitivity * 0.1f;
        float mouseY = mouseDelta.y * mouseSensitivity * 0.1f;

        // 1. 상하 회전 (절대 각도)
        absolutePitch -= mouseY;
        absolutePitch = Mathf.Clamp(absolutePitch, minVerticalAngle, maxVerticalAngle);

        // 2. 좌우 회전 (절대 각도)
        absoluteYaw += mouseX;

        // ★ 휠체어에 타고 있을 때만 85도 제한 작동
        if (!isDetached)
        {
            float bodyYaw = targetBody.eulerAngles.y; // 휠체어(몸체)가 바라보는 절대 각도

            // 몸체 방향과 현재 카메라 시선의 각도 차이 계산 (-180 ~ 180도)
            float angleDifference = Mathf.DeltaAngle(bodyYaw, absoluteYaw);

            // 차이가 85도를 넘어가면, 카메라를 85도 위치에 고정시켜 휠체어가 억지로 밀고 가도록 처리
            if (angleDifference > maxHorizontalAngle)
            {
                absoluteYaw = bodyYaw + maxHorizontalAngle;
            }
            else if (angleDifference < -maxHorizontalAngle)
            {
                absoluteYaw = bodyYaw - maxHorizontalAngle;
            }
        }

        // 3. 최종 회전 적용 (휠체어의 회전을 곱하지 않고 독립적으로 적용)
        transform.rotation = Quaternion.Euler(absolutePitch, absoluteYaw, 0f);
    }

    // 휠체어에 다시 탔을 때 고개를 정면으로 초기화
    public void ResetView()
    {
        isDetached = false;
        absoluteYaw = targetBody.eulerAngles.y; // 휠체어 정면으로 시선 정렬
        absolutePitch = 0f;
    }
}