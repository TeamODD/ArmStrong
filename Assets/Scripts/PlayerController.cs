using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Speeds")]
    public float baseMoveSpeed = 2f;            // 기본 속도
    public float maxBonusSpeed = 6f;            // 최대 속도
    public float reverseMoveSpeed = 1.5f;       // 후진 속도
    public float rotationSpeed = 100f;          // 회전 속도

    [Header("Gauge Settings")]
    public float gaugeBuildUpPerPress = 15f;    // 클릭 당 게이지
    public float gaugeDecayRate = 30f;          // 게이지 줄어드는 속도
    public float maxGauge = 100f;               // 최대 게이지

    [Header("Inertia & Braking Settings")]
    public float normalDeceleration = 10f;      // 브레이크 감속 속도
    public float coastingDeceleration = 1f;     // 자연 감속 속도

    [Header("Crash / Fall Over Settings")]
    public float crashVelocityThreshold = 3.5f; // 넘어지는 최소 충돌 속도
    public bool isFallenOver = false;           // 넘어짐 상태 플래그

    [Header("Crash Physics")]
    public float randomSpin = 3f;               // 랜덤 회전

    [Header("Animation Settings")]
    public Animator anim;                   // 휠체어(또는 캐릭터)의 Animator 컴포넌트
    public float animSpeedMultiplier = 0.5f;    // 실제 속도를 애니메이션 속도(보통 1.0)에 맞게 조율하는 배율

    [Header("Cinematic Transition")]
    public Image fadeImage;                // 방금 만든 FadeImage 연결
    private bool isCrawling = false;       // 기어가는 연출 중인지 체크

    // 플레이어의 초기 위치를 기억해둘 변수
    private Vector3 initialPlayerLocalPos;
    private Quaternion initialPlayerLocalRot;
    private Animator playerAnim;           // 애니메이터 캐싱

    private float currentGauge = 0f;            // 현재 게이지
    private float currentSpeed = 0f;            // 현재 속도

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        playerAnim = GetComponentInChildren<Animator>();

        // 게임 시작 시 휠체어에 앉아있는 플레이어의 상대적 위치/회전값 저장
        if (playerAnim != null)
        {
            initialPlayerLocalPos = playerAnim.transform.localPosition;
            initialPlayerLocalRot = playerAnim.transform.localRotation;
        }
    }


    void Update()
    {
        // 1. 플레이어가 쓰러진 상태일 때
        if (isFallenOver)
        {
            // 아직 기어가는 중이 아니라면 E키 입력 확인
            if (!isCrawling)
            {
                CheckMountInput();
            }

            // ★ 여기서 return을 해줘야 쓰러져 있을 때 W,A,S,D로 휠체어가 움직이지 않습니다.
            return;
        }

        // 2. 쓰러지지 않은 평소 상태일 때 (기존 코드들)
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
        if (anim == null) return;

        float absSpeed = Mathf.Abs(currentSpeed);

        // 속도가 0.1 이상이면 움직이는 것으로 간주
        bool isMoving = absSpeed > baseMoveSpeed;

        // 애니메이터에게 현재 움직이고 있는지 상태를 알려줌
        anim.SetBool("IsMoving", isMoving);

        if (isMoving)
        {
            // 움직일 때는 원래 계산하신 대로 속도에 비례해서 배속 재생
            float speedRatio = absSpeed / baseMoveSpeed;
            anim.SetFloat("PushSpeed", speedRatio);
        }
        else
        {
            // ★ 핵심 포인트 ★
            // 멈췄을 때 배속을 0으로 만들면 애니메이션이 멈춰서 끝을 맺지 못합니다.
            // 그래서 멈추는 순간 배속을 1배속(정상 속도)으로 돌려주어, 
            // 하던 팔젓기 동작을 끝까지 마저 하고 대기 자세로 넘어가게 합니다.
            anim.SetFloat("PushSpeed", 1f);
        }
    }
    void HandleInput()
    {
        if (Keyboard.current == null) return;

        bool isWPressed = Keyboard.current.wKey.isPressed;
        bool isSPressed = Keyboard.current.sKey.isPressed;
        bool isSpacePressed = Keyboard.current.spaceKey.isPressed;

        // 1. W 연타 감지
        if (Keyboard.current.wKey.wasPressedThisFrame)
        {
            currentGauge += gaugeBuildUpPerPress;
            currentGauge = Mathf.Clamp(currentGauge, 0f, maxGauge);
        }

        // 2. 게이지 소모 처리
        if (currentGauge > 0)
        {
            currentGauge -= gaugeDecayRate * Time.deltaTime;
            currentGauge = Mathf.Max(currentGauge, 0f);
        }

        // 3. 속도 계산 로직
        if (isWPressed && !isSpacePressed)
        {
            float gaugeRatio = currentGauge / maxGauge;
            float targetSpeed = baseMoveSpeed + (maxBonusSpeed * gaugeRatio);

            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 10f);
        }
        else
        {
            if (isSpacePressed)     // 스페이스바 클릭 시
            {
                float currentDeceleration = (currentGauge > 5f) ? 3f : normalDeceleration;
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, currentDeceleration * Time.deltaTime);
            }
            else if (isSPressed)    // S 클릭 시
            {
                if (currentSpeed > 0f)
                {
                    float currentDeceleration = (currentGauge > 5f) ? 3f : normalDeceleration;
                    currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, currentDeceleration * Time.deltaTime);
                }
                else
                {
                    currentSpeed = Mathf.MoveTowards(currentSpeed, -reverseMoveSpeed, normalDeceleration * Time.deltaTime);
                }
            }
            else
            {
                if (currentSpeed > 0f)
                {
                    currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, coastingDeceleration * Time.deltaTime);
                }
                else if (currentSpeed < 0f)
                {
                    currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, coastingDeceleration * Time.deltaTime);
                }
            }
        }
    }

    void MovePlayer()
    {
        // 4. A, D 회전 처리
        float turnInput = 0f;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed) turnInput = -1f;
            if (Keyboard.current.dKey.isPressed) turnInput = 1f;
        }

        if (turnInput != 0f)
        {
            float turnAmount = turnInput * rotationSpeed * Time.fixedDeltaTime;
            Quaternion turnRotation = Quaternion.Euler(0f, turnAmount, 0f);
            rb.MoveRotation(rb.rotation * turnRotation);
        }

        // 5. 전진/후진 속도 적용
        Vector3 targetVelocity = transform.forward * currentSpeed;
        targetVelocity.y = rb.linearVelocity.y;

        rb.linearVelocity = targetVelocity;
    }

    // 6. 벽 강하게 부딪혔을 때 넘어지는 연출 (충돌 감지)
    private void OnCollisionEnter(Collision collision)
    {
        if (isFallenOver) return;

        if (collision.gameObject.CompareTag("Ground"))
        {
            return;
        }

        if (collision.relativeVelocity.magnitude < crashVelocityThreshold)
        {
            return;
        }

        FallOver(collision);
    }

    void FallOver(Collision collision)
    {
        isFallenOver = true;
        currentSpeed = 0f;

        // 물리 제약 해제 및 회전 저항(Damping) 설정
        rb.constraints = RigidbodyConstraints.None;
        rb.angularDamping = 2f;
        rb.linearDamping = 1f;

        // 1. 기존 속도 강제 초기화
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        ContactPoint contact = collision.contacts[0];
        Vector3 normal = contact.normal; // 벽에서 튕겨나오는 정면 방향

        // ★ 휠체어와 분리 및 애니메이션 실행
        EjectPlayer(normal);

        // 2. 휠체어 튕겨나가기 (플레이어와 안 겹치게 휠체어를 뒤로 살짝 밀어줌)
        Vector3 bounceDirection = (normal + (Vector3.up * 0.2f)).normalized;
        float bounceForce = 3.0f;
        rb.AddForce(bounceDirection * (bounceForce * rb.mass), ForceMode.Impulse);

        // 3. 휠체어 넘어뜨리기
        rb.ResetCenterOfMass();
        float tipDirection = Random.value > 0.5f ? 1f : -1f;
        Vector3 sideTipTorque = transform.forward * tipDirection * 15f;
        Vector3 randomTorque = new Vector3(
            Random.Range(-randomSpin, randomSpin) * 0.3f,
            Random.Range(-randomSpin, randomSpin) * 0.3f,
            Random.Range(-randomSpin, randomSpin) * 0.3f
        );
        rb.AddTorque((sideTipTorque + randomTorque) * rb.mass, ForceMode.Impulse);
    }

    void EjectPlayer(Vector3 normal)
    {
        Animator playerAnim = GetComponentInChildren<Animator>();

        if (playerAnim != null)
        {
            // 1. 휠체어와 플레이어의 콜라이더 가져오기 (기존 로직 유지)
            Collider[] wheelchairColliders = GetComponentsInChildren<Collider>();
            Collider[] playerColliders = playerAnim.GetComponentsInChildren<Collider>();

            // 2. 휠체어와 플레이어 겹침 방지 (일시적으로 충돌 무시)
            foreach (Collider wc in wheelchairColliders)
            {
                foreach (Collider pc in playerColliders)
                {
                    Physics.IgnoreCollision(wc, pc, true);
                }
            }

            // 3. 휠체어에서 플레이어 분리
            playerAnim.transform.SetParent(null);

            // ★ 3. 핵심: 플레이어를 벽 반대 방향(normal)으로 살짝 빼주기
            // offset 수치를 조절해서 벽을 안 뚫는 최적의 거리를 찾으세요 (예: 0.5f ~ 1.0f)
            float offsetDistance = 0.8f;

            // y축(높이)은 건드리지 않도록 normal의 y값을 0으로 만듭니다.
            Vector3 pushBackDir = new Vector3(normal.x, -0.55f, normal.z).normalized;
            playerAnim.transform.position += pushBackDir * offsetDistance;

            // 4. 루트 모션 켜기 (애니메이션 동작만큼 실제 좌표도 앞으로 이동하게 함)
            playerAnim.applyRootMotion = true;

            // 5. 쓰러지는 애니메이션 실행!
            playerAnim.SetTrigger("Fall");

            // 6. 카메라 시점 분리 (PlayerCamera.cs에 만들어둔 기능)
            if (Camera.main != null)
            {
                PlayerCamera pCam = Camera.main.GetComponent<PlayerCamera>();
                if (pCam != null) pCam.isDetached = true;
            }

            // 7. 쓰러지는 애니메이션이 끝날 즈음(예: 2초 뒤) 충돌을 다시 켜줌
            // 애니메이션 길이에 맞춰 delay 시간을 늘려주세요. (1초 -> 2초)
            StartCoroutine(RestoreCollision(wheelchairColliders, playerColliders, 1.0f));
        }
    }
    void CheckMountInput()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            Debug.Log("버튼눌림");
            // 화면 정중앙(카메라 시점)에서 앞으로 레이저를 쏩니다.
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            // 카메라에서 15f(거리) 이내에 무언가 맞았다면
            if (Physics.Raycast(ray, out RaycastHit hit, 15f))
            {
                Debug.Log("뭔가맞음");
                if (hit.collider.transform.IsChildOf(this.transform) || hit.collider.gameObject == this.gameObject)
                {
                    Debug.Log("휠체어임");

                    StartCoroutine(CrawlAndMountRoutine());
                }
            }
        }
    }
    // ★ 2. 기어가기 -> 암전 -> 탑승 연출 코루틴
    IEnumerator CrawlAndMountRoutine()
    {
        isCrawling = true;

        if (playerAnim != null)
        {
            // 1. 기어가는 애니메이션 즉시 실행
            playerAnim.SetTrigger("Crawl");

            // 2. 바닥 파고듦 방지 (Y축으로 살짝 올려주기)
            // 캐릭터가 땅에 파고드는 정도에 따라 0.3f 값을 조절하세요 (예: 0.2f ~ 0.5f)
            float heightOffset = 0.3f;
            playerAnim.transform.position += Vector3.up * heightOffset;

            // 회전 목표(휠체어 방향) 계산
            Vector3 lookDir = transform.position - playerAnim.transform.position;
            lookDir.y = 0;

            Quaternion startRot = playerAnim.transform.rotation;
            Quaternion targetRot = lookDir != Vector3.zero ? Quaternion.LookRotation(lookDir) : startRot;

            // --- 여기서부터 2초 동안 회전과 애니메이션을 동시에 진행합니다 ---
            float duration = 2.0f; // 총 기어가는 시간
            float turnDuration = 1.0f; // 몸을 완전히 돌리는 데 걸리는 시간
            float time = 0f;

            while (time < duration)
            {
                time += Time.deltaTime;

                // 1초(turnDuration) 동안만 부드럽게 회전 (Lerp/Slerp 사용)
                if (time < turnDuration)
                {
                    playerAnim.transform.rotation = Quaternion.Slerp(startRot, targetRot, time / turnDuration);
                }

                yield return null; // 다음 프레임까지 대기 (애니메이션은 계속 재생됨)
            }
        }

        // 화면 암전 (투명도 0 -> 1)
        yield return StartCoroutine(FadeScreen(0f, 1f, 1.0f));

        // 위치 리셋 및 ★속도/게이지 초기화★
        ResetToWheelchair();

        // 아주 잠깐 대기
        yield return new WaitForSeconds(0.5f);

        // 화면 밝아짐 (투명도 1 -> 0)
        yield return StartCoroutine(FadeScreen(1f, 0f, 1.0f));

        isFallenOver = false;
        isCrawling = false;
    }

    void ResetToWheelchair()
    {
        // 휠체어 똑바로 세우기
        transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);

        // ★ 3. 이동 속도 및 물리 엔진 관성 완전 초기화
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        currentSpeed = 0f;
        currentGauge = 0f;
        anim.SetFloat("PushSpeed", 0f);


        if (playerAnim != null)
        {
            playerAnim.transform.SetParent(this.transform);
            playerAnim.transform.localPosition = initialPlayerLocalPos;
            playerAnim.transform.localRotation = initialPlayerLocalRot;

            playerAnim.applyRootMotion = false;
            playerAnim.Play("Idle");
        }

        if (Camera.main != null)
        {
            PlayerCamera pCam = Camera.main.GetComponent<PlayerCamera>();
            if (pCam != null) pCam.isDetached = false;
        }
    }

    // ★ 4. 화면을 서서히 어둡게/밝게 해주는 UI 코루틴
    IEnumerator FadeScreen(float startAlpha, float endAlpha, float duration)
    {
        if (fadeImage == null) yield break;

        float time = 0;
        Color c = fadeImage.color;

        while (time < duration)
        {
            time += Time.deltaTime;
            // Lerp를 이용해 시간에 따라 투명도를 부드럽게 변경
            c.a = Mathf.Lerp(startAlpha, endAlpha, time / duration);
            fadeImage.color = c;
            yield return null; // 다음 프레임까지 대기
        }

        c.a = endAlpha;
        fadeImage.color = c;
    }

    // 충돌 무시를 해제하는 코루틴
    System.Collections.IEnumerator RestoreCollision(Collider[] wheelchairCols, Collider[] playerCols, float delay)
    {
        yield return new WaitForSeconds(delay);

        foreach (Collider wc in wheelchairCols)
        {
            foreach (Collider pc in playerCols)
            {
                // 그 사이 오브젝트가 파괴되었을 수도 있으니 null 체크
                if (wc != null && pc != null)
                {
                    Physics.IgnoreCollision(wc, pc, false);
                }
            }
        }
    }
}