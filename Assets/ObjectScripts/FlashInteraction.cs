using UnityEngine;

public class FlashInteraction : MonoBehaviour
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
        if (isPickedUp)
            return;

        isPickedUp = true;

        // Rigidbody가 있다면 물리 정지
        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Collider 비활성화
        Collider[] colliders = GetComponentsInChildren<Collider>();

        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        // 플레이어 손 위치에 고정
        transform.SetParent(holdPosition);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        // 테이프 활성화
        if (tapeObject != null)
        {
            tapeObject.SetActive(true);
        }

    }

    public void ToggleLight()
    {
        if (!isPickedUp)
            return;

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