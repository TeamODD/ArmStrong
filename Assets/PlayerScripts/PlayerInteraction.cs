using System.Collections;
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

    [Header("Item")]
    [SerializeField] private PlayerItemManager playerItemManager;
    private ItemPickUp currentItem;
    private CardKeyDoor currentCardKeyDoor;
    private bool isInteractionMessageShowing;

    private FlashInteraction currentFlashlight;
    private BedInteraction currentBed;
    private PlayerController currentWheelchair;
    private ElevatorInteraction currentElevator;

    private CrawlingController crawlingController;
    private SawCutInteraction currentSawCut;

    private bool isSawCutting;
    private Coroutine interactionMessageCoroutine;

    private void Awake()
    {
        crawlingController = GetComponent<CrawlingController>();
        interactionDistanceInWheelchair = interactionDistance + 1f;
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        // 톱질 중
        if (isSawCutting)
        {
            if (!Keyboard.current.eKey.isPressed)
            {
                StopSawCutting();
            }

            return;
        }

        // 일반 상호작용
        CheckInteraction();
        UpdateUI();

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            ExecuteInteraction();
        }
    }

    private void CheckInteraction()
    {
        currentFlashlight = null;
        currentBed = null;
        currentWheelchair = null;
        currentElevator = null;
        currentItem = null;
        currentCardKeyDoor = null;
        currentSawCut = null;

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

            // -------------------------
            // 엘리베이터
            // -------------------------

            ElevatorInteraction elevator =
                hit.collider.GetComponentInParent<ElevatorInteraction>();

            if (elevator != null)
            {
                currentElevator = elevator;
                return;
            }

            // -------------------------
            // 아이템
            // -------------------------

            ItemPickUp item =
                hit.collider.GetComponentInParent<ItemPickUp>();

            if (item != null)
            {
                currentItem = item;
                return;
            }

            // -------------------------
            // 카드키 리더기
            // -------------------------

            CardKeyDoor cardKeyDoor =
                hit.collider.GetComponentInParent<CardKeyDoor>();

            if (cardKeyDoor != null)
            {
                currentCardKeyDoor = cardKeyDoor;
                return;
            }

            // -------------------------
            // 톱질 오브젝트
            // -------------------------

            SawCutInteraction sawCut =
                hit.collider.GetComponentInParent<SawCutInteraction>();

            if (sawCut != null)
            {
                currentSawCut = sawCut;
                return;
            }
        }
    }

    private void UpdateUI()
    {
        if (uiManager == null)
            return;

        // 카드키 필요 등의 안내창이 표시되는 동안
        // E 상호작용 UI를 숨김
        if (isInteractionMessageShowing)
        {
            uiManager.SetInteractionUI(false);
        }
        else
        {
            bool hasNormalInteraction =
                currentFlashlight != null ||
                currentBed != null ||
                currentElevator != null ||
                currentItem != null ||
            currentCardKeyDoor != null ||
                currentSawCut != null;

            uiManager.SetInteractionUI(hasNormalInteraction);
        }

        uiManager.SetWheelchairUI(
            currentWheelchair != null
        );
    }

    private void ExecuteInteraction()
    {
        if (isInteractionMessageShowing)
            return;
        // --------------------------------
        // 침대 안에 있는 경우
        // --------------------------------

        if (bedManager != null &&
            bedManager.IsHiding)
        {
            bedManager.ExitBed();
            return;
        }

        if (crawlingController != null &&
            !crawlingController.CanUseInteractions())
            return;

        // --------------------------------
        // 휠체어
        // --------------------------------

        if (currentWheelchair != null)
        {
            if (uiManager != null)
                uiManager.SetWheelchairUI(false);

            crawlingController.Mount(currentWheelchair);
            return;
        }

        // --------------------------------
        // 손전등
        // --------------------------------

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

        // --------------------------------
        // 침대
        // --------------------------------

        if (currentBed != null)
        {
            currentBed.Interact();
            return;
        }

        // --------------------------------
        // 엘리베이터
        // --------------------------------

        if (currentElevator != null)
        {
            currentElevator.Interact();
            return;
        }

        // --------------------------------
        // 아이템
        // --------------------------------

        if (currentItem != null)
        {
            currentItem.Interact(playerItemManager);
            return;
        }

        // --------------------------------
        // 카드키 리더기
        // --------------------------------

        if (currentCardKeyDoor != null)
        {
            bool hasCardKey =
                playerItemManager != null &&
                playerItemManager.HasCardKey;

            currentCardKeyDoor.Interact(playerItemManager);

            if (!hasCardKey)
            {
                if (interactionMessageCoroutine == null)
                {
                    interactionMessageCoroutine =
                        StartCoroutine(ShowInteractionMessage());
                }
            }

            return;
        }

        // --------------------------------
        // 톱
        // --------------------------------

        if (currentSawCut != null)
        {
            StartSawCutting();
            return;
        }
    }

    private IEnumerator ShowInteractionMessage()
    {
        // 이미 안내 중이면 무시
        if (isInteractionMessageShowing)
            yield break;

        isInteractionMessageShowing = true;

        if (uiManager != null)
            uiManager.SetInteractionUI(false);

        yield return new WaitForSeconds(3f);

        isInteractionMessageShowing = false;
        interactionMessageCoroutine = null;
    }

    private void StartSawCutting()
    {
        if (currentSawCut == null)
            return;

        if (playerItemManager == null ||
            !playerItemManager.HasSaw)
        {
            currentSawCut.ShowSawRequiredUI();
            return;
        }

        bool started =
            currentSawCut.StartCutting(playerItemManager);

        if (!started)
            return;

        isSawCutting = true;

        PlayerCamera pCam =
            playerCamera.GetComponent<PlayerCamera>();

        if (pCam != null)
            pCam.SetCameraLocked(true);
    }
    private void StopSawCutting()
    {
        isSawCutting = false;

        PlayerCamera pCam =
            playerCamera.GetComponent<PlayerCamera>();

        if (pCam != null)
            pCam.SetCameraLocked(false);

        if (currentSawCut != null)
            currentSawCut.StopCutting();
    }
}