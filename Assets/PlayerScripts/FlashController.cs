using UnityEngine;
using UnityEngine.InputSystem;

public class FlashController : MonoBehaviour
{

    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform flashlightHoldPosition;

    [Header("Pickup")]
    [SerializeField] private float pickupDistance = 3f;

    [Header("UI")]
    [SerializeField] private GameObject pickupUI;

    private FlashPickUp currentFlashlight;
    private FlashPickUp heldFlashlight;
    void Update()
    {
        CheckForFlashlight();

        if (currentFlashlight != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            PickupFlashlight();
        }

        if (heldFlashlight != null &&
            Keyboard.current.fKey.wasPressedThisFrame)
        {
            heldFlashlight.ToggleLight();
        }
    }

    private void CheckForFlashlight()
    {
        currentFlashlight = null;

        Ray ray = playerCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        if (Physics.Raycast(ray, out RaycastHit hit, pickupDistance))
        {
            FlashPickUp flashlight =
                hit.collider.GetComponentInParent<FlashPickUp>();

            if (flashlight != null && !flashlight.IsPickedUp)
            {
                currentFlashlight = flashlight;
            }
        }

        if (pickupUI != null)
            pickupUI.SetActive(currentFlashlight != null);
    }

    private void PickupFlashlight()
    {
        heldFlashlight = currentFlashlight;

        heldFlashlight.Pickup(flashlightHoldPosition);

        currentFlashlight = null;

        if (pickupUI != null)
            pickupUI.SetActive(false);
    }
}
