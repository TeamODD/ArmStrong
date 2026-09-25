using UnityEngine;

public class ElevatorInteraction : MonoBehaviour
{
    [SerializeField] private ElevatorManager elevatorManager;

    public void Interact()
    {
        if (elevatorManager != null)
        {
            elevatorManager.OpenElevatorUI();
        }
    }
}