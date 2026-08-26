using UnityEngine;

public class FlashPickUp : MonoBehaviour
{
    [Header("Flashlight")]
    [SerializeField] private Light flashlightLight;

    [Header("Tape")]
    [SerializeField] private GameObject tapeObject;

    private bool isPickedUp = false;
    private bool isLightOn = false;
    public bool IsPickedUp => isPickedUp;

    public void Pickup(Transform holdPosition)
    {
        isPickedUp = true;

        // Rigidbody가 있다면 물리 정지
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>();

        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        // 플레이어 손 위치에 고정
        transform.SetParent(holdPosition);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        if (tapeObject != null)
        {
            tapeObject.SetActive(true);
        }

        // 주웠을 때 손전등은 꺼진 상태
        SetLight(false);
    }

    public void ToggleLight()
    {
        SetLight(!isLightOn);
    }

    private void SetLight(bool state)
    {
        isLightOn = state;

        if (flashlightLight != null)
        {
            flashlightLight.enabled = state;
        }
    }
}
