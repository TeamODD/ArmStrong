    using System.Collections;
    using UnityEngine;
    using UnityEngine.AI;

    public class MonsterAI : MonoBehaviour
    {
        [Header("Patrol")]
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private float arrivalDistance = 0.5f;
        [SerializeField] private float idleDuration = 3f;

        [Header("Animation")]
        [SerializeField] private Animator animator;

        [Header("Vision")]
        [SerializeField] private Transform player;
        [SerializeField] private Transform wheelchair;

        [SerializeField] private float viewDistance = 15f;
        [SerializeField] private float viewAngle = 120f;
        [SerializeField] private float eyeHeight = 1.5f;
        [SerializeField] private float lookRotationSpeed = 360f;

        [Header("Chase")]
        [SerializeField] private float patrolSpeed = 0.5f;
        [SerializeField] private float chaseSpeed = 2f;
        [Header("Lost Target")]
        [SerializeField] private float lostSightGraceTime = 1f;

        private float lastTimePlayerSeen;
        private Vector3 lastSeenPosition;
        private Vector3 lastKnownDirection = Vector3.forward;

        // 상태 관리 변수들
        private bool isDetected = false;
        private bool isLookingAtPlayer = false;
        private bool isChasing = false;
        private bool isInvestigating = false;
        private bool isLookingAround = false; // 추가: 주변을 두리번거리는 상태

        private NavMeshAgent agent;
        private int currentPoint = 0;
        private bool isWaiting = false;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();

            if (animator == null)
                animator = GetComponent<Animator>();
        }

        private void Start()
        {
            if (patrolPoints.Length == 0)
                return;

            MoveToNextPoint();
        }

        private void Update()
        {
            // 평상시 미발견 상태일 때 감지
            if (!isDetected)
            {
                if (CanSeePlayer())
                {
                    OnPlayerDetected();
                }
            }

            // 플레이어를 처음 발견하고 비명지르기 위해 바라보는 중
            if (isLookingAtPlayer)
            {
                LookAtPlayer();
                return;
            }

            // 추적 중
            if (isChasing)
            {
                ChasePlayer();
                return;
            }

            // 놓친 지점으로 달려가며 조사 중
            if (isInvestigating)
            {
                InvestigateLastSeenPosition();
                return;
            }

            // 도착 후 제자리에서 두리번거리는 중 (Update에서는 아무것도 안하고 코루틴이 처리)
            if (isLookingAround)
            {
                return;
            }

            // 기존 순찰
            if (patrolPoints.Length == 0 || isWaiting)
                return;

            if (!agent.pathPending &&
                agent.remainingDistance <= arrivalDistance)
            {
                StartCoroutine(WaitAtPoint());
            }
        }

        private bool IsPlayerOnWheelchair()
        {
            return player.IsChildOf(wheelchair);
        }

        private void MoveToNextPoint()
        {
            agent.speed = patrolSpeed;
            agent.isStopped = false;
            agent.SetDestination(patrolPoints[currentPoint].position);
            animator.SetBool("IsWalking", true);

            currentPoint++;
            if (currentPoint >= patrolPoints.Length)
            {
                currentPoint = 0;
            }
        }

        private IEnumerator WaitAtPoint()
        {
            isWaiting = true;
            agent.isStopped = true;

            while (agent.velocity.sqrMagnitude > 0.01f)
            {
                yield return null;
            }

            animator.SetBool("IsWalking", false);
            yield return new WaitForSeconds(idleDuration);

            isWaiting = false;
            MoveToNextPoint();
        }

        private void OnPlayerDetected()
        {
            isDetected = true;
            isLookingAtPlayer = true;

            Transform target = IsPlayerOnWheelchair() ? wheelchair : player;
            lastSeenPosition = target.position;
            lastTimePlayerSeen = Time.time;

            agent.isStopped = true;
            animator.SetBool("IsWalking", false);

            Debug.Log("플레이어 발견!");
        }

        private void LookAtPlayer()
        {
            Transform visionTarget = IsPlayerOnWheelchair() ? wheelchair : player;
            Vector3 direction = visionTarget.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                lookRotationSpeed * Time.deltaTime
            );

            if (Quaternion.Angle(transform.rotation, targetRotation) < 1f)
            {
                transform.rotation = targetRotation;
                isLookingAtPlayer = false;
                StartCoroutine(ScreamAndStartChase());
            }
        }
        private IEnumerator ScreamAndStartChase()
        {
            Debug.Log("Scream!");

            // 추가: 애니메이터의 Scream 트리거 작동
            animator.SetTrigger("Scream");

            float screamDuration = 2.5f;
            float elapsedTime = 0f;

            while (elapsedTime < screamDuration)
            {
                if (CanSeePlayer())
                {
                    Transform target = IsPlayerOnWheelchair() ? wheelchair : player;
                    UpdateLastSeen(target.position);
                    lastTimePlayerSeen = Time.time;
                }
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            ResumeChase();
        }

        // 추적 상태를 켜는 공통 메서드 (놓쳤다가 다시 찾았을 때 재사용)
        private void ResumeChase()
        {
            isChasing = true;
            isInvestigating = false;
            isLookingAround = false;

            agent.speed = chaseSpeed;
            agent.isStopped = false;

            animator.SetBool("IsRunning", true);
            animator.SetBool("IsWalking", false);
            Debug.Log("추적 재개!");
        }

        private void ChasePlayer()
        {
            Transform target = IsPlayerOnWheelchair() ? wheelchair : player;

            if (CanSeePlayer())
            {
                // 실제로 플레이어를 보고 있음
                lastTimePlayerSeen = Time.time;

                UpdateLastSeen(target.position);
                agent.isStopped = false;
                agent.SetDestination(target.position);
            }
            else
            {
                // 마지막으로 실제 시야에서 본 이후 지난 시간
                float timeSinceLastSeen = Time.time - lastTimePlayerSeen;

                // ★ 시야에서 사라져도 일정 시간 동안은
                // 플레이어의 현재 위치를 계속 알고 추적
                if (timeSinceLastSeen <= lostSightGraceTime)
                {
                    // 플레이어의 실시간 위치로 이동
                    UpdateLastSeen(target.position);
                    agent.isStopped = false;
                    agent.SetDestination(target.position);

                    Debug.Log("시야 밖이지만 플레이어 위치를 계속 추적 중...");
                }
                else
                {
                    // 1초가 지나면 그때부터 마지막 위치만 기억
                    isChasing = false;
                    isInvestigating = true;

                    agent.isStopped = false;
                    agent.SetDestination(lastSeenPosition);

                    Debug.Log("플레이어 위치를 완전히 놓쳤습니다!");
                }
            }
        }

        private void InvestigateLastSeenPosition()
        {
            // 마지막 위치로 달려가는 도중에 다시 시야에 들어오면 즉시 추적 재개
            if (CanSeePlayer())
            {
                ResumeChase();
                return;
            }

            if (agent.pathPending)
                return;

            // 마지막 목격 위치 도착
            if (agent.remainingDistance <= arrivalDistance)
            {
                isInvestigating = false;
                // 주변을 둘러보는 코루틴 시작
                StartCoroutine(LookAroundRoutine());
            }
        }
        private void UpdateLastSeen(Vector3 currentTargetPos)
        {
            // 1. 이전 위치와 현재 위치를 비교하여 플레이어의 이동 방향 계산
            Vector3 moveDir = currentTargetPos - lastSeenPosition;
            moveDir.y = 0f; // 상하 높이는 무시

            // 플레이어가 조금이라도 움직였다면 방향을 갱신 (가만히 서있었다면 이전 방향 유지)
            if (moveDir.sqrMagnitude > 0.01f)
            {
                lastKnownDirection = moveDir.normalized;
            }

            // 2. 마지막 위치 갱신
            lastSeenPosition = currentTargetPos;
        }

        // 도착 후 제자리에서 주변을 탐색하는 코루틴
        private IEnumerator LookAroundRoutine()
        {
            Debug.Log("마지막 목격 위치 도착. 주변을 둘러봅니다.");
            isLookingAround = true;
            agent.isStopped = true;

            animator.SetBool("IsRunning", false);
            animator.SetBool("IsWalking", false);
            // 필요하다면 이곳에 "주변을 둘러보는 애니메이션"을 추가할 수 있습니다.

            // 1단계: 플레이어가 도망친(사라진) 방향으로 회전하기
            if (lastKnownDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lastKnownDirection);

                // 몬스터가 해당 방향을 거의 다 바라볼 때까지 회전
                while (Quaternion.Angle(transform.rotation, targetRotation) > 5f)
                {
                    // 회전하는 도중 발견하면 즉시 추적 재개
                    if (CanSeePlayer()) { ResumeChase(); yield break; }

                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, lookRotationSpeed * Time.deltaTime);
                    yield return null;
                }
            }

            float lookTime = 4f; // 4초간 주변 탐색
            float elapsed = 0f;

            while (elapsed < lookTime)
            {
                // 두리번거리는 도중 다시 발견!
                if (CanSeePlayer())
                {
                    ResumeChase();
                    yield break; // 코루틴 즉시 종료
                }

                // 좌우로 빙글빙글 돌면서 시야 탐색 (예: 사인 곡선을 이용한 부드러운 스윕)
                float turnSpeed = Mathf.Sin(elapsed * Mathf.PI) * 120f;
                transform.Rotate(Vector3.up, turnSpeed * Time.deltaTime);

                elapsed += Time.deltaTime;
                yield return null;
            }

            // 끝까지 못 찾음 -> 초기화 후 순찰로 복귀
            Debug.Log("플레이어를 찾지 못했습니다. 순찰로 복귀합니다.");
            isLookingAround = false;
            isDetected = false;

            MoveToNextPoint();
        }

        private bool IsValidVisionTarget(Transform target)
        {
            if (target == player || target.IsChildOf(player))
                return true;

            if (IsPlayerOnWheelchair())
            {
                if (target == wheelchair || target.IsChildOf(wheelchair))
                    return true;
            }

            return false;
        }
        private bool CanSeePlayer()
        {
            Vector3 eyePosition = transform.position + Vector3.up * eyeHeight;
            Transform visionTarget = IsPlayerOnWheelchair() ? wheelchair : player;

            // 타겟의 위치를 발끝이 아닌 가슴(약 1~1.5m) 높이로 보정
            float targetHeightOffset = IsPlayerOnWheelchair() ? 1.0f : 0.3f;

            Vector3 targetCenterPos = visionTarget.position + Vector3.up * targetHeightOffset;

            Vector3 directionToTarget = targetCenterPos - eyePosition;
            float real3DDistance = directionToTarget.magnitude;

            // 1. 수평 거리 체크
            Vector3 flatDirectionToTarget = directionToTarget;
            flatDirectionToTarget.y = 0f;
            float flatDistance = flatDirectionToTarget.magnitude;

            if (flatDistance > viewDistance)
                return false;

            // 2. 정면 체크 (추적 중일 때는 시야각 제한을 무시하여 옆으로 빠르게 지나가도 놓치지 않음)
            if (!isChasing)
            {
                float angle = Vector3.Angle(transform.forward, flatDirectionToTarget);
                if (angle > viewAngle * 0.5f)
                    return false;
            }

            // 3. Raycast (발끝이 아닌 가슴 높이로 쏨)
            if (Physics.Raycast(eyePosition, directionToTarget.normalized, out RaycastHit hit, real3DDistance))
            {
                if (IsValidVisionTarget(hit.transform))
                {
                    Debug.DrawLine(eyePosition, hit.point, Color.green);
                    return true;
                }
                Debug.DrawLine(eyePosition, hit.point, Color.red);
            }

            return false;
        }

        // OnDrawGizmosSelected 로직 유지...
        private void OnDrawGizmosSelected()
        {
            Vector3 eyePosition = transform.position + Vector3.up * eyeHeight;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(eyePosition, viewDistance);

            Vector3 leftDirection = Quaternion.Euler(0, -viewAngle * 0.5f, 0) * transform.forward;
            Vector3 rightDirection = Quaternion.Euler(0, viewAngle * 0.5f, 0) * transform.forward;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(eyePosition, eyePosition + leftDirection * viewDistance);
            Gizmos.DrawLine(eyePosition, eyePosition + rightDirection * viewDistance);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(eyePosition, eyePosition + transform.forward * viewDistance);
        }
    }