using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class SimpleDoor : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float interactionDistance = 2.2f;
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float rotationSpeed = 180f;

    private Quaternion closedRotation;
    private bool isOpen;

    private void Start()
    {
        closedRotation = transform.localRotation;
    }

    private void Update()
    {
        if (player == null)
        {
            return;
        }

        float distance = Vector3.Distance(
            player.position,
            transform.position
        );

        if (distance <= interactionDistance && InteractPressed())
        {
            isOpen = !isOpen;
        }

        Quaternion targetRotation =
            closedRotation *
            Quaternion.Euler(0f, isOpen ? openAngle : 0f, 0f);

        transform.localRotation = Quaternion.RotateTowards(
            transform.localRotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private bool InteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null &&
               Keyboard.current.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }
}