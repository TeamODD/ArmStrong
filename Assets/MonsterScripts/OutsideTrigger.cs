using UnityEngine;
using UnityEngine.AI;

public class OutsideTrigger : MonoBehaviour
{
    [Header("Monster")]
    [SerializeField] private NavMeshAgent monsterAgent;
    [SerializeField] private MonsterAI monsterAI;

    [Header("Teleport Target")]
    [SerializeField] private Transform teleportPoint;

    private bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered)
            return;

        CrawlingController player =
            other.GetComponentInParent<CrawlingController>();

        if (player == null)
            return;

        if (monsterAgent == null ||
            monsterAI == null ||
            teleportPoint == null)
            return;

        triggered = true;

        // 몬스터 위치 이동
        monsterAgent.Warp(teleportPoint.position);

        // 지정한 방향 바라보기
        monsterAgent.transform.rotation =
            teleportPoint.rotation;

        // 고함 → 추적
        monsterAI.ForceScreamAndChase();
    }
}