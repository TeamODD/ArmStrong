using UnityEngine;

public class BedInteraction : MonoBehaviour
{
    [Header("Hide Point")]
    [SerializeField] private Transform hidePoint;

    [Header("Bed Manager")]
    [SerializeField] private BedManager bedManager;

    public void Interact()
    {
        if (bedManager == null)
        {
            Debug.LogWarning($"{name}: BedManager가 연결되지 않았습니다.");
            return;
        }

        if (hidePoint == null)
        {
            Debug.LogWarning($"{name}: HidePoint가 설정되지 않았습니다.");
            return;
        }

        bedManager.Interact(this, hidePoint);
    }
}