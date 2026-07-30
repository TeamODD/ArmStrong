using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerRagdollController : MonoBehaviour
{
    public enum PlayerState { OnWheelchair, Flying, FallenDown }

    [Header("State")]
    public PlayerState currentState = PlayerState.OnWheelchair;

    [Header("Flight Settings (살짝 떨어지는 연출)")]
    public float slightEjectForce = 2.5f;         // 부딪혔을 때 아주 살짝만 튕겨나갈 힘
    public float upwardForceRatio = 0.05f;        // 위로 뜨는 수치 (0에 가깝게)
    public float wallStickImpactThreshold = 0.5f; // 낮은 속도로 부딪혀도 바로 쓰러지도록 설정

    [Header("Mount Settings (조작 불가 & 시선 탑승)")]
    public float mountDistance = 5f;              // 바라보고 탑승 가능한 최대 거리
    public float rotateToWheelchairSpeed = 10f;   // 탑승 시 휠체어를 돌아보는 회전 속도

    private Rigidbody rb;
    private Collider col;
    private Camera mainCam;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        mainCam = Camera.main;

        // 초기 탑승 상태: 물리 및 콜라이더 꺼두기
        rb.isKinematic = true;
        if (col != null) col.enabled = false;
    }

    void Update()
    {
        switch (currentState)
        {
            case PlayerState.FallenDown:
                // 다리가 다쳐 이동 불가: 그 자리에서 화면으로 휠체어를 조준하고 E키 누름
                CheckLookAtWheelchairAndMount();
                break;
        }
    }

    // 1. 휠체어 충돌 시 아주 살짝 정면으로 떨어짐
    public void EjectAndFly(Vector3 forwardDirection)
    {
        currentState = PlayerState.Flying;

        if (col != null) col.enabled = true;
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.None;

        // [핵심 1] 휠체어와 동일하게 기존 관성을 완벽히 제거 (날아가는 궤적 안정화)
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // [핵심 2] 날아갈 방향 (앞으로 + 위로 조금)
        Vector3 flyDirection = (forwardDirection + Vector3.up * upwardForceRatio).normalized;

        // [핵심 3] 질량을 곱한 추진력 부여
        rb.AddForce(flyDirection * (slightEjectForce * rb.mass), ForceMode.Impulse);

        // [핵심 4] 휠체어가 넘어지듯, 플레이어도 앞으로 고꾸라지며 떨어지도록 토크(회전력) 추가
        // transform.right를 축으로 돌리면 앞으로 굴러떨어지는 느낌이 납니다.
        Vector3 tumbleTorque = transform.forward;
        Vector3 randomTorque = new Vector3(
            Random.Range(-2f, 2f),
            Random.Range(-2f, 2f),
            Random.Range(-2f, 2f)
        );

        rb.AddTorque((tumbleTorque + randomTorque) * rb.mass, ForceMode.Impulse);

        if (mainCam != null)
        {
            mainCam.GetComponent<PlayerCamera>().isDetached = true;
        }
    }

    // 2. 바닥이나 벽에 부딪히면 그 자리에 쓰러짐
    private void OnCollisionEnter(Collision collision)
    {
        if (currentState != PlayerState.Flying) return;

        // 방금 내린 휠체어와 닿은 것은 무시
        if (collision.gameObject.GetComponentInParent<PlayerController>() != null) return;

        if (collision.relativeVelocity.magnitude >= wallStickImpactThreshold)
        {
            currentState = PlayerState.FallenDown;

            // [핵심 5] 땅에 닿자마자 속도를 0으로 만들어버리면 움직임이 뚝 끊깁니다.
            // 아래의 속도 강제 초기화 코드를 제거하여 물리 엔진이 자연스럽게 미끄러지며 멈추게 둡니다.
            // rb.linearVelocity = Vector3.zero; 
            // rb.angularVelocity = Vector3.zero;

            // 대신, 바닥에서 너무 멀리 미끄러지지 않도록 저항(Damping)을 일시적으로 높여줍니다. (Unity 6 기준)
            rb.linearDamping = 3f;
            rb.angularDamping = 3f;

            Debug.Log("플레이어가 쓰러졌습니다! (화면으로 휠체어를 바라보고 E키를 누르세요)");
        }
    }

    // 3. 화면(시선)으로 휠체어를 조준하고 'E'키 클릭하여 재탑승
    void CheckLookAtWheelchairAndMount()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (mainCam == null) mainCam = Camera.main;
            if (mainCam == null) return;

            Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            // 수정됨: Raycast 대신 RaycastAll을 사용하여 내 몸을 관통해 뒤에 있는 휠체어까지 찾습니다.
            RaycastHit[] hits = Physics.RaycastAll(ray, mountDistance);

            foreach (RaycastHit hit in hits)
            {
                // 자기 자신(플레이어 콜라이더)이 레이에 맞은 건 무시하고 넘어감
                if (hit.collider == col) continue;

                PlayerController wheelchair = hit.collider.GetComponentInParent<PlayerController>();

                if (wheelchair != null)
                {
                    float distToWheelchair = Vector3.Distance(transform.position, wheelchair.transform.position);
                    if (distToWheelchair <= mountDistance)
                    {
                        RemountWheelchair(wheelchair);
                        return; // 탑승에 성공하면 레이캐스트 반복문 즉시 종료
                    }
                }
            }
        }
    }

    // 4. 휠체어 바라보며 재탑승 처리
    void RemountWheelchair(PlayerController wheelchair)
    {
        currentState = PlayerState.OnWheelchair;

        // 1) 휠체어 바라보는 방향으로 플레이어 몸체 회전
        Vector3 lookDir = (wheelchair.transform.position - transform.position).normalized;
        lookDir.y = 0; // Y축 회전만 적용
        if (lookDir != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookDir);
        }

        // 2) 휠체어 자식으로 다시 부착 및 위치 복원
        transform.SetParent(wheelchair.transform);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        // 3) 물리 및 콜라이더 비활성화
        rb.isKinematic = true;
        if (col != null) col.enabled = false;

        // 4) 휠체어 상태 및 직립 회전 복구
        wheelchair.isFallenOver = false;
        wheelchair.transform.rotation = Quaternion.Euler(0, wheelchair.transform.eulerAngles.y, 0);

        Rigidbody wheelchairRb = wheelchair.GetComponent<Rigidbody>();
        if (wheelchairRb != null)
        {
            wheelchairRb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        if (mainCam != null)
        {
            mainCam.GetComponent<PlayerCamera>().ResetView();
        }
        Debug.Log("휠체어를 바라보고 다시 탑승했습니다!");
    }
}