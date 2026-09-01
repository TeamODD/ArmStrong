using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private FlashManager flashManager;

    [Header("Ray")]
    [SerializeField] private float interactionDistance = 3f;

    [Header("UI")]
    [SerializeField] private UIManager uiManager;

    private FlashInteraction currentFlashlight;
    private BedInteraction currentBed;
    private PlayerController currentWheelchair;

    private void Update()
    {
        CheckInteraction();
        UpdateUI();

        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            ExecuteInteraction();
        }
    }

    private void CheckInteraction()
    {
        currentFlashlight = null;
        currentBed = null;
        currentWheelchair = null;

        if (playerCamera == null)
            return;

        Ray ray = playerCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactionDistance))
        {
            // -------------------------
            // 휠체어
            // -------------------------

            PlayerController wheelchair =
                hit.collider.GetComponentInParent<PlayerController>();

            if (wheelchair != null)
            {
                currentWheelchair = wheelchair;
                return;
            }

            // -------------------------
            // 손전등
            // -------------------------

            FlashInteraction flashlight =
                hit.collider.GetComponentInParent<FlashInteraction>();

            if (flashlight != null && !flashlight.IsPickedUp)
            {
                currentFlashlight = flashlight;
                return;
            }

            // -------------------------
            // 침대
            // -------------------------

            BedInteraction bed =
                hit.collider.GetComponentInParent<BedInteraction>();

            if (bed != null)
            {
                currentBed = bed;
                return;
            }
        }
    }
    private void UpdateUI()
    {
        bool hasNormalInteraction =
            currentFlashlight != null ||
            currentBed != null;

        bool hasWheelchairInteraction =
            currentWheelchair != null;

        if (uiManager != null)
        {
            uiManager.SetInteractionUI(hasNormalInteraction);
            uiManager.SetWheelchairUI(hasWheelchairInteraction);
        }
    }

    private void ExecuteInteraction()
    {
        // 휠체어
        if (currentWheelchair != null)
        {
            uiManager.SetWheelchairUI(false);

            GetComponent<CrawlingController>().Mount(currentWheelchair);
            return;
        }

        // 손전등
        if (currentFlashlight != null)
        {
            if (flashManager != null)
            {
                flashManager.PickupFlashlight(
                    currentFlashlight
                );
            }

            return;
        }

        // 침대
        if (currentBed != null)
        {
            currentBed.Interact();
            return;
        }
    }
}