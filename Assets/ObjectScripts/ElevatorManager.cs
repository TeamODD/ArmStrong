using System.Collections;
using UnityEngine;

public class ElevatorManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIManager uiManager;

    [Header("Elevator UI")]
    [SerializeField] private GameObject elevatorUI;

    [Header("Wheelchair Required UI")]
    [SerializeField] private GameObject wheelchairRequiredUI;

    [Header("Player")]
    [SerializeField] private GameObject player;

    [Header("Wheelchair")]
    [SerializeField] private PlayerController wheelchairController;

    [Header("Floor Positions")]
    [SerializeField] private Transform[] floorPositions;

    private bool isUsingElevator;
    private Coroutine wheelchairRequiredCoroutine;

    public bool IsUsingElevator => isUsingElevator;


    private void Awake()
    {
        if (elevatorUI != null)
            elevatorUI.SetActive(false);

        if (wheelchairRequiredUI != null)
            wheelchairRequiredUI.SetActive(false);
    }


    // =========================================================
    // ���������� UI ����
    // =========================================================

    public void OpenElevatorUI()
    {
        if (isUsingElevator)
            return;

        // ��ü�� ž�� ���°� �ƴϸ� �ȳ�
        if (!IsRidingWheelchair())
        {
            ShowWheelchairRequiredUI();
            return;
        }

        isUsingElevator = true;

        // �÷��̾� �̵� ����
        StopPlayerMovement();

        // UI ǥ��
        if (elevatorUI != null)
            elevatorUI.SetActive(true);

        // ���콺 Lock ����
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }


    // =========================================================
    // ��ü�� ž�� ����
    // =========================================================

    private bool IsRidingWheelchair()
    {
        if (wheelchairController == null ||
            player == null)
        {
            return false;
        }

        return player.transform.IsChildOf(
            wheelchairController.transform
        );
    }


    // =========================================================
    // ��ü�� �ʿ� �ȳ�
    // =========================================================

    private void ShowWheelchairRequiredUI()
    {
        if (wheelchairRequiredUI == null)
            return;

        if (wheelchairRequiredCoroutine != null)
        {
            StopCoroutine(wheelchairRequiredCoroutine);
        }

        wheelchairRequiredUI.SetActive(true);

        wheelchairRequiredCoroutine =
            StartCoroutine(HideWheelchairRequiredUI());
    }


    private IEnumerator HideWheelchairRequiredUI()
    {
        yield return new WaitForSeconds(3f);

        if (wheelchairRequiredUI != null)
            wheelchairRequiredUI.SetActive(false);

        wheelchairRequiredCoroutine = null;
    }


    // =========================================================
    // ���������� UI �ݱ�
    // =========================================================

    public void CloseElevatorUI()
    {
        if (!isUsingElevator)
            return;

        isUsingElevator = false;

        if (elevatorUI != null)
            elevatorUI.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        ResumePlayerMovement();
    }


    // =========================================================
    // �� ����
    // =========================================================

    public void SelectFloor(int floorIndex)
    {
        if (!isUsingElevator)
            return;

        if (floorPositions == null ||
            floorIndex < 0 ||
            floorIndex >= floorPositions.Length)
        {
            Debug.LogWarning(
                $"[ElevatorManager] �߸��� �� �ε���: {floorIndex}"
            );

            return;
        }

        StartCoroutine(
            MoveToFloorRoutine(floorIndex)
        );
    }


    // =========================================================
    // �� �̵�
    // =========================================================

    private IEnumerator MoveToFloorRoutine(int floorIndex)
    {
        // �ߺ� �Է� ����
        isUsingElevator = false;

        // UI �ݱ�
        if (elevatorUI != null)
            elevatorUI.SetActive(false);

        // ���콺 Lock
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // --------------------------------
        // ȭ�� ����
        // --------------------------------

        yield return StartCoroutine(
            uiManager.FadeScreen(
                0f,
                1f,
                1.0f
            )
        );

        // --------------------------------
        // �÷��̾� / ��ü�� �̵�
        // --------------------------------

        MovePlayerToFloor(floorIndex);

        // --------------------------------
        // ȭ�� �����
        // --------------------------------

        yield return StartCoroutine(
            uiManager.FadeScreen(
                1f,
                0f,
                1.0f
            )
        );

        // --------------------------------
        // �̵� �簳
        // --------------------------------

        ResumePlayerMovement();

        Debug.Log(
            $"[ElevatorManager] {floorIndex + 1}������ �̵�"
        );
    }


    // =========================================================
    // �� ��ġ�� �̵�
    // =========================================================

    private void MovePlayerToFloor(int floorIndex)
    {
        Transform target = floorPositions[floorIndex];

        if (target == null)
        {
            Debug.LogWarning(
                $"[ElevatorManager] {floorIndex + 1}�� ��ġ�� �����ϴ�."
            );

            return;
        }

        if (!IsRidingWheelchair())
            return;

        // --------------------------------
        // Rigidbody
        // --------------------------------

        Rigidbody rb =
            wheelchairController.GetComponent<Rigidbody>();

        // --------------------------------
        // Animator
        // --------------------------------

        Animator animator =
            wheelchairController.GetComponent<Animator>();


        // --------------------------------
        // ���� OFF
        // --------------------------------

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.isKinematic = true;
        }


        // --------------------------------
        // Root Motion OFF
        // --------------------------------

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }


        // --------------------------------
        // ��ġ / ȸ�� �̵�
        // --------------------------------

        if (rb != null)
        {
            rb.position = target.position;
            rb.rotation = target.rotation;
        }
        else
        {
            wheelchairController.transform.position =
                target.position;

            wheelchairController.transform.rotation =
                target.rotation;
        }


        // --------------------------------
        // �̵� �� �ӵ� ����
        // --------------------------------

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }


        // --------------------------------
        // ���� ON
        // --------------------------------

        if (rb != null)
        {
            rb.isKinematic = false;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }


    // =========================================================
    // �÷��̾� �̵� ����
    // =========================================================

    private void StopPlayerMovement()
    {
        if (wheelchairController != null)
        {
            wheelchairController.enabled = false;
        }
    }


    // =========================================================
    // �÷��̾� �̵� �簳
    // =========================================================

    private void ResumePlayerMovement()
    {
        if (wheelchairController != null)
        {
            wheelchairController.enabled = true;
        }
    }
}