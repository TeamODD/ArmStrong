using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject gameOverUI;

    private bool isGameOver;

    private void Awake()
    {
        if (gameOverUI != null)
            gameOverUI.SetActive(false);

        Time.timeScale = 1f;
    }

    public void GameOver()
    {
        if (isGameOver)
            return;

        isGameOver = true;

        // 게임 정지
        Time.timeScale = 0f;

        // UI 표시
        if (gameOverUI != null)
            gameOverUI.SetActive(true);

        // 마우스 사용
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // 다시 시작
    public void RestartGame()
    {
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 첫 번째 씬부터 다시 시작
        SceneManager.LoadScene(0);
    }

    // 게임 종료
    public void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}