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

        // audio
        [Header("Chase Audio")]
        [SerializeField] private ChaseAudioManager chaseAudio;
        [Header("Monster Sound")]
        [SerializeField] private MonsterHowlAudio monsterHowl;
        private float lastTimePlayerSeen;
        private Vector3 lastSeenPosition;
        private Vector3 lastKnownDirection = Vector3.forward;

        // ���� ���� ������
        private bool isDetected = false;
        private bool isLookingAtPlayer = false;
        private bool isChasing = false;
        private bool isInvestigating = false;
        private bool isLookingAround = false; // �߰�: �ֺ��� �θ����Ÿ��� ����

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
            // ���� �̹߰� ������ �� ����
            if (!isDetected)
            {
                if (CanSeePlayer())
                {
                    OnPlayerDetected();
                }
            }

            // �÷��̾ ó�� �߰��ϰ� ��������� ���� �ٶ󺸴� ��
            if (isLookingAtPlayer)
            {
                LookAtPlayer();
                return;
            }

            // ���� ��
            if (isChasing)
            {
                ChasePlayer();
                return;
            }

            // ��ģ �������� �޷����� ���� ��
            if (isInvestigating)
            {
                InvestigateLastSeenPosition();
                return;
            }

            // ���� �� ���ڸ����� �θ����Ÿ��� �� (Update������ �ƹ��͵� ���ϰ� �ڷ�ƾ�� ó��)
            if (isLookingAround)
            {
                return;
            }

            // ���� ����
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

            Debug.Log("�÷��̾� �߰�!");

            if (chaseAudio != null)
            {
                chaseAudio.StartChase();
            }

            if (monsterHowl != null)
            monsterHowl.OnDetected();
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

            // �߰�: �ִϸ������� Scream Ʈ���� �۵�
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

        // ���� ���¸� �Ѵ� ���� �޼��� (���ƴٰ� �ٽ� ã���� �� ����)
        private void ResumeChase()
        {
            isChasing = true;
            isInvestigating = false;
            isLookingAround = false;

            agent.speed = chaseSpeed;
            agent.isStopped = false;

            animator.SetBool("IsRunning", true);
            animator.SetBool("IsWalking", false);
            Debug.Log("���� �簳!");

            if (monsterHowl != null)
                monsterHowl.OnChase();
        }

        private void ChasePlayer()
        {
            Transform target = IsPlayerOnWheelchair() ? wheelchair : player;

            if (CanSeePlayer())
            {
                // ������ �÷��̾ ���� ����
                lastTimePlayerSeen = Time.time;

                UpdateLastSeen(target.position);
                agent.isStopped = false;
                agent.SetDestination(target.position);
            }
            else
            {
                // ���������� ���� �þ߿��� �� ���� ���� �ð�
                float timeSinceLastSeen = Time.time - lastTimePlayerSeen;

                // �� �þ߿��� ������� ���� �ð� ������
                // �÷��̾��� ���� ��ġ�� ��� �˰� ����
                if (timeSinceLastSeen <= lostSightGraceTime)
                {
                    // �÷��̾��� �ǽð� ��ġ�� �̵�
                    UpdateLastSeen(target.position);
                    agent.isStopped = false;
                    agent.SetDestination(target.position);

                    Debug.Log("�þ� �������� �÷��̾� ��ġ�� ��� ���� ��...");
                }
                else
                {
                    // 1�ʰ� ������ �׶����� ������ ��ġ�� ���
                    isChasing = false;
                    isInvestigating = true;

                    agent.isStopped = false;
                    agent.SetDestination(lastSeenPosition);

                    Debug.Log("�÷��̾� ��ġ�� ������ ���ƽ��ϴ�!");

                    // Only stop chase howl after actually losing the target.
                    if (monsterHowl != null)
                        monsterHowl.OnSearch();
                }
            }
        }

        private void InvestigateLastSeenPosition()
        {
            // ������ ��ġ�� �޷����� ���߿� �ٽ� �þ߿� ������ ��� ���� �簳
            if (CanSeePlayer())
            {
                ResumeChase();
                return;
            }

            if (agent.pathPending)
                return;

            // ������ ��� ��ġ ����
            if (agent.remainingDistance <= arrivalDistance)
            {
                isInvestigating = false;
                // �ֺ��� �ѷ����� �ڷ�ƾ ����
                StartCoroutine(LookAroundRoutine());
            }
        }
        private void UpdateLastSeen(Vector3 currentTargetPos)
        {
            // 1. ���� ��ġ�� ���� ��ġ�� ���Ͽ� �÷��̾��� �̵� ���� ���
            Vector3 moveDir = currentTargetPos - lastSeenPosition;
            moveDir.y = 0f; // ���� ���̴� ����

            // �÷��̾ �����̶� �������ٸ� ������ ���� (������ ���־��ٸ� ���� ���� ����)
            if (moveDir.sqrMagnitude > 0.01f)
            {
                lastKnownDirection = moveDir.normalized;
            }

            // 2. ������ ��ġ ����
            lastSeenPosition = currentTargetPos;
        }

        // ���� �� ���ڸ����� �ֺ��� Ž���ϴ� �ڷ�ƾ
        private IEnumerator LookAroundRoutine()
        {
            Debug.Log("������ ��� ��ġ ����. �ֺ��� �ѷ����ϴ�.");
            isLookingAround = true;
            agent.isStopped = true;

            animator.SetBool("IsRunning", false);
            animator.SetBool("IsWalking", false);
            // �ʿ��ϴٸ� �̰��� "�ֺ��� �ѷ����� �ִϸ��̼�"�� �߰��� �� �ֽ��ϴ�.

            // 1�ܰ�: �÷��̾ ����ģ(�����) �������� ȸ���ϱ�
            if (lastKnownDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lastKnownDirection);

                // ���Ͱ� �ش� ������ ���� �� �ٶ� ������ ȸ��
                while (Quaternion.Angle(transform.rotation, targetRotation) > 5f)
                {
                    // ȸ���ϴ� ���� �߰��ϸ� ��� ���� �簳
                    if (CanSeePlayer()) { ResumeChase(); yield break; }

                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, lookRotationSpeed * Time.deltaTime);
                    yield return null;
                }

            }

            float lookTime = 4f; // 4�ʰ� �ֺ� Ž��
            float elapsed = 0f;

            while (elapsed < lookTime)
            {
                // �θ����Ÿ��� ���� �ٽ� �߰�!
                if (CanSeePlayer())
                {
                    ResumeChase();
                    yield break; // �ڷ�ƾ ��� ����
                }

                // �¿�� ���ۺ��� ���鼭 �þ� Ž�� (��: ���� ��� �̿��� �ε巯�� ����)
                float turnSpeed = Mathf.Sin(elapsed * Mathf.PI) * 120f;
                transform.Rotate(Vector3.up, turnSpeed * Time.deltaTime);

                elapsed += Time.deltaTime;
                yield return null;
            }

            // ������ �� ã�� -> �ʱ�ȭ �� ������ ����
            Debug.Log("�÷��̾ ã�� ���߽��ϴ�. ������ �����մϴ�.");
            isLookingAround = false;
            isDetected = false;

            // 플레이어를 완전히 놓쳤을 때 음악 복구
            if (chaseAudio != null)
            {
                chaseAudio.StopChase();
            }

            // Resume idle howls only after finishing the search.
            if (monsterHowl != null)
                monsterHowl.OnCalm();

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

            // Ÿ���� ��ġ�� �߳��� �ƴ� ����(�� 1~1.5m) ���̷� ����
            float targetHeightOffset = IsPlayerOnWheelchair() ? 1.0f : 0.3f;

            Vector3 targetCenterPos = visionTarget.position + Vector3.up * targetHeightOffset;

            Vector3 directionToTarget = targetCenterPos - eyePosition;
            float real3DDistance = directionToTarget.magnitude;

            // 1. ���� �Ÿ� üũ
            Vector3 flatDirectionToTarget = directionToTarget;
            flatDirectionToTarget.y = 0f;
            float flatDistance = flatDirectionToTarget.magnitude;

            if (flatDistance > viewDistance)
                return false;

            // 2. ���� üũ (���� ���� ���� �þ߰� ������ �����Ͽ� ������ ������ �������� ��ġ�� ����)
            if (!isChasing)
            {
                float angle = Vector3.Angle(transform.forward, flatDirectionToTarget);
                if (angle > viewAngle * 0.5f)
                    return false;
            }

            // 3. Raycast (�߳��� �ƴ� ���� ���̷� ��)
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

        // OnDrawGizmosSelected ���� ����...
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