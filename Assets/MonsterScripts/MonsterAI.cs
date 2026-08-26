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
    [SerializeField] private float lookDuration = 2f;

    private bool isDetected = false;
    private bool isLookingAtPlayer = false;
    private bool isChasing = false;

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
        // 아직 발견하지 않았다면 감지
        if (!isDetected)
        {
            if (CanSeePlayer())
            {
                OnPlayerDetected();
            }
        }

        // 플레이어를 바라보는 중
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
        agent.isStopped = false;

        agent.SetDestination(
            patrolPoints[currentPoint].position
        );

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

        // 우선 이동 중지 명령
        agent.isStopped = true;

        // 실제 속도가 거의 0이 될 때까지 대기
        while (agent.velocity.sqrMagnitude > 0.01f)
        {
            yield return null;
        }

        // 완전히 멈춘 후 Idle 애니메이션
        animator.SetBool("IsWalking", false);

        yield return new WaitForSeconds(idleDuration);

        isWaiting = false;

        MoveToNextPoint();
    }
    private void OnPlayerDetected()
    {
        isDetected = true;
        isLookingAtPlayer = true;

        agent.isStopped = true;

        animator.SetBool("IsWalking", false);

        Debug.Log("플레이어 발견!");
    }
    private void LookAtPlayer()
    {
        Transform visionTarget =
            IsPlayerOnWheelchair() ? wheelchair : player;

        Vector3 direction =
            visionTarget.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            lookRotationSpeed * Time.deltaTime
        );

        // 회전 완료
        if (Quaternion.Angle(
            transform.rotation,
            targetRotation) < 1f)
        {
            transform.rotation = targetRotation;

            isLookingAtPlayer = false;

            StartCoroutine(StartChaseAfterDelay());
        }
    }
    private IEnumerator StartChaseAfterDelay()
    {
        Debug.Log("플레이어를 바라보는 중...");

        yield return new WaitForSeconds(2f);

        isChasing = true;
        agent.isStopped = false;

        animator.SetBool("IsWalking", true);

        Debug.Log("추적 시작!");
    }
    private void ChasePlayer()
    {
        Transform target =
            IsPlayerOnWheelchair() ? wheelchair : player;

        agent.SetDestination(target.position);
    }
    private bool IsValidVisionTarget(Transform target)
    {
        // 항상 플레이어 본체는 감지
        if (target == player || target.IsChildOf(player))
            return true;

        // 플레이어가 휠체어에 탑승 중이라면
        // 휠체어의 일부도 감지
        if (IsPlayerOnWheelchair())
        {
            if (target == wheelchair || target.IsChildOf(wheelchair))
                return true;
        }

        return false;
    }
    private bool CanSeePlayer()
    {
        // 몬스터 눈 위치
        Vector3 eyePosition =
            transform.position + Vector3.up * eyeHeight;

        // 플레이어가 휠체어를 타고 있으면 휠체어를 기준으로,
        // 아니면 플레이어를 기준으로 거리/방향 계산
        Transform visionTarget =
            IsPlayerOnWheelchair() ? wheelchair : player;

        Vector3 directionToTarget =
            visionTarget.position - eyePosition;

        float distanceToTarget =
            directionToTarget.magnitude;

        // 1. 거리 체크
        if (distanceToTarget > viewDistance)
            return false;

        // 2. 정면 체크
        float angle = Vector3.Angle(
            transform.forward,
            directionToTarget
        );

        if (angle > viewAngle * 0.5f)
            return false;

        // 3. Raycast
        if (Physics.Raycast(
            eyePosition,
            directionToTarget.normalized,
            out RaycastHit hit,
            distanceToTarget))
        {
            if (IsValidVisionTarget(hit.transform))
            {
                Debug.DrawLine(
                    eyePosition,
                    hit.point,
                    Color.green
                );

                return true;
            }

            Debug.DrawLine(
                eyePosition,
                hit.point,
                Color.red
            );
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        // 눈 위치
        Vector3 eyePosition =
            transform.position + Vector3.up * eyeHeight;

        // 시야 거리 원
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(eyePosition, viewDistance);

        // 시야각 좌우 방향 계산
        Vector3 leftDirection =
            Quaternion.Euler(0, -viewAngle * 0.5f, 0) *
            transform.forward;

        Vector3 rightDirection =
            Quaternion.Euler(0, viewAngle * 0.5f, 0) *
            transform.forward;

        // 시야각 양쪽 선
        Gizmos.color = Color.red;

        Gizmos.DrawLine(
            eyePosition,
            eyePosition + leftDirection * viewDistance
        );

        Gizmos.DrawLine(
            eyePosition,
            eyePosition + rightDirection * viewDistance
        );

        // 정면 방향
        Gizmos.color = Color.blue;

        Gizmos.DrawLine(
            eyePosition,
            eyePosition + transform.forward * viewDistance
        );
    }
}