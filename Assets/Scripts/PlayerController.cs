using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Human Setup")]
    public CrawlingController humanController;  // 분리한 Human 스크립트 연결

    [Header("Movement Speeds")]
    public float baseMoveSpeed = 2f;
    public float maxBonusSpeed = 6f;
    public float reverseMoveSpeed = 1.5f;
    public float rotationSpeed = 100f;

    [Header("Gauge Settings")]
    public float gaugeBuildUpPerPress = 15f;
    public float gaugeDecayRate = 30f;
    public float maxGauge = 100f;

    [Header("Inertia & Braking Settings")]
    public float normalDeceleration = 10f;
    public float coastingDeceleration = 1f;

    [Header("Crash / Fall Over Settings")]
    public float crashVelocityThreshold = 3.5f;
    public bool isFallenOver = false;

    [Header("Crash Physics")]
    public float randomSpin = 3f;

    [Header("Wheelchair Animation")]
    public Animator wheelchairAnim; // 휠체어 자체 애니메이터가 있다면 할당 (없으면 비워둬도 됨)

    private float currentGauge = 0f;
    private float currentSpeed = 0f;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        if (isFallenOver) return; // 쓰러진 상태면 휠체어 자체 업데이트 중지

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
        float absSpeed = Mathf.Abs(currentSpeed);
        bool isMoving = absSpeed >= 1f;
        wheelchairAnim.SetBool("IsMoving", isMoving);
        wheelchairAnim.SetFloat("PushSpeed", isMoving ? (absSpeed / baseMoveSpeed) : 1f);
    }

    void HandleInput()
    {
        if (Keyboard.current == null) return;

        bool isWPressed = Keyboard.current.wKey.isPressed;
        bool isSPressed = Keyboard.current.sKey.isPressed;
        bool isSpacePressed = Keyboard.current.spaceKey.isPressed;

        if (Keyboard.current.wKey.wasPressedThisFrame)
        {
            currentGauge = Mathf.Clamp(currentGauge + gaugeBuildUpPerPress, 0f, maxGauge);
        }

        if (currentGauge > 0)
        {
            currentGauge = Mathf.Max(currentGauge - (gaugeDecayRate * Time.deltaTime), 0f);
        }

        if (currentSpeed == 0f) currentGauge = 0f;

        if (isWPressed && !isSpacePressed)
        {
            float targetSpeed = baseMoveSpeed + (maxBonusSpeed * (currentGauge / maxGauge));
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 10f);
        }
        else
        {
            if (isSpacePressed)
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, ((currentGauge > 5f) ? 3f : normalDeceleration) * Time.deltaTime);
            }
            else if (isSPressed)
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, currentSpeed > 0f ? 0f : -reverseMoveSpeed, normalDeceleration * Time.deltaTime);
            }
            else
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, coastingDeceleration * Time.deltaTime);
            }
        }
    }

    void MovePlayer()
    {
        float turnInput = 0f;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed) turnInput = -1f;
            if (Keyboard.current.dKey.isPressed) turnInput = 1f;
        }

        if (turnInput != 0f)
        {
            float turnAmount = turnInput * rotationSpeed * Time.fixedDeltaTime;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turnAmount, 0f));
        }

        Vector3 targetVelocity = transform.forward * currentSpeed;
        targetVelocity.y = rb.linearVelocity.y;
        rb.linearVelocity = targetVelocity;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isFallenOver || collision.gameObject.CompareTag("Ground")) return;
        if (collision.relativeVelocity.magnitude < crashVelocityThreshold) return;

        FallOver(collision);
    }

    void FallOver(Collision collision)
    {
        isFallenOver = true;
        currentSpeed = 0f;

        rb.constraints = RigidbodyConstraints.None;
        rb.angularDamping = 2f;
        rb.linearDamping = 1f;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector3 normal = collision.contacts[0].normal;

        // ★ 캐릭터 스크립트로 튕겨나가는 이벤트 전달
        if (humanController != null)
        {
            humanController.EjectFromWheelchair(normal, this);
        }

        // 휠체어 튕겨나가기
        Vector3 bounceDirection = (normal + (Vector3.up * 0.2f)).normalized;
        rb.AddForce(bounceDirection * (3.0f * rb.mass), ForceMode.Impulse);
        rb.ResetCenterOfMass();
        Vector3 randomTorque = new Vector3(
            Random.Range(-randomSpin, randomSpin) * 0.3f,
            Random.Range(-randomSpin, randomSpin) * 0.3f,
            Random.Range(-randomSpin, randomSpin) * 0.3f
        );
        rb.AddTorque(((transform.forward * (Random.value > 0.5f ? 1f : -1f) * 15f) + randomTorque) * rb.mass, ForceMode.Impulse);
    }

    // ★ 캐릭터가 휠체어에 탑승 완료했을 때 호출될 초기화 함수
    public void ResetWheelchairPhysics()
    {
        transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        currentSpeed = 0f;
        currentGauge = 0f;
        isFallenOver = false;
    }
}