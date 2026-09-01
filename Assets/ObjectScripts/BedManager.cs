using UnityEngine;

public class BedManager : MonoBehaviour
{
    private bool isHiding = false;

    private BedInteraction currentBed;
    private Transform currentHidePoint;

    public bool IsHiding => isHiding;

    public void Interact(
        BedInteraction bed,
        Transform hidePoint)
    {
        // 침대 밖 → 들어가기
        if (!isHiding)
        {
            EnterBed(bed, hidePoint);
        }
        // 침대 안 → 나오기
        else
        {
            ExitBed();
        }
    }

    private void EnterBed(
        BedInteraction bed,
        Transform hidePoint)
    {
        currentBed = bed;
        currentHidePoint = hidePoint;

        isHiding = true;

        Debug.Log("침대에 숨기 시작");
    }

    private void ExitBed()
    {
        isHiding = false;

        Debug.Log("침대에서 나오기");

        currentBed = null;
        currentHidePoint = null;
    }
}