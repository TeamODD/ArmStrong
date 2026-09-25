using UnityEngine;

public class ItemPickUp : MonoBehaviour
{
    public enum ItemType
    {
        CardKey,
        Saw
    }

    [Header("Item")]
    [SerializeField] private ItemType itemType;

    private bool isPickedUp;

    public void Interact(PlayerItemManager itemManager)
    {
        if (isPickedUp || itemManager == null)
            return;

        switch (itemType)
        {
            case ItemType.CardKey:
                itemManager.GetCardKey();
                break;

            case ItemType.Saw:
                itemManager.GetSaw();
                break;
        }

        isPickedUp = true;

        // 오브젝트 제거
        gameObject.SetActive(false);
    }
}
