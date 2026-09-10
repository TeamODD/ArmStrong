using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private FlashManager flashManager;

    [Header("Ray")]
    [SerializeField] private float interactionDistance = 3f;
    private float interactionDistanceInWheelchair;

    [Header("UI")]
    [SerializeField] private UIManager uiManager;

    [Header("Bed")]
    [SerializeField] private BedManager bedManager;

    private FlashInteraction currentFlashlight;
    private BedInteraction currentBed;
    private PlayerController currentWheelchair;
    private CrawlingController crawlingController;

    private void Awake()
    {
        crawlingController = GetComponent<CrawlingController>();
        interactionDistanceInWheelchair = interactionDistance + 1f;
    }

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

        if (crawlingController != null &&
            !crawlingController.CanUseInteractions())
            return;

        if (playerCamera == null)
            return;

        float currentInteractionDistance =
        crawlingController != null && crawlingController.enabled
        ? interactionDistance
        : interactionDistanceInWheelchair;


        Ray ray = playerCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            currentInteractionDistance))
        {
            // -------------------------
            // 휠체어
            // -------------------------

            PlayerController wheelchair =
                hit.collider.GetComponentInParent<PlayerController>();

            if (crawlingController != null &&
                crawlingController.CanRemountWheelchair(wheelchair))
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
        if (bedManager != null && bedManager.IsHiding)
        {
            bedManager.ExitBed();
            return;
        }

        if (crawlingController != null &&
            !crawlingController.CanUseInteractions())
            return;

        // 휠체어
        if (currentWheelchair != null)
        {
            if (uiManager != null)
                uiManager.SetWheelchairUI(false);

            crawlingController.Mount(currentWheelchair);
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