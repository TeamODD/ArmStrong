using UnityEngine;

public class MonsterGameOver : MonoBehaviour
{
    [SerializeField] private GameOverManager gameOverManager;
    [SerializeField] private MonsterAI monsterAI;

    private void Awake()
    {
        if (monsterAI == null)
            monsterAI = GetComponent<MonsterAI>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // --------------------------------
        // 몬스터가 실제 추적 중이 아니면 무시
        // --------------------------------

        if (monsterAI == null ||
            !monsterAI.IsChasing)
            return;

        // --------------------------------
        // 플레이어 본체 확인
        // --------------------------------

        CrawlingController player =
            other.GetComponentInParent<CrawlingController>();

        if (player != null)
        {
            gameOverManager.GameOver();
        }
    }
}