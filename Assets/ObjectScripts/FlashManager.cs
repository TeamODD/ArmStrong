using UnityEngine;
using UnityEngine.InputSystem;

public class FlashManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform flashlightHoldPosition;

    private FlashInteraction heldFlashlight;

    private void Update()
    {
        if (heldFlashlight != null &&
            Keyboard.current != null &&
            Keyboard.current.fKey.wasPressedThisFrame)
        {
            heldFlashlight.ToggleLight();
        }
    }

    public void PickupFlashlight(FlashInteraction flashlight)
    {
        if (flashlight == null || flashlight.IsPickedUp)
            return;

        heldFlashlight = flashlight;

        heldFlashlight.Pickup(flashlightHoldPosition);
    }
}