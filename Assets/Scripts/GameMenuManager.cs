using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.EventSystems;

public class GameMenusManager : MonoBehaviour
{
    public static GameMenusManager Instance;

    [Header("Pause/Game State")]
    public bool isPaused = false;

    [Header("UI Panels")]
    public GameObject startMenuUI;
    public GameObject settingsUI;
    public GameObject creditsUI;
    public GameObject gameOverUI;

    [Header("GameOver Buttons")]
    public GameObject continueButton;
    public GameObject quitButton;

    [Header("Start Menu Button")]
    public GameObject startButton;

    [Header("Player Hearts UI")]
    public PlayerHealthUI playerHealthUI;

    private CanvasGroup gameOverCanvas;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (gameOverUI != null)
        {
            gameOverCanvas = gameOverUI.GetComponent<CanvasGroup>();
            if (gameOverCanvas == null)
                gameOverCanvas = gameOverUI.AddComponent<CanvasGroup>();

            gameOverUI.SetActive(false);
            gameOverCanvas.alpha = 0f;
        }

        ShowStartMenu();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Automatically assign buttons
        if (gameOverUI != null)
        {
            continueButton = gameOverUI.transform.Find("ContinueButton")?.gameObject;
            quitButton = gameOverUI.transform.Find("QuitButton")?.gameObject;
        }

        if (startMenuUI != null)
            startButton = startMenuUI.transform.Find("StartButton")?.gameObject;

        // Automatically assign PlayerHealthUI if not assigned
        if (playerHealthUI == null)
            playerHealthUI = FindObjectOfType<PlayerHealthUI>();
    }

    #region UI Management
    public void ShowStartMenu()
    {
        HideAllMenus();
        startMenuUI?.SetActive(true);
        isPaused = true;
        Time.timeScale = 0f;

        if (EventSystem.current != null && startButton != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(startButton);
        }
    }

    public void OpenSettings() { HideAllMenus(); settingsUI?.SetActive(true); }
    public void CloseSettings() { HideAllMenus(); startMenuUI?.SetActive(true); }
    public void OpenCredits() { HideAllMenus(); creditsUI?.SetActive(true); }
    public void CloseCredits() { HideAllMenus(); startMenuUI?.SetActive(true); }

    private void HideAllMenus()
    {
        startMenuUI?.SetActive(false);
        settingsUI?.SetActive(false);
        creditsUI?.SetActive(false);
        gameOverUI?.SetActive(false);
    }
    #endregion

    #region Start Game / Scene Management
    public void PlayGame()
    {
        HideAllMenus();
        isPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainScene");
    }
    #endregion

    #region Game Over
    public void TriggerGameOver()
    {
        isPaused = true;

        // Reset hearts above head first
        if (playerHealthUI != null)
        {
            playerHealthUI.ResetHearts();
            StartCoroutine(AnimateHeartsThenGameOver());
        }
        else
        {
            // Fallback if no PlayerHealthUI assigned
            ShowGameOverUI();
        }
    }

    private IEnumerator AnimateHeartsThenGameOver()
    {
        if (playerHealthUI != null)
        {
            yield return StartCoroutine(playerHealthUI.AnimateAllHeartsDeplete());
        }

        ShowGameOverUI();
    }

    private void ShowGameOverUI()
    {
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);
            if (gameOverCanvas == null)
                gameOverCanvas = gameOverUI.GetComponent<CanvasGroup>();

            StartCoroutine(FadeInGameOverUI());

            if (EventSystem.current != null && continueButton != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(continueButton);
            }
        }
    }

    public void ContinueGame()
    {
        if (GameMenusManager.Instance == null) return;

        // Hide Game Over UI
        gameOverUI?.SetActive(false);

        // Unpause the game
        isPaused = false;
        Time.timeScale = 1f;

        // Respawn the player at checkpoint
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.Respawn();
        }
    }
    public bool IsGameOverActive() => gameOverUI != null && gameOverUI.activeSelf;

    private IEnumerator FadeInGameOverUI()
    {
        float fadeDuration = 1f;
        float timer = 0f;

        if (gameOverCanvas != null)
            gameOverCanvas.alpha = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            if (gameOverCanvas != null)
                gameOverCanvas.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            yield return null;
        }

        if (gameOverCanvas != null)
            gameOverCanvas.alpha = 1f;
    }

    public void RestartGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
    #endregion
}
