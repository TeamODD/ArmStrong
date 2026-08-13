using UnityEngine;

public class FlashPickUp : MonoBehaviour
{
    private bool isPickedUp = false;

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
    }
}
