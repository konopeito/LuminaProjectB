using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameOverManager : MonoBehaviour
{
    public GameObject gameOverUI; // Panel/Canvas
    public float fadeDuration = 1f; // Fade-in time
    [HideInInspector] public bool isPaused = false;
    private bool isGameOver = false;
    private CanvasGroup canvasGroup;

    void Start()
    {
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(false);
            canvasGroup = gameOverUI.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameOverUI.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;
        }
    }

    public void TriggerGameOver()
    {
        if (isGameOver) return;

        isGameOver = true;
        isPaused = true; // Stop game logic

        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);
            StartCoroutine(FadeInUI());
        }
    }

    private IEnumerator FadeInUI()
    {
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit called");
    }
}
