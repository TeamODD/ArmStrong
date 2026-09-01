using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Human Setup")]
    public CrawlingController humanController;

    [Header("Movement Speeds")]
    public float rotationSpeed = 100f;

    [Header("Crash / Fall Over Settings")]
    public float crashVelocityThreshold = 3.5f;
    public bool isFallenOver = false;

    [Header("Crash Physics")]
    public float randomSpin = 3f;

    [Header("Wheelchair Animation")]
    public Animator wheelchairAnim;

    [Header("Slope Alignment Settings")]
    public float alignSpeed = 10f;
    public float rayDistance = 1.0f; // 휠체어 피벗 위치에 따라 0.5f ~ 1.0f 등 조절 필요
    public LayerMask groundLayer; // 인스펙터에서 바닥 레이어 할당 필수

    [Tooltip("휠체어 중심에서 앞바퀴 레이까지의 거리")]
    public float frontRayOffset = 0.5f;
    [Tooltip("휠체어 중심에서 뒷바퀴 레이까지의 거리")]
    public float backRayOffset = 0.5f;
    [Tooltip("레이 시작 높이 (바닥에서부터)")]
    public float rayHeight = 0.5f;

    [Header("Slope Sliding Settings")]
    public float slipThresholdAngle = 5f; // 이 각도 이상의 경사에서만 미끄러짐 (아주 미세한 언덕 무시)
    public float maxSlipSpeed = 8f;       // 최대 미끄러짐 속도
    public float slipAcceleration = 5f;   // 미끄러지는 가속도 (경사가 가파를수록 이 수치를 기반으로 더 빨리 빨라짐)

    [Header("Wheelchair Push System")]
    public float gaugeBuildUpRate = 40f; // 꾹 누르고 있을 때 초당 게이지 차오르는 속도 (2.5초면 MAX)
    public float gaugeDecayRate = 20f;   // 뗐을 때 초당 게이지 줄어드는 속도
    public float maxGauge = 100f;
    public float gaugeBuildUpPerPress = 15f;
    private float currentGauge = 0f;

    [Header("Push Physics")]
    public float basePushForce = 10f;    // 게이지 0일 때 기본 미는 힘
    public float maxPushForce = 30f;     // 게이지 MAX일 때 최대 미는 힘
    public float baseMaxSpeed = 2f;      // 처음 출발할 때 제한 속도
    public float boostMaxSpeed = 8f;     // 최고 속도

    [Header("Real Physics Settings")]
    public float pushForce = 50f;          // W 연타 시 밀어내는 기본 힘
    public float maxPhysicSpeed = 8f;      // 평지에서의 최대 속도
    public float coastingDrag = 0.1f;        // 가만히 있을 때 자연스럽게 멈추는 저항(마찰력)
    public float brakeDrag = 0.4f;          // 브레이크(스페이스바) 밟았을 때의 저항

    private Rigidbody rb;
    private bool isGrounded = false; // 현재 바닥에 닿아있는지 체크

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.centerOfMass = new Vector3(0, -1.0f, 0);
    }
    void Update()
    {
        if (isFallenOver) return;

        HandleInput();
        UpdateAnimation();
    }
    void FixedUpdate()
    {
        if (isFallenOver) return;
        MovePlayer();
    }

    void UpdateAnimation()
    {
        if (wheelchairAnim == null) return;

        Vector3 flatVelocity = rb.linearVelocity;
        flatVelocity.y = 0f;
        float actualSpeed = flatVelocity.magnitude;

        bool isMoving = actualSpeed >= 0.2f; // 조금 더 민감하게 바퀴가 굴러가도록 기준을 낮춤
        wheelchairAnim.SetBool("IsMoving", isMoving);

        if (isMoving)
        {
            float animSpeedRatio = actualSpeed / baseMaxSpeed;
            animSpeedRatio = Mathf.Clamp(animSpeedRatio, 0.5f, 2.5f);

            wheelchairAnim.SetFloat("PushSpeed", animSpeedRatio);
        }
        else
        {
            wheelchairAnim.SetFloat("PushSpeed", 1f);
        }
    }  // 플레이어 손 관련 애니메이션
    void HandleInput()
    {
        if (Keyboard.current == null) return;
        bool isSpacePressed = Keyboard.current.spaceKey.isPressed;

        if (isSpacePressed)
        {
            currentGauge = Mathf.MoveTowards(currentGauge, 0f, maxGauge * 10f * Time.deltaTime);
        }
        else
        {
            // 연타를 할 때만 게이지를 상승시킵니다.
            if (Keyboard.current.wKey.wasPressedThisFrame)
            {
                currentGauge = Mathf.Clamp(currentGauge + gaugeBuildUpPerPress, 0f, maxGauge);
            }

            float fastDecay = gaugeDecayRate * 2.5f;
            currentGauge = Mathf.Max(currentGauge - (fastDecay * Time.deltaTime), 0f);
        }
    } // W 연타 게이지 관련
    void MovePlayer()
    {
        // 1. 좌우 회전 및 지형 기울기 맞춤
        float turnInput = 0f;
        if (Keyboard.current.aKey.isPressed) turnInput = -1f;
        if (Keyboard.current.dKey.isPressed) turnInput = 1f;

        if (turnInput != 0f)
        {
            float turnAmount = turnInput * rotationSpeed * Time.fixedDeltaTime;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turnAmount, 0f));
        }

        AlignToGroundMultiRay();

        bool isWPressed = Keyboard.current.wKey.isPressed;
        bool isSPressed = Keyboard.current.sKey.isPressed;
        bool isSpacePressed = Keyboard.current.spaceKey.isPressed;

        // 2. 물리 적용 및 속도 제한 로직
        if (isSpacePressed)
        {
            rb.linearDamping = brakeDrag;
            if (isGrounded && rb.linearVelocity.sqrMagnitude < 0.2f)
            {
                rb.linearVelocity = Vector3.zero;
            }
        }
        else if (isWPressed)
        {
            rb.linearDamping = coastingDrag;

            if (currentGauge > 0.1f) // 연타를 해서 게이지가 쌓인 상태 (과부하 모드)
            {
                float gaugeRatio = currentGauge / maxGauge;
                float currentMaxSpeed = Mathf.Lerp(baseMaxSpeed, boostMaxSpeed, gaugeRatio);
                float currentPushPower = Mathf.Lerp(basePushForce, maxPushForce, gaugeRatio);

                rb.AddForce(transform.forward * currentPushPower, ForceMode.Acceleration);

                if (rb.linearVelocity.magnitude > currentMaxSpeed)
                    rb.linearVelocity = rb.linearVelocity.normalized * currentMaxSpeed;
            }
            else // 연타를 안 해서 게이지가 없는 상태 (정속 주행 모드)
            {
                // 정속 주행 시에는 basePushForce만 사용하고, baseMaxSpeed까지만 제한합니다.
                rb.AddForce(transform.forward * basePushForce, ForceMode.Acceleration);

                if (rb.linearVelocity.magnitude > baseMaxSpeed)
                    rb.linearVelocity = rb.linearVelocity.normalized * baseMaxSpeed;
            }
        }
        else if (isSPressed) // ★ 후진: 전진 중일 때는 브레이크, 멈추면 후진
        {
            // 휠체어가 현재 앞을 향해 이동 중인 속도를 구합니다. (후진 중이면 마이너스 값이 나옴)
            float forwardVelocity = Vector3.Dot(rb.linearVelocity, transform.forward);

            // 1. 아직 앞으로 가고 있는 중일 때 (속도가 남아있을 때)
            if (forwardVelocity > baseMaxSpeed - 0.3f)
            {
                // 브레이크 저항을 적용하여 서서히 멈추게 합니다.
                rb.linearDamping = brakeDrag;
            }
            // 2. 전진 속도가 거의 다 줄어들었거나 이미 후진 중일 때
            else
            {
                rb.linearDamping = coastingDrag;

                rb.AddForce(-transform.forward * basePushForce, ForceMode.Acceleration);

                if (rb.linearVelocity.magnitude > baseMaxSpeed)
                {
                    rb.linearVelocity = rb.linearVelocity.normalized * baseMaxSpeed;
                }
            }
        }
        else
        {
            rb.linearDamping = coastingDrag;
        }

        if (isGrounded)
        {
            Vector3 rightDir = transform.right;
            float lateralSpeed = Vector3.Dot(rb.linearVelocity, rightDir);

            rb.AddForce(-rightDir * lateralSpeed, ForceMode.VelocityChange);
        }
    } // 플레이어 움직임 관련
    void AlignToGroundMultiRay()
    {

        // 1. 앞뒤 레이 시작 지점 계산
        Vector3 frontRayOrigin = transform.position + (transform.forward * frontRayOffset) + (Vector3.up * rayHeight);
        Vector3 backRayOrigin = transform.position - (transform.forward * backRayOffset) + (Vector3.up * rayHeight);

        // 디버그용 기본 레이 (빨간색)
        Debug.DrawRay(frontRayOrigin, Vector3.down * rayDistance, Color.red);
        Debug.DrawRay(backRayOrigin, Vector3.down * rayDistance, Color.red);

        RaycastHit frontHit, backHit;
        // 2. 앞뒤로 레이를 쏩니다.
        bool frontGrounded = Physics.Raycast(frontRayOrigin, Vector3.down, out frontHit, rayDistance, groundLayer);
        bool backGrounded = Physics.Raycast(backRayOrigin, Vector3.down, out backHit, rayDistance, groundLayer);

        Vector3 currentGroundNormal = Vector3.up; // 현재 밟고 있는 바닥의 기울기(법선)

        // 3. 둘 다 바닥에 닿았을 때가 가장 정확한 경사도를 얻을 수 있습니다.
        if (frontGrounded && backGrounded)
        {
            isGrounded = true;

            // [디버그] 바닥에 닿은 레이 (초록색)
            Debug.DrawRay(frontRayOrigin, Vector3.down * frontHit.distance, Color.green);
            Debug.DrawRay(backRayOrigin, Vector3.down * backHit.distance, Color.green);

            // 핵심 로직: 뒷바퀴 닿은 점 -> 앞바퀴 닿은 점을 향하는 벡터가 
            // 바로 경사로 표면의 실제 앞방향(Forward) 벡터가 됩니다.
            Vector3 surfaceForward = (frontHit.point - backHit.point).normalized;

            // 두 지점의 법선 벡터(Normal)를 평균내어 더 정확한 Up 벡터를 구합니다.
            Vector3 avgNormal = (frontHit.normal + backHit.normal).normalized;
            currentGroundNormal = avgNormal;    

            // 4. 표면 앞방향과 평균 Up 벡터를 이용해 목표 회전값 계산
            Quaternion targetRotation = Quaternion.LookRotation(surfaceForward, avgNormal);

            // 5. 부드러운 회전 적용
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * alignSpeed));
        }
        // 예외 처리: 한쪽만 닿았거나 둘 다 안 닿았을 때
        else if (frontGrounded) // 앞바퀴만 닿음 (낭떠러지 진입 등)
        {
            currentGroundNormal = frontHit.normal;
            isGrounded = true;
            FixRotationSingleRay(frontHit);
        }
        else if (backGrounded) // 뒷바퀴만 닿음
        {
            isGrounded = true;
            FixRotationSingleRay(backHit);
        }
        else // 둘 다 공중
        {
            isGrounded = false;
        }
    } // 경사에 따른 휠체어 오브젝트 기울기
    void FixRotationSingleRay(RaycastHit hit)
    {
        Vector3 projectedForward = Vector3.ProjectOnPlane(transform.forward, hit.normal).normalized;
        if (projectedForward.sqrMagnitude > 0)
        {
            Quaternion targetRotation = Quaternion.LookRotation(projectedForward, hit.normal);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * alignSpeed));
        }
    } // 보조 휠체어 기울기
    private void OnCollisionEnter(Collision collision)
    {
        if (isFallenOver || collision.gameObject.CompareTag("Ground")) return;
        if (collision.relativeVelocity.magnitude < crashVelocityThreshold) return;

        FallOver(collision);
    } // 벽에 닿았을 시
    void FallOver(Collision collision)
    {
        isFallenOver = true;

        rb.constraints = RigidbodyConstraints.None;
        rb.angularDamping = 2f;
        rb.linearDamping = 1f;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector3 normal = collision.contacts[0].normal;

        if (humanController != null)
        {
            humanController.EjectFromWheelchair(normal, this);
        }

        Vector3 bounceDirection = (normal + (Vector3.up * 0.2f)).normalized;
        rb.AddForce(bounceDirection * (3.0f * rb.mass), ForceMode.Impulse);
        rb.ResetCenterOfMass();
        Vector3 randomTorque = new Vector3(
            Random.Range(-randomSpin, randomSpin) * 0.3f,
            Random.Range(-randomSpin, randomSpin) * 0.3f,
            Random.Range(-randomSpin, randomSpin) * 0.3f
        );
        rb.AddTorque(((transform.forward * (Random.value > 0.5f ? 1f : -1f) * 15f) + randomTorque) * rb.mass, ForceMode.Impulse);
    } // 휠체어에서 추락 후 휠체어 튕겨남
    public void ResetWheelchairPhysics()
    {
        transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        currentGauge = 0f;
        isFallenOver = false;
        isGrounded = false;
    } // 휠체어 관련 물리 초기화
}