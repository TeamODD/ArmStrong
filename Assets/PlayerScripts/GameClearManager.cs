using UnityEngine;
using UnityEngine.SceneManagement;

public class GameClearManager : MonoBehaviour
{
    [Header("Clear UI")]
    [SerializeField] private GameObject clearUI;

    private bool isCleared;

    public void GameClear()
    {
        if (isCleared)
            return;

        isCleared = true;

        // 클리어 UI 표시
        if (clearUI != null)
        {
            clearUI.SetActive(true);
        }

        // 마우스 잠금 해제
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 게임 정지
        Time.timeScale = 0f;

        // 모든 오디오 정지
        AudioListener.pause = true;

        Debug.Log("Game Clear!");
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;

        AudioListener.pause = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 첫 번째 씬부터 다시 시작
        SceneManager.LoadScene(0);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }
}