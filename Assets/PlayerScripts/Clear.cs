using UnityEngine;

public class Clear : MonoBehaviour
{
    [Header("Game Clear")]
    [SerializeField] private GameClearManager gameClearManager;

    private bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered)
            return;

        CrawlingController player =
            other.GetComponentInParent<CrawlingController>();

        if (player == null)
            return;

        triggered = true;

        if (gameClearManager != null)
        {
            gameClearManager.GameClear();
        }
    }
}